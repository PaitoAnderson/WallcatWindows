using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Wallcat.Services;
using Wallcat.Util;

namespace Wallcat
{
    // TODO: Update Wallpaper Daily

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
            _iconAnimation.Start();

            // Add Menu items
            var channels = _wallcatService.GetChannels();
            _contextMenu.MenuItems.Add(new MenuItem("Featured Channels") { Enabled = false });
            _contextMenu.MenuItems.AddRange(channels.Select(channel => new MenuItem(channel.title, SelectChannel) { Tag = channel }).ToArray());
            _contextMenu.MenuItems.AddRange(new[]
            {
                new MenuItem("-") { Enabled = false },
                new MenuItem("Create Channel...", (sender, args) => Process.Start("https://wall.cat/partners")),
                new MenuItem("-") { Enabled = false },
                new MenuItem("Quit Wallcat", (sender, args) => Application.Exit())
            });

            // Set Current Image Info
            if (Properties.Settings.Default.CurrentWallpaper != null)
            {
                UpdateMenuCurrentImage(Properties.Settings.Default.CurrentWallpaper);
            }

            // Onboarding
            if (Properties.Settings.Default.CurrentChannel == null)
            {
                var channel = channels.FirstOrDefault(x => x.isDefault);
                if (channel != null)
                {
                    Properties.Settings.Default.CurrentChannel = channel;
                    Properties.Settings.Default.Save();
                    UpdateWallpaper();

                    _trayIcon.ShowBalloonTip(10 * 1000, "Welcome to Wallcat", $"Enjoy the {channel.title} channel!", ToolTipIcon.Info);
                }
            }

            _iconAnimation.Stop();
        }

        private void UpdateWallpaper()
        {
            var channel = Properties.Settings.Default.CurrentChannel;
            if (channel != null)
                SelectChannel(new MenuItem { Tag = channel }, null);
        }

        private void SelectChannel(object sender, EventArgs e)
        {
            _iconAnimation.Start();

            try
            {
                var channel = (Channel)((MenuItem)sender).Tag;
                var wallpaper = _wallcatService.GetWallpaper(channel.id);
                if (wallpaper.id == Properties.Settings.Default.CurrentWallpaper?.id) return;
                var filePath = DownloadFile.Get(wallpaper.url.o);
                SetWallpaper.Apply(filePath, SetWallpaper.Style.Span);

                // Update Settings
                Properties.Settings.Default.CurrentChannel = channel;
                Properties.Settings.Default.CurrentWallpaper = wallpaper;
                Properties.Settings.Default.LastChecked = DateTime.Now.Date;
                Properties.Settings.Default.Save();

                // Update Menu
                UpdateMenuCurrentImage(wallpaper);
            }
            finally
            {
                _iconAnimation.Stop();
            }
        }

        private void UpdateMenuCurrentImage(Wallpaper wallpaper)
        {
            const string tag = "CurrentImage";
            const string campaign = "?utm_source=windows&utm_medium=menuItem&utm_campaign=wallcat";

            // Remove previous Current Image (If Any)
            for (var i = _contextMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                var s = _contextMenu.MenuItems[i].Tag as string;
                if (s != null && s == tag)
                {
                    _contextMenu.MenuItems.Remove(_contextMenu.MenuItems[i]);
                }
            }

            // Add Current Image
            _contextMenu.MenuItems.Add(0, new MenuItem("Current Image") { Enabled = false, Tag = tag });
            _contextMenu.MenuItems.Add(1, new MenuItem($"{wallpaper.title} by {wallpaper.partner.first} {wallpaper.partner.last}",
                (sender, args) => Process.Start(wallpaper.sourceUrl + campaign))
            { Tag = tag });
            _contextMenu.MenuItems.Add(2, new MenuItem("-") { Enabled = false, Tag = tag });
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;
        }
    }
}
