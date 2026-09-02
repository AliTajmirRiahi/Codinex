using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Codinex.Core.Chat;
using Codinex.Core.Interfaces.Git;
using Codinex.Core.Interfaces.Services;
using Codinex.Storage.Managers;
using Microsoft.VisualStudio.Shell;

#pragma warning disable CS4014, VSTHRD110, VSTHRD001 // vs-threading analyzers suppressed project-wide for the VS-integration layer; call sites are audited manually.

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
        private FrameworkElement _commitTextBox;
        private readonly Dictionary<Control, RoutedEventHandler> _commitActionButtonClickHandlers = new();
        private DispatcherTimer _commitOperationMonitor;
        private ICommand _commitOperationCommand;
        private bool _isCommitOperationInProgress;
        private bool _commitOperationObservedUnavailable;

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

                TrackCommitActionButtons(mainWindow);

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

                CompleteCommitOperation();
                DetachCommitActionButtonHandlers();

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
                _commitTextBox = null;
                _state.Reset();
                SetCommitActionButtonsEnabled(true);
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
            _commitTextBox = point.CommitTextBox;

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

            // A generated message is pending user review once we reach ResultReady, and a
            // native commit may also already be running. Keep the native "Commit All"/"Commit
            // Staged" buttons disabled in both cases so the user can't commit twice or commit
            // the still-unreviewed suggestion.
            SetCommitActionButtonsEnabled(!_isCommitOperationInProgress && _state.Phase != GitCommitPhase.ResultReady);

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
            row.Children.Add(stopButton);

            // Progress bar sits outside/below the row above, sized to match the commit
            // message textbox's width (via a live binding) instead of stretching the full
            // width of the row it used to be embedded in.
            var spinner = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 2, 2, 0)
            };
            // Theme-aware moving indicator color — native equivalent of the WebView2 CSS
            // variable --vs-access-tool-tip-color (VS auto-injects that from this same key).
            spinner.SetResourceReference(
                Control.ForegroundProperty,
                Microsoft.VisualStudio.PlatformUI.EnvironmentColors.AccessKeyToolTipBrushKey);

            if (_commitTextBox != null)
            {
                spinner.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding(nameof(FrameworkElement.ActualWidth))
                {
                    Source = _commitTextBox,
                    Mode = System.Windows.Data.BindingMode.OneWay
                });
            }
            else
            {
                spinner.Width = 64;
            }

            var container = new StackPanel { Orientation = Orientation.Vertical };
            container.Children.Add(row);
            container.Children.Add(spinner);

            return container;
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

            if (point == null)
            {
                _state.SetError("Couldn't find the Git commit message box.");
                Render();
                ScheduleErrorAutoReset();
                return;
            }

            var textBox = point.Value.CommitTextBox;
            if (!CommitTextBoxWriter.TryWrite(textBox, message))
            {
                _state.SetError("Couldn't write the generated commit message.");
                Render();
                ScheduleErrorAutoReset();
                return;
            }

            // Lock the box (read-only, not disabled — disabled greys it out) while the
            // suggestion is pending, so the user reviews it via Approve/Reject instead of
            // editing it mid-review. Deferred to ApplicationIdle: TryWrite's paste is only
            // queued, not yet processed, at this point — locking immediately races it and
            // a read-only/disabled control silently drops the still-pending paste.
            textBox.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                new Action(() => CommitTextBoxWriter.SetReadOnly(textBox, true)));

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
                CommitTextBoxWriter.TryClear(textBox);
            }

            _state.Reset();
            Render();
            SetCommitActionButtonsEnabled(true);
        }

        private void TrackCommitActionButtons(Window mainWindow)
        {
            var buttons = GitCommitVisualTreeLocator.FindCommitActionButtons(mainWindow);
            var currentButtons = new HashSet<Control>(buttons);

            foreach (var button in buttons)
            {
                if (!_commitActionButtonClickHandlers.ContainsKey(button))
                {
                    RoutedEventHandler handler = OnCommitActionButtonClick;
                    button.AddHandler(ButtonBase.ClickEvent, handler, true);
                    _commitActionButtonClickHandlers[button] = handler;
                }

                button.Unloaded -= OnCommitActionButtonUnloaded;
                button.Unloaded += OnCommitActionButtonUnloaded;

                if (_isCommitOperationInProgress || _state.Phase == GitCommitPhase.ResultReady)
                {
                    SetCommitActionButtonEnabled(button, false);
                }
            }

            var staleButtons = new List<Control>();
            foreach (var button in _commitActionButtonClickHandlers.Keys)
            {
                if (!currentButtons.Contains(button))
                {
                    staleButtons.Add(button);
                }
            }

            foreach (var button in staleButtons)
            {
                DetachCommitActionButtonHandler(button);
            }
        }

        private void OnCommitActionButtonUnloaded(object sender, RoutedEventArgs e)
        {
            DetachCommitActionButtonHandler(sender as Control);
        }

        private void DetachCommitActionButtonHandlers()
        {
            var buttons = new List<Control>(_commitActionButtonClickHandlers.Keys);
            foreach (var button in buttons)
            {
                DetachCommitActionButtonHandler(button);
            }
        }

        private void DetachCommitActionButtonHandler(Control button)
        {
            if (button == null) return;

            if (_commitActionButtonClickHandlers.TryGetValue(button, out var handler))
            {
                button.RemoveHandler(ButtonBase.ClickEvent, handler);
                _commitActionButtonClickHandlers.Remove(button);
            }

            button.Unloaded -= OnCommitActionButtonUnloaded;
        }

        private void OnCommitActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Control control)) return;

            var commandButton = FindClickedCommitCommandButton(control, e.OriginalSource as DependencyObject);
            if (commandButton == null) return;

            if (_isCommitOperationInProgress)
            {
                e.Handled = true;
                _ = control.Dispatcher.BeginInvoke(
                    DispatcherPriority.Send,
                    new Action(() => SetCommitActionButtonsEnabled(false)));
                return;
            }

            BeginCommitOperation(commandButton);
        }

        private void BeginCommitOperation(Control control)
        {
            _isCommitOperationInProgress = true;
            _commitOperationObservedUnavailable = false;
            ObserveCommitOperationCommand(FindCommitCommandButton(control)?.Command);
            EnsureCommitOperationMonitor();

            // Defer the actual IsEnabled write until the native Click handler has had a chance
            // to execute its command. This still runs before another input event can trigger a
            // second commit, but avoids interfering with Visual Studio's existing commit path.
            _ = control.Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                new Action(() =>
                {
                    SetCommitActionButtonsEnabled(false);
                    _commitOperationMonitor?.Start();
                }));
        }

        private void ObserveCommitOperationCommand(ICommand command)
        {
            if (_commitOperationCommand != null)
            {
                _commitOperationCommand.CanExecuteChanged -= OnCommitOperationCanExecuteChanged;
            }

            _commitOperationCommand = command;

            if (_commitOperationCommand != null)
            {
                _commitOperationCommand.CanExecuteChanged += OnCommitOperationCanExecuteChanged;
            }
        }

        private void OnCommitOperationCanExecuteChanged(object sender, EventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            _ = dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (!_isCommitOperationInProgress) return;

                    if (CanAnyCommitActionExecute())
                    {
                        CompleteCommitOperation();
                    }
                    else
                    {
                        _commitOperationObservedUnavailable = true;
                        SetCommitActionButtonsEnabled(false);
                    }
                }));
        }

        private void EnsureCommitOperationMonitor()
        {
            if (_commitOperationMonitor != null) return;

            _commitOperationMonitor = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _commitOperationMonitor.Tick += OnCommitOperationMonitorTick;
        }

        private void OnCommitOperationMonitorTick(object sender, EventArgs e)
        {
            if (!_isCommitOperationInProgress)
            {
                _commitOperationMonitor?.Stop();
                return;
            }

            if (CanAnyCommitActionExecute())
            {
                if (_commitOperationObservedUnavailable)
                {
                    CompleteCommitOperation();
                }
                else
                {
                    SetCommitActionButtonsEnabled(false);
                }
            }
            else
            {
                _commitOperationObservedUnavailable = true;
                SetCommitActionButtonsEnabled(false);
            }
        }

        private void CompleteCommitOperation()
        {
            _commitOperationMonitor?.Stop();
            ObserveCommitOperationCommand(null);

            if (!_isCommitOperationInProgress) return;

            _isCommitOperationInProgress = false;
            _commitOperationObservedUnavailable = false;
            SetCommitActionButtonsEnabled(_state.Phase != GitCommitPhase.ResultReady);
        }

        private static bool CanAnyCommitActionExecute()
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null) return true;

            var foundButton = false;
            foreach (var button in GitCommitVisualTreeLocator.FindCommitActionButtons(mainWindow))
            {
                foundButton = true;

                var commandButton = FindCommitCommandButton(button);
                if (commandButton != null)
                {
                    if (CanExecuteCommitAction(commandButton))
                    {
                        return true;
                    }
                }
                else if (button.IsEnabled)
                {
                    return true;
                }
            }

            return !foundButton;
        }

        private static bool CanExecuteCommitAction(ButtonBase button)
        {
            var command = button.Command;
            if (command == null) return button.IsEnabled;

            var parameter = button.CommandParameter;
            if (command is RoutedCommand routedCommand)
            {
                return routedCommand.CanExecute(parameter, button.CommandTarget ?? button);
            }

            return command.CanExecute(parameter);
        }

        private static void SetCommitActionButtonsEnabled(bool enabled)
        {
            try
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow == null) return;

                foreach (var button in GitCommitVisualTreeLocator.FindCommitActionButtons(mainWindow))
                {
                    SetCommitActionButtonEnabled(button, enabled);
                }
            }
            catch
            {
                // Never disrupt Visual Studio.
            }
        }

        private static void SetCommitActionButtonEnabled(Control button, bool enabled)
        {
            if (enabled)
            {
                button.IsEnabled = true;
            }

            var commandButton = FindCommitCommandButton(button);
            if (commandButton != null)
            {
                commandButton.IsEnabled = enabled;
            }
            else if (!enabled && button is ButtonBase buttonBase)
            {
                buttonBase.IsEnabled = false;
            }
        }

        private static ButtonBase FindClickedCommitCommandButton(Control trackedControl, DependencyObject originalSource)
        {
            var current = originalSource;
            while (current != null)
            {
                if (current is ButtonBase clickedButton)
                {
                    return GitCommitVisualTreeLocator.IsCommitActionButton(clickedButton)
                        ? clickedButton
                        : null;
                }

                if (ReferenceEquals(current, trackedControl)) break;

                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return FindCommitCommandButton(trackedControl);
        }

        private static ButtonBase FindCommitCommandButton(Control control)
        {
            if (control is ButtonBase buttonBase) return buttonBase;

            var descendantButton = FindVisualDescendants<ButtonBase>(control)
                .FirstOrDefault(GitCommitVisualTreeLocator.IsCommitActionButton);
            if (descendantButton != null) return descendantButton;

            return FindVisualDescendants<ButtonBase>(control)
                .FirstOrDefault(button => button.Command != null)
                ?? FindVisualDescendants<ButtonBase>(control).FirstOrDefault();
        }

        private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            var stack = new Stack<DependencyObject>();
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                stack.Push(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is T match)
                {
                    yield return match;
                }

                count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
                for (var i = 0; i < count; i++)
                {
                    stack.Push(System.Windows.Media.VisualTreeHelper.GetChild(current, i));
                }
            }
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
