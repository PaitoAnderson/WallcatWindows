using System;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace Wallcat.Services
{
    public class WallcatService : IWallcatService
    {
        private const string ApiHost = @"https://beta.wall.cat/api/v1";

        private static readonly HttpClient Client = new HttpClient();
        public Channel[] GetChannels()
        {
            var response = Client.GetAsync($"{ApiHost}/channels").Result;
            response.EnsureSuccessStatusCode();
            return new JavaScriptSerializer().Deserialize<ChannelResponse>(response.Content.ReadAsStringAsync().Result).payload;
        }

        public Wallpaper GetWallpaper(string channelId)
        {
            var response = Client.GetAsync($"{ApiHost}/channels/{channelId}/image/{DateTime.Now:yyyy-MM-dd}T00:00:00.000Z").Result;
            response.EnsureSuccessStatusCode();
            return new JavaScriptSerializer().Deserialize<WallpaperResponse>(response.Content.ReadAsStringAsync().Result).payload.image;

        }
    }

    internal class ChannelResponse
    {
        public bool success { get; set; }
        public Channel[] payload { get; set; }
    }

    internal class WallpaperResponse
    {
        public bool success { get; set; }
        public WallpaperImageResponse payload { get; set; }
    }

    internal class WallpaperImageResponse
    {
        public Wallpaper image { get; set; }
    }
}