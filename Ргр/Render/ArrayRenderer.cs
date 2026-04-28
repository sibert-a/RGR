using System.Drawing;
using System.Linq;

namespace RGR_TIMP_S4.Render
{
    public static class ArrayRenderer
    {
        public static void Draw(Graphics g, Scene scene)
        {
            if (scene == null || scene.Elements == null) return;

            // Все видимые элементы
            foreach (var elem in scene.Elements.Where(e => e.IsVisible))
                DrawElement(g, elem.X, elem.Y, elem.Value.ToString(), elem.BackgroundColor);

            // Знак сравнения
            if (scene.Comparison != null)
            {
                var (left, right, sign) = scene.Comparison.Value;
                float centerX1 = left.X + GeometryHelper.SquareSize / 2f;
                float centerY1 = left.Y + GeometryHelper.SquareSize / 2f;
                float centerX2 = right.X + GeometryHelper.SquareSize / 2f;
                float centerY2 = right.Y + GeometryHelper.SquareSize / 2f;
                float signX = (centerX1 + centerX2) / 2f;
                float signY = (centerY1 + centerY2) / 2f;
                using (var font = new Font("Arial", 18, FontStyle.Bold))
                {
                    var sz = g.MeasureString(sign, font);
                    g.DrawString(sign, font, Brushes.DarkRed, signX - sz.Width / 2, signY - sz.Height / 2);
                }
            }
        }

        private static void DrawElement(Graphics g, float x, float y, string text, Color color)
        {
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, x, y, GeometryHelper.SquareSize, GeometryHelper.SquareSize);
            g.DrawRectangle(Pens.Black, x, y, GeometryHelper.SquareSize, GeometryHelper.SquareSize);
            using (var font = new Font("Arial", 9, FontStyle.Bold))
            {
                var sz = g.MeasureString(text, font);
                float tx = x + (GeometryHelper.SquareSize - sz.Width) / 2;
                float ty = y + (GeometryHelper.SquareSize - sz.Height) / 2;
                g.DrawString(text, font, Brushes.Black, tx, ty);
            }
        }
    }
}