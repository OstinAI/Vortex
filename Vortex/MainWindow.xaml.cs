using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;
using System.Text;


namespace Vortex
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _weatherTimer;
        private TextBlock _txtInButton;
        private TextBlock _txtPogoda;
        private TextBlock _txtSotr; // 🟢 добавляем (если захочешь кэшировать ссылку)

        public MainWindow()
        {
            InitializeComponent();

            // 🟢 Сотрудники
            sotr.ApplyTemplate();
            _txtSotr = sotr.Template.FindName("txtpsotr", sotr) as TextBlock;

            // Если шаблон кнопки ещё не прогрузился — дождёмся Loaded
            if (_txtSotr == null)
                sotr.Loaded += Sotr_Loaded;
            else
                LoadSotrudnikiCount();

            // 🟢 Погода
            pogoda.ApplyTemplate();
            _txtPogoda = pogoda.Template.FindName("txtpogoda", pogoda) as TextBlock;

            if (_txtPogoda == null)
                pogoda.Loaded += Pogoda_Loaded;

            LoadWeather();

            _weatherTimer = new DispatcherTimer();
            _weatherTimer.Interval = TimeSpan.FromMinutes(10);
            _weatherTimer.Tick += (s, e) => LoadWeather();
            _weatherTimer.Start();

            // 🕒 Календарь
            btnCalendar.ApplyTemplate();
            _txtInButton = btnCalendar.Template.FindName("txtDateTime", btnCalendar) as TextBlock;
            if (_txtInButton == null)
                btnCalendar.Loaded += BtnCalendar_Loaded;

            StartClock();
        }


        // 🕒 Видео фон
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "8738602291808.mp4");

            if (!File.Exists(videoPath))
            {
                MessageBox.Show("Видео не найдено: " + videoPath);
                return;
            }

            try
            {
                BackgroundVideo.Source = new Uri(videoPath, UriKind.Absolute);
                BackgroundVideo.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка воспроизведения видео: " + ex.Message);
            }
        }

        // 🕒 Видео фон
        private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            BackgroundVideo.Position = TimeSpan.Zero;
            BackgroundVideo.Play();
        }

        // 🕒 Видео фон
        private void BackgroundVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Ошибка загрузки видео: " + e.ErrorException.Message);
        }

        // 🚪 Кнопка выхода из программы
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 🟢 При наведении — запустить анимацию
        private void Exit_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn == null) return;

                var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                if (gifImage == null) return;

                var controller = ImageBehavior.GetAnimationController(gifImage);
                if (controller != null)
                {
                    controller.GotoFrame(0); // начать с первого кадра
                    controller.Play();        // запустить анимацию
                }
                else
                {
                    // Если контроллер не найден, принудительно загрузим гифку
                    var animatedImage = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/import.gif"));
                    ImageBehavior.SetAnimatedSource(gifImage, animatedImage);
                    ImageBehavior.SetAutoStart(gifImage, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при запуске гифки: " + ex.Message);
            }
        }

        // 🔴 При уходе — остановить и вернуть первый кадр
        private void Exit_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn == null) return;

                var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                if (gifImage == null) return;

                var controller = ImageBehavior.GetAnimationController(gifImage);
                if (controller != null)
                {
                    controller.Pause();
                    controller.GotoFrame(0); // вернуть в исходное положение
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при остановке гифки: " + ex.Message);
            }
        }

        // 🔄 Переключатель режима окна (F11)
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // Переключаем в обычное окно (тестовый адаптивный режим)
                    this.WindowState = WindowState.Normal;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.Width = 1280;
                    this.Height = 720;
                }
                else
                {
                    // Возвращаем полноэкранный режим
                    this.WindowState = WindowState.Maximized;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowStyle = WindowStyle.None;
                }
            }
        }

        // Обработчик для кнопки "Свернуть"
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 🟢 Верхний лейбл При наведении — просто перезапустить гифку
        private void Ex_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                    if (gifImage == null) return;

                    var controller = ImageBehavior.GetAnimationController(gifImage);
                    if (controller != null)
                    {
                        controller.GotoFrame(0); // с первого кадра
                        controller.Play();        // запустить снова
                    }
                    else
                    {
                        // если не найден контроллер — создаём гиф заново
                        var animatedImage = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/import.gif"));
                        ImageBehavior.SetAnimatedSource(gifImage, animatedImage);
                        ImageBehavior.SetAutoStart(gifImage, true);
                        ImageBehavior.SetRepeatBehavior(gifImage, System.Windows.Media.Animation.RepeatBehavior.Forever);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при наведении: " + ex.Message);
            }
        }

        // 🔴 Верхний лейбл При уходе — ничего не останавливаем (пусть гиф крутится вечно)
        private void Ex_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                    if (gifImage == null) return;

                    var controller = ImageBehavior.GetAnimationController(gifImage);
                    if (controller != null)
                    {
                        // просто продолжаем крутить — без Pause
                        controller.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при уходе курсора: " + ex.Message);
            }
        }

        // Календарь
        // 🕒 Автообновление даты и времени на кнопке
        private void BtnCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            btnCalendar.ApplyTemplate();
            _txtInButton = btnCalendar.Template.FindName("txtDateTime", btnCalendar) as TextBlock;
            UpdateNow(); // Обновим сразу, как нашли
        }

        // 🕒 Автообновление даты и времени на кнопке
        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateNow();
            _clockTimer.Start();
            UpdateNow(); // моментальный первый вывод
        }

        // 🕒 Автообновление даты и времени на кнопке
        private void UpdateNow()
        {
            // Формат: "Ср, 29.10.2025\n20:43:12"
            var culture = new CultureInfo("ru-RU");
            string day = DateTime.Now.ToString("ddd", culture);
            // Приведём к "Ср" с заглавной буквы
            day = culture.TextInfo.ToTitleCase(day);

            string date = DateTime.Now.ToString("dd.MM.yyyy", culture);
            string time = DateTime.Now.ToString("HH:mm:ss", culture);

            if (_txtInButton != null)
                _txtInButton.Text = $"{day}, {date}\n          {time}";
        }

        // Оставляем обработчики, как ты просил, ничего не делаем
        private void Ex_Calendar(object sender, RoutedEventArgs e) { }

        // 🟢 Функция загрузки погоды из Google Sheets
        private void Pogoda_Loaded(object sender, RoutedEventArgs e)
        {
            _txtPogoda = pogoda.Template.FindName("txtpogoda", pogoda) as TextBlock;
            LoadWeather();
        }

        // 🟢 Погода
        private async void LoadWeather()
        {
            try
            {
                string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTVN1kC-5uPhdBGi5l9NGQcgsAzw8jd5_MvxsHb_s3H4YyBjxQ6QZon1sdEqXOktJHXBOork-lw0amD/pub?gid=1861136659&single=true&output=csv";

                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;

                    // 👇 Асинхронное ожидание (не блокирует интерфейс)
                    string csv = await client.DownloadStringTaskAsync(url);

                    string[] lines = csv.Split('\n');

                    string city = "";
                    string temperature = "";

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(',');
                        if (parts.Length < 2) continue;

                        string key = parts[0].Trim().Replace("\"", "");
                        string value = parts[1].Trim().Replace("\"", "");

                        if (key == "Город")
                            city = value;
                        else if (key == "Температура сейчас")
                            temperature = value;
                    }

                    if (_txtPogoda != null)
                    {
                        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(temperature))
                            _txtPogoda.Text = $"{city}\n{temperature}";
                        else
                            _txtPogoda.Text = "Нет данных";
                    }
                }
            }
            catch (Exception ex)
            {
                if (_txtPogoda != null)
                    _txtPogoda.Text = "Ошибка";
                Console.WriteLine("Ошибка погоды: " + ex.Message);
            }
        }

        // 🟢 Кнопка погода нажать
        private void Ex_pogoda(object sender, RoutedEventArgs e)
        {
            LoadWeather();
        }

        // 🟢 Когда кнопка "Сотрудники" готова — ищем текст и обновляем
        private void Sotr_Loaded(object sender, RoutedEventArgs e)
        {
            _txtSotr = sotr.Template.FindName("txtpsotr", sotr) as TextBlock;
            LoadSotrudnikiCount();
        }

        // 🧭 Загрузка данных о количестве сотрудников (без зависаний)
        private async void LoadSotrudnikiCount()
        {
            try
            {
                // ⚙️ ссылка на твой лист "Сотрудники"
                string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQK1ZKl5ICKREKat0WHoyLBz-HW6pZubzWQcRZGTNUclyt-RCeW-bnCD0btiDzjPC6Kp57AyiOQsPfV/pub?output=csv&gid=0\r\n";

                if (_txtSotr == null)
                    _txtSotr = sotr.Template.FindName("txtpsotr", sotr) as TextBlock;

                if (_txtSotr != null)
                    _txtSotr.Text = "загрузка...";

                using (var client = new WebClient())
                {
                    // 💤 скачиваем асинхронно, чтобы не зависал UI
                    string csvData = await client.DownloadStringTaskAsync(new Uri(url));
                    string[] lines = csvData.Split('\n');

                    if (lines.Length > 1)
                    {
                        string a2 = lines[1].Split(',')[0].Trim();
                        if (_txtSotr != null)
                            _txtSotr.Text = string.IsNullOrEmpty(a2) ? "0" : a2;
                    }
                    else if (_txtSotr != null)
                    {
                        _txtSotr.Text = "нет данных";
                    }
                }
            }
            catch
            {
                if (_txtSotr != null)
                    _txtSotr.Text = "ошибка";
            }
        }

        // 🟢 Обработка нажатия на кнопку "Сотрудники"
        private void Ex_sotr(object sender, RoutedEventArgs e)
        {
            LoadSotrudnikiCount();
        }
    }


}
