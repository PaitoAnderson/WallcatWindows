
using System.Threading.Tasks;

namespace Wallcat.Services
{
    public interface IWallcatService
    {
        Task<Channel[]> GetChannels();

        Task<Wallpaper> GetWallpaper(string channelId);
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

        public WallpaperPartner partner { get; set; }
        public WallpaperUrls url { get; set; }

        public string sourceUrl { get; set; }
    }

    public class WallpaperPartner
    {
        public string first { get; set; }
        public string last { get; set; }
    }

    public class WallpaperUrls
    {
        public string o { get; set; }
    }
}