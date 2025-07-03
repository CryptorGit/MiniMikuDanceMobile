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
    private bool _settingMenuOpen;
    private double _bottomWidthRatio = 1.0;
    private double _cameraSensitivity = 1.0;

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
        if (_viewMenuOpen)
            HideSettingMenu();
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
        _settingMenuOpen = !_settingMenuOpen;
        SettingMenu.IsVisible = _settingMenuOpen;
        if (_settingMenuOpen && SettingContent is SettingView sv)
        {
            sv.WidthRatio = _bottomWidthRatio;
            sv.Sensitivity = _cameraSensitivity;
            sv.WidthRatioChanged += ratio =>
            {
                _bottomWidthRatio = ratio;
            };
            sv.SensitivityChanged += v =>
            {
                _cameraSensitivity = v;
            };
        }
        HideViewMenu();
        UpdateLayout();
    }

    private void HideViewMenu()
    {
        _viewMenuOpen = false;
        ViewMenu.IsVisible = false;
        HideSettingMenu();
    }

    private void HideSettingMenu()
    {
        _settingMenuOpen = false;
        SettingMenu.IsVisible = false;
    }

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;

        AbsoluteLayout.SetLayoutBounds(TopMenu, new Rect(0, 0, W, TopMenuHeight));
        AbsoluteLayout.SetLayoutFlags(TopMenu, AbsoluteLayoutFlags.None);

        double menuHeight = H - TopMenuHeight;
        AbsoluteLayout.SetLayoutBounds(ViewMenu, new Rect(0, TopMenuHeight, 200, ViewMenu.IsVisible ? menuHeight : 0));
        AbsoluteLayout.SetLayoutFlags(ViewMenu, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(SettingMenu, new Rect(0, TopMenuHeight, 250, SettingMenu.IsVisible ? menuHeight : 0));
        AbsoluteLayout.SetLayoutFlags(SettingMenu, AbsoluteLayoutFlags.None);
    }
}
