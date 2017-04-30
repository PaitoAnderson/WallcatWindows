using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Wallcat.Util
{
    public class SetWallpaperLegacy
    {
        const uint SPI_SETDESKWALLPAPER = 0x14;
        const uint SPI_GETDESKWALLPAPER = 0x73;

        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uAction, int uParam, string lpvParam, int fuWinIni);

        public static void Apply(string tempFilePath, DesktopWallpaperPosition style)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var destWallFilePath = Path.Combine(appDataFolder + @"\Microsoft\Windows\Themes", "WallcatWallpaper.tmp");

            if (File.Exists(destWallFilePath))
                File.Delete(destWallFilePath);

            File.Move(tempFilePath, destWallFilePath);

            var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            if (style == DesktopWallpaperPosition.Fill)
            {
                key.SetValue(@"WallpaperStyle", 10.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == DesktopWallpaperPosition.Fit)
            {
                key.SetValue(@"WallpaperStyle", 6.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == DesktopWallpaperPosition.Span) // Windows 8 or newer only!
            {
                key.SetValue(@"WallpaperStyle", 22.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == DesktopWallpaperPosition.Stretch)
            {
                key.SetValue(@"WallpaperStyle", 2.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == DesktopWallpaperPosition.Tile)
            {
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 1.ToString());
            }
            if (style == DesktopWallpaperPosition.Center)
            {
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, destWallFilePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
