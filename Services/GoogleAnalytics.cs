using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Wallcat.Services
{
    public enum GoogleAnalyticsCategory
    {
        system,
        channel,
        wallpaper
    }

    public enum GoogleAnalyticsAction
    {
        appInstalled,
        appLaunched,
        appQuit,

        wallpaperSet,
        wallpaperShared,
        wallpaperSourceTapped,

        channelSubscribed,
        channelUnsubscribed,
        channelCreateTapped
    }

    public enum GoogleAnalyticsDimension
    {
        wallpaperId,
        wallpaperTitle,

        partnerId,
        partnerName,

        channelId,
        channelTitle
    }

    public struct DimensionTuple
    {
        public GoogleAnalyticsDimension Dimension;
        public string Value;

        public DimensionTuple(GoogleAnalyticsDimension dim, string val)
        {
            Dimension = dim;
            Value = val;
        }
    }

    public class GoogleAnalytics
    {
        private const string ApiHost = @"https://www.google-analytics.com";
        private const string GoogleAnalyticsTrackingID = "UA-89588678-1";

        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        public async Task SubmitEvent(GoogleAnalyticsCategory category, GoogleAnalyticsAction action)
        {
            await SubmitEvent(category, action, string.Empty, new DimensionTuple[] { });
        }

        public async Task SubmitEvent(GoogleAnalyticsCategory category, GoogleAnalyticsAction action, string label, DimensionTuple[] dimensions)
        {
            try
            {
                var content = GoogleAnalyticsSetup();
                content.Add(new KeyValuePair<string, string>("ec", category.ToString()));
                content.Add(new KeyValuePair<string, string>("ea", action.ToString()));
                content.Add(new KeyValuePair<string, string>("el", label));
                foreach(var dimension in dimensions)
                {
                    content.Add(new KeyValuePair<string, string>(GetDimension(dimension.Dimension), dimension.Value));
                }

                await Client.PostAsync($"{ApiHost}/collect", new FormUrlEncodedContent(content.ToArray()));
            }
            catch (Exception) { /* I don't care if this fails..*/ }
        }

        public async Task SubmitException(string exceptionDescription)
        {
            try
            {
                var content = GoogleAnalyticsSetup();
                content.Add(new KeyValuePair<string, string>("exd", exceptionDescription));

                await Client.PostAsync($"{ApiHost}/collect", new FormUrlEncodedContent(content.ToArray()));
            }
            catch (Exception) { /* I don't care if this fails..*/ }
        }

        private List<KeyValuePair<string, string>> GoogleAnalyticsSetup()
        {
            return new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("v", 1.ToString()),
                new KeyValuePair<string, string>("tid", GoogleAnalyticsTrackingID),
                new KeyValuePair<string, string>("cid", Properties.Settings.Default.UniqueIdentifier.Value.ToString()),
                new KeyValuePair<string, string>("t", "event"),
                new KeyValuePair<string, string>("ds", "Windows"),
                new KeyValuePair<string, string>("an", "Wallcat for Windows"),
                new KeyValuePair<string, string>("av", Assembly.GetExecutingAssembly().GetName().Version.ToString())
            };
        }

        private static string GetDimension(GoogleAnalyticsDimension dimension)
        {
            switch (dimension)
            {
                case GoogleAnalyticsDimension.wallpaperId:
                    return "cd1";
                case GoogleAnalyticsDimension.wallpaperTitle:
                    return "cd2";
                case GoogleAnalyticsDimension.partnerId:
                    return "cd7";
                case GoogleAnalyticsDimension.partnerName:
                    return "cd4";
                case GoogleAnalyticsDimension.channelId:
                    return "cd5";
                case GoogleAnalyticsDimension.channelTitle:
                    return "cd6";
                default:
                    throw new Exception($"Invalid Dimension: {dimension}");
            }
        }
    }
}
