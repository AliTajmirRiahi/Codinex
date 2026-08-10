namespace Codinex.VisualStudio;

/// <summary>
/// Persistence GUIDs for tool windows, shared between the Codinex.VSIX package
/// (which registers/creates the panes) and services in this project that need to
/// show them without a direct reference to the pane types.
/// </summary>
public static class ToolWindowGuids
{
    public const string CodeChangesToolWindow = "6c2a9e2f-7c9e-4b0a-9a6f-3a7a8f6c9b2d";
}
