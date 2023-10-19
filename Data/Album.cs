using System.IO.Compression;
using System.Security.Cryptography;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.GameCore;
using Il2CppGameLogic;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Data;

public class Album
{
    private readonly Logger _logger = new(nameof(Album));
    public int Index { get; }
    public string Path { get; }
    public bool IsPackaged { get; }
    public AlbumInfo Info { get; }
    public Sprite Cover => this.GetCover();
    public AnimatedCover AnimatedCover => this.GetAnimatedCover();
    public AudioClip Music => this.GetAudio();
    public AudioClip Demo => this.GetAudio("demo");
    public Dictionary<int, Sheet> Sheets { get; } = new();

    public Album(string path, int index)
    {
        if (Directory.Exists(path))
        {
            // Load album from directory
            if (!File.Exists($"{path}\\info.json"))
            {
                _logger.Error($"Could not find info.json at: {path}\\info.json");
                throw new FileNotFoundException();
            }

            Info = Json.Deserialize<AlbumInfo>(File.ReadAllText($"{path}\\info.json"));
        }
        else if (File.Exists(path))
        {
            // Load album from package
            using var zip = ZipFile.OpenRead(path);
            var info = zip.GetEntry("info.json");
            if (info == null)
            {
                _logger.Error($"Could not find info.json in package: {path}");
                throw new FileNotFoundException();
            }

            Info = Json.Deserialize<AlbumInfo>(new StreamReader(info.Open()).ReadToEnd());
            IsPackaged = true;
        }
        else
        {
            _logger.Error($"Could not find album at: {path}");
            throw new FileNotFoundException();
        }

        Index = index;
        Path = path;

        GetSheets();
    }

    public bool HasFile(string name)
    {
        if (IsPackaged)
        {
            using var zip = ZipFile.OpenRead(Path);
            return zip.GetEntry(name) != null;
        }

        var path = $"{Path}\\{name}";
        return File.Exists(path);
    }

    public MemoryStream OpenFileStream(string file)
    {
        if (IsPackaged)
        {
            using var zip = ZipFile.OpenRead(Path);
            var entry = zip.GetEntry(file);
            if (entry != null) return (MemoryStream)entry.Open();

            _logger.Error($"Could not find file in package: {file}");
            throw new FileNotFoundException();
        }

        var path = $"{Path}\\{file}";
        if (File.Exists(path))
        {
            var stream = new MemoryStream();
            File.OpenRead(path).CopyTo(stream);
            stream.Position = 0;

            return stream;
        }

        _logger.Error($"Could not find file: {path}");
        throw new FileNotFoundException();
    }

    public static string GetHash(MemoryStream stream)
    {
        var hash = MD5.Create().ComputeHash(stream.ToArray());
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private void GetSheets()
    {
        // Adds to the Sheets dictionary
        foreach (var difficulty in Info.Difficulties.Keys)
        {
            using var stream = OpenFileStream($"map{difficulty}.bms");
            var hash = GetHash(stream);

            // TODO: Use BMSCLoader to load the mapX.bms file and create the StageInfo object

            var mapName = $"{Index}_map{difficulty}";
            var stageInfo = ScriptableObject.CreateInstance<StageInfo>();
            stageInfo.mapName = mapName;
            stageInfo.music = $"{Index}";
            stageInfo.difficulty = difficulty;
            stageInfo.bpm = 100; // TODO: Get from BMS
            stageInfo.md5 = hash;
            stageInfo.sceneEvents = new Il2CppSystem.Collections.Generic.List<SceneEvent>(); // TODO: Get from BMS
            stageInfo.name = Info.Name;

            Sheets.Add(difficulty, new Sheet(hash, stageInfo));
        }
    }
}