using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion("1.0.*")]
namespace SpotifyControlCli
{
    public static class Program
    {
        private const int WM_APPCOMMAND = 0x0319;

        private static readonly IntPtr SPOTIFY_APPCOMMAND_NEXT = (IntPtr)720896;
        private static readonly IntPtr SPOTIFY_APPCOMMAND_PLAY_PAUSE = (IntPtr)917504;
        private static readonly IntPtr SPOTIFY_APPCOMMAND_PREVIOUS = (IntPtr)786432;
        private static readonly IntPtr SPOTIFY_APPCOMMAND_STOP = (IntPtr)851968;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Spotify process.
        /// </summary>
        private static Process? SpotifyProcess { get; set; }

        /// <summary>
        /// Init all the things..
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static void Main(string[] args)
        {
            // Do we need to show app options?
            if (args == null ||
                args.Length == 0 ||
                args.Length > 1)
            {
                ShowAppOptions();
                return;
            }

            // Find, or start, Spotify.
            try
            {
                SpotifyProcess = GetSpotifyProcess() ??
                                 StartNewSpotifyInstance();

                if (SpotifyProcess == null)
                {
                    throw new Exception("Unable to find or create a new instance of Spotify.");
                }
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return;
            }

            // Attempt to perform the argument as command.
            try
            {
                PerformAppCommand(args[0]);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
            }
        }

        /// <summary>
        /// Get the Spotify process.
        /// </summary>
        /// <returns>Spotify process.</returns>
        private static Process? GetSpotifyProcess() =>
            Process.GetProcessesByName("spotify")
                .FirstOrDefault();

        /// <summary>
        /// Get the assembly version.
        /// </summary>
        /// <returns>Version.</returns>
        private static string GetVersion()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString()
                   ?? "0.1";
        }

        /// <summary>
        /// Perform the Spotify command.
        /// </summary>
        /// <param name="arg">Command-line argument.</param>
        private static void PerformAppCommand(
            string arg)
        {
            if (SpotifyProcess == null)
            {
                return;
            }

            var command = arg switch
            {
                "pp" => SPOTIFY_APPCOMMAND_PLAY_PAUSE,
                "p" => SPOTIFY_APPCOMMAND_PREVIOUS,
                "n" => SPOTIFY_APPCOMMAND_NEXT,
                "s" => SPOTIFY_APPCOMMAND_STOP,
                _ => throw new Exception($"Unknown command: {arg}"),
            };

            PostMessage(
                SpotifyProcess.MainWindowHandle,
                WM_APPCOMMAND,
                (IntPtr)0,
                command);
        }

        /// <summary>
        /// Show the various app options.
        /// </summary>
        private static void ShowAppOptions()
        {
            ConsoleEx.WriteObjects(
                "Spotify Control v",
                GetVersion(),
                Environment.NewLine,
                Environment.NewLine,
                "Usage:",
                Environment.NewLine,
                "  sc [",
                ConsoleColor.Blue,
                "command",
                (byte)0x00,
                "]",
                Environment.NewLine,
                Environment.NewLine,
                "Commands:",
                Environment.NewLine);

            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  pp  ",
                (byte)0x00,
                "Play/pause.",
                Environment.NewLine);

            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  p   ",
                (byte)0x00,
                "Previous track.",
                Environment.NewLine);

            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  n   ",
                (byte)0x00,
                "Next track.",
                Environment.NewLine);

            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  s   ",
                (byte)0x00,
                "Stop playing.",
                Environment.NewLine);
        }

        /// <summary>
        /// Attempt to locate Spotify and start a new instance of it.
        /// </summary>
        /// <returns>Spotify process.</returns>
        private static Process? StartNewSpotifyInstance()
        {
            ConsoleEx.WriteObjects(
                "Spotify process not found.",
                Environment.NewLine,
                "Looking for executable to start new instance..",
                Environment.NewLine);

            var folders = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            folders.AddRange(
                DriveInfo.GetDrives()
                    .Where(n => n.IsReady)
                    .Select(n => n.RootDirectory.FullName)
                    .ToList());

            var index = -1;

            string? path = null;

            while (true)
            {
                index++;

                if (index == folders.Count)
                {
                    break;
                }

                try
                {
                    var files = Directory.GetFiles(
                        folders[index],
                        "spotify.exe",
                        SearchOption.TopDirectoryOnly);

                    if (files.Any())
                    {
                        path = files[0];
                        break;
                    }
                }
                catch
                {
                    //
                }

                try
                {
                    folders.AddRange(
                        Directory.GetDirectories(
                            folders[index],
                            "*",
                            SearchOption.TopDirectoryOnly));
                }
                catch
                {
                    //
                }
            }

            if (path == null)
            {
                return null;
            }

            var startInfo = new ProcessStartInfo(path);
            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            Thread.Sleep(3000);

            return process;
        }
    }
}