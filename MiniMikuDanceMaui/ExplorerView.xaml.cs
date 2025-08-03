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
    private readonly HashSet<string>? _allowedExtensions;
    private readonly bool _directoryMode;

    public event EventHandler<string>? FileSelected;
    public event EventHandler<string>? DirectorySelected;

    public ExplorerView(string rootPath, IEnumerable<string>? extensions = null, bool directoryMode = false)
    {
        _rootPath = rootPath;
        if (extensions != null)
            _allowedExtensions = extensions.Select(e => e.ToLowerInvariant()).ToHashSet();
        _directoryMode = directoryMode;
        InitializeComponent();
        SelectLabel.IsVisible = _directoryMode;
    }

    public void LoadDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        _currentPath = path;
        LogService.WriteLine($"Load directory: {path}");
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
        catch
        {
            PathLabel.Text = _currentPath;
        }
    }

    private void OnUpClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Up clicked");
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
                if (_directoryMode)
                {
                    LogService.WriteLine($"Directory chosen: {item.Path}");
                    SelectLabel.Text = "選択済";
                    DirectorySelected?.Invoke(this, item.Path);
                    return; // keep selection to indicate choice
                }

                LogService.WriteLine($"Enter directory: {item.Path}");
                LoadDirectory(item.Path);
            }
            else
            {
                LogService.WriteLine($"File chosen: {Path.GetFileName(item.Path)}");
                FileSelected?.Invoke(this, item.Path);
            }

            FileList.SelectedItem = null;
        }
    }

    private void OnSelectClicked(object? sender, EventArgs e)
    {
        if (_directoryMode)
        {
            LogService.WriteLine($"Directory chosen: {_currentPath}");
            DirectorySelected?.Invoke(this, _currentPath);
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
