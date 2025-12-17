using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using test5;
using WpfAnimatedGif;


namespace Vortex
{
    public partial class AddEmployeeWindow : Window, WindowManager.IChildWindow
    {
        public WindowManager.ChildWindowPosition Position
            => WindowManager.ChildWindowPosition.RightPanel;

        private Storyboard dropStoryboard;
        private bool _isClosing = false;
        public static bool IsClosingNow = false;
        public int? EmployeeId { get; private set; }

        public AddEmployeeWindow()
        {
            InitializeComponent();

            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = false;
            WindowStartupLocation = WindowStartupLocation.Manual;

            Loaded += (s, e) => UpdatePdfButtonState();
            Loaded += AddBlur;

            dropStoryboard = (Storyboard)Resources["DropRunStoryboard"];
            Loaded += (s, e) =>
            {
                dropStoryboard.Stop();
                dropStoryboard.Begin();
            };

            var dropTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            dropTimer.Tick += (s, e) =>
            {
                dropStoryboard.Stop();
                dropStoryboard.Begin();
            };
            dropTimer.Start();
        }


        public AddEmployeeWindow(WindowManager manager, Window owner) : this()
        {
            Owner = owner;

            manager.Register(this);
            manager.ShowAnimatedWindow(this);
        }




        // ⭐ Конструктор №2 — открыть сотрудника по ID
        public AddEmployeeWindow(int employeeId) : this()
        {
            EmployeeId = employeeId;

            // 👉 Здесь позже будет подгрузка данных сотрудника из API
            // LoadEmployee(employeeId);
        }

        private void RegisterGlobalClick()
        {
            // Кликинг в других окнах
            foreach (Window w in Application.Current.Windows)
            {
                if (w != this)
                    w.PreviewMouseDown += GlobalClickCheck;
            }

            // Глобальный клик по экрану
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, GlobalClickCheck);
        }

        private void GlobalClickCheck(object sender, MouseButtonEventArgs e)
        {
            if (_isClosing) return;

            // абсолютные координаты
            Point p = e.GetPosition(Application.Current.MainWindow);
            Point screen = Application.Current.MainWindow.PointToScreen(p);

            double x = screen.X;
            double y = screen.Y;

            bool inside =
                x >= this.Left &&
                x <= this.Left + this.Width &&
                y >= this.Top &&
                y <= this.Top + this.Height;

            if (!inside)
                SafeClose();
        }

       
       

        // 🔵 Обновление размеров и положения — идентично EmployeesWindow
        public void UpdateFromMain(double mainLeft, double mainTop, double mainWidth, double mainHeight)
        {
            // Твои пропорции одинаковы — оставляем
            double marginLeft = 1800;
            double marginTop = 135;
            double marginRight = 40;
            double marginBottom = 125;

            // Размер ЭКРАНА (на него мы и ориентируемся)
            double screenWidth = 2560;
            double screenHeight = 1440;

            // Минимальный размер окна
            const double MIN_WIDTH = 500;
            const double MIN_HEIGHT = 600;

            // ─────────────────────────────────────────────
            // 1. Положение — по экрану, а не по mainWindow
            // ─────────────────────────────────────────────
            double left = marginLeft;
            double top = marginTop;

            // ─────────────────────────────────────────────
            // 2. Размер — тоже по экрану, как ты и хотел
            // ─────────────────────────────────────────────
            double width = screenWidth - marginLeft - marginRight;
            double height = screenHeight - marginTop - marginBottom;

            if (width < MIN_WIDTH) width = MIN_WIDTH;
            if (height < MIN_HEIGHT) height = MIN_HEIGHT;

            // Границы экрана
            if (left + width > screenWidth)
                left = screenWidth - width - 20;

            if (top + height > screenHeight)
                top = screenHeight - height - 20;

            if (left < 0) left = 0;
            if (top < 0) top = 0;

            // ─────────────────────────────────────────────
            // 3. Применяем (окно всегда один-в-один как до этого)
            // ─────────────────────────────────────────────
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        // ❌ Закрытие окна
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SafeClose();
        }

        // 🟢 Анимация EXIT кнопки
        private void Exit_MouseEnter(object sender, MouseEventArgs e)
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
                        controller.GotoFrame(0);
                        controller.Play();
                    }
                    else
                    {
                        var animatedImage = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/exit2.gif"));
                        ImageBehavior.SetAnimatedSource(gifImage, animatedImage);
                        ImageBehavior.SetAutoStart(gifImage, true);
                    }
                }
            }
            catch { }
        }

        private void Exit_MouseLeave(object sender, MouseEventArgs e)
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
                        controller.Pause();
                        controller.GotoFrame(0);
                    }
                }
            }
            catch { }
        }

        // 🔥 Добавление сотрудника
        // 🔵 КНОПКА ВНИЗУ
        private async void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            // ─────────────────────────────────────────
            // 1️⃣ РЕЖИМ РЕДАКТИРОВАНИЯ СУЩЕСТВУЮЩЕГО СОТРУДНИКА
            // ─────────────────────────────────────────
            if (EmployeeId.HasValue)
            {
                // Если сейчас режим ПРОСМОТРА (поля заблокированы) —
                // просто включаем редактирование и выходим
                if (FullNameBox.IsReadOnly)
                {
                    SetEditable(true);              // делаем все поля редактируемыми
                    Ex_3.Visibility = Visibility.Visible; // показываем кнопку "Загрузка PDF файла"

                    // меняем текст кнопки на "Сохранить изменения"
                    UpdateButtonLabel(btn, "Сохранить изменения", 23, FontWeights.Bold);
                    ResetButtonText(btn); // на всякий случай вернуть белый цвет текста

                    return; // НИКАКОГО запроса на сервер тут нет
                }

                // Если мы уже в режиме редактирования (поля разблокированы),
                // то это именно сохранение изменений
                await UpdateEmployee(btn);
                return;
            }

            // ─────────────────────────────────────────
            // 2️⃣ СТАРАЯ ЛОГИКА — СОЗДАНИЕ НОВОГО СОТРУДНИКА
            // ─────────────────────────────────────────

            // Проверка пустых обязательных полей
            if (string.IsNullOrWhiteSpace(FullNameBox.Text) ||
                string.IsNullOrWhiteSpace(PhoneBox.Text) ||
                string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                UpdateButtonLabel(btn, "Заполните обязательные поля", 20, FontWeights.Bold);
                SetButtonTextError(btn); // текст становится красным

                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    Dispatcher.Invoke(() =>
                    {
                        ResetButtonText(btn);  // вернуть белый текст
                        UpdateButtonLabel(btn, "Добавить нового сотрудника", 25, FontWeights.Normal);
                    });
                });

                return;
            }

            // 1️⃣ Формируем объект
            var obj = new
            {
                company = Doc.GlobalSession.CompanyName,
                username = EmailBox.Text,
                password = "TEMP",
                role = "User",

                full_name = FullNameBox.Text,
                phone = PhoneBox.Text,
                email = EmailBox.Text,
                position = PositionBox.Text,
                birth_date = BirthDateBox.Text,
                hire_date = HireDateBox.Text,
                address = AddressBox.Text,
                notes = NotesBox.Text,

                status = string.IsNullOrWhiteSpace(StatusBox.Text)
                            ? "active"
                            : StatusBox.Text,

                resume_path = ResumePathBox.Text
            };

            int newUserId = -1;

            // 2️⃣ Создание сотрудника в базе
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization",
                    "Bearer " + Doc.GlobalSession.Token);

                string url = ApiConfig.EmployeesCreate;
                string json = JsonConvert.SerializeObject(obj);

                var resp = await http.PostAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                string resultJson = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    UpdateButtonLabel(btn,
                        "✖ Ошибка при создании",
                        20,
                        FontWeights.Bold);

                    await Task.Delay(3000);
                    UpdateButtonLabel(btn, "Добавить нового сотрудника", 25, FontWeights.Normal);
                    return;
                }

                dynamic data = JsonConvert.DeserializeObject(resultJson);
                newUserId = data.user_id;
                this.EmployeeId = newUserId;
            }

            // 4️⃣ Загрузка PDF
            if (!string.IsNullOrWhiteSpace(ResumePathBox.Text) &&
                File.Exists(ResumePathBox.Text))
            {
                await UploadFile(
                    $"{ApiConfig.BaseUrl}/api/upload/resume/{EmployeeId}",
                    ResumePathBox.Text,
                    "resume.pdf"
                );
            }

            // 5️⃣ Загрузка аватара
            if (!string.IsNullOrWhiteSpace(AvatarPathBox.Text) &&
                File.Exists(AvatarPathBox.Text))
            {
                await UploadFile(
                    $"{ApiConfig.BaseUrl}/api/upload/avatar/{EmployeeId}",
                    AvatarPathBox.Text,
                    "avatar.png"
                );
            }


            // 6️⃣ Красивое уведомление в кнопке
            UpdateButtonLabel(btn,
                "✔ Сотрудник добавлен!",
                22,
                FontWeights.Bold);

            // 7️⃣ Очищаем форму
            ClearAllFields();

            // 8️⃣ Сразу обновляем список сотрудников
            foreach (Window w in Application.Current.Windows)
            {
                if (w is EmployeesWindow empWin)
                {
                    await empWin.LoadEmployees();
                    empWin.EmployeesScroll.ScrollToTop();
                    break;
                }
            }

            // 9️⃣ Через 5 сек вернуть стандартный текст
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                Dispatcher.Invoke(() =>
                    UpdateButtonLabel(btn, "Добавить нового сотрудника", 25, FontWeights.Normal)
                );
            });
        }

        private void SetButtonTextError(Button btn)
        {
            var label = btn.Template.FindName("AddEmployeeLabel", btn) as TextBlock;

            if (label != null)
                label.Foreground = new SolidColorBrush(Color.FromRgb(255, 60, 60)); // ярко-красный
        }

        private void ResetButtonText(Button btn)
        {
            var label = btn.Template.FindName("AddEmployeeLabel", btn) as TextBlock;

            if (label != null)
                label.Foreground = Brushes.White; // вернуть белый
        }

        // 🔵 Сохранение изменений существующего сотрудника
        private async Task UpdateEmployee(Button btn)
        {
            // те же проверки обязательных полей
            if (string.IsNullOrWhiteSpace(FullNameBox.Text) ||
                string.IsNullOrWhiteSpace(PhoneBox.Text) ||
                string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                UpdateButtonLabel(btn, "Заполните обязательные поля", 20, FontWeights.Bold);
                SetButtonTextError(btn);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    Dispatcher.Invoke(() =>
                    {
                        ResetButtonText(btn);
                        UpdateButtonLabel(btn, "Сохранить изменения", 23, FontWeights.Bold);
                    });
                });

                return;
            }

            var obj = new
            {
                id = EmployeeId,   // ← ВОТ ЭТО ДОБАВЛЯЕМ
                full_name = FullNameBox.Text,
                phone = PhoneBox.Text,
                email = EmailBox.Text,
                position = PositionBox.Text,
                birth_date = BirthDateBox.Text,
                hire_date = HireDateBox.Text,
                address = AddressBox.Text,
                notes = NotesBox.Text,
                status = string.IsNullOrWhiteSpace(StatusBox.Text)
                            ? "active"
                            : StatusBox.Text,
                resume_path = ResumePathBox.Text
            };

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization",
                    "Bearer " + Doc.GlobalSession.Token);

                string url = ApiConfig.EmployeesUpdate;
                string json = JsonConvert.SerializeObject(obj);

                var resp = await http.PostAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));


                if (!resp.IsSuccessStatusCode)
                {
                    UpdateButtonLabel(btn,
                        "✖ Ошибка при обновлении",
                        20,
                        FontWeights.Bold);

                    await Task.Delay(3000);
                    UpdateButtonLabel(btn, "Сохранить изменения", 23, FontWeights.Bold);
                    return;
                }
            }

            // при необходимости можно тоже перезагрузить список сотрудников
            foreach (Window w in Application.Current.Windows)
            {
                if (w is EmployeesWindow empWin)
                {
                    await empWin.LoadEmployees();
                    break;
                }
            }

            UpdateButtonLabel(btn, "✔ Изменения сохранены", 22, FontWeights.Bold);

            _ = Task.Run(async () =>
            {
                await Task.Delay(4000);
                Dispatcher.Invoke(() =>
                    UpdateButtonLabel(btn, "Сохранить изменения", 23, FontWeights.Bold)
                );
            });
        }

        private void UpdatePdfButtonState()
        {
            var label = Ex_3.Template.FindName("AddEmployeeLabel", Ex_3) as TextBlock;
            if (label == null) return;

            if (string.IsNullOrWhiteSpace(ResumePathBox.Text))
            {
                label.Text = "Файл не прикреплён";
                label.Foreground = Brushes.Orange;
                return;
            }

            label.Text = "Файл PDF";
            label.Foreground = Brushes.White;
        }

        private void AddBlur(object sender, RoutedEventArgs e)
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = unchecked((int)0x99000000);
            // прозрачность + цвет (99 — 60% прозрачности)

            int accentStructSize = Marshal.SizeOf(accent);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private void PhoneInternational_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void PhoneInternational_TextChanged(object sender, TextChangedEventArgs e)
        {
            string digits = new string(PhoneBox.Text.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
            {
                PhoneBox.Text = "+";
                PhoneBox.CaretIndex = PhoneBox.Text.Length;
                return;
            }

            string formatted = "+" + digits[0];

            if (digits.Length > 1)
                formatted += " (" + digits.Substring(1, Math.Min(3, digits.Length - 1));

            if (digits.Length > 4)
                formatted += ") " + digits.Substring(4, Math.Min(3, digits.Length - 4));

            if (digits.Length > 7)
                formatted += "-" + digits.Substring(7, Math.Min(2, digits.Length - 7));

            if (digits.Length > 9)
                formatted += "-" + digits.Substring(9, Math.Min(2, digits.Length - 9));

            PhoneBox.Text = formatted;
            PhoneBox.CaretIndex = PhoneBox.Text.Length;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                box.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFF"));
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                box.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222"));
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!string.IsNullOrWhiteSpace(tb.Text))
                    tb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFF"));
                else
                    tb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222"));
            }
        }

        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox_TextChanged(sender, e); // подсветка

            PhoneInternational_TextChanged(sender, e); // маска номера
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // отключаем стандартное поведение Enter

                // Переход на следующее поле
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                (sender as UIElement)?.MoveFocus(request);
            }
        }

        // Разрешаем только цифры и Backspace
        private void BirthDate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text[0]);
        }

        // Авто подстановка YYYY-MM-DD
        private void BirthDate_OnInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            // Оставляем только цифры из текущего текста
            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());

            // Если уже 8 цифр — запретить ввод
            if (digits.Length >= 8)
            {
                e.Handled = true;
                return;
            }

            // Добавляем новую цифру
            digits += e.Text;

            tb.Text = FormatDate(digits);

            // Установить курсор после вставленной цифры
            tb.SelectionStart = tb.Text.Length;

            e.Handled = true;
        }

        private void BirthDate_OnKeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Backspace
            if (e.Key == Key.Back)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());

                if (digits.Length == 0)
                {
                    e.Handled = true;
                    tb.Text = "";
                    return;
                }

                // Удаляем последнюю цифру
                digits = digits.Substring(0, digits.Length - 1);

                tb.Text = FormatDate(digits);
                tb.SelectionStart = tb.Text.Length;

                e.Handled = true;
            }
        }

        private string FormatDate(string d)
        {
            if (d.Length <= 4)
                return d;
            if (d.Length <= 6)
                return d.Insert(4, "-");
            return d.Insert(4, "-").Insert(7, "-");
        }

        private void EmailBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@._-";

            // если введённый символ НЕ входит в разрешённые — останавливаем ввод
            if (!allowed.Contains(e.Text))
                e.Handled = true;
        }

        private void EmailBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void StatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Например:
            if (StatusBox.SelectedItem is ComboBoxItem item)
            {
                string status = item.Content.ToString();
                Console.WriteLine("Статус: " + status);
            }
        }

        private void StatusBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                var popup = cb.Template.FindName("PART_Popup", cb) as System.Windows.Controls.Primitives.Popup;
                if (popup != null)
                {
                    popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    popup.PlacementTarget = cb;
                    popup.StaysOpen = false;

                    popup.AllowsTransparency = true;
                    popup.PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade;
                }
            }
        }

        private TextBlock GetLabelFromButton()
        {
            if (Ex_3.Template == null)
                return null;

            return Ex_3.Template.FindName("AddEmployeeLabel", Ex_3) as TextBlock;
        }

        private void SelectResumeFile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            // 🔥 ВАЖНО: без этого label всегда null
            btn.ApplyTemplate();

            var label = btn?.Template.FindName("AddEmployeeLabel", btn) as TextBlock;

            // ЕСЛИ ПУТЬ ЕСТЬ
            if (!string.IsNullOrWhiteSpace(ResumePathBox.Text))
            {
                // Путь локальный, но файл удалён
                if (!File.Exists(ResumePathBox.Text) && ResumePathBox.Text.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                {
                    if (label != null)
                    {
                        label.Text = "Файл отсутствует";
                        label.Foreground = Brushes.Red;
                    }
                    return;
                }

                // Серверный путь → открыть через браузер
                if (!File.Exists(ResumePathBox.Text))
                {
                    OpenPdf(ResumePathBox.Text);

                    if (label != null)
                    {
                        label.Text = "Файл PDF";
                        label.Foreground = Brushes.White;
                    }
                    return;
                }

                // Локальный PDF → открыть локально
                OpenLocalPdf(ResumePathBox.Text);

                if (label != null)
                {
                    label.Text = "Файл PDF";
                    label.Foreground = Brushes.White;
                }
                return;
            }

            // ЕСЛИ PDF НЕТ — открыть диалог выбора файла
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "PDF документы|*.pdf";

            if (dialog.ShowDialog() == true)
            {
                ResumePathBox.Text = dialog.FileName;

                if (label != null)
                {
                    label.Text = "Файл загружен";
                    label.Foreground = Brushes.Lime;
                    label.FontWeight = FontWeights.Bold;
                }
            }
            else
            {
                if (label != null)
                {
                    label.Text = "Файл не прикреплён";
                    label.Foreground = Brushes.Orange;
                }
            }
        }

        private void OpenPdf(string path)
        {
            // Если путь серверный — делаем абсолютным
            if (!path.StartsWith("http"))
                path = ApiConfig.BaseUrl + path;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть PDF.", "Ошибка");
            }
        }

        private void OpenLocalPdf(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть PDF.", "Ошибка");
            }
        }

        private void OpenResumeFromViewMode(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResumePathBox.Text))
            {
                var label = Ex_3.Template.FindName("AddEmployeeLabel", Ex_3) as TextBlock;
                if (label != null)
                {
                    label.Text = "Файл отсутствует";
                    label.Foreground = Brushes.Red;
                    label.FontWeight = FontWeights.Bold;
                }

                // через 2 секунды вернуть обратно
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    Dispatcher.Invoke(() =>
                    {
                        var lbl = Ex_3.Template.FindName("AddEmployeeLabel", Ex_3) as TextBlock;
                        if (lbl != null)
                        {
                            lbl.Text = "Файл PDF";
                            lbl.Foreground = Brushes.White;
                            lbl.FontWeight = FontWeights.Normal;
                        }
                    });
                });

                return;
            }


            string path = ResumePathBox.Text;

            // если серверный путь
            if (path.StartsWith("http"))
            {
                OpenPdf(path);   // откроет через браузер
                return;
            }

            // если локальный файл
            if (File.Exists(path))
            {
                OpenLocalPdf(path);
                return;
            }

            MessageBox.Show("Файл резюме не найден.", "Ошибка");
        }

        private void SelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Фото (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (dlg.ShowDialog() != true)
            {
                AvatarPlaceholder.Visibility = Visibility.Visible;
                AvatarPlaceholder.Opacity = 1;
                return;
            }

            string originalPath = dlg.FileName;

            // Загружаем изображение
            BitmapImage source = new BitmapImage(new Uri(originalPath));

            int size = Math.Min(source.PixelWidth, source.PixelHeight);
            int x = (source.PixelWidth - size) / 2;
            int y = (source.PixelHeight - size) / 2;

            // 1️⃣ Квадратный crop
            CroppedBitmap square = new CroppedBitmap(source, new Int32Rect(x, y, size, size));

            // 2️⃣ Делаем круг
            WriteableBitmap wb = new WriteableBitmap(square);

            int w = wb.PixelWidth;
            int h = wb.PixelHeight;

            int stride = w * 4;
            byte[] pixels = new byte[h * stride];
            wb.CopyPixels(pixels, stride, 0);

            int center = w / 2;
            double radius = w / 2;

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    double dx = px - center;
                    double dy = py - center;

                    if (Math.Sqrt(dx * dx + dy * dy) > radius)
                    {
                        int index = py * stride + px * 4;
                        pixels[index + 3] = 0; // прозрачность
                    }
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);

            // 3️⃣ Сохраняем PNG
            string tempPath = Path.GetTempFileName() + ".png";
            using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(wb));
                enc.Save(fs);
            }

            // 4️⃣ Показываем фото
            AvatarImage.Source = new BitmapImage(new Uri(tempPath));

            // Надпись больше не нужна → опускаем вниз
            Panel.SetZIndex(AvatarPlaceholder, 0);
            Panel.SetZIndex(AvatarImage, 1);

            // 5️⃣ Скрываем надпись полностью
            AvatarPlaceholder.Visibility = Visibility.Collapsed;
            AvatarPlaceholder.Opacity = 0;

            // 6️⃣ ЛОКАЛЬНОЕ сохранение пути
            AvatarPathBox.Text = tempPath;
        }

        private async Task UploadFile(string url, string filePath, string fileName)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Doc.GlobalSession.Token);

                var form = new MultipartFormDataContent();
                form.Add(new ByteArrayContent(File.ReadAllBytes(filePath)), "file", fileName);

                await client.PostAsync(url, form);
            }
        }

        private void UpdateButtonLabel(object sender, string text, double size, FontWeight weight)
        {
            var button = sender as Button;
            if (button == null) return;

            TextBlock label = button.Template.FindName("AddEmployeeLabel", button) as TextBlock;
            if (label == null) return;

            label.Text = text;
            label.FontSize = size;
            label.FontWeight = weight;
        }


        private void ClearAllFields()
        {
            FullNameBox.Text = "";
            PhoneBox.Text = "";
            EmailBox.Text = "";
            PositionBox.Text = "";
            BirthDateBox.Text = "";
            HireDateBox.Text = "";
            AddressBox.Text = "";
            NotesBox.Text = "";

            ResumePathBox.Text = "";
            AvatarPathBox.Text = "";

            AvatarImage.Source = null;
            AvatarPlaceholder.Visibility = Visibility.Visible;
            AvatarPlaceholder.Opacity = 1;

            if (StatusBox.Items.Count > 0)
                StatusBox.SelectedIndex = 0;
        }

        public void OpenInViewMode(EmployeesWindow.EmployeeDto emp)
        {
            EmployeeId = emp.id;

            // 🟦 Заполняем поля
            FullNameBox.Text = emp.full_name ?? "";
            PhoneBox.Text = emp.phone ?? "";
            EmailBox.Text = emp.email ?? "";
            PositionBox.Text = emp.position ?? "";
            BirthDateBox.Text = emp.birth_date ?? "";
            HireDateBox.Text = emp.hire_date ?? "";
            AddressBox.Text = emp.address ?? "";
            NotesBox.Text = emp.notes ?? "";
            StatusBox.Text = emp.status ?? "";

            // 🟦 Резюме
            // 🟦 Резюме
            if (!string.IsNullOrWhiteSpace(emp.resume_path))
                ResumePathBox.Text = ApiConfig.BaseUrl + emp.resume_path;

            // 🟦 Фото
            try
            {
                if (!string.IsNullOrWhiteSpace(emp.avatar_path))
                {
                    AvatarImage.Source = new BitmapImage(
                        new Uri(ApiConfig.BaseUrl + emp.avatar_path)
                    );
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { }


            // 🟦 Заголовок
            TitleText.Text = "Информация о сотруднике";


            // ========================================================
            // ⭐ ПРЕВРАЩАЕМ КНОПКУ Ex_2 ИЗ "МАССОВАЯ ЗАГРУЗКА" → "АРХИВИРОВАТЬ"
            // ========================================================

            Ex_2.Visibility = Visibility.Visible;     // делаем видимой
            Ex_2.Click -= SelectResumeFile_Click;     // снимаем загрузку PDF
            Ex_2.Click -= OpenResumeFromViewMode;     // снимем на всякий случай
            Ex_2.Click -= ArchiveEmployee_Click;      // чтобы не дублировать
            Ex_2.Click += ArchiveEmployee_Click;      // новый клик

            Ex_2.Dispatcher.InvokeAsync(() =>
            {
                Ex_2.ApplyTemplate();

                var lbl = Ex_2.Template.FindName("AddEmployeeLabel", Ex_2) as TextBlock;
                if (lbl != null)
                {
                    lbl.Text = "Архивировать";
                    lbl.FontSize = 17;
                    lbl.FontWeight = FontWeights.Bold;
                    lbl.Foreground = Brushes.White;
                }
            });

            // ⭐ Меняем GIF на иконку "files.gif"
            Ex_2.Dispatcher.InvokeAsync(() =>
            {
                Ex_2.ApplyTemplate();

                var gifImage = Ex_2.Template.FindName("GifImage", Ex_2) as Image;
                if (gifImage != null)
                {
                    var newGif = new BitmapImage(
                        new Uri("pack://application:,,,/Vortex;component/Assets/Images/files.gif", UriKind.Absolute)
                    );

                    ImageBehavior.SetAnimatedSource(gifImage, newGif);
                    ImageBehavior.SetAutoStart(gifImage, false);

                    // Анимация при наведении
                    Ex_2.MouseEnter += (s2, e3) =>
                    {
                        var controller = ImageBehavior.GetAnimationController(gifImage);
                        controller?.GotoFrame(0);
                        controller?.Play();
                    };

                    Ex_2.MouseLeave += (s2, e3) =>
                    {
                        var controller = ImageBehavior.GetAnimationController(gifImage);
                        controller?.Pause();
                        controller?.GotoFrame(0);
                    };
                }
            });


            // ========================================================
            // ⭐ КНОПКА PDF Ex_3 — как у тебя было
            // ========================================================

            Ex_3.Visibility = Visibility.Visible;

            Ex_3.Dispatcher.InvokeAsync(() =>
            {
                Ex_3.ApplyTemplate();

                var pdfLabel = Ex_3.Template.FindName("AddEmployeeLabel", Ex_3) as TextBlock;
                if (pdfLabel != null)
                {
                    pdfLabel.Text = "Файл PDF";
                    pdfLabel.FontSize = 18;
                    pdfLabel.Foreground = Brushes.White;
                    pdfLabel.FontWeight = FontWeights.Normal;
                }
            });

            Ex_3.Click -= SelectResumeFile_Click;
            Ex_3.Click += OpenResumeFromViewMode;


            // ========================================================
            // ⭐ Режим просмотра
            // ========================================================

            SetEditable(false);

            UpdateButtonLabel(Ex_4, "Редактировать", 25, FontWeights.Bold);

            Ex_4.Loaded += (s, e2) =>
            {
                UpdateButtonLabel(Ex_4, "Редактировать", 25, FontWeights.Bold);

                var gifImage = Ex_4.Template.FindName("GifImage", Ex_4) as Image;
                if (gifImage != null)
                {
                    ImageBehavior.SetAnimatedSource(gifImage, null);

                    var editGif = new BitmapImage(
                        new Uri("pack://application:,,,/Vortex;component/Assets/Images/edit.gif", UriKind.Absolute)
                    );

                    ImageBehavior.SetAnimatedSource(gifImage, editGif);
                    ImageBehavior.SetAutoStart(gifImage, false);

                    Ex_4.MouseEnter += (s2, e3) =>
                    {
                        var controller = ImageBehavior.GetAnimationController(gifImage);
                        controller?.GotoFrame(0);
                        controller?.Play();
                    };

                    Ex_4.MouseLeave += (s2, e3) =>
                    {
                        var controller = ImageBehavior.GetAnimationController(gifImage);
                        controller?.Pause();
                        controller?.GotoFrame(0);
                    };

                    Ex_4.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateButtonLabel(Ex_4, "Редактировать", 25, FontWeights.Bold);
                    });
                }
            };
        }

        // ⭐ КНОПКА АРХИВИРОВАТЬ
        private async void ArchiveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!EmployeeId.HasValue) return;

            var btn = sender as Button;

            // Меняем текст на ожидание
            UpdateButtonLabel(btn, "Архивирование...", 20, FontWeights.Bold);

            var obj = new
            {
                id = EmployeeId.Value,
                status = "archived"
            };

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization",
                    "Bearer " + Doc.GlobalSession.Token);

                string url = ApiConfig.EmployeesUpdate;
                string json = JsonConvert.SerializeObject(obj);

                var resp = await http.PostAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (!resp.IsSuccessStatusCode)
                {
                    UpdateButtonLabel(btn, "Ошибка архивации", 20, FontWeights.Bold);
                    await Task.Delay(3000);

                    UpdateButtonLabel(btn, "Архивировать", 20, FontWeights.Bold);
                    return;
                }
            }

            // Уведомление
            UpdateButtonLabel(btn, "✔ Архивирован", 20, FontWeights.Bold);

            // Обновляем список сотрудников
            foreach (Window w in Application.Current.Windows)
            {
                if (w is EmployeesWindow empWin)
                {
                    await empWin.LoadEmployees();
                    break;
                }
            }

            await Task.Delay(2000);

            // Закрываем окно
            SafeClose();
        }

        private void SetEditable(bool enabled)
        {
            bool readOnly = !enabled;

            FullNameBox.IsReadOnly = readOnly;
            PhoneBox.IsReadOnly = readOnly;
            EmailBox.IsReadOnly = readOnly;
            PositionBox.IsReadOnly = readOnly;
            BirthDateBox.IsReadOnly = readOnly;
            HireDateBox.IsReadOnly = readOnly;
            AddressBox.IsReadOnly = readOnly;
            NotesBox.IsReadOnly = readOnly;

            StatusBox.IsEnabled = enabled;

            double opacity = enabled ? 1.0 : 0.6;

            FullNameBox.Opacity = opacity;
            PhoneBox.Opacity = opacity;
            EmailBox.Opacity = opacity;
            PositionBox.Opacity = opacity;
            BirthDateBox.Opacity = opacity;
            HireDateBox.Opacity = opacity;
            AddressBox.Opacity = opacity;
            NotesBox.Opacity = opacity;
            StatusBox.Opacity = opacity;
        }

        private void SafeClose()
        {
            if (_isClosing) return;

            _isClosing = true;
            IsClosingNow = true;

            // закрываем через WindowManager, если есть
            if (Owner is MainWindow main && main.manager != null)
            {
                main.manager.CloseAnimatedWindow(this);
            }
            else
            {
                Close();
            }

            IsClosingNow = false;
        }




    }
}
