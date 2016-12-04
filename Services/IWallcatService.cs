
namespace Wallcat.Services
{
    public interface IWallcatService
    {
        Channel[] GetChannels();

        Wallpaper GetWallpaper(string channelId);
    }

    public class Channel
    {
        public string id { get; set; }
        public string title { get; set; }
        public bool isDefault { get; set; }
    }

    public class Wallpaper
    {
        public string id { get; set; }
        public string title { get; set; }
        public WallpaperUrls url { get; set; }

        public string sourceUrl { get; set; }

        public string webLocation { get; set; }
    }

    public class WallpaperUrls
    {
        public string s { get; set; }
        public string m { get; set; }
        public string l { get; set; }
        public string o { get; set; }
    }
}