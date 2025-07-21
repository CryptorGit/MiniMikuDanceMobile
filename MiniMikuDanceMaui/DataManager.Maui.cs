using Microsoft.Maui.Storage;
using System.IO;
using MiniMikuDance.Data;

namespace MiniMikuDance.Data;

public partial class DataManager
{
    partial Stream? OpenPackageFile(string path)
    {
        try
        {
            return FileSystem.OpenAppPackageFileAsync(path).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}
