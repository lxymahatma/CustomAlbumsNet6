using Il2CppAssets.Scripts.GameCore;

namespace CustomAlbums.Data;

public class Sheet
{
    public string MD5 { get; }
    public StageInfo StageInfo { get; }

    public Sheet(string md5, StageInfo stageInfo)
    {
        MD5 = md5;
        StageInfo = stageInfo;
    }
}