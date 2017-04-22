using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Wallcat.Services;
using Wallcat.Util;

namespace Wallcat
{
    // TODO: Multi Monitor Support

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

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.Run(new MyCustomApplicationContext());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
#if DEBUG
            MessageBox.Show(e.Exception.ToString(), "Thread Exception!");
#endif
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            MessageBox.Show((e.ExceptionObject as Exception).ToString(), "Unhandled Exception!");
#endif
        }
    }

    public class MyCustomApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenu _contextMenu;
        private readonly IconAnimation _iconAnimation;
        private readonly WallcatService _wallcatService;
        private System.Threading.Timer _timer;

        public MyCustomApplicationContext()
        {
            Application.ApplicationExit += OnApplicationExit;
            SystemEvents.PowerModeChanged += OnPowerChange;

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
            var channels = _wallcatService.GetChannels().Result;
            _contextMenu.MenuItems.Add(new MenuItem("Featured Channels") { Enabled = false });
            _contextMenu.MenuItems.AddRange(channels.Select(channel => new MenuItem(channel.title, SelectChannel) { Tag = channel, Checked = IsCurrentChannel(channel) }).ToArray());
            _contextMenu.MenuItems.AddRange(new[]
            {
                new MenuItem("-") { Enabled = false },
                new MenuItem("Create Channel...", (sender, args) => Process.Start("https://beta.wall.cat/partners")),
                new MenuItem("Start at login", (sender, args) => CreateStartupShortcut()) { Checked = IsEnabledAtStartup() },
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

                    _trayIcon.ShowBalloonTip(10 * 1000, "Welcome to Wallcat", $"Enjoy the {channel.title} channel!", ToolTipIcon.Info);
                }
            }

            UpdateWallpaper();
            MidnightUpdate();

            _iconAnimation.Stop();
        }

        private void UpdateWallpaper()
        {
            if (Properties.Settings.Default.LastChecked != DateTime.Now.Date)
            {
                var channel = Properties.Settings.Default.CurrentChannel;
                if (channel != null)
                    SelectChannel(new MenuItem { Tag = channel }, null);
            }
        }

        private async void SelectChannel(object sender, EventArgs e)
        {
            _iconAnimation.Start();

            try
            {
                var channel = (Channel)((MenuItem)sender).Tag;
                var wallpaper = await _wallcatService.GetWallpaper(channel.id);
                if (wallpaper.id == Properties.Settings.Default.CurrentWallpaper?.id) return;
                var filePath = await DownloadFile.Get(wallpaper.url.o);
                var style = Environment.OSVersion.Version.Major >= 8 ? SetWallpaper.Style.Span : SetWallpaper.Style.Fill;
                SetWallpaper.Apply(filePath, style);

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
            
            for (var i = _contextMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                // Remove previous Current Image (If Any)
                if (_contextMenu.MenuItems[i].Tag is string s)
                {
                    if (s == tag)
                    {
                        _contextMenu.MenuItems.Remove(_contextMenu.MenuItems[i]);
                    }
                }

                // Update Checkmark
                if (_contextMenu.MenuItems[i].Tag is Channel c)
                {
                    _contextMenu.MenuItems[i].Checked = false;
                    if (c.id == wallpaper.channel.id)
                    {
                        _contextMenu.MenuItems[i].Checked = true;
                    }
                }
            }

            // Add Current Image
            _contextMenu.MenuItems.Add(0, new MenuItem("Current Image") { Enabled = false, Tag = tag });
            _contextMenu.MenuItems.Add(1, new MenuItem($"{wallpaper.title} by {wallpaper.partner.first} {wallpaper.partner.last}",
                (sender, args) => Process.Start(wallpaper.sourceUrl + campaign))
            { Tag = tag });
            _contextMenu.MenuItems.Add(2, new MenuItem("-") { Enabled = false, Tag = tag });
        }

        private void MidnightUpdate()
        {
            var updateTime = new TimeSpan(24, 1, 0) - DateTime.Now.TimeOfDay;
            _timer = new System.Threading.Timer(x =>
            {
                UpdateWallpaper();
                MidnightUpdate();
            }, null, updateTime, Timeout.InfiniteTimeSpan);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;
        }

        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Retry.Do(() => UpdateWallpaper(), TimeSpan.FromSeconds(15));
                    MidnightUpdate();
                    break;
                case PowerModes.Suspend:
                    _timer.Dispose();
                    break;
            }
        }

        private void CreateStartupShortcut()
        {
            string pathToExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string pathToShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Wallcat.lnk");

            if (IsEnabledAtStartup())
            {
                System.IO.File.Delete(pathToShortcut);
            }
            else
            {
                var shortcut = (IWshShortcut)new WshShell().CreateShortcut(pathToShortcut);

                shortcut.Description = "Enjoy a new, beautiful wallpaper, every day.";
                shortcut.TargetPath = pathToExe;
                shortcut.Save();
            }

            foreach (MenuItem menuItem in _contextMenu.MenuItems)
            {
                if (menuItem.Text == "Start at login")
                {
                    menuItem.Checked = IsEnabledAtStartup();
                    break;
                }
            }
        }

        private static bool IsEnabledAtStartup()
        {
            return System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Wallcat.lnk"));
        }

        private static bool IsCurrentChannel(Channel channel)
        {
            var currentWallpaper = Properties.Settings.Default.CurrentWallpaper;
            if (currentWallpaper != null)
            {
                return channel.id == currentWallpaper.channel.id;
            }
            else
            {
                return channel.isDefault;
            }
        }
    }
}
