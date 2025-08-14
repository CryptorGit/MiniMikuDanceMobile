using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using MiniMikuDanceMaui.Models;

namespace MiniMikuDanceMaui.Views;

public partial class ExplorerView : ContentView
{
    private string _currentPath = string.Empty;
    private readonly string _rootPath;
    private readonly HashSet<string>? _allowedExtensions;

    public event EventHandler<string>? FileSelected;

    public ExplorerView(string rootPath, IEnumerable<string>? extensions = null)
    {
        _rootPath = rootPath;
        if (extensions != null)
            _allowedExtensions = extensions.Select(e => e.ToLowerInvariant()).ToHashSet();
        InitializeComponent();
    }

    public void LoadDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        _currentPath = path;
        UpdatePathDisplay();

        IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(path);
        if (_allowedExtensions != null)
        {
            entries = entries.Where(p => Directory.Exists(p) ||
                _allowedExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()));
        }

        var items = entries
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
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            _currentPath = _rootPath;
            PathLabel.Text = _rootPath;
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

    private void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FileItem item)
        {
            if (item.IsDirectory)
            {
                LoadDirectory(item.FullPath);
            }
            else
            {
                FileSelected?.Invoke(this, item.FullPath);
            }
            FileList.SelectedItem = null;
        }
    }
}
