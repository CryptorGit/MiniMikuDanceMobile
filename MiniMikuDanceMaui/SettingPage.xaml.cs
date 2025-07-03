using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

public partial class SettingPage : ContentPage
{
    private const double TopMenuHeight = 36;
    private bool _viewMenuOpen;

    public SettingPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
    }

    private void OnViewMenuTapped(object? sender, TappedEventArgs e)
    {
        _viewMenuOpen = !_viewMenuOpen;
        ViewMenu.IsVisible = _viewMenuOpen;
        UpdateLayout();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateLayout();
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        HideViewMenu();
        await Navigation.PopToRootAsync();
    }

    private void OnSettingClicked(object? sender, EventArgs e)
    {
        // already on setting page
        HideViewMenu();
    }

    private void HideViewMenu()
    {
        _viewMenuOpen = false;
        ViewMenu.IsVisible = false;
    }

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;

        AbsoluteLayout.SetLayoutBounds(TopMenu, new Rect(0, 0, W, TopMenuHeight));
        AbsoluteLayout.SetLayoutFlags(TopMenu, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ViewMenu, new Rect(0, TopMenuHeight, 200, ViewMenu.IsVisible ? 100 : 0));
        AbsoluteLayout.SetLayoutFlags(ViewMenu, AbsoluteLayoutFlags.None);
    }
}
