using System.IO;
using System.Net;

namespace Wallcat.Util
{
    public static class DownloadFile
    {
        public static string Get(string url)
        {
            var filePath = Path.GetTempFileName();
            new WebClient().DownloadFile(url, filePath);
            return filePath;
        }
    }
}