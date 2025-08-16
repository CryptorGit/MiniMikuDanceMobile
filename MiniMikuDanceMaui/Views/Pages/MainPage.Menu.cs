using Microsoft.Maui.Controls;
using System;
using MiniMikuDanceMaui.Views.Panels;

namespace MiniMikuDanceMaui.Views.Pages;

public partial class MainPage
{
    private void UpdateOverlay() => MenuOverlay.IsVisible = _viewMenuOpen || _settingMenuOpen || _fileMenuOpen;

    private void SetMenuVisibility(ref bool flag, View menu, bool visible)
    {
        flag = visible;
        menu.IsVisible = visible;
        UpdateOverlay();
    }

    private void ShowViewMenu()
    {
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, true);
    }

    private void OnViewMenuTapped(object? sender, TappedEventArgs e)
    {
        var visible = !_viewMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, visible);
        UpdateLayout();
    }

    private void ShowSettingMenu()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, true);
        if (SettingContent is SettingView sv)
        {
            sv.HeightRatio = _bottomHeightRatio;
            sv.RotateSensitivity = _renderer.RotateSensitivity;
            sv.PanSensitivity = _renderer.PanSensitivity;
            sv.IkBoneSize = _renderer.IkBoneScale;
            sv.BonePickPixels = _renderer.BonePickPixels;
        }
    }

    private void OnSettingMenuTapped(object? sender, EventArgs e)
    {
        var visible = !_settingMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, visible);
        UpdateLayout();
    }

    private void ShowFileMenu()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, true);
    }

    private void OnFileMenuTapped(object? sender, TappedEventArgs e)
    {
        var visible = !_fileMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, visible);
        UpdateLayout();
    }

    private void OnOpenInViewerClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        SelectedModelPath.Text = string.Empty;
        _selectedModelPath = null;
        _modelDir = null;
        _modelScale = 1f;
        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenusAndLayout();
    }

    private void HideAllMenus()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        UpdateLayout();
    }

    private void HideAllMenusAndLayout()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        UpdateLayout();
    }
}

