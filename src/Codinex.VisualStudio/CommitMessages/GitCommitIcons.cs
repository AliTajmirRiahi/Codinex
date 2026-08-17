using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Codinex.VisualStudio.CommitMessages
{
    /// <summary>
    /// Builds small themed icon elements from the paths of
    /// src/Codinex.UI/ToolWindows/Resources/Icons/{magic-wand-sparkles,check-compact}.svg,
    /// scaled to fit a Viewbox so they follow the host button's foreground brush.
    /// </summary>
    internal static class GitCommitIcons
    {
        private const string WandSparklesDim =
            "M32 192C32 198.4 35.8 204.2 41.7 206.7L91.8 228.2L113.3 278.3C115.8 284.2 121.6 288 128 288C134.4 288 140.2 284.2 142.7 278.3L164.2 228.2L214.3 206.7C220.2 204.2 224 198.4 224 192C224 185.6 220.2 179.8 214.3 177.3L164.2 155.8L142.7 105.7C140.2 99.8 134.4 96 128 96C121.6 96 115.8 99.8 113.3 105.7L91.8 155.8L41.7 177.3C35.8 179.8 32 185.6 32 192zM224 96C224 99.3 226 102.2 229 103.4L265.8 118.2L280.6 155C281.8 158 284.8 160 288 160C291.2 160 294.2 158 295.4 155L310.2 118.2L347 103.4C350 102.2 352 99.2 352 96C352 92.8 350 89.8 347 88.6L310.2 73.8L295.4 37C294.2 34 291.2 32 288 32C284.8 32 281.8 34 280.6 37L265.8 73.8L229 88.6C226 89.8 224 92.8 224 96zM357.4 181.4C391.1 215.1 424.9 248.9 458.7 282.7C493.8 247.6 528.8 212.6 563.9 177.5C571.7 169.7 576.1 159.1 576.1 148C576.1 136.9 571.7 126.4 563.9 118.5L521.5 76.2C513.6 68.4 503 64 492 64C481 64 470.4 68.4 462.5 76.2C427.4 111.3 392.4 146.3 357.3 181.4zM400 464C400 470.4 403.8 476.2 409.7 478.7L459.8 500.2L481.3 550.3C483.8 556.2 489.6 560 496 560C502.4 560 508.2 556.2 510.7 550.3L532.2 500.2L582.3 478.7C588.2 476.2 592 470.4 592 464C592 457.6 588.2 451.8 582.3 449.3L532.2 427.8L510.7 377.7C508.2 371.8 502.4 368 496 368C489.6 368 483.8 371.8 481.3 377.7L459.8 427.8L409.7 449.3C403.8 451.8 400 457.6 400 464z";

        private const string WandSparklesMain =
            "M458.6 282.6L357.4 181.4L76.2 462.5C68.4 470.4 64 481 64 492C64 503 68.4 513.6 76.2 521.5L118.5 563.8C126.4 571.6 137 576 148 576C159 576 169.6 571.6 177.5 563.8L458.6 282.6z";

        private const string CheckCompact =
            "M4.52 8.99C4.39 8.99 4.27 8.94 4.17 8.85L1.15 5.86C0.95 5.67 0.95 5.35 1.15 5.15C1.34 4.95 1.66 4.95 1.86 5.15L4.53 7.79L10.15 2.15C10.35 1.95 10.66 1.95 10.86 2.15C11.06 2.34 11.06 2.66 10.86 2.86L4.89 8.85C4.79 8.95 4.66 9 4.54 9L4.52 8.99Z";

        public static Viewbox CreateWandSparkles(double size = 14)
        {
            var canvas = new Canvas { Width = 640, Height = 640 };

            var dim = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(WandSparklesDim),
                Opacity = 0.4
            };
            dim.SetBinding(System.Windows.Shapes.Shape.FillProperty, GetForegroundBinding());

            var main = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(WandSparklesMain)
            };
            main.SetBinding(System.Windows.Shapes.Shape.FillProperty, GetForegroundBinding());

            canvas.Children.Add(dim);
            canvas.Children.Add(main);

            return new Viewbox
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Child = canvas,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static Viewbox CreateCheckCompact(double size = 12)
        {
            var canvas = new Canvas { Width = 12, Height = 12 };

            var check = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(CheckCompact)
            };
            check.SetBinding(System.Windows.Shapes.Shape.FillProperty, GetForegroundBinding());

            canvas.Children.Add(check);

            return new Viewbox
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Child = canvas,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static System.Windows.Data.Binding GetForegroundBinding()
        {
            return new System.Windows.Data.Binding(nameof(Control.Foreground))
            {
                RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Control), 1)
            };
        }
    }
}
