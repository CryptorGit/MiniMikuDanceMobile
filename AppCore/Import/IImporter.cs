namespace MiniMikuDance.Import;

using MiniMikuDance.Data;

public interface IImporter
{
    MmdModel LoadModel(string path);
    MmdMotion LoadMotion(string path);
}

