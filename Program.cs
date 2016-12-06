using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Wallcat.Services;
using Wallcat.Util;

namespace Wallcat
{
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
#if DEBUG
            Console.WriteLine($@"Current Channel: {Properties.Settings.Default.CurrentChannel}");
            Console.WriteLine($@"Current Wallpaper: {Properties.Settings.Default.CurrentWallpaper}");
            Console.WriteLine($@"Last Checked: {Properties.Settings.Default.LastChecked}");
            Console.WriteLine($@"Storage: {ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath}");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }
    }

    public class MyCustomApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly WallcatService _wallcatService;

        public MyCustomApplicationContext()
        {
            _wallcatService = new WallcatService();

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(new MenuItem("Channels") { Enabled = false });
            var channels = _wallcatService.GetChannels();
            foreach (var channel in channels)
            {
                contextMenu.MenuItems.Add(new MenuItem(channel.title, SelectChannel) { Tag = channel.id });
            }

            contextMenu.MenuItems.Add(new MenuItem("-") { Enabled = false });
            contextMenu.MenuItems.Add(new MenuItem("Credit", Website));
            contextMenu.MenuItems.Add(new MenuItem("Exit", Exit));

            // Set Current Channel
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.CurrentChannel))
            {
                Properties.Settings.Default.CurrentChannel = channels.FirstOrDefault(x => x.isDefault)?.id;
                Properties.Settings.Default.Save();
            }
            UpdateWallpaper();

            // Initialize Tray Icon
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenu = contextMenu,
                Visible = true
            };
        }

        private void UpdateWallpaper()
        {
            if (Properties.Settings.Default.LastChecked != DateTime.Now.Date)
            {
                var channel = Properties.Settings.Default.CurrentChannel;
                if (string.IsNullOrWhiteSpace(channel) == false)
                    SelectChannel(new MenuItem { Tag = channel }, null);
            }
        }


        private void SelectChannel(object sender, EventArgs e)
        {
            var channel = (string)((MenuItem)sender).Tag;
            var wallpaper = _wallcatService.GetWallpaper(channel);
            if (wallpaper.id == Properties.Settings.Default.CurrentWallpaper) return;
            var filePath = DownloadFile.Get(wallpaper.url.o);
            SetWallpaper.Apply(filePath, SetWallpaper.Style.Span);

            // Update Settings
            Properties.Settings.Default.CurrentChannel = channel;
            Properties.Settings.Default.CurrentWallpaper = wallpaper.id;
            Properties.Settings.Default.LastChecked = DateTime.Now.Date;
            Properties.Settings.Default.Save();
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
