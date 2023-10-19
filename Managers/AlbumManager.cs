using CustomAlbums.Data;

namespace CustomAlbums.Managers
{
    internal class AlbumManager
    {
        public static readonly int Uid = 999;
        public static readonly string JsonName = $"ALBUM{Uid + 1}";
        public static readonly string MusicPackage = $"music_package_{Uid}";
        public static readonly string SearchPath = "Custom_Albums";
        public static readonly string SearchExtension = "mdm";
        public static readonly Dictionary<string, string> Languages = new()
        {
            { "English", "Custom Albums" },
            { "ChineseS", "自定义" },
            { "ChineseT", "自定義" },
            { "Japanese", "カスタムアルバム" },
            { "Korean", "커스텀앨범" }
        };

        public static Dictionary<string, Album> LoadedAlbums { get; } = new();

        public static IEnumerable<string> GetAllUid()
        {
            return LoadedAlbums.Select(album => $"{Uid}-{album.Value.Index}").ToList();
        }
    }
}
