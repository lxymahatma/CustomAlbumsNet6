using CustomAlbums.Contracts;
using NAudio.Vorbis;

namespace CustomAlbums.Implements;

public class OggFileWrapper : IAudioWrapper
{
    private readonly VorbisWaveReader _oggFile;

    public OggFileWrapper(VorbisWaveReader oggFile) => _oggFile = oggFile;
    public int Channels => _oggFile.WaveFormat.Channels;
    public int SampleRate => _oggFile.WaveFormat.SampleRate;
    public string Extension => "ogg";
    public long Length => _oggFile.Length;
    public long Position => _oggFile.Position;
    public long GetSampleCount() => Length / _oggFile.WaveFormat.BitsPerSample / 8;

    public int ReadSamples(float[] array) => _oggFile.Read(array, 0, array.Length);

    public void Abort() => _oggFile.Dispose();
}