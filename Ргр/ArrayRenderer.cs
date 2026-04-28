using System;
using System.Drawing;
using System.Windows.Forms;

namespace РГР
{
    public static class ArrayRenderer
    {
        private static readonly Color DefaultColor = Color.LightSkyBlue;
        private static readonly Color SortedColor = Color.LightGreen;
        private const int SquareSize = 35;
        private const int Spacing = 3;

        public static void Draw(Graphics g, SortingContext ctx, Size canvasSize)
        {
            if (ctx.Array == null || ctx.Array.Length == 0) return;

            int totalWidth = ctx.Array.Length * (SquareSize + Spacing) - Spacing;
            int startX = Math.Max(10, (canvasSize.Width - totalWidth) / 2);
            int startY = (canvasSize.Height - SquareSize) / 2;

            // Отрисовка основного массива
            for (int i = 0; i < ctx.Array.Length; i++)
            {
                if ((ctx.ExternalElementIndex1 == i && ctx.IsFlying1) ||
                    (ctx.ExternalElementIndex2 == i && ctx.IsFlying2))
                    continue;
                if (ctx.ExternalElementIndex1 == i && ctx.ExternalElement1.HasValue) continue;
                if (ctx.ExternalElementIndex2 == i && ctx.ExternalElement2.HasValue) continue;

                int x = startX + i * (SquareSize + Spacing);
                int y = startY;
                Color backColor = ctx.IsSorted[i] ? SortedColor : DefaultColor;
                using (var brush = new SolidBrush(backColor))
                    g.FillRectangle(brush, x, y, SquareSize, SquareSize);
                g.DrawRectangle(Pens.Black, x, y, SquareSize, SquareSize);

                string text = ctx.Array[i].ToString();
                using (var font = new Font("Arial", 9, FontStyle.Bold))
                {
                    var textSize = g.MeasureString(text, font);
                    float textX = x + (SquareSize - textSize.Width) / 2;
                    float textY = y + (SquareSize - textSize.Height) / 2;
                    g.DrawString(text, font, Brushes.Black, textX, textY);
                }
            }

            // Летящие элементы
            if (ctx.IsFlying1)
                DrawElement(g, ctx.FlyX1, ctx.FlyY1, ctx.FlyingValue1.ToString(), DefaultColor);
            if (ctx.IsFlying2)
                DrawElement(g, ctx.FlyX2, ctx.FlyY2, ctx.FlyingValue2.ToString(), DefaultColor);

            // Панель сравнения (только когда не в полёте)
            if (!ctx.IsFlying1 && !ctx.IsFlying2 && ctx.ExternalElement1.HasValue)
            {
                if (ctx.ExternalElement2.HasValue && ctx.ExternalElementIndex2.HasValue)
                {
                    var (x1, y1, x2, y2) = GetComparisonTargetPosition(ctx, ctx.ExternalElementIndex1.Value, ctx.ExternalElementIndex2.Value, canvasSize);
                    DrawElement(g, x1, y1, ctx.ExternalElement1.Value.ToString(), DefaultColor);

                    int centerX = (x1 + x2) / 2 + SquareSize / 2;
                    int centerY = y1 + SquareSize / 2;
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
                    var (x, y) = GetElementScreenPosition(ctx, ctx.ExternalElementIndex1.Value, canvasSize);
                    int targetY = y - 70;
                    DrawElement(g, x, targetY, ctx.ExternalElement1.Value.ToString(), DefaultColor);
                }
            }
        }

        private static void DrawElement(Graphics g, float x, float y, string text, Color color)
        {
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, x, y, SquareSize, SquareSize);
            g.DrawRectangle(Pens.Black, x, y, SquareSize, SquareSize);
            using (var font = new Font("Arial", 9, FontStyle.Bold))
            {
                var textSize = g.MeasureString(text, font);
                float textX = x + (SquareSize - textSize.Width) / 2;
                float textY = y + (SquareSize - textSize.Height) / 2;
                g.DrawString(text, font, Brushes.Black, textX, textY);
            }
        }

        // Вспомогательные методы геометрии (дублируются из SortingContext для независимости отрисовки)
        private static (int x, int y) GetElementScreenPosition(SortingContext ctx, int index, Size canvasSize)
        {
            int totalWidth = ctx.Array.Length * (SquareSize + Spacing) - Spacing;
            int startX = Math.Max(10, (canvasSize.Width - totalWidth) / 2);
            int startY = (canvasSize.Height - SquareSize) / 2;
            int x = startX + index * (SquareSize + Spacing);
            return (x, startY);
        }

        private static (int x1, int y1, int x2, int y2) GetComparisonTargetPosition(
            SortingContext ctx, int index1, int index2, Size canvasSize)
        {
            var (elemX1, elemY1) = GetElementScreenPosition(ctx, index1, canvasSize);
            var (elemX2, elemY2) = GetElementScreenPosition(ctx, index2, canvasSize);
            float centerX1 = elemX1 + SquareSize / 2f;
            float centerX2 = elemX2 + SquareSize / 2f;
            float midX = (centerX1 + centerX2) / 2f;
            float halfGap = 40f;
            float targetCenterX1 = midX - halfGap;
            float targetCenterX2 = midX + halfGap;
            int x1 = (int)(targetCenterX1 - SquareSize / 2f);
            int x2 = (int)(targetCenterX2 - SquareSize / 2f);
            int y1 = elemY1 - 70;
            int y2 = elemY2 - 70;
            x1 = Math.Max(5, x1);
            x2 = Math.Min(canvasSize.Width - SquareSize - 5, x2);
            return (x1, y1, x2, y2);
        }
    }
}
