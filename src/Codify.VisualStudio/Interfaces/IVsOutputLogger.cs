namespace Codify.VisualStudio.Interfaces
{
    /// <summary>
    /// Abstracts writing diagnostic information to Visual Studio output window.
    /// </summary>
    public interface IVsOutputLogger
    {
        /// <summary>
        /// Writes a diagnostic message to the output window.
        /// </summary>
        void WriteLine(string message);
    }
}