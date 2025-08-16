using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public class MainPage : ContentPage
{
    public MainPage()
    {
        Content = new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = "Hello, MiniMikuDance" }
            }
        };
    }
}
