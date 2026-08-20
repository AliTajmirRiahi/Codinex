using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Codinex.Core.Chat;
using Codinex.Core.Interfaces;
using Codinex.Storage.Managers;
using Microsoft.VisualStudio.Shell;

namespace Codinex.VisualStudio.CommitMessages
{
    /// <summary>
    /// Injects a "Generate Commit Message" control into Visual Studio's native Git Changes
    /// window, next to the commit textbox, and drives the generate/approve/reject flow.
    ///
    /// Visual Studio's Git Changes commit box has no supported extension point for third-party
    /// AI providers (its own wand button is hard-wired to GitHub Copilot). This walks the live
    /// WPF visual tree to find the commit textbox by AutomationId and inject a sibling control —
    /// an unsupported technique that depends on Microsoft's private internal layout and can
    /// silently stop working on a VS update. It fails soft: if injection or writing ever fails,
    /// nothing is shown and VS is never disrupted.
    /// </summary>
    internal sealed class GitCommitButtonInjector(SettingsManager settingsManager, ICommitMessageGenerator generator, IErrorHandler errorHandler)
    {
        private const string MarkerTag = "Codinex_GitCommit_Injected";
        private static readonly TimeSpan ErrorAutoResetDelay = TimeSpan.FromSeconds(3);

        private readonly GitCommitGenerationState _state = new();

        private ContentControl _host;
        private Panel _hostPanel;
        private int? _insertedGridRow;
        private CancellationTokenSource _cts;
        private bool _isIconRow;

        /// <summary>
        /// Attempts to (re)inject the control. Safe to call repeatedly (idempotent, never throws).
        /// </summary>
        public void TryInject()
        {
            try
            {
                if (!GitCommitButtonVisibility.IsCodinexOpen)
                {
                    Remove();
                    return;
                }

                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow == null) return;

                var point = GitCommitVisualTreeLocator.Find(mainWindow);
                if (point == null) return;

                var injectionPoint = point.Value;

                if (IsAlreadyInjected(injectionPoint.HostPanel)) return;

                Inject(injectionPoint);
            }
            catch
            {
                // Never disrupt Visual Studio.
            }
        }

        private static bool IsAlreadyInjected(Panel parent)
        {
            foreach (UIElement child in parent.Children)
            {
                if ((child as FrameworkElement)?.Tag as string == MarkerTag)
                {
                    return true;
                }
            }

            return false;
        }

        public void Remove()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_host == null) return;

                if (_host.Parent is Grid grid)
                {
                    var row = Grid.GetRow(_host);
                    grid.Children.Remove(_host);

                    if (_insertedGridRow.HasValue && _insertedGridRow.Value >= 0 && _insertedGridRow.Value < grid.RowDefinitions.Count)
                    {
                        foreach (UIElement child in grid.Children)
                        {
                            var childRow = Grid.GetRow(child);
                            if (childRow > row)
                            {
                                Grid.SetRow(child, childRow - 1);
                            }
                        }

                        grid.RowDefinitions.RemoveAt(_insertedGridRow.Value);
                    }
                }
                else if (_host.Parent is Panel panel)
                {
                    panel.Children.Remove(_host);
                }
                else
                {
                    _hostPanel?.Children.Remove(_host);
                }

                _host = null;
                _hostPanel = null;
                _insertedGridRow = null;
                _state.Reset();
            }
            catch
            {
                // Never disrupt Visual Studio.
            }
        }

        private void Inject(GitCommitInjectionPoint point)
        {
            _isIconRow = point.IsIconRow;
            _hostPanel = point.HostPanel;
            _insertedGridRow = null;

            _host = new ContentControl
            {
                Tag = MarkerTag,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = _isIconRow ? new Thickness(2, 0, 0, 0) : new Thickness(4, 2, 4, 2),
                HorizontalAlignment = _isIconRow ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
            };

            if (!_isIconRow && point.HostPanel is Grid grid)
            {
                var insertRow = Grid.GetRow(point.Anchor);
                var col = Grid.GetColumn(point.Anchor);
                var colSpan = Grid.GetColumnSpan(point.Anchor);

                grid.RowDefinitions.Insert(insertRow, new RowDefinition { Height = GridLength.Auto });
                _insertedGridRow = insertRow;

                foreach (UIElement child in grid.Children)
                {
                    var r = Grid.GetRow(child);
                    if (r >= insertRow)
                    {
                        Grid.SetRow(child, r + 1);
                    }
                }

                Grid.SetRow(_host, insertRow);
                Grid.SetColumn(_host, col);
                if (colSpan > 1) Grid.SetColumnSpan(_host, colSpan);

                grid.Children.Add(_host);
            }
            else if (_isIconRow)
            {
                // Icon row (StackPanel of icon buttons) — insert at the very start, to the left
                // of the native wand/'#' icons.
                point.HostPanel.Children.Insert(0, _host);
            }
            else
            {
                var index = point.HostPanel.Children.IndexOf(point.Anchor);
                if (index < 0) index = point.HostPanel.Children.Count;
                point.HostPanel.Children.Insert(index, _host);
            }

            _state.Reset();
            Render();
        }

        private void Render()
        {
            if (_host == null) return;

            switch (_state.Phase)
            {
                case GitCommitPhase.Generating:
                    _host.Content = BuildThinkingRow();
                    break;
                case GitCommitPhase.ResultReady:
                    _host.Content = BuildApproveRejectRow();
                    break;
                case GitCommitPhase.Error:
                    _host.Content = BuildErrorRow(_state.ErrorMessage);
                    break;
                default:
                    _host.Content = BuildIdleButton();
                    break;
            }
        }

        private Button BuildIdleButton()
        {
            var button = new Button
            {
                ToolTip = "Generate Commit Message (Codinex AI)",
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (s, e) => StartGeneration();

            if (_isIconRow)
            {
                button.Content = GitCommitIcons.CreateWandSparkles(18);
                button.Padding = new Thickness(0);
                button.Margin = new Thickness(0);
                button.Background = Brushes.Transparent;
                button.BorderThickness = new Thickness(0);

                button.HorizontalAlignment = HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Center;

                button.Width = 24;
                button.MinWidth = 24;
                button.MaxWidth = 24;

                return button;
            }

            button.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ButtonStyleKey);

            var icon = GitCommitIcons.CreateWandSparkles(18);
            var label = new TextBlock
            {
                Text = "Generate Commit Message",
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(icon);
            content.Children.Add(label);

            button.Content = content;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Padding = new Thickness(6, 4, 6, 4);

            return button;
        }

        private StackPanel BuildThinkingRow()
        {
            var spinner = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 64,
                Height = 3,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            // Theme-aware moving indicator color — native equivalent of the WebView2 CSS
            // variable --vs-access-tool-tip-color (VS auto-injects that from this same key).
            spinner.SetResourceReference(
                Control.ForegroundProperty,
                Microsoft.VisualStudio.PlatformUI.EnvironmentColors.AccessKeyToolTipBrushKey);

            //var label = new TextBlock
            //{
            //    Text = "Thinking on your changes...",
            //    FontSize = 10,
            //    Margin = new Thickness(4, 0, 0, 0),
            //    VerticalAlignment = VerticalAlignment.Center
            //};

            var stopButton = new Button
            {
                Content = GitCommitIcons.CreateCircleX(18),
                ToolTip = "Stop generating commit message",
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 24,
                MinWidth = 24,
                MaxWidth = 24,
            };
            stopButton.Click += (s, e) => _cts?.Cancel();

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = _isIconRow ? new Thickness(2, 0, 0, 0) : new Thickness(2, 4, 2, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(spinner);
            row.Children.Add(stopButton);
            //row.Children.Add(label);
            return row;
        }

        private StackPanel BuildApproveRejectRow()
        {
            var approveIcon = GitCommitIcons.CreateCheckCompact(9);
            var approveLabel = new TextBlock
            {
                Text = "Approve",
                FontSize = 10,
                Margin = new Thickness(3, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var approveContent = new StackPanel { Orientation = Orientation.Horizontal };
            approveContent.Children.Add(approveIcon);
            approveContent.Children.Add(approveLabel);

            var approveButton = new Button
            {
                Content = approveContent,
                ToolTip = "Keep the generated commit message",
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            approveButton.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ButtonStyleKey);
            approveButton.Click += (s, e) => Approve();

            var rejectButton = new Button
            {
                Content = new TextBlock { Text = "Reject", FontSize = 10 },
                ToolTip = "Discard the generated commit message",
                Padding = new Thickness(4, 1, 4, 1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            rejectButton.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ButtonStyleKey);
            rejectButton.Click += (s, e) => Reject();

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(approveButton);
            row.Children.Add(rejectButton);
            return row;
        }

        private TextBlock BuildErrorRow(string message)
        {
            return new TextBlock
            {
                Text = message,
                FontSize = _isIconRow ? 10 : 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = _isIconRow ? new Thickness(2, 0, 0, 0) : new Thickness(2, 4, 2, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.8
            };
        }

        private void StartGeneration()
        {
            if (_state.Phase == GitCommitPhase.Generating) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _state.SetGenerating();
            Render();

#pragma warning disable VSSDK007, VSTHRD110
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(() => GenerateAsync(token));
#pragma warning restore VSSDK007, VSTHRD110
        }

        private async Task GenerateAsync(CancellationToken token)
        {
            string message = null;
            string errorText = null;

            try
            {
                message = await generator.GenerateAsync(settingsManager.GetCommitMessageSystemPrompt(), token);
            }
            catch (OperationCanceledException)
            {
                errorText = null; // cancelled — silently return to idle below
            }
            catch (NoGitChangesException)
            {
                errorText = "No changes to commit.";
            }
            catch (CommitMessageProviderException ex)
            {
                // Real provider error (network/auth/quota/etc.) — show its actual message
                // instead of writing it into the commit box as if it were a real message.
                errorText = ex.Message;
            }
            catch (Exception ex)
            {
                errorHandler?.Handle(ex, nameof(GitCommitButtonInjector));
                errorText = "Couldn't generate a commit message.";
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (token.IsCancellationRequested)
            {
                _state.Reset();
                Render();
                return;
            }

            if (errorText == null && string.IsNullOrWhiteSpace(message))
            {
                errorText = "Couldn't generate a commit message.";
            }

            if (errorText != null)
            {
                if (string.IsNullOrEmpty(errorText))
                {
                    _state.Reset();
                    Render();
                }
                else if (GitChangesInfoBar.TryShowError(errorText))
                {
                    // Shown as a real VS InfoBar banner — go straight back to the idle wand
                    // instead of also showing the compact inline error text.
                    _state.Reset();
                    Render();
                }
                else
                {
                    // InfoBar host not found (e.g. window not currently open) — fall back to
                    // the compact inline error text in the icon row.
                    _state.SetError(errorText);
                    Render();
                    ScheduleErrorAutoReset();
                }

                return;
            }

            // Re-locate fresh: the tree may have changed while we were awaiting.
            var mainWindow = Application.Current?.MainWindow;
            var point = mainWindow != null ? GitCommitVisualTreeLocator.Find(mainWindow) : null;

            if (point != null)
            {
                var textBox = point.Value.CommitTextBox;
                CommitTextBoxWriter.TryWrite(textBox, message);

                // Lock the box (read-only, not disabled — disabled greys it out) while the
                // suggestion is pending, so the user reviews it via Approve/Reject instead of
                // editing it mid-review. Deferred to ApplicationIdle: TryWrite's paste is only
                // queued, not yet processed, at this point — locking immediately races it and
                // a read-only/disabled control silently drops the still-pending paste.
                textBox.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => CommitTextBoxWriter.SetReadOnly(textBox, true)));
            }

            _state.SetResultReady();
            Render();
        }

        private void ScheduleErrorAutoReset()
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = ErrorAutoResetDelay };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (_state.Phase == GitCommitPhase.Error)
                {
                    _state.Reset();
                    Render();
                }
            };
            timer.Start();
        }

        private void Approve()
        {
            ReenableCommitTextBox();
            _state.Reset();
            Render();
        }

        private void Reject()
        {
            // Unlock first — clearing the box while it's still read-only would silently drop
            // the write (same reason locking has to wait for the write to land in the first
            // place: a read-only/disabled control ignores a still-pending paste).
            var textBox = ReenableCommitTextBox();

            if (textBox != null)
            {
                CommitTextBoxWriter.TryWrite(textBox, string.Empty);
            }

            _state.Reset();
            Render();
        }

        private static FrameworkElement ReenableCommitTextBox()
        {
            var mainWindow = Application.Current?.MainWindow;
            var point = mainWindow != null ? GitCommitVisualTreeLocator.Find(mainWindow) : null;

            if (point == null) return null;

            CommitTextBoxWriter.SetReadOnly(point.Value.CommitTextBox, false);
            return point.Value.CommitTextBox;
        }
    }
}
