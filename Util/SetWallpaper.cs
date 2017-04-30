using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Wallcat.Util
{
    public class SetWallpaper
    {
        public enum DesktopWallpaperPosition
        {
            Center = 0,
            Tile = 1,
            Stretch = 2,
            Fit = 3,
            Fill = 4,
            Span = 5,
        }

        [ComImport]
        [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetMonitorDevicePathAt(uint monitorIndex);

            uint GetMonitorDevicePathCount();

            void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);

            [return: MarshalAs(UnmanagedType.I4)]
            DesktopWallpaperPosition GetPosition();

            void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
        }


        [ComImport]
        [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
        public class DesktopWallpaperCoclass { }

        public static void Apply(uint? monitorIndex, string tempFilePath, DesktopWallpaperPosition style)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var destWallFilePath = Path.Combine(appDataFolder + @"\Microsoft\Windows\Themes", $"WallcatWallpaper{monitorIndex}.tmp");

            if (File.Exists(destWallFilePath))
                File.Delete(destWallFilePath);

            File.Move(tempFilePath, destWallFilePath);

            var dw = (IDesktopWallpaper)new DesktopWallpaperCoclass();

            // Set Wallpaper
            dw.SetWallpaper((monitorIndex != null) ? dw.GetMonitorDevicePathAt(monitorIndex.Value) : null, destWallFilePath);

            // Set Style
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
        }
    }
}
