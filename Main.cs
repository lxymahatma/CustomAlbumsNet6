using CustomAlbums.Patches;
using MelonLoader;

namespace CustomAlbums;

public class Main : MelonMod
{
    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        ModSettings.Register();
        AssetPatch.AttachHook();
    }
}