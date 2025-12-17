using System;
using System.Windows;
using System.Windows.Media;

namespace Vortex
{
    public enum NeonSide
    {
        Bottom,
        Top,
        Left,
        Right
    }

    // РЕЖИМЫ РАБОТЫ GIF
    public enum GifMode
    {
        Always,
        OnHover,
        Static
    }

    // ДОБАВЛЕНО: НАПРАВЛЕНИЕ НАКЛОНА
    public enum SkewDirection
    {
        None,
        Left,
        Right
    }

    public class ButtonProps : DependencyObject
    {
        // GIF SOURCE
        public static readonly DependencyProperty GifSourceProperty =
            DependencyProperty.RegisterAttached(
                "GifSource",
                typeof(string),
                typeof(ButtonProps),
                new PropertyMetadata(null));

        public static void SetGifSource(DependencyObject element, string value)
            => element.SetValue(GifSourceProperty, value);

        public static string GetGifSource(DependencyObject element)
            => (string)element.GetValue(GifSourceProperty);


        // GIF MODE
        public static readonly DependencyProperty GifModeProperty =
            DependencyProperty.RegisterAttached(
                "GifMode",
                typeof(GifMode),
                typeof(ButtonProps),
                new PropertyMetadata(GifMode.OnHover));

        public static void SetGifMode(DependencyObject element, GifMode value)
            => element.SetValue(GifModeProperty, value);

        public static GifMode GetGifMode(DependencyObject element)
            => (GifMode)element.GetValue(GifModeProperty);



        // NEON SIDE
        public static readonly DependencyProperty NeonSideProperty =
            DependencyProperty.RegisterAttached(
                "NeonSide",
                typeof(NeonSide),
                typeof(ButtonProps),
                new PropertyMetadata(NeonSide.Bottom));

        public static void SetNeonSide(DependencyObject element, NeonSide value)
            => element.SetValue(NeonSideProperty, value);

        public static NeonSide GetNeonSide(DependencyObject element)
            => (NeonSide)element.GetValue(NeonSideProperty);


        // GIF WIDTH
        public static readonly DependencyProperty GifWidthProperty =
            DependencyProperty.RegisterAttached(
                "GifWidth",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(Double.NaN));

        public static void SetGifWidth(DependencyObject obj, double value)
            => obj.SetValue(GifWidthProperty, value);

        public static double GetGifWidth(DependencyObject obj)
            => (double)obj.GetValue(GifWidthProperty);


        // GIF HEIGHT
        public static readonly DependencyProperty GifHeightProperty =
            DependencyProperty.RegisterAttached(
                "GifHeight",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(Double.NaN));

        public static void SetGifHeight(DependencyObject obj, double value)
            => obj.SetValue(GifHeightProperty, value);

        public static double GetGifHeight(DependencyObject obj)
            => (double)obj.GetValue(GifHeightProperty);


        // GIF MARGIN
        public static readonly DependencyProperty GifMarginProperty =
            DependencyProperty.RegisterAttached(
                "GifMargin",
                typeof(Thickness),
                typeof(ButtonProps),
                new PropertyMetadata(new Thickness(0)));

        public static void SetGifMargin(DependencyObject obj, Thickness value)
            => obj.SetValue(GifMarginProperty, value);

        public static Thickness GetGifMargin(DependencyObject obj)
            => (Thickness)obj.GetValue(GifMarginProperty);


        // GIF OFFSET X
        public static readonly DependencyProperty GifOffsetXProperty =
            DependencyProperty.RegisterAttached(
                "GifOffsetX",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(0.0));

        public static void SetGifOffsetX(DependencyObject obj, double value)
            => obj.SetValue(GifOffsetXProperty, value);

        public static double GetGifOffsetX(DependencyObject obj)
            => (double)obj.GetValue(GifOffsetXProperty);


        // GIF OFFSET Y
        public static readonly DependencyProperty GifOffsetYProperty =
            DependencyProperty.RegisterAttached(
                "GifOffsetY",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(0.0));

        public static void SetGifOffsetY(DependencyObject obj, double value)
            => obj.SetValue(GifOffsetYProperty, value);

        public static double GetGifOffsetY(DependencyObject obj)
            => (double)obj.GetValue(GifOffsetYProperty);

        public static readonly DependencyProperty TextSourceProperty =
            DependencyProperty.RegisterAttached(
                "TextSource",
                typeof(string),
                typeof(ButtonProps),
                new PropertyMetadata(null));

        public static void SetTextSource(DependencyObject obj, string value)
            => obj.SetValue(TextSourceProperty, value);

        public static string GetTextSource(DependencyObject obj)
            => (string)obj.GetValue(TextSourceProperty);

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.RegisterAttached(
                "TextColor",
                typeof(Brush),
                typeof(ButtonProps),
                new PropertyMetadata(Brushes.White));

        public static void SetTextColor(DependencyObject obj, Brush value)
            => obj.SetValue(TextColorProperty, value);

        public static Brush GetTextColor(DependencyObject obj)
            => (Brush)obj.GetValue(TextColorProperty);


        public static readonly DependencyProperty TextSizeProperty =
            DependencyProperty.RegisterAttached(
                "TextSize",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(16.0));

        public static void SetTextSize(DependencyObject obj, double value)
            => obj.SetValue(TextSizeProperty, value);

        public static double GetTextSize(DependencyObject obj)
            => (double)obj.GetValue(TextSizeProperty);


        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.RegisterAttached(
                "TextMargin",
                typeof(Thickness),
                typeof(ButtonProps),
                new PropertyMetadata(new Thickness(80, 0, 10, 0)));

        public static void SetTextMargin(DependencyObject obj, Thickness value)
            => obj.SetValue(TextMarginProperty, value);

        public static Thickness GetTextMargin(DependencyObject obj)
            => (Thickness)obj.GetValue(TextMarginProperty);

        public static readonly DependencyProperty TransformOriginProperty =
            DependencyProperty.RegisterAttached(
                "TransformOrigin",
                typeof(Point),
                typeof(ButtonProps),
                new PropertyMetadata(new Point(0.5, 0.5))); // по умолчанию центр

        public static void SetTransformOrigin(DependencyObject obj, Point value)
            => obj.SetValue(TransformOriginProperty, value);

        public static Point GetTransformOrigin(DependencyObject obj)
            => (Point)obj.GetValue(TransformOriginProperty);

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.RegisterAttached(
                "Background",
                typeof(Brush),
                typeof(ButtonProps),
                new PropertyMetadata(null));

        public static void SetBackground(DependencyObject obj, Brush value)
            => obj.SetValue(BackgroundProperty, value);

        public static Brush GetBackground(DependencyObject obj)
            => (Brush)obj.GetValue(BackgroundProperty);

        public static readonly DependencyProperty HighlightColorProperty =
            DependencyProperty.RegisterAttached(
                "HighlightColor",
                typeof(Brush),
                typeof(ButtonProps),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(120, 0, 255, 255))));

        public static void SetHighlightColor(DependencyObject obj, Brush value)
            => obj.SetValue(HighlightColorProperty, value);

        public static Brush GetHighlightColor(DependencyObject obj)
            => (Brush)obj.GetValue(HighlightColorProperty);

        // =====================================================================================
        // ====================== ДОБАВЛЕНО: НАКЛОН ============================================
        // =====================================================================================

        public static readonly DependencyProperty SkewDirectionProperty =
            DependencyProperty.RegisterAttached(
                "SkewDirection",
                typeof(SkewDirection),
                typeof(ButtonProps),
                new PropertyMetadata(SkewDirection.None));

        public static void SetSkewDirection(DependencyObject obj, SkewDirection value)
            => obj.SetValue(SkewDirectionProperty, value);

        public static SkewDirection GetSkewDirection(DependencyObject obj)
            => (SkewDirection)obj.GetValue(SkewDirectionProperty);


        public static readonly DependencyProperty SkewAngleProperty =
            DependencyProperty.RegisterAttached(
                "SkewAngle",
                typeof(double),
                typeof(ButtonProps),
                new PropertyMetadata(16.0));

        public static void SetSkewAngle(DependencyObject obj, double value)
            => obj.SetValue(SkewAngleProperty, value);

        public static double GetSkewAngle(DependencyObject obj)
            => (double)obj.GetValue(SkewAngleProperty);

        // =====================================================================================
        // ====================== ДОБАВЛЕНО: СКРУГЛЕНИЕ ========================================
        // =====================================================================================

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(ButtonProps),
                new PropertyMetadata(new CornerRadius(0)));

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
            => obj.SetValue(CornerRadiusProperty, value);

        public static CornerRadius GetCornerRadius(DependencyObject obj)
            => (CornerRadius)obj.GetValue(CornerRadiusProperty);

        // =====================================================================================
        // ============= ДОБАВЛЕНО: чтобы ContentPresenter не ломал/не дублировал ===============
        // =====================================================================================

        public static readonly DependencyProperty ShowContentPresenterProperty =
            DependencyProperty.RegisterAttached(
                "ShowContentPresenter",
                typeof(bool),
                typeof(ButtonProps),
                new PropertyMetadata(false));

        public static void SetShowContentPresenter(DependencyObject obj, bool value)
            => obj.SetValue(ShowContentPresenterProperty, value);

        public static bool GetShowContentPresenter(DependencyObject obj)
            => (bool)obj.GetValue(ShowContentPresenterProperty);
    }
}
