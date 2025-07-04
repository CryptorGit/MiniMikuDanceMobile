using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class ExplorerView : ContentView
{
    private string _currentPath = string.Empty;
    private readonly string _rootPath;

    public event EventHandler<string>? FileSelected;

    public ExplorerView(string rootPath)
    {
        _rootPath = rootPath;
        InitializeComponent();
    }

    public void LoadDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        _currentPath = path;
        UpdatePathDisplay();
        
        var items = Directory.EnumerateFileSystemEntries(path)
            .Select(p => new FileItem(p))
            .OrderByDescending(f => f.IsDirectory)
            .ThenBy(f => f.Name)
            .ToList();
        FileList.ItemsSource = items;
    }

    private void UpdatePathDisplay()
    {
        try
        {
            var baseParent = Directory.GetParent(_rootPath)?.FullName ?? _rootPath;
            string text;
            if (_currentPath.StartsWith(baseParent))
            {
                var relative = _currentPath.Substring(baseParent.Length).TrimStart(Path.DirectorySeparatorChar);
                var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                var rootName = Path.GetFileName(baseParent);
                text = string.IsNullOrEmpty(relative)
                    ? rootName
                    : rootName + " > " + string.Join(" > ", segments);
            }
            else
            {
                text = _currentPath;
            }

            PathLabel.Text = text;
        }
        catch
        {
            PathLabel.Text = _currentPath;
        }
    }

    private void OnUpClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentPath)) return;
        if (Path.GetFullPath(_currentPath).TrimEnd(Path.DirectorySeparatorChar)
            .Equals(Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            LoadDirectory(_rootPath);
            return;
        }

        var parent = Directory.GetParent(_currentPath)?.FullName;
        if (!string.IsNullOrEmpty(parent) && parent.StartsWith(_rootPath))
        {
            LoadDirectory(parent);
        }
    }

    private void OnReloadClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentPath))
        {
            LoadDirectory(_currentPath);
        }
    }

    private void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FileItem item)
        {
            if (item.IsDirectory)
            {
                LoadDirectory(item.Path);
            }
            else
            {
                FileSelected?.Invoke(this, item.Path);
            }
            FileList.SelectedItem = null;
        }
    }

    private class FileItem
    {
        public FileItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
            IsDirectory = Directory.Exists(path);
            if (IsDirectory)
            {
                Modified = Directory.GetLastWriteTime(path);
                SizeText = "";
            }
            else
            {
                Modified = File.GetLastWriteTime(path);
                try
                {
                    long size = new FileInfo(path).Length;
                    SizeText = size.ToString();
                }
                catch
                {
                    SizeText = "";
                }
            }
        }

        public string Path { get; }
        public string Name { get; }
        public bool IsDirectory { get; }
        public DateTime Modified { get; }
        public string SizeText { get; }
    }
}
