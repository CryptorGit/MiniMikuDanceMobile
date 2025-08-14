using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class MainPage
{
    private void SetMenuVisibility(Grid menu, ref bool isOpen, bool visible)
    {
        isOpen = visible;
        menu.IsVisible = visible;
    }

    private void HideAllMenus()
    {
        SetMenuVisibility(ViewMenu, ref _viewMenuOpen, false);
        SetMenuVisibility(SettingMenu, ref _settingMenuOpen, false);
        SetMenuVisibility(FileMenu, ref _fileMenuOpen, false);
        UpdateOverlay();
        UpdateLayout();
    }

    private void HideAllMenusAndLayout() => HideAllMenus();

    private void OnViewMenuTapped(object? sender, TappedEventArgs e)
    {
        var newVisible = !_viewMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ViewMenu, ref _viewMenuOpen, newVisible);
        UpdateOverlay();
        UpdateLayout();
    }

    private void OnSettingMenuTapped(object? sender, EventArgs e)
    {
        var newVisible = !_settingMenuOpen;
        HideAllMenus();
        SetMenuVisibility(SettingMenu, ref _settingMenuOpen, newVisible);
        if (_settingMenuOpen && SettingContent is SettingView sv)
        {
            sv.HeightRatio = _bottomHeightRatio;
            sv.RotateSensitivity = _renderer.RotateSensitivity;
            sv.PanSensitivity = _renderer.PanSensitivity;
            sv.IkBoneSize = _renderer.IkBoneScale;
            sv.BonePickPixels = _renderer.BonePickPixels;
        }
        UpdateOverlay();
        UpdateLayout();
    }

    private void OnFileMenuTapped(object? sender, TappedEventArgs e)
    {
        var newVisible = !_fileMenuOpen;
        HideAllMenus();
        SetMenuVisibility(FileMenu, ref _fileMenuOpen, newVisible);
        UpdateOverlay();
        UpdateLayout();
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e) => HideAllMenusAndLayout();

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e) => HideAllMenusAndLayout();
}
