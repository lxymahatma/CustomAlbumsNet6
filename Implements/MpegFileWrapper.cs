using CustomAlbums.Contracts;
using NLayer;

namespace CustomAlbums.Implements;

public class MpegFileWrapper : IAudioWrapper
{
    private readonly MpegFile _mpegFile;

    public MpegFileWrapper(MpegFile mpegFile) => _mpegFile = mpegFile;

    public int Channels => _mpegFile.Channels;
    public int SampleRate => _mpegFile.SampleRate;
    public string Extension => "mp3";
    public long Length => _mpegFile.Length;
    public long Position => _mpegFile.Position;
    public long GetSampleCount() => _mpegFile.Length / sizeof(float);

    public int ReadSamples(float[] array) => _mpegFile.ReadSamples(array, 0, array.Length);

    public void Abort() => _mpegFile.Dispose();
}