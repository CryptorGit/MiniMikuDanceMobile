using Android.App;
using Android.Content;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true)]
public class MainActivity : Microsoft.Maui.MauiAppCompatActivity
{
    internal static TaskCompletionSource<Android.Net.Uri?>? FolderPickerTcs { get; set; }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == 1001)
        {
            var uri = (resultCode == Result.Ok) ? data?.Data : null;
            FolderPickerTcs?.TrySetResult(uri);
        }
    }
}
