using Android.App;
using Android.Content.PM;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace MiniMikuDanceMaui;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
