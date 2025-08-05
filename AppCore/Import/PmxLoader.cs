using System;
using System.IO;
using System.Text;
using MMDTools;

namespace MiniMikuDance.Import;

public class PmxHeader
{
    public float Version { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelNameEnglish { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string CommentEnglish { get; init; } = string.Empty;
}

public class PmxFile
{
    public required PmxHeader Header { get; init; }
    public required PMXObject Model { get; init; }
}

/// <summary>
/// PMX ファイルを読み込むローダー。
/// </summary>
public class PmxLoader
{
    /// <summary>
    /// ストリームから PMX モデルを読み込む。
    /// </summary>
    /// <param name="stream">PMX ファイルのストリーム</param>
    /// <returns>読み込んだ PMX データ</returns>
    public PmxFile Load(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));

        long start = stream.CanSeek ? stream.Position : 0;
        using var br = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        var magic = new string(br.ReadChars(4));
        if (magic != "PMX ")
            throw new InvalidDataException("PMX header not found");

        float version = br.ReadSingle();
        byte headerSize = br.ReadByte();
        var globals = br.ReadBytes(headerSize);
        Encoding textEncoding = globals.Length > 0 && globals[0] == 0 ? Encoding.Unicode : Encoding.UTF8;

        string modelName = ReadString(br, textEncoding);
        string modelNameEn = ReadString(br, textEncoding);
        string comment = ReadString(br, textEncoding);
        string commentEn = ReadString(br, textEncoding);

        var header = new PmxHeader
        {
            Version = version,
            ModelName = modelName,
            ModelNameEnglish = modelNameEn,
            Comment = comment,
            CommentEnglish = commentEn
        };

        if (stream.CanSeek)
            stream.Position = start;

        var model = PMXParser.Parse(stream);
        return new PmxFile { Header = header, Model = model };
    }

    /// <summary>
    /// ファイルパスから PMX モデルを読み込む。
    /// </summary>
    public PmxFile Load(string path)
    {
        using var fs = File.OpenRead(path);
        return Load(fs);
    }

    private static string ReadString(BinaryReader br, Encoding encoding)
    {
        int len = br.ReadInt32();
        var bytes = br.ReadBytes(len);
        return encoding.GetString(bytes);
    }
}
