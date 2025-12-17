using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace Vortex
{
    public static class VortexButtonBehavior
    {
        public static bool GetEnable(DependencyObject obj)
            => (bool)obj.GetValue(EnableProperty);

        public static void SetEnable(DependencyObject obj, bool value)
            => obj.SetValue(EnableProperty, value);

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(VortexButtonBehavior),
                new PropertyMetadata(false, OnEnableChanged));


        // =============================================================
        // ========== ВКЛЮЧЕНИЕ / ОТКЛЮЧЕНИЕ ОБРАБОТЧИКОВ ============
        // =============================================================
        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                if ((bool)e.NewValue == true)
                {
                    btn.MouseMove += Btn_MouseMove;
                    btn.MouseEnter += Btn_MouseEnter;
                    btn.MouseLeave += Btn_MouseLeave;
                    btn.Click += Btn_Click;

                    // >>> ДОБАВЛЯЕМ <<<
                    btn.Loaded += Btn_Loaded;
                }
                else
                {
                    btn.MouseMove -= Btn_MouseMove;
                    btn.MouseEnter -= Btn_MouseEnter;
                    btn.MouseLeave -= Btn_MouseLeave;
                    btn.Click -= Btn_Click;

                    btn.Loaded -= Btn_Loaded;
                }

            }
        }

        private static void Btn_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var gifMode = ButtonProps.GetGifMode(btn);
                var gifPath = ButtonProps.GetGifSource(btn);

                if (gifMode != GifMode.Always)
                    return;

                if (string.IsNullOrWhiteSpace(gifPath))
                    return;

                var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                if (gifImage == null)
                    return;

                try
                {
                    gifPath = gifPath.TrimStart('/');
                    string packPath = $"pack://application:,,,/{gifPath}";

                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(packPath, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();

                    ImageBehavior.SetAnimatedSource(gifImage, img);
                    ImageBehavior.SetRepeatBehavior(gifImage, RepeatBehavior.Forever);
                }
                catch { }
            }
        }

        // =============================================================
        // =========== МЫШЬ ДВИЖЕНИЕ → световое пятно + волна =========
        // =============================================================
        private static void Btn_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                var content = btn.Template.FindName("contentGrid", btn) as FrameworkElement;
                if (content == null) return;

                var spot = btn.Template.FindName("GlowSpot", btn) as FrameworkElement;
                var wave = btn.Template.FindName("WavePulse", btn) as FrameworkElement;
                if (spot == null || wave == null) return;

                var pos = e.GetPosition(content);

                Canvas.SetLeft(spot, pos.X - spot.Width / 2);
                Canvas.SetTop(spot, pos.Y - spot.Height / 2);

                Canvas.SetLeft(wave, pos.X - wave.Width / 2);
                Canvas.SetTop(wave, pos.Y - wave.Height / 2);
            }
        }


        // ======================== MOUSE ENTER ========================
        private static void Btn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                PlayHoverSound();
                VisualStateManager.GoToState(btn, "MouseOver", true);

                var gifPath = ButtonProps.GetGifSource(btn);
                var gifMode = ButtonProps.GetGifMode(btn);

                if (!string.IsNullOrWhiteSpace(gifPath))
                {
                    try
                    {
                        var gifImage = btn.Template.FindName("GifImage", btn) as Image;
                        if (gifImage != null)
                        {
                            gifPath = gifPath.TrimStart('/');
                            string packPath = $"pack://application:,,,/{gifPath}";

                            var img = new BitmapImage();
                            img.BeginInit();
                            img.UriSource = new Uri(packPath, UriKind.Absolute);
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();

                            if (gifMode == GifMode.Always)
                            {
                                ImageBehavior.SetAnimatedSource(gifImage, img);
                                ImageBehavior.SetRepeatBehavior(gifImage, RepeatBehavior.Forever);
                            }
                            else if (gifMode == GifMode.OnHover)
                            {
                                ImageBehavior.SetAnimatedSource(gifImage, img);
                                ImageBehavior.SetRepeatBehavior(gifImage, RepeatBehavior.Forever);
                            }
                            else if (gifMode == GifMode.Static)
                            {
                                gifImage.Source = img; // только первый кадр
                            }
                        }
                    }
                    catch { }
                }

                StartWave(btn);
            }
        }










        // ======================== MOUSE LEAVE ========================
        private static void Btn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                VisualStateManager.GoToState(btn, "Normal", true);
                StopWave(btn);

                var gifMode = ButtonProps.GetGifMode(btn);
                var gifImage = btn.Template.FindName("GifImage", btn) as Image;

                if (gifImage == null)
                    return;

                var controller = ImageBehavior.GetAnimationController(gifImage);

                if (gifMode == GifMode.OnHover)
                {
                    // Останавливаем (замораживаем) GIF
                    controller?.Pause();

                    // Возвращаем в самый первый кадр
                    try
                    {
                        controller?.GotoFrame(0);
                    }
                    catch
                    {
                        // Иногда GIF без кадров — игнорируем
                    }
                }
            }
        }




        // ======================== ПУСК ВОЛНЫ =========================
        private static void StartWave(Button btn)
        {
            var wave = btn.Template.FindName("WavePulse", btn) as FrameworkElement;
            if (wave == null) return;

            var st = wave.RenderTransform as ScaleTransform;
            if (st == null)
            {
                st = new ScaleTransform(1, 1);
                wave.RenderTransform = st;
            }

            wave.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0.6, 0, TimeSpan.FromSeconds(1))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });

            st.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1, 4, TimeSpan.FromSeconds(1))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });

            st.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1, 4, TimeSpan.FromSeconds(1))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
        }


        // ======================== ОСТАНОВ ВОЛНЫ ======================
        private static void StopWave(Button btn)
        {
            var wave = btn.Template.FindName("WavePulse", btn) as FrameworkElement;
            if (wave == null) return;

            wave.BeginAnimation(UIElement.OpacityProperty, null);

            if (wave.RenderTransform is ScaleTransform st)
            {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                st.ScaleX = 1;
                st.ScaleY = 1;
            }
        }


        // ========================== CLICK ============================
        private static void Btn_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }


        // ========================= SOUNDS ============================
        private static readonly MediaPlayer hoverPlayer = new MediaPlayer();
        private static readonly MediaPlayer clickPlayer = new MediaPlayer();

        private static bool hoverLoaded = false;
        private static bool clickLoaded = false;


        private static void PlayHoverSound()
        {
            if (!hoverLoaded)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "socket.mp3");
                if (File.Exists(path))
                {
                    hoverPlayer.Open(new Uri(path, UriKind.Absolute));
                    hoverLoaded = true;
                }
            }

            hoverPlayer.Position = TimeSpan.Zero;
            hoverPlayer.Play();
        }

        private static void PlayClickSound()
        {
            if (!clickLoaded)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "rezkoe.mp3");
                if (File.Exists(path))
                {
                    clickPlayer.Open(new Uri(path, UriKind.Absolute));
                    clickLoaded = true;
                }
            }

            clickPlayer.Position = TimeSpan.Zero;
            clickPlayer.Play();
        }
    }
}
