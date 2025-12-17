using System.Windows;

namespace Vortex
{
    public partial class Tools : Window, WindowManager.IChildWindow
    {
        // 🔹 Позиция окна — сообщает менеджеру, ГДЕ его держать
        public WindowManager.ChildWindowPosition Position
            => WindowManager.ChildWindowPosition.RightPanel;

        public Tools(WindowManager manager, Window owner)
        {
            InitializeComponent();

            Owner = owner;

            // 🔹 регистрируем окно в менеджере
            manager.Register(this);

            // 🔹 хоткеи (F1, F11)
            manager.AttachGlobalHotkeys(this);

            // 🔹 ОТКРЫТИЕ ЧЕРЕЗ МЕНЕДЖЕР
            manager.ShowAnimatedWindow(this); // ← без анимации, если вы её отключили
        }
    }
}
