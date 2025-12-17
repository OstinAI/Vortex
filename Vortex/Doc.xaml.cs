using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vortex;
using Vortex.Properties;

namespace test5
{
    public partial class Doc : Window
    {
        private const string APP_VERSION = "1.0.12";

        public Doc()
        {

            InitializeComponent();
            LoadVersion();


            if (!string.IsNullOrEmpty(Settings.Default.SavedCompany))
                UsernameTextBox1.Text = Settings.Default.SavedCompany;

            if (!string.IsNullOrEmpty(Settings.Default.SavedLogin))
            {
                UsernameTextBox.Text = Settings.Default.SavedLogin;
                RememberLoginCheckBox.IsChecked = true;
            }

            UsernameTextBox.TextChanged += (s, e) =>
                UsernamePlaceholder.Visibility = UsernameTextBox.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;

            PasswordBox.PasswordChanged += (s, e) =>
                PasswordPlaceholder.Visibility = PasswordBox.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;

            UsernameTextBox1.TextChanged += (s, e) =>
                UsernamePlaceholder1.Visibility = UsernameTextBox1.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;

            try
            {
                string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "8738602291808.mp4");
                BackgroundVideo.Source = new Uri(videoPath, UriKind.Absolute);
                BackgroundVideo.LoadedBehavior = MediaState.Manual;
                BackgroundVideo.UnloadedBehavior = MediaState.Manual;
                BackgroundVideo.MediaEnded += (s, e) =>
                {
                    BackgroundVideo.Position = TimeSpan.Zero;
                    BackgroundVideo.Play();
                };
                BackgroundVideo.Play();
            }
            catch
            {
                Console.WriteLine("Ошибка загрузки видео.");
            }
        }

        private void LoadVersion()
        {
            try
            {
                string version = APP_VERSION;
                txtVersion.Text = $"версия {version}";
            }
            catch
            {
                txtVersion.Text = "ошибка версии";
            }
        }

        private static string HashSHA256(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private void HighlightError(Control c)
        {
            c.BorderBrush = Brushes.Red;
            c.BorderThickness = new Thickness(2);
        }

        private void ClearHighlight(Control c)
        {
            c.ClearValue(Border.BorderBrushProperty);
            c.ClearValue(Border.BorderThicknessProperty);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string company = Normalize(UsernameTextBox1.Text);
            string username = Normalize(UsernameTextBox.Text);
            string password = Normalize(PasswordBox.Password);

            ClearHighlight(UsernameTextBox1);
            ClearHighlight(UsernameTextBox);
            ClearHighlight(PasswordBox);

            if (string.IsNullOrEmpty(company) ||
                string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Введите название компании, логин и пароль.";

                if (string.IsNullOrEmpty(company)) HighlightError(UsernameTextBox1);
                if (string.IsNullOrEmpty(username)) HighlightError(UsernameTextBox);
                if (string.IsNullOrEmpty(password)) HighlightError(PasswordBox);
                return;
            }

            string passwordHash = HashSHA256(password);

            var loginObj = new
            {
                company = company,
                username = username,
                password = passwordHash
            };

            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(8);

                    string url = ApiConfig.AuthLogin;

                    string json = JsonConvert.SerializeObject(loginObj);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await http.PostAsync(url, content);

                    if (!resp.IsSuccessStatusCode)
                    {
                        ErrorMessage.Text = "Нет связи с сервером.";
                        HighlightError(UsernameTextBox);
                        HighlightError(PasswordBox);
                        return;
                    }

                    string respJson = await resp.Content.ReadAsStringAsync();
                    var auth = JsonConvert.DeserializeObject<LoginResponse>(respJson);

                    if (auth == null || auth.Status != "ok")
                    {
                        ErrorMessage.Text = auth?.Message ?? "Неверный логин или пароль.";
                        PasswordBox.Password = "";
                        PasswordPlaceholder.Visibility = Visibility.Visible;

                        HighlightError(UsernameTextBox);
                        HighlightError(PasswordBox);
                        return;
                    }

                    GlobalSession.Token = auth.Token;
                    GlobalSession.Role = auth.Role;
                    GlobalSession.CompanyId = auth.CompanyId;
                    GlobalSession.CompanyName = company;

                    // 💾 Сохраняем токен на диск (ОБЯЗАТЕЛЬНО)
                    Settings.Default.Token = auth.Token;
                    Settings.Default.Save();

                    if (RememberLoginCheckBox.IsChecked == true)
                    {
                        Settings.Default.SavedCompany = company;
                        Settings.Default.SavedLogin = username;
                    }
                    else
                    {
                        Settings.Default.SavedCompany = "";
                        Settings.Default.SavedLogin = "";
                    }

                    Settings.Default.Save();

                    bool updating = await CheckForUpdatesFromServer();

                    if (updating)
                        return; // ⛔ НЕ открываем MainWindow

                    OpenMainWindow();

                }
            }
            catch
            {
                ErrorMessage.Text = "Сервер не отвечает.";
                HighlightError(UsernameTextBox);
                HighlightError(PasswordBox);
            }
        }

        private async Task<bool> CheckForUpdatesFromServer()
        {
            var obj = new
            {
                company = GlobalSession.CompanyName,
                current_version = APP_VERSION
            };

            using (var http = new HttpClient())
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(obj),
                    Encoding.UTF8,
                    "application/json");

                var resp = await http.PostAsync(ApiConfig.UpdateCheck, content);
                if (!resp.IsSuccessStatusCode)
                    return false;

                var result = JsonConvert.DeserializeObject<UpdateCheckResponse>(
                    await resp.Content.ReadAsStringAsync());

                if (result == null)
                    return false;

                // 🔴 ОБЯЗАТЕЛЬНОЕ ОБНОВЛЕНИЕ
                if (result.Status == "update_available" || result.Status == "update_required")
                {
                    // блокируем интерфейс
                    DisableLoginUI();

                    // показываем overlay
                    UpdateOverlay.Visibility = Visibility.Visible;
                    UpdateProgress.Visibility = Visibility.Visible;
                    UpdatePercentText.Text = "0 %";
                    UpdateTitleText.Text = "Идёт обновление…";
                    UpdateSubText.Text = "";

                    // ▶ СРАЗУ начинаем обновление
                    await DownloadAndInstallUpdate(result);

                    return true; // ⛔ дальше вход запрещён
                }
            }

            return false; // версии совпадают — можно входить
        }

        private async Task DownloadAndInstallUpdate(UpdateCheckResponse result)
        {
            try
            {
                // ===== UI: показываем экран обновления =====
                UpdateOverlay.Visibility = Visibility.Visible;
                
                LoginButton.IsEnabled = false;
                LoginButton2.IsEnabled = false;
                UsernameTextBox.IsEnabled = false;
                UsernameTextBox1.IsEnabled = false;
                PasswordBox.IsEnabled = false;
                RememberLoginCheckBox.IsEnabled = false;

                UpdateProgress.Value = 0;
                UpdatePercentText.Text = "0 %";

                // ===== скачивание =====
                string downloadUrl = ApiConfig.UpdateDownload(result.File);
                string tempZip = Path.Combine(Path.GetTempPath(), result.File);

                using (var http = new HttpClient())
                using (var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long total = response.Content.Headers.ContentLength ?? 1;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        long read = 0;
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            read += bytesRead;

                            int percent = (int)(read * 100 / total);

                            Dispatcher.Invoke(() =>
                            {
                                UpdateProgress.Value = percent;
                                UpdatePercentText.Text = percent + " %";
                            });
                        }
                    }
                }

                UpdatePercentText.Text = "Установка...";

                // ===== распаковка =====
                string tempDir = Path.Combine(Path.GetTempPath(), "VortexUpdate");
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(tempZip, tempDir);

                // ===== BAT =====
                string script = Path.Combine(tempDir, "update.bat");
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                File.WriteAllText(script,
        $@"@echo off
chcp 65001 > nul
timeout /t 2 > nul

xcopy ""{tempDir}\*"" ""{baseDir}"" /E /Y /Q

start """" ""{exePath}""

rmdir /s /q ""{tempDir}""
exit
");

                Process.Start(new ProcessStartInfo
                {
                    FileName = script,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления:\n" + ex.Message);
            }
        }

        private class LoginResponse
        {
            public string Status { get; set; }
            public string Role { get; set; }
            public string EmployeeRole { get; set; }
            public string CompanyId { get; set; }
            public string Token { get; set; }
            public string Message { get; set; }
        }

        public class UpdateCheckResponse
        {
            public string Status { get; set; }
            public string LatestVersion { get; set; }
            public string File { get; set; }
        }

        public static class GlobalSession
        {
            public static string Token { get; set; }
            public static string CompanyId { get; set; }
            public static string CompanyName { get; set; }
            public static string Role { get; set; }
        }

        private static string Normalize(string s)
        {
            return (s ?? "")
                .Replace("\uFEFF", "")
                .Replace("\u00A0", " ")
                .Trim();
        }

        private void OpenMainWindow()
        {
            MainWindow m = new MainWindow();
            m.Show();
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
                
        private void DisableLoginUI()
        {
            LoginButton.IsEnabled = false;
            LoginButton2.IsEnabled = false;
            UsernameTextBox.IsEnabled = false;
            UsernameTextBox1.IsEnabled = false;
            PasswordBox.IsEnabled = false;
            RememberLoginCheckBox.IsEnabled = false;
        }
        

    }
}
