using System;
using System.IO;

namespace MiniMikuDanceMaui;

public class FileItem
{
    public FileItem(string fullPath)
    {
        FullPath = fullPath;
        Name = System.IO.Path.GetFileName(fullPath);
        IsDirectory = Directory.Exists(fullPath);
        if (IsDirectory)
        {
            Modified = Directory.GetLastWriteTime(fullPath);
            SizeText = "";
        }
        else
        {
            Modified = File.GetLastWriteTime(fullPath);
            try
            {
                long size = new FileInfo(fullPath).Length;
                SizeText = size.ToString();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                SizeText = "";
                HasError = true;
            }
        }
    }

    public string FullPath { get; }
    public string Name { get; }
    public bool IsDirectory { get; }
    public DateTime Modified { get; }
    public string SizeText { get; }
    public bool HasError { get; private set; }
}
