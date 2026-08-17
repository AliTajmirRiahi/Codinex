using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Codinex.Core.Chat;
using Codinex.Core.Interfaces;
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
    internal sealed class GitCommitButtonInjector(ICommitMessageGenerator generator, IErrorHandler errorHandler)
    {
        private const string MarkerTag = "Codinex_GitCommit_Injected";
        private static readonly TimeSpan ErrorAutoResetDelay = TimeSpan.FromSeconds(3);

        private readonly GitCommitGenerationState _state = new();

        private ContentControl _host;
        private CancellationTokenSource _cts;
        private bool _isIconRow;

        /// <summary>
        /// Attempts to (re)inject the control. Safe to call repeatedly (idempotent, never throws).
        /// </summary>
        public void TryInject()
        {
            try
            {
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

        private void Inject(GitCommitInjectionPoint point)
        {
            _isIconRow = point.IsIconRow;

            _host = new ContentControl
            {
                Tag = MarkerTag,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = _isIconRow ? new Thickness(2, 0, 0, 0) : new Thickness(4, 2, 4, 2),
                HorizontalAlignment = _isIconRow ? HorizontalAlignment.Left : HorizontalAlignment.Stretch
            };

            if (!_isIconRow && point.HostPanel is Grid grid)
            {
                var insertRow = Grid.GetRow(point.Anchor);
                var col = Grid.GetColumn(point.Anchor);
                var colSpan = Grid.GetColumnSpan(point.Anchor);

                grid.RowDefinitions.Insert(insertRow, new RowDefinition { Height = GridLength.Auto });

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
                ToolTip = "Generate Commit Message (Codinex AI)"
            };
            button.Click += (s, e) => StartGeneration();

            if (_isIconRow)
            {
                // Icon-only, flat (no chrome) — same footprint and look as the native wand/'#'
                // buttons beside it: just the glyph, no visible button background/border at rest.
                button.Content = GitCommitIcons.CreateWandSparkles(14);
                button.Padding = new Thickness(2);
                button.BorderThickness = new Thickness(0);
                button.HorizontalAlignment = HorizontalAlignment.Left;
                return button;
            }

            button.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ButtonStyleKey);

            var icon = GitCommitIcons.CreateWandSparkles();
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
                Width = 32,
                Height = 3,
                VerticalAlignment = VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = "Thinking on your changes...",
                FontSize = 10,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = _isIconRow ? new Thickness(2, 0, 0, 0) : new Thickness(2, 4, 2, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(spinner);
            row.Children.Add(label);
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
                Margin = new Thickness(0, 0, 2, 0)
            };
            approveButton.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ButtonStyleKey);
            approveButton.Click += (s, e) => Approve();

            var rejectButton = new Button
            {
                Content = new TextBlock { Text = "Reject", FontSize = 10 },
                ToolTip = "Discard the generated commit message",
                Padding = new Thickness(4, 1, 4, 1)
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
                message = await generator.GenerateAsync(token);
            }
            catch (OperationCanceledException)
            {
                errorText = null; // cancelled — silently return to idle below
            }
            catch (NoGitChangesException)
            {
                errorText = "No changes to commit.";
            }
            catch (Exception ex)
            {
                errorHandler?.Handle(ex, nameof(GitCommitButtonInjector));
                errorText = "Couldn't generate a commit message.";
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (token.IsCancellationRequested) return;

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
                else
                {
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
                CommitTextBoxWriter.TryWrite(point.Value.CommitTextBox, message);
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
            _state.Reset();
            Render();
        }

        private void Reject()
        {
            var mainWindow = Application.Current?.MainWindow;
            var point = mainWindow != null ? GitCommitVisualTreeLocator.Find(mainWindow) : null;

            if (point != null)
            {
                CommitTextBoxWriter.TryWrite(point.Value.CommitTextBox, string.Empty);
            }

            _state.Reset();
            Render();
        }
    }
}
