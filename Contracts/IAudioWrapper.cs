namespace CustomAlbums.Contracts;

public interface IAudioWrapper
{
    int Channels { get; }
    int SampleRate { get; }
    string Extension { get; }
    long GetSampleCount();
    int ReadSamples(float[] array);
    void Abort();
}