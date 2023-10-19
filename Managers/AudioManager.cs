using CustomAlbums.Contracts;
using CustomAlbums.Data;
using CustomAlbums.Implements;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using NAudio.Vorbis;
using NLayer;
using UnityEngine;
using Action = Il2CppSystem.Action;
using Logger = CustomAlbums.Utilities.Logger;

// ReSharper disable ComplexConditionExpression
// ReSharper disable AccessToModifiedClosure

namespace CustomAlbums.Managers;

public static class AudioManager
{
    public const int AsyncReadSpeed = 4096;

    private static Coroutine _currentCoroutine;
    private static readonly Dictionary<string, Coroutine> Coroutines = new();
    private static readonly Logger Logger = new(nameof(AudioManager));

    public static bool SwitchLoad(string name)
    {
        if (!Coroutines.TryGetValue(name, out _currentCoroutine)) return false;
        Logger.Msg($"Switching to async load of {name}");
        return true;
    }

    /// <summary>
    ///     Loads an audio clip from an mp3 file.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static AudioClip LoadClipFromMp3(Stream stream, string name)
    {
        var mp3 = new MpegFile(stream);
        var mp3Wrapper = new MpegFileWrapper(mp3);

        if (name.EndsWith("_music") && mp3.SampleRate != 44100)
            Logger.Warning($"{name}.mp3 is not 44.1khz, desyncs may occur! Consider switching to .ogg format or using 44.1khz");

        return LoadClipFromStream(stream, name, mp3Wrapper);
    }

    /// <summary>
    ///     Loads an audio clip from an ogg file.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static AudioClip LoadClipFromOgg(Stream stream, string name)
    {
        var ogg = new VorbisWaveReader(stream);
        var oggWrapper = new OggFileWrapper(ogg);

        return LoadClipFromStream(stream, name, oggWrapper);
    }

    /// <summary>
    ///     Loads an audio clip from a stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="name"></param>
    /// <param name="audioWrapper"></param>
    /// <returns></returns>
    private static AudioClip LoadClipFromStream(Stream stream, string name, IAudioWrapper audioWrapper)
    {
        var sampleCount = audioWrapper.GetSampleCount();
        var audioClip = AudioClip.Create(name, (int)sampleCount / audioWrapper.Channels,
            audioWrapper.Channels, audioWrapper.SampleRate, false);

        var remainingSamples = sampleCount;
        var index = 0;

        Coroutine coroutine = null;
        coroutine = CreateCoroutine((Il2CppSystem.Func<bool>)delegate
        {
            // Stop coroutine if the asset is unloaded
            if (audioClip is null)
            {
                Coroutines.Remove(name);
                if (_currentCoroutine == coroutine) _currentCoroutine = null;

                Logger.Msg($"Aborting async load of {name}.{audioWrapper.Extension}");
                audioWrapper.Abort();
                return true;
            }

            // Pause coroutine if it is not active
            if (coroutine != _currentCoroutine) return false;

            var sampleArray = new float[Math.Min(AsyncReadSpeed, remainingSamples)];
            var readCount = audioWrapper.ReadSamples(sampleArray);

            audioClip.SetData(sampleArray, index / audioWrapper.Channels);

            index += readCount;
            remainingSamples -= readCount;

            if (remainingSamples > 0 && readCount != 0) return false;

            stream.Dispose();

            Coroutines.Remove(name);
            _currentCoroutine = null;

            Logger.Msg($"Finished async load of {name}.");
            return true;
        });

        Coroutines.Add(name, coroutine);
        _currentCoroutine = coroutine;

        return audioClip;
    }

    /// <summary>
    ///     Creates a coroutine that runs the pass in function.
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    private static Coroutine CreateCoroutine(Il2CppSystem.Func<bool> update)
    {
        return SingletonMonoBehaviour<CoroutineManager>.instance.StartCoroutine(
            (Action)delegate { }, update);
    }

    /// <summary>
    ///     Gets the audio clip for the specified album.
    /// </summary>
    /// <param name="album">The specified album.</param>
    /// <param name="name">This is "music" or "demo". Defaults to "music".</param>
    /// <returns></returns>
    public static AudioClip GetAudio(this Album album, string name = "music")
    {
        if (album.HasFile($"{name}.mp3"))
        {
            // Load music.mp3
            using var stream = album.OpenFileStream($"{name}.mp3");
            return LoadClipFromMp3(stream, name);
        }

        if (album.HasFile($"{name}.ogg"))
        {
            // Load music.ogg
            using var stream = album.OpenFileStream($"{name}.ogg");
            return LoadClipFromOgg(stream, name);
        }

        // No music file found
        Logger.Error($"Could not find audio file for {name} in {album.Info.Name}");
        return null;
    }
}