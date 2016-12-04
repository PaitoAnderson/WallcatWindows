using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Wallcat.Util
{
    public class SetWallpaper
    {
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public enum Style
        {
            Fill,
            Fit,
            Span,
            Stretch,
            Tile,
            Center
        }

        public static bool Apply(string tempFilePath, Style style)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var destWallFilePath = Path.Combine(appDataFolder + @"\Microsoft\Windows\Themes", "WallcatWallpaper.tmp");

            try
            {
                if (File.Exists(destWallFilePath))
                    File.Delete(destWallFilePath);

                File.Move(tempFilePath, destWallFilePath);

                var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

                if (style == Style.Fill)
                {
                    key.SetValue(@"WallpaperStyle", 10.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }
                if (style == Style.Fit)
                {
                    key.SetValue(@"WallpaperStyle", 6.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }
                if (style == Style.Span) // Windows 8 or newer only!
                {
                    key.SetValue(@"WallpaperStyle", 22.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }
                if (style == Style.Stretch)
                {
                    key.SetValue(@"WallpaperStyle", 2.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }
                if (style == Style.Tile)
                {
                    key.SetValue(@"WallpaperStyle", 0.ToString());
                    key.SetValue(@"TileWallpaper", 1.ToString());
                }
                if (style == Style.Center)
                {
                    key.SetValue(@"WallpaperStyle", 0.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, destWallFilePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
