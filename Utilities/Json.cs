using System.Text.Json;

namespace CustomAlbums.Utilities
{
    public static class Json
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        }
    }
}
