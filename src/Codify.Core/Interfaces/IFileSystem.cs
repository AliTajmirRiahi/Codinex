
namespace Codify.Core.Interfaces
{
    public interface IFileSystem
    {
        bool Exists(string path);

        string ReadAllText(string path);
    }
}
