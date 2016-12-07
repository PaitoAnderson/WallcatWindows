using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Wallcat.Services;
using Wallcat.Util;

namespace Wallcat
{
    // TODO: Update Wallpaper Daily
    // TODO: Display info about the wallpaper

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
            Console.WriteLine($@"Storage: {System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath}");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }
    }

    public class MyCustomApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenu _contextMenu;
        private readonly IconAnimation _iconAnimation;
        private readonly WallcatService _wallcatService;

        public MyCustomApplicationContext()
        {
            Application.ApplicationExit += OnApplicationExit;

            _wallcatService = new WallcatService();
            _contextMenu = new ContextMenu();
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenu = _contextMenu,
                Visible = true
            };
            _iconAnimation = new IconAnimation(ref _trayIcon);

            // Add Menu Items
            _iconAnimation.Start();
            var channels = _wallcatService.GetChannels();
            _contextMenu.MenuItems.Add("Channels", channels.Select(x => new MenuItem(x.title, SelectChannel) { Tag = x.id }).ToArray());
            _contextMenu.MenuItems.AddRange(new[]
            {
                new MenuItem("Credit", (sender, args) => Process.Start("https://wall.cat/")),
                new MenuItem("Exit", (sender, args) => Application.Exit())
            });

            // Set Default Channel
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.CurrentChannel))
            {
                var channel = channels.FirstOrDefault(x => x.isDefault);
                if (channel != null)
                {
                    Properties.Settings.Default.CurrentChannel = channel.id;
                    Properties.Settings.Default.Save();
                    UpdateWallpaper();

                    _trayIcon.ShowBalloonTip(10 * 1000, "Welcome to Wallcat", $"Enjoy the {channel.title} channel!", ToolTipIcon.Info);
                }
            }
            _iconAnimation.Stop();
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
            _iconAnimation.Start();

            try
            {
                var channel = (string) ((MenuItem) sender).Tag;
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
            finally
            {
                _iconAnimation.Stop();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;
        }
    }
}
