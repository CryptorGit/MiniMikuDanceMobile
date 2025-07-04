using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class ExplorerView : ContentView
{
    public ExplorerView()
    {
        InitializeComponent();
    }

    public void LoadDirectory(string path)
    {
        var items = Directory.EnumerateFileSystemEntries(path)
            .Select(p => new FileItem(p))
            .ToList();
        FileList.ItemsSource = items;
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
