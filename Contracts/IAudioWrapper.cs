namespace CustomAlbums.Contracts;

public interface IAudioWrapper
{
    int Channels { get; }
    int SampleRate { get; }
    string Extension { get; }
    long Length { get; }
    long Position { get; }
    long GetSampleCount();
    int ReadSamples(float[] array);
    void Abort();
}