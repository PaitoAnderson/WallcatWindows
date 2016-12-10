using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Wallcat.Util
{
    public static class DownloadFile
    {
        public static async Task<string> Get(string url)
        {
            var filePath = Path.GetTempFileName();
            await new WebClient().DownloadFileTaskAsync(new Uri(url), filePath);
            return filePath;
        }
    }
}