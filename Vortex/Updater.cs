using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace Vortex
{
    public class Updater
    {
        private const string RepoApiUrl = "https://api.github.com/repos/OstinAI/Vortex/releases/latest";
        private const string TempFile = "update.zip";
        private const string VersionFile = "version.txt";

        public static void CheckForUpdates()
        {
            try
            {
                string currentVersion = File.Exists(VersionFile)
                    ? File.ReadAllText(VersionFile).Trim()
                    : "0.0.0";

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Vortex-Updater");

                    string json = client.DownloadString(RepoApiUrl);
                    var release = JObject.Parse(json);

                    string latestVersion = release["tag_name"].ToString().Trim().TrimStart('v', 'V');
                    string downloadUrl = release["assets"][0]["browser_download_url"].ToString();

                    if (IsNewer(latestVersion, currentVersion))
                    {
                        // 💬 Показываем уведомление только если есть новая версия
                        var result = MessageBox.Show(
                            $"Доступна новая версия Vortex {latestVersion}.\nОбновить сейчас?",
                            "Обновление Vortex",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                            DownloadAndInstall(downloadUrl, latestVersion);
                    }
                    // ❌ Если обновления нет — ничего не показываем
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки обновлений: {ex.Message}");
            }
        }

        private static bool IsNewer(string latest, string current)
        {
            try
            {
                latest = latest.Trim().TrimStart('v', 'V');
                current = current.Trim().TrimStart('v', 'V');
                Version v1 = new Version(latest);
                Version v2 = new Version(current);
                return v1.CompareTo(v2) > 0;
            }
            catch
            {
                return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void DownloadAndInstall(string url, string newVersion)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Vortex-Updater");
                    client.DownloadFile(url, TempFile);
                }

                string tempDir = Path.Combine(Path.GetTempPath(), "VortexUpdate");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                ZipFile.ExtractToDirectory(TempFile, tempDir);

                string script = Path.Combine(tempDir, "update.bat");
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                File.WriteAllText(script, $@"
@echo off
timeout /t 2 >nul
xcopy ""{tempDir}\*.*"" ""{baseDir}"" /s /y /i >nul
del ""{TempFile}"" >nul 2>&1
start """" ""{exePath}""
exit
");

                File.WriteAllText(Path.Combine(baseDir, VersionFile), newVersion);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{script}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка установки обновления: {ex.Message}");
            }
        }
    }
}
