using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.IO;

namespace MiniMikuDanceMaui;

public static class FolderSelector
{
    public static async Task<string?> PickFolderAsync()
    {
#if ANDROID
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select any file inside the folder"
        });
        return result != null ? Path.GetDirectoryName(result.FullPath) : null;
#else
        if (FolderPicker.Default.IsSupported)
        {
            var folder = await FolderPicker.Default.PickAsync();
            return folder?.Folder?.Path;
        }
        return null;
#endif
    }
}
