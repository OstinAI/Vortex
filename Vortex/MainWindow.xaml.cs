using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;


namespace Vortex
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _clockTimer;
        private TextBlock _txtInButton;
        private EmployeesWindow employeesWindow;
        public WindowManager manager;
        private bool _toolsOpened;
        private Button[] _toolsButtons;
        private Popup _hoverPopup;
        private TextBlock _hoverText;
        private DispatcherTimer _videoTimer;
        private DispatcherTimer _hoverAutoHideTimer;
        private Tools toolsWindow;


        public MainWindow()
        {
            InitializeComponent();


            manager = new WindowManager(this);
            manager.AttachGlobalHotkeys(this);
            this.Tag = "RunTime";


            StartClock();
        }

        // 🔸 Обработчик события загрузки окна
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitHoverText();

            InitToolsSimple();

            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "Video", "8738602291808.mp4");

            if (!File.Exists(path))
            {
                MessageBox.Show("Видео не найдено");
                return;
            }

            BgPlayer.Source = new Uri(path, UriKind.Absolute);
            BgPlayer.MediaOpened += BgPlayer_MediaOpened;
            BgPlayer.Play();
        }

        // 🔸 Добавить кнопки инструментов сюда
        private void InitToolsSimple()
        {
            _toolsButtons = new Button[]
               {
                 // 🔸 Нижний ряд (слева → направо)
                 Tools,
                 Ex_Инструменты12,
                 Ex_Инструменты123,

                 // 🔹 Верхний ряд (слева → направо)
                 Ex_Инструменты1_1,
                 Ex_Инструменты1_12,
                 Ex_Инструменты1_123
                };


            foreach (var b in _toolsButtons)
            {
                if (b == null) continue;

                // transform вместо Margin
                b.RenderTransform = new TranslateTransform(-120, 0);
                b.Opacity = 0;
                b.IsHitTestVisible = false;
            }
        }

        // 🔸 Настройки анимаций кнопки инструменты
        private void Ex_Инструменты_Click(object sender, RoutedEventArgs e)
        {
            _toolsOpened = !_toolsOpened;

            int columns = _toolsButtons.Length / 2; // верх + низ

            for (int col = 0; col < columns; col++)
            {
                int lowerIndex = col;
                int upperIndex = col + columns;

                TimeSpan delay = TimeSpan.FromMilliseconds(col * 240); // скорость "волны"

                AnimateButton(_toolsButtons[lowerIndex], delay);
                AnimateButton(_toolsButtons[upperIndex], delay);
            }
        }

        // 🔸 Настройки анимаций кнопки инструменты
        private void Animate(object target, double to, TimeSpan delay)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(720),   // ⬅ медленнее
                BeginTime = delay,
                EasingFunction = new QuinticEase              // ⬅ мягче
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            if (target is TranslateTransform tt)
                tt.BeginAnimation(TranslateTransform.XProperty, anim);
            else if (target is UIElement el)
                el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        // 🔸 Анимация кнопки инструменты
        private void AnimateButton(Button b, TimeSpan delay)
        {
            if (b == null) return;

            var tt = (TranslateTransform)b.RenderTransform;

            double toX = _toolsOpened ? 0 : -120;
            double toOpacity = _toolsOpened ? 1 : 0;

            b.IsHitTestVisible = _toolsOpened;

            Animate(tt, toX, delay);
            Animate(b, toOpacity, delay);
        }
        // 🔸 Добавить кнопки инструментов сюда для отоброжения текста при наведении
        private void InitHoverText()
        {
            _hoverText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 14,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20))
            };

            Border border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = _hoverText.Background,
                Child = _hoverText,
                RenderTransformOrigin = new Point(0.5, 1),
                RenderTransform = new ScaleTransform(1, 0) // 🔥 вместо Opacity
            };

            _hoverPopup = new Popup
            {
                Child = border,
                Placement = PlacementMode.Top,
                StaysOpen = true,
                AllowsTransparency = true,   // 🔥 ВАЖНО
                IsHitTestVisible = false,
                IsOpen = true
            };


            _hoverAutoHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _hoverAutoHideTimer.Tick += (s, e) =>
            {
                _hoverAutoHideTimer.Stop();
                AnimateHover(0);
            };

            // регистрация кнопок
            RegisterHover(Tools, "Настройки");
            RegisterHover(Ex_Инструменты12, "Сотрудники");
            RegisterHover(Ex_Инструменты123, "Добавить сотрудника");

            RegisterHover(Ex_Инструменты1_1, "Сотрудники (верх)");
            RegisterHover(Ex_Инструменты1_12, "Отчёты");
            RegisterHover(Ex_Инструменты1_123, "Статистика");
        }

        private void AnimateHover(double to)
        {
            if (_hoverPopup?.Child is Border border &&
                border.RenderTransform is ScaleTransform st)
            {
                var anim = new DoubleAnimation
                {
                    To = to == 1 ? 1 : 0,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseOut
                    }
                };

                st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        // 🔸 Обработчик наведения для кнопок инструментов
        private void RegisterHover(Button btn, string text)
        {
            if (btn == null) return;

            btn.MouseEnter += (s, e) =>
            {
                _hoverText.Text = text;
                _hoverPopup.PlacementTarget = btn;

                _hoverAutoHideTimer.Stop();
                AnimateHover(1);          // 🔥 ПОКАЗ
                _hoverAutoHideTimer.Start();
            };

            btn.MouseLeave += (s, e) =>
            {
                _hoverAutoHideTimer.Stop();
                AnimateHover(0);          // 🔥 СКРЫТЬ
            };
        }

        // 🔸 Обработчик видеофона: начало воспроизведения
        private void BgPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            _videoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };

            _videoTimer.Tick += VideoLoopTick;
            _videoTimer.Start();
        }

        // 🔸 Обработчик видеофона: зацикливание
        private void VideoLoopTick(object sender, EventArgs e)
        {
            if (!BgPlayer.NaturalDuration.HasTimeSpan)
                return;

            var duration = BgPlayer.NaturalDuration.TimeSpan;

            // 🔴 КЛЮЧ: не даём дойти до конца
            if (BgPlayer.Position >= duration - TimeSpan.FromMilliseconds(300))
            {
                BgPlayer.Position = TimeSpan.FromMilliseconds(80);
            }
        }

        // 🚪 Кнопка выхода из программы
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
                
        // Обработчик для кнопки "Свернуть"
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 🟢 Обработка нажатия на кнопку "Сотрудники"
        private void Employees_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Если окно уже открыто — закрываем через менеджер
            if (employeesWindow != null && employeesWindow.IsVisible)
            {
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is AddEmployeeWindow)
                        w.Close();
                }

                manager.CloseAnimatedWindow(employeesWindow);
                employeesWindow = null;
                return;
            }

            // 2️⃣ Если не открыто — создаём (покажется само через менеджер)
            employeesWindow = new EmployeesWindow(manager);
            employeesWindow.Owner = this;

            employeesWindow.Closed += (_, __) =>
            {
                employeesWindow = null;
            };
        }


        // 🟢 Главное окно: изменён размер
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            manager.UpdateAll();
        }

        // 🟢 Главное окно: перемещение на экране
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            manager.UpdateAll();
        }

        // 🟢 Главное окно: изменение состояния (Normal/Maximized)
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
                manager.UpdateAll();
        }

        // 🟢 Таймер обновления состояния интерфейса
        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateNow();
            _clockTimer.Start();
            UpdateNow();
        }

        // 🟢 Метод обновления отображаемого времени
        private void UpdateNow()
        {
            var culture = new CultureInfo("ru-RU");

            string day = DateTime.Now.ToString("ddd", culture);
            day = culture.TextInfo.ToTitleCase(day);

            string date = DateTime.Now.ToString("dd.MM.yyyy", culture);
            string time = DateTime.Now.ToString("HH:mm:ss", culture);

            string text = $"{day}, {date}\n{time}";

            // Старый вариант: внутренняя кнопка с txtDateTime
            if (_txtInButton != null)
                _txtInButton.Text = text;

            // Новый вариант: VORTEX кнопка
            if (btnClock != null)
                ButtonProps.SetTextSource(btnClock, text);
        }

        // 🟢 Кнопка окна настройки
        private void ToolsButton_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ ЕСЛИ ОКНО УЖЕ ОТКРЫТО → ЗАКРЫВАЕМ
            if (toolsWindow != null && toolsWindow.IsVisible)
            {
                manager.CloseAnimatedWindow(toolsWindow);
                toolsWindow = null;
                return;
            }

            // 2️⃣ ЕСЛИ НЕ ОТКРЫТО → ОТКРЫВАЕМ
            toolsWindow = new Tools(manager, this);
            toolsWindow.Owner = this;

            toolsWindow.Closed += (_, __) =>
            {
                toolsWindow = null;
            };
        }














    }


}
