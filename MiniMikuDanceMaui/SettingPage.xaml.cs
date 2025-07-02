using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

public partial class SettingPage : ContentPage
{
    private bool _sidebarOpen;
    private const double SidebarWidthRatio = 0.35;

    public SettingPage()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;

        MenuButton.Clicked += async (s, e) => await AnimateSidebar(!_sidebarOpen);
        var overlayTap = new TapGestureRecognizer();
        overlayTap.Tapped += async (s, e) => await AnimateSidebar(false);
        MenuOverlay.GestureRecognizers.Add(overlayTap);

        HomeBtn.Clicked += async (s, e) =>
        {
            await Navigation.PopToRootAsync();
            await AnimateSidebar(false);
        };
        SettingBtn.Clicked += async (s, e) => await AnimateSidebar(false);
        PickFolderBtn.Clicked += async (s, e) => await PickFolder();
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;
        AbsoluteLayout.SetLayoutBounds(MenuOverlay, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(MenuOverlay, AbsoluteLayoutFlags.None);
        MenuOverlay.IsVisible = _sidebarOpen;

        AbsoluteLayout.SetLayoutBounds(MenuButton, new Rect(W - 72, H - 72, 56, 56));
        AbsoluteLayout.SetLayoutFlags(MenuButton, AbsoluteLayoutFlags.None);

        double menuWidth = W * SidebarWidthRatio;
        double sidebarX = _sidebarOpen ? W - menuWidth : W;
        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, menuWidth, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);
    }

    private async Task AnimateSidebar(bool open)
    {
        double menuWidth = Width * SidebarWidthRatio;
        double dest = open ? Width - menuWidth : Width;
        MenuOverlay.IsVisible = open;
        await Sidebar.LayoutTo(new Rect(dest, 0, menuWidth, Height), 280, Easing.SinOut);
        _sidebarOpen = open;
        UpdateLayout();
    }

    private async Task PickFolder()
    {
        try
        {
            var folder = await FolderSelector.PickFolderAsync();
            if (!string.IsNullOrEmpty(folder))
                FolderLabel.Text = folder;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
