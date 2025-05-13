using System.IO;

namespace OptifineInstaller
{
    public interface IResourceProvider
    {
        Stream GetResourceStream(string path);
    }
}