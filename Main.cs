using MelonLoader;

namespace CustomAlbumsNet6
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            MelonLogger.Msg("Hello world!");
        }
    }
}