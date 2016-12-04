using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Wallcat.Services;
using Wallcat.Util;

namespace Wallcat
{
    // TODO: Remember Channel
    // TODO: Update Wallpaper Daily
    // TODO: Make cat blink when busy

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }
    }

    public class MyCustomApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly WallcatService _wallcatService;

        private Channel CurrentChannel { get; set; }
        private Wallpaper CurrentWallpaper { get; set; }

        public MyCustomApplicationContext()
        {
            _wallcatService = new WallcatService();
            
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(new MenuItem("Channels") { Enabled = false });
            var channels = _wallcatService.GetChannels();
            foreach (var channel in channels)
            {
                contextMenu.MenuItems.Add(new MenuItem(channel.title, SelectChannel) { Tag = channel });
            }
            SelectChannel(new MenuItem { Tag = channels.FirstOrDefault(x => x.isDefault)}, null);
            contextMenu.MenuItems.Add(new MenuItem("-") { Enabled = false });
            contextMenu.MenuItems.Add(new MenuItem("Credit", Website));
            contextMenu.MenuItems.Add(new MenuItem("Exit", Exit));

            // Initialize Tray Icon
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenu = contextMenu,
                Visible = true
            };
        }

        private void SelectChannel(object sender, EventArgs e)
        {
            var channel = (Channel)((MenuItem)sender).Tag;
            if (channel == null) return;
            var wallpaper = _wallcatService.GetWallpaper(channel.id);
            if (wallpaper.id == CurrentWallpaper?.id) return;
            var filePath = DownloadFile.Get(wallpaper.url.o);
            SetWallpaper.Apply(filePath, SetWallpaper.Style.Span);

            CurrentChannel = channel;
            CurrentWallpaper = wallpaper;
        }

        private static void Website(object sender, EventArgs e)
        {
            Process.Start("https://wall.cat/");
        }

        private void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
