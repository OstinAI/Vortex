using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;
using Vortex;
using Vortex.Properties;

namespace test5
{
    public partial class Doc : Window
    {
        public Doc()
        {
            InitializeComponent();

            // 🟢 Проверка обновлений при запуске программы
            try
            {
                Vortex.Updater.CheckForUpdates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки обновлений: {ex.Message}");
            }

            // Загрузить версию программы
            LoadVersion();

            // 🟢 Автозаполнение
            if (!string.IsNullOrEmpty(Settings.Default.SavedCompany))
                UsernameTextBox1.Text = Settings.Default.SavedCompany;

            if (!string.IsNullOrEmpty(Settings.Default.SavedLogin))
            {
                UsernameTextBox.Text = Settings.Default.SavedLogin;
                RememberLoginCheckBox.IsChecked = true;
            }

            // 🟢 Подсказки
            UsernameTextBox.TextChanged += (s, e) =>
                UsernamePlaceholder.Visibility = UsernameTextBox.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            PasswordBox.PasswordChanged += (s, e) =>
                PasswordPlaceholder.Visibility = PasswordBox.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            UsernameTextBox1.TextChanged += (s, e) =>
                UsernamePlaceholder1.Visibility = UsernameTextBox1.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;

            // 🟢 Фоновое видео
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
                Console.WriteLine("⚠️ Ошибка загрузки видео.");
            }
        }



        private void LoadVersion()
        {
            try
            {
                string versionPath = "version.txt";
                if (File.Exists(versionPath))
                {
                    string version = File.ReadAllText(versionPath).Trim();
                    txtVersion.Text = $"версия {version}";
                }
                else
                {
                    txtVersion.Text = "версия неизвестна";
                }
            }
            catch
            {
                txtVersion.Text = "ошибка версии";
            }
        }

        // 🟢 Кнопка "Вход"
        // 🟢 Кнопка "Вход"
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string company = Normalize(UsernameTextBox1.Text);
            string username = Normalize(UsernameTextBox.Text);
            string password = Normalize(PasswordBox.Password);

            if (string.IsNullOrEmpty(company) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Введите название компании, логин и пароль.";
                return;
            }

            string clientSheetUrl = Settings.Default.ClientSheetUrl;
            string savedCompany = Settings.Default.SavedCompany;

            // Если компания изменилась — сбросить старую ссылку
            if (savedCompany != company)
                clientSheetUrl = null;

            // 🔹 Если ссылка отсутствует — пробуем получить её из дата-центра по названию компании
            if (string.IsNullOrEmpty(clientSheetUrl))
            {
                try
                {
                    using (var http = new HttpClient())
                    {
                        string csvUrl =
                            "https://docs.google.com/spreadsheets/d/14IMOT9VgUNkvbYiRxaY8t1ZGUYWCb-VspQdCT9IS2YI/gviz/tq?tqx=out:csv&sheet=Пользователь";
                        string csv = await http.GetStringAsync(csvUrl);
                        csv = Normalize(csv);

                        var rows = ParseCsv(csv);
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var f = rows[i];
                            if (f.Length < 5) continue;

                            string sheetCompany = Normalize(f[3].Replace("\"", ""));
                            string dbLink = Normalize(f[4].Replace("\"", ""));

                            if (sheetCompany.Equals(company, StringComparison.OrdinalIgnoreCase))
                            {
                                clientSheetUrl = dbLink;
                                Settings.Default.SavedCompany = company;
                                Settings.Default.ClientSheetUrl = dbLink;
                                Settings.Default.Save();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при доступе к дата-центру:\n" + ex.Message);
                }
            }

            // 🔹 Проверяем сотрудника (если есть ссылка)
            bool employeeOk = false;
            if (!string.IsNullOrEmpty(clientSheetUrl))
                employeeOk = await AuthenticateEmployee(username, password, clientSheetUrl);

            // 🔹 Если не найден в клиентской таблице — проверяем логин/пароль интегратора
            if (!employeeOk)
            {
                (bool isIntegrator, string link) = await TryGetLinkFromDataCenter(company, username, password);
                if (isIntegrator)
                {
                    Settings.Default.SavedCompany = company;
                    Settings.Default.ClientSheetUrl = link;
                    Settings.Default.SavedLogin = username;
                    Settings.Default.SavedPassword = password;
                    Settings.Default.Save();

                    OpenMainWindow();
                    return;
                }
                else if (!string.IsNullOrEmpty(link))
                {
                    // нашли компанию клиента, но не сотрудника
                    Settings.Default.SavedCompany = company;
                    Settings.Default.ClientSheetUrl = link;
                    Settings.Default.Save();
                    clientSheetUrl = link;

                    // ещё раз проверяем сотрудника после обновления ссылки
                    employeeOk = await AuthenticateEmployee(username, password, clientSheetUrl);
                }
            }

            if (employeeOk)
            {
                Settings.Default.SavedLogin = username;
                Settings.Default.SavedPassword = password;
                Settings.Default.Save();

                OpenMainWindow();
            }
            else
            {
                ErrorMessage.Text = "Неверный логин или пароль.";
            }
        }


        // 🔹 Проверка интегратора и получение ссылки компании
        private async Task<(bool isIntegrator, string link)> TryGetLinkFromDataCenter(string company, string username, string password)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    string csvUrl =
                        "https://docs.google.com/spreadsheets/d/14IMOT9VgUNkvbYiRxaY8t1ZGUYWCb-VspQdCT9IS2YI/gviz/tq?tqx=out:csv&sheet=Пользователь";
                    string csv = await http.GetStringAsync(csvUrl);
                    csv = Normalize(csv);

                    var rows = ParseCsv(csv);
                    for (int i = 1; i < rows.Count; i++)
                    {
                        var f = rows[i];
                        if (f.Length < 5) continue;

                        string sheetLogin = Normalize(f[0].Replace("\"", ""));
                        string sheetPassword = Normalize(f[1].Replace("\"", ""));
                        string sheetAccess = Normalize(f[2].Replace("\"", ""));
                        string sheetCompany = Normalize(f[3].Replace("\"", ""));
                        string dbLink = Normalize(f[4].Replace("\"", ""));

                        if (sheetLogin == username && sheetPassword == password &&
                            sheetCompany.Equals(company, StringComparison.OrdinalIgnoreCase))
                        {
                            if (sheetAccess.Contains("Админ") || sheetAccess.Contains("Интегратор"))
                                return (true, dbLink);
                            else
                                return (false, dbLink);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при доступе к дата-центру:\n" + ex.Message);
            }
            return (false, null);
        }

        // 🔹 Проверка логина и пароля сотрудника в таблице клиента
        private async Task<bool> AuthenticateEmployee(string username, string password, string clientSheetUrl)
        {
            try
            {
                string id = ExtractSpreadsheetId(clientSheetUrl);
                if (string.IsNullOrEmpty(id)) return false;

                string sheetName = "Сотрудники";
                string encoded = Uri.EscapeDataString(sheetName);
                string csvUrl =
                    $"https://docs.google.com/spreadsheets/d/{id}/gviz/tq?tqx=out:csv&sheet={encoded}";

                using (var http = new HttpClient())
                {
                    string csv = await http.GetStringAsync(csvUrl);
                    csv = Normalize(csv);

                    var rows = ParseCsv(csv);
                    if (rows.Count <= 1) return false;

                    // колонки: 0-число, 1-ФИО, 2-должность, 3-логин, 4-пароль
                    for (int i = 1; i < rows.Count; i++)
                    {
                        var f = rows[i];
                        if (f.Length < 5) continue;

                        string login = Normalize(f[3].Replace("\"", ""));
                        string pass = Normalize(f[4].Replace("\"", ""));

                        if (login == username && pass == password)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // 🧩 Вспомогательные методы
        private static string Normalize(string s)
        {
            return (s ?? "")
                .Replace("\uFEFF", "")
                .Replace("\u00A0", " ")
                .Trim();
        }

        private static List<string[]> ParseCsv(string csv)
        {
            var list = new List<string[]>();
            using (var reader = new StringReader(csv))
            using (var parser = new TextFieldParser(reader))
            {
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields != null)
                        list.Add(fields);
                }
            }
            return list;
        }

        private static string ExtractSpreadsheetId(string url)
        {
            var m = Regex.Match(url, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
            return m.Success ? m.Groups[1].Value : null;
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
    }
}
