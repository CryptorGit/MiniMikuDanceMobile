using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

public partial class SettingPage : ContentPage
{

    public SettingPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        // no dynamic layout required
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }

    private void OnSettingClicked(object? sender, EventArgs e)
    {
        // already on setting page
    }
}
