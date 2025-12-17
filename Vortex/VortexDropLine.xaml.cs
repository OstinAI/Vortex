using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Vortex
{
    public partial class VortexDropLine : UserControl
    {
        private Storyboard drop;
        private DispatcherTimer timer;

        public VortexDropLine()
        {
            InitializeComponent();

            drop = (Storyboard)Resources["DropRunStoryboard"];

            Loaded += (s, e) => Start();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) => Start();
            timer.Start();
        }

        private void Start()
        {
            drop.Stop();

            Canvas.SetLeft(GlowDrop1, 0);
            Canvas.SetLeft(GlowOuter1, 0);

            Canvas.SetLeft(GlowDrop2, 0);
            Canvas.SetLeft(GlowOuter2, 0);

            Canvas.SetLeft(GlowDrop3, 0);
            Canvas.SetLeft(GlowOuter3, 0);

            Canvas.SetLeft(GlowDrop4, 0);
            Canvas.SetLeft(GlowOuter4, 0);

            drop.Begin();
        }
    }
}
