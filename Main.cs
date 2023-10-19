using MelonLoader;

namespace CustomAlbums
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            ModSettings.Register();
            Patches.AssetPatch.AttachHook();
        }
    }
}