using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Codinex.VisualStudio.CommitMessages
{
    internal readonly struct GitCommitInjectionPoint
    {
        public GitCommitInjectionPoint(FrameworkElement commitTextBox, Panel hostPanel, FrameworkElement anchor, bool isIconRow)
        {
            CommitTextBox = commitTextBox;
            HostPanel = hostPanel;
            Anchor = anchor;
            IsIconRow = isIconRow;
        }

        public FrameworkElement CommitTextBox { get; }

        public Panel HostPanel { get; }

        public FrameworkElement Anchor { get; }

        /// <summary>
        /// True when <see cref="HostPanel"/> is the native icon strip next to the commit box
        /// (holds Copilot's wand button and the "insert work item" '#' button) — the control
        /// should render as a compact icon in this case, not a full-width row.
        /// </summary>
        public bool IsIconRow { get; }
    }

    /// <summary>
    /// Locates the Git Changes commit-message textbox in Visual Studio's native window, and the
    /// best place to inject a sibling control next to it.
    /// </summary>
    internal static class GitCommitVisualTreeLocator
    {
        // Confirmed via static BAML decompilation of Microsoft.TeamFoundation.Git.Provider.dll
        // (VS 2022 17.14 and VS 2026 18.3): the commit box is LabeledTextBox "commentTextBox"
        // with NameAndAutomationId="Commit comment". Older/alternate ids kept as a fallback net.
        private static readonly string[] TextBoxCandidateIds =
        {
            "Commit comment",
            "commentTextBox",
            "textCommitMessage",
            "CommitMessageTextBox"
        };

        // The icon strip that hosts Copilot's own "Generate commit message" wand button and the
        // "Insert item" '#' button — GitPendingChangesPageView.xaml, StackPanel Grid.Column="1"
        // inside "WorkItemActions". Located by the AutomationProperties.Name/x:Name Microsoft
        // assigned those buttons, since the strip itself carries no Name/AutomationId.
        private static readonly string[] IconRowAnchorNames =
        {
            "InsertItem",
            "describeChangesButton"
        };

        private static readonly string[] IconRowAnchorAutomationNames =
        {
            "Insert item",
            "Generate commit message"
        };

        public static GitCommitInjectionPoint? Find(Window mainWindow)
        {
            if (mainWindow == null) return null;

            var textBox = FindTextBox(mainWindow);
            if (textBox == null) return null;

            var iconRowAnchor = FindIconRowAnchor(mainWindow);
            if (iconRowAnchor != null && VisualTreeHelper.GetParent(iconRowAnchor) is Panel iconRowPanel)
            {
                return new GitCommitInjectionPoint(textBox, iconRowPanel, iconRowAnchor, isIconRow: true);
            }

            // Fallback: no icon strip found (e.g. ShowMessageActionButtons is off) — insert a
            // full row above the first ancestor panel of the textbox with >=3 children.
            var fallback = FindRowAbove(textBox);
            return fallback == null
                ? (GitCommitInjectionPoint?)null
                : new GitCommitInjectionPoint(textBox, fallback.Value.Panel, fallback.Value.Anchor, isIconRow: false);
        }

        private static FrameworkElement FindTextBox(DependencyObject root)
        {
            foreach (var id in TextBoxCandidateIds)
            {
                if (FindByAutomationId(root, id) is FrameworkElement fe) return fe;
            }

            return null;
        }

        private static FrameworkElement FindIconRowAnchor(DependencyObject root)
        {
            foreach (var name in IconRowAnchorNames)
            {
                if (FindByName(root, name) is FrameworkElement fe) return fe;
            }

            foreach (var name in IconRowAnchorAutomationNames)
            {
                if (FindByAutomationName(root, name) is FrameworkElement fe) return fe;
            }

            return null;
        }

        private static (Panel Panel, FrameworkElement Anchor)? FindRowAbove(FrameworkElement textBox)
        {
            DependencyObject current = textBox;

            while (true)
            {
                var up = VisualTreeHelper.GetParent(current);
                if (up == null) return null;

                // Skip template internals of the textbox itself and small wrapper panels
                // (section-content grids with <=2 children) until we reach the true layout
                // container that also hosts other sibling controls (>=3 children).
                if (up is TextBox || up is RichTextBox
                    || up.GetType().Name.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    current = up;
                    continue;
                }

                if (up is Panel panel)
                {
                    if (panel.Children.Count >= 3)
                    {
                        return (panel, current as FrameworkElement ?? textBox);
                    }

                    current = up;
                    continue;
                }

                current = up;
            }
        }

        private static DependencyObject FindByAutomationId(DependencyObject root, string id)
        {
            return FindWhere(root, fe => fe.Name == id || AutomationProperties.GetAutomationId(fe) == id);
        }

        private static DependencyObject FindByName(DependencyObject root, string name)
        {
            return FindWhere(root, fe => fe.Name == name);
        }

        private static DependencyObject FindByAutomationName(DependencyObject root, string automationName)
        {
            return FindWhere(root, fe => AutomationProperties.GetName(fe) == automationName);
        }

        private static DependencyObject FindWhere(DependencyObject root, Func<FrameworkElement, bool> predicate)
        {
            if (root == null) return null;

            var stack = new Stack<DependencyObject>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is FrameworkElement fe && predicate(fe))
                {
                    return current;
                }

                var count = VisualTreeHelper.GetChildrenCount(current);
                for (var i = count - 1; i >= 0; i--)
                {
                    stack.Push(VisualTreeHelper.GetChild(current, i));
                }
            }

            return null;
        }
    }
}
