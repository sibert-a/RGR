using System;
using System.Drawing;
using РГР.SortingCore;

namespace РГР.Render
{
    public static class ArrayRenderer
    {
        private static readonly Color DefaultColor = Color.LightSkyBlue;
        private static readonly Color SortedColor = Color.LightGreen;

        public static void Draw(Graphics g, SortingContext ctx, Size canvasSize)
        {
            if (ctx.Array == null || ctx.Array.Length == 0) return;
            int n = ctx.Array.Length;

            // Основной массив
            for (int i = 0; i < n; i++)
            {
                if (ctx.ExternalElementIndex1 == i && ctx.IsFlying1 ||
                    ctx.ExternalElementIndex2 == i && ctx.IsFlying2)
                    continue;
                if (ctx.ExternalElementIndex1 == i && ctx.ExternalElement1.HasValue) continue;
                if (ctx.ExternalElementIndex2 == i && ctx.ExternalElement2.HasValue) continue;

                var (x, y) = GeometryHelper.GetElementScreenPosition(n, canvasSize, i);
                Color backColor = ctx.IsSorted[i] ? SortedColor : DefaultColor;
                using (var brush = new SolidBrush(backColor))
                    g.FillRectangle(brush, x, y, GeometryHelper.SquareSize, GeometryHelper.SquareSize);
                g.DrawRectangle(Pens.Black, x, y, GeometryHelper.SquareSize, GeometryHelper.SquareSize);

                string text = ctx.Array[i].ToString();
                using (var font = new Font("Arial", 9, FontStyle.Bold))
                {
                    var textSize = g.MeasureString(text, font);
                    float textX = x + (GeometryHelper.SquareSize - textSize.Width) / 2;
                    float textY = y + (GeometryHelper.SquareSize - textSize.Height) / 2;
                    g.DrawString(text, font, Brushes.Black, textX, textY);
                }
            }

            // Летящие элементы
            if (ctx.IsFlying1)
                DrawElement(g, ctx.FlyX1, ctx.FlyY1, ctx.FlyingValue1.ToString(), DefaultColor);
            if (ctx.IsFlying2)
                DrawElement(g, ctx.FlyX2, ctx.FlyY2, ctx.FlyingValue2.ToString(), DefaultColor);

            // Панель сравнения
            if (!ctx.IsFlying1 && !ctx.IsFlying2 && ctx.ExternalElement1.HasValue)
            {
                if (ctx.ExternalElement2.HasValue && ctx.ExternalElementIndex2.HasValue)
                {
                    var (x1, y1, x2, y2) = GeometryHelper.GetComparisonTargetPosition(
                        n, canvasSize,
                        ctx.ExternalElementIndex1.Value,
                        ctx.ExternalElementIndex2.Value);

                    DrawElement(g, x1, y1, ctx.ExternalElement1.Value.ToString(), DefaultColor);

                    int centerX = (x1 + x2) / 2 + GeometryHelper.SquareSize / 2;
                    int centerY = y1 + GeometryHelper.SquareSize / 2;
                    using (var font = new Font("Arial", 18, FontStyle.Bold))
                    {
                        var signSize = g.MeasureString(ctx.ComparisonSign, font);
                        float signX = centerX - signSize.Width / 2;
                        float signY = centerY - signSize.Height / 2;
                        g.DrawString(ctx.ComparisonSign, font, Brushes.DarkRed, signX, signY);
                    }
                    DrawElement(g, x2, y2, ctx.ExternalElement2.Value.ToString(), DefaultColor);
                }
                else if (ctx.ExternalElementIndex1.HasValue)
                {
                    var (x, y) = GeometryHelper.GetElementScreenPosition(n, canvasSize, ctx.ExternalElementIndex1.Value);
                    int targetY = y - GeometryHelper.VerticalComparisonOffset;
                    DrawElement(g, x, targetY, ctx.ExternalElement1.Value.ToString(), DefaultColor);
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
                var textSize = g.MeasureString(text, font);
                float textX = x + (GeometryHelper.SquareSize - textSize.Width) / 2;
                float textY = y + (GeometryHelper.SquareSize - textSize.Height) / 2;
                g.DrawString(text, font, Brushes.Black, textX, textY);
            }
        }
    }
}