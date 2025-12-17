using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Vortex
{
    public class WindowManager
    {
        private const double SIDE_PANEL_TOP = 90;
        private const double SIDE_PANEL_RIGHT = 30;
        private const double SIDE_PANEL_BOTTOM = 20;

        private readonly Window _main;
        private readonly List<IChildWindow> _children = new List<IChildWindow>();

        public WindowManager(Window main)
        {
            _main = main;

            _main.SizeChanged += (s, e) => UpdateAll();
            _main.LocationChanged += (s, e) => UpdateAll();
            _main.StateChanged += (s, e) =>
            {
                if (_main.WindowState != WindowState.Minimized)
                    UpdateAll();
            };
        }

        // ============================================================
        //  РЕГИСТРАЦИЯ ДОЧЕРНИХ ОКОН
        // ============================================================
        public void Register(IChildWindow window)
        {
            if (!_children.Contains(window))
                _children.Add(window);

            UpdateAll();
        }

        // ============================================================
        //  ОБНОВЛЕНИЕ ПОЗИЦИЙ ВСЕХ ДОЧЕРНИХ ОКОН
        // ============================================================
        public void UpdateAll()
        {
            if (_main.WindowState == WindowState.Minimized)
                return;

            Rect wa = GetMainWorkArea();

            for (int i = 0; i < _children.Count; i++)
            {
                IChildWindow child = _children[i];
                Window win = child as Window;
                if (win == null || !win.IsVisible)
                    continue;

                PositionWindow(win, child, wa);
            }
        }

        // ============================================================
        //  ИНТЕРФЕЙС ДОЧЕРНЕГО ОКНА
        // ============================================================
        public interface IChildWindow
        {
            ChildWindowPosition Position { get; }
        }

        public enum ChildWindowPosition
        {
            RightPanel,
            LeftPanel,
            BottomPanel,
            Floating
        }

        // ============================================================
        //  ПОКАЗ / ЗАКРЫТИЕ ОКОН
        // ============================================================
        public void ShowWindow(Window w)
        {
            AttachGlobalHotkeys(w);

            w.WindowStartupLocation = WindowStartupLocation.Manual;
            w.Opacity = 0;

            IChildWindow child = w as IChildWindow;
            if (child != null)
            {
                Rect wa = GetMainWorkArea();
                PositionWindow(w, child, wa);
            }

            w.Show();
            w.Opacity = 1;
        }

        public void CloseWindow(Window w)
        {
            if (w != null) w.Close();
        }

        // Совместимость со старым кодом
        public void ShowAnimatedWindow(Window w) { ShowWindow(w); }
        public void CloseAnimatedWindow(Window w) { CloseWindow(w); }

        // ============================================================
        //  ГЛОБАЛЬНЫЕ ХОТКЕИ
        // ============================================================
        public void AttachGlobalHotkeys(Window w)
        {
            w.KeyDown += (s, e) =>
            {
                if (e.Key == Key.F1)
                {
                    _main.WindowState = WindowState.Minimized;
                    e.Handled = true;
                }
                else if (e.Key == Key.F11)
                {
                    ToggleHalfScreen(_main);
                    e.Handled = true;
                }
            };
        }

        public void ToggleHalfScreen(Window w)
        {
            Rect wa = GetMainWorkArea();

            if (w.WindowState == WindowState.Maximized)
            {
                w.WindowState = WindowState.Normal;
                w.WindowStyle = WindowStyle.SingleBorderWindow;
                w.ResizeMode = ResizeMode.CanResize;

                w.Width = wa.Width / 2;
                w.Height = wa.Height;

                w.Left = wa.Right - w.Width;
                w.Top = wa.Top;
            }
            else
            {
                w.WindowStyle = WindowStyle.None;
                w.ResizeMode = ResizeMode.NoResize;
                w.WindowState = WindowState.Maximized;
            }

            UpdateAll();
        }

        // ============================================================
        //  ПОЗИЦИОНИРОВАНИЕ
        // ============================================================
        private void PositionWindow(Window win, IChildWindow child, Rect wa)
        {
            switch (child.Position)
            {
                case ChildWindowPosition.RightPanel:
                    {
                        double maxWidth = wa.Width - SIDE_PANEL_RIGHT;
                        double maxHeight = wa.Height - SIDE_PANEL_TOP - SIDE_PANEL_BOTTOM;

                        double targetW = Math.Min(win.MaxWidth > 0 ? win.MaxWidth : maxWidth, maxWidth);
                        double targetH = Math.Min(win.MaxHeight > 0 ? win.MaxHeight : maxHeight, maxHeight);

                        // важно: не меньше Min
                        win.Width = Math.Max(win.MinWidth, targetW);
                        win.Height = Math.Max(win.MinHeight, targetH);

                        win.Left = Px(wa.Left + wa.Width - win.Width - SIDE_PANEL_RIGHT);
                        win.Top = Px(wa.Top + SIDE_PANEL_TOP);
                        break;
                    }

                case ChildWindowPosition.LeftPanel:
                    win.Left = Px(wa.Left);
                    win.Top = Px(wa.Top + SIDE_PANEL_TOP);
                    break;

                case ChildWindowPosition.BottomPanel:
                    win.Left = Px(wa.Left + wa.Width / 2 - win.Width / 2);
                    win.Top = Px(wa.Top + wa.Height - win.Height - SIDE_PANEL_BOTTOM);
                    break;

                case ChildWindowPosition.Floating:
                    break;
            }
        }

        private static double Px(double v) { return Math.Round(v); }

        // ============================================================
        //  WORKAREA ИМЕННО ТОГО МОНИТОРА, ГДЕ MAIN WINDOW
        // ============================================================
        private Rect GetMainWorkArea()
        {
            IntPtr hwnd = new WindowInteropHelper(_main).Handle;

            // если ещё не создан Handle (редко), fallback
            if (hwnd == IntPtr.Zero)
                return SystemParameters.WorkArea;

            IntPtr hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if (!GetMonitorInfo(hMon, ref mi))
                return SystemParameters.WorkArea;

            // rcWork = рабочая область без панели задач
            int left = mi.rcWork.Left;
            int top = mi.rcWork.Top;
            int width = mi.rcWork.Right - mi.rcWork.Left;
            int height = mi.rcWork.Bottom - mi.rcWork.Top;

            return new Rect(left, top, width, height);
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
    }
}
