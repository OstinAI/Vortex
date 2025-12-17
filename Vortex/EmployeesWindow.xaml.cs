using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using test5;

namespace Vortex
{
    public partial class EmployeesWindow : Window, WindowManager.IChildWindow
    {
        // 🔹 позиция окна — справа сверху
        public WindowManager.ChildWindowPosition Position
            => WindowManager.ChildWindowPosition.RightPanel;
              
        private readonly WindowManager manager;
        private bool _isListMode = true; // при открытии окна мы в списке

        public EmployeesWindow(WindowManager manager)
        {
            InitializeComponent();

            this.manager = manager;

            // === БАЗОВЫЕ НАСТРОЙКИ ОКНА ===
            ShowActivated = false;
            ShowInTaskbar = false;
            Topmost = false;
            WindowStartupLocation = WindowStartupLocation.Manual;

            // === ПОДКЛЮЧЕНИЕ К WINDOW MANAGER ===
            manager.Register(this);
            manager.ShowAnimatedWindow(this);

            // === ГАРАНТИРОВАННАЯ ИНИЦИАЛИЗАЦИЯ ПОСЛЕ ПОКАЗА ОКНА ===
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                Console.WriteLine("EmployeesWindow → LoadEmployees()");

                // подгоняем размеры под экран
                var wa = SystemParameters.WorkArea;
                double margin = 40;

                Width = Math.Min(Width, wa.Width - margin);
                Height = Math.Min(Height, wa.Height - margin);

                // загрузка сотрудников
                await LoadEmployees();

            }), DispatcherPriority.Loaded);
        }

        // ❌ Кнопка закрытия
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is Window owner)
            {
                manager?.CloseAnimatedWindow(this);

                // 🔥 ВАЖНО: вернуть фокус
                owner.Activate();
                owner.Focus();
            }
            else
            {
                manager?.CloseAnimatedWindow(this);
            }
        }

        // ➕ КНОПКА "ДОБАВИТЬ СОТРУДНИКА" Открывает окно добавления сотрудника
        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow main)
            {
                new AddEmployeeWindow(main.manager, main);
            }
        }

        // 📦 DTO-ОТВЕТ СЕРВЕРА СО СПИСКОМ СОТРУДНИКОВ, Используется для десериализации JSON
        public class EmployeeResponse
        {
            public string status { get; set; }
            public List<EmployeeDto> employees { get; set; }
        }

        // 👤 DTO СОТРУДНИКА, Полное описание данных сотрудника, приходящих с сервера
        public class EmployeeDto
        {
            public int id { get; set; }
            public string username { get; set; }
            public string role { get; set; }
            public string full_name { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public string birth_date { get; set; }
            public string hire_date { get; set; }
            public string position { get; set; }
            public string address { get; set; }
            public string status { get; set; }
            public string notes { get; set; }
            public string resume_path { get; set; }

            // ⭐ ДОБАВЬ ЭТО ⭐
            public string avatar_path { get; set; }
        }

        // 📋 ЗАГРУЗКА СПИСКА АКТИВНЫХ СОТРУДНИКОВ, Основной метод для режима "Список"
        public async Task LoadEmployees()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add(
                        "Authorization",
                        "Bearer " + Doc.GlobalSession.Token
                    );

                    string json = await http.GetStringAsync(ApiConfig.EmployeesList);
                    Console.WriteLine(json); // ← важно для отладки

                    var response = Newtonsoft.Json.JsonConvert
                        .DeserializeObject<EmployeeResponse>(json);

                    if (response == null || response.employees == null)
                        return;

                    EmployeesPanel.Children.Clear();

                    foreach (var emp in response.employees)
                    {
                        // фильтр архивных (БЕЗОПАСНЫЙ)
                        if (!string.IsNullOrEmpty(emp.status) &&
                            emp.status.Equals("archived", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var btn = new Button
                        {
                            Style = (Style)FindResource("EmployeeCardButtonStyle"),
                            DataContext = emp,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        Grid.SetColumn(btn, 0);


                        btn.Click += EmployeeButton_Click;

                        EmployeesPanel.RowDefinitions.Add(
                            new RowDefinition { Height = GridLength.Auto }
                        );

                        Grid.SetRow(btn, EmployeesPanel.RowDefinitions.Count - 1);
                        EmployeesPanel.Children.Add(btn);

                    }

                    Console.WriteLine($"Карточек добавлено: {EmployeesPanel.Children.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки сотрудников: " + ex);
            }
        }

        // 🖱️ КЛИК ПО КАРТОЧКЕ СОТРУДНИКА, Открывает окно сотрудника в режиме просмотра
        private void EmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var emp = btn.DataContext as EmployeeDto;
            if (emp == null) return;

            if (Owner is MainWindow main)
            {
                var w = new AddEmployeeWindow(main.manager, main);
                w.OpenInViewMode(emp);
            }
        }

        // 🖱️ ПЛАВНЫЙ СКРОЛЛ — ПЕРЕХВАТ КОЛЕСА МЫШИ, Блокируем стандартный скролл и запускаем анимацию
        private void EmployeesScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            double offset = EmployeesScroll.VerticalOffset - e.Delta; // e.Delta = 120 или -120

            AnimateScroll(offset);
        }

        // 🎞️ АНИМИРОВАННЫЙ СКРОЛЛ, Плавно прокручивает ScrollViewer
        private void AnimateScroll(double toValue)
        {
            if (toValue < 0) toValue = 0;
            if (toValue > EmployeesScroll.ScrollableHeight)
                toValue = EmployeesScroll.ScrollableHeight;

            DoubleAnimation animation = new DoubleAnimation
            {
                From = EmployeesScroll.VerticalOffset,
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            AnimationClock clock = animation.CreateClock();
            EmployeesScroll.ApplyAnimationClock(ScrollViewerBehavior.VerticalOffsetProperty, clock);
        }

        // 🧩 ATTACHED BEHAVIOR ДЛЯ ScrollViewer, Позволяет анимировать VerticalOffset (который сам по себе не анимируется)
        public static class ScrollViewerBehavior
        {
            public static readonly DependencyProperty VerticalOffsetProperty =
                DependencyProperty.RegisterAttached(
                    "VerticalOffset",
                    typeof(double),
                    typeof(ScrollViewerBehavior),
                    new PropertyMetadata(0.0, OnVerticalOffsetChanged));

            public static void SetVerticalOffset(DependencyObject obj, double value)
            {
                obj.SetValue(VerticalOffsetProperty, value);
            }

            public static double GetVerticalOffset(DependencyObject obj)
            {
                return (double)obj.GetValue(VerticalOffsetProperty);
            }

            private static void OnVerticalOffsetChanged(
                DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset((double)e.NewValue);
                }
            }
        }

        // 🗄️ ЗАГРУЗКА АРХИВНЫХ СОТРУДНИКОВ, Используется для режима "Архив"
        private async Task LoadArchivedEmployees()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add(
                        "Authorization",
                        "Bearer " + Doc.GlobalSession.Token
                    );

                    string json = await http.GetStringAsync(ApiConfig.EmployeesList);
                    var resp = Newtonsoft.Json.JsonConvert.DeserializeObject<EmployeeResponse>(json);

                    if (resp?.employees == null)
                        return;

                    EmployeesPanel.Children.Clear();

                    foreach (var emp in resp.employees)
                    {
                        if (emp.status != "archived")
                            continue;

                        var btn = new Button
                        {
                            Style = (Style)FindResource("EmployeeCardButtonStyle"),
                            DataContext = emp
                        };

                        btn.Click += EmployeeButton_Click;
                        EmployeesPanel.Children.Add(btn);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки архивных сотрудников: " + ex.Message);
            }
        }

        // 🗄️ КНОПКА "АРХИВ" Переключает режим на архив сотрудников
        private async void ARC_Click(object sender, RoutedEventArgs e)
        {
            if (_isListMode == false)
                return; // уже в архиве — ничего не делаем

            _isListMode = false;

            await LoadArchivedEmployees();
        }

        // 📋 КНОПКА "СПИСОК" Возвращает из архива в основной список
        private async void Spis_Click(object sender, RoutedEventArgs e)
        {
            if (_isListMode)
                return; // 🔒 УЖЕ В СПИСКЕ — НИЧЕГО НЕ ДЕЛАЕМ

            _isListMode = true;

            await LoadEmployees();
        }









    }
}
