using System;
using System.Drawing;

namespace RGR_TIMP_S4.Render
{
    public static class GeometryHelper
    {
        public const int SquareSize = 35;
        public const int Spacing = 3;
        public const int VerticalComparisonOffset = 70;
        public const int TempRowVerticalOffset = 2 * VerticalComparisonOffset; // 140
        public const float ComparisonHalfGap = 40f;

        public static (int x, int y) GetElementScreenPosition(int arrayLength, Size canvasSize, int index, int yOffset = 0)
        {
            int totalWidth = arrayLength * (SquareSize + Spacing) - Spacing;
            int startX = Math.Max(10, (canvasSize.Width - totalWidth) / 2);
            int startY = (canvasSize.Height - SquareSize) / 2;
            int x = startX + index * (SquareSize + Spacing);
            return (x, startY + yOffset);
        }

        public static (int x1, int y1, int x2, int y2) GetComparisonTargetPosition(
            int arrayLength, Size canvasSize, int index1, int index2)
        {
            var (elemX1, elemY1) = GetElementScreenPosition(arrayLength, canvasSize, index1);
            var (elemX2, elemY2) = GetElementScreenPosition(arrayLength, canvasSize, index2);
            float centerX1 = elemX1 + SquareSize / 2f;
            float centerX2 = elemX2 + SquareSize / 2f;
            float midX = (centerX1 + centerX2) / 2f;
            float targetCenterX1 = midX - ComparisonHalfGap;
            float targetCenterX2 = midX + ComparisonHalfGap;
            int x1 = (int)(targetCenterX1 - SquareSize / 2f);
            int x2 = (int)(targetCenterX2 - SquareSize / 2f);
            int y1 = elemY1 - VerticalComparisonOffset;
            int y2 = elemY2 - VerticalComparisonOffset;
            x1 = Math.Max(5, x1);
            x2 = Math.Min(canvasSize.Width - SquareSize - 5, x2);
            return (x1, y1, x2, y2);
        }
    }
}