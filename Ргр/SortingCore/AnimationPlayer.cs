using RGR_TIMP_S4.Render;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RGR_TIMP_S4.SortingCore
{
    internal class AnimationPlayer
    {
        private const int VerticalSteps = 16;
        private const int HorizontalStepsBase = 20;
        private const int SwapSteps = 40;
        private const int FrameDelayMs = 5;

        private readonly Panel canvas;
        private readonly SortingContext ctx;
        private Scene scene => ctx.Scene;

        private VisualElement currentFlyingSingle = null;
        public (int x, int y) GetPositionOnCanvas(int index) =>
            GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, index);

        public AnimationPlayer(Panel canvas, SortingContext ctx)
        {
            this.canvas = canvas;
            this.ctx = ctx;
        }

        public void InvalidateCanvas() => canvas.Invalidate();
        public Size GetCanvasSize() => canvas.ClientSize;

        // Перестроить сцену по текущему массиву
        public void InitializeScene(int[] array)
        {
            scene.Clear();
            var sz = canvas.ClientSize;
            for (int i = 0; i < array.Length; i++)
            {
                var (x, y) = GeometryHelper.GetElementScreenPosition(array.Length, sz, i);
                scene.Elements.Add(new VisualElement
                {
                    Value = array[i],
                    X = x,
                    Y = y,
                    ArrayIndex = i,
                    BackgroundColor = ctx.IsSorted[i] ? Color.LightGreen : Color.LightSkyBlue
                });
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные анимации движения
        // ----------------------------------------------------------------
        private async Task AnimateLiftTwo(VisualElement e1, VisualElement e2, float toY1, float toY2)
        {
            float sY1 = e1.Y, sY2 = e2.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float e = 1 - (float)Math.Pow(1 - t, 2);
                e1.Y = sY1 + (toY1 - sY1) * e;
                e2.Y = sY2 + (toY2 - sY2) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateApproachTwo(VisualElement e1, VisualElement e2, float toX1, float toX2)
        {
            float sX1 = e1.X, sX2 = e2.X;
            int steps = HorizontalStepsBase;
            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float e = 1 - (1 - t) * (1 - t);
                e1.X = sX1 + (toX1 - sX1) * e;
                e2.X = sX2 + (toX2 - sX2) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateLandTwo(VisualElement e1, VisualElement e2, float toY1, float toY2)
        {
            float sY1 = e1.Y, sY2 = e2.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float e = t * t;
                e1.Y = sY1 + (toY1 - sY1) * e;
                e2.Y = sY2 + (toY2 - sY2) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateLiftSingle(VisualElement el, float toY)
        {
            float sY = el.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float e = 1 - (float)Math.Pow(1 - t, 2);
                el.Y = sY + (toY - sY) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateLandSingle(VisualElement el, float toY)
        {
            float sY = el.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float e = t * t;
                el.Y = sY + (toY - sY) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateMoveTo(VisualElement el, float toX, float toY)
        {
            float sX = el.X, sY = el.Y;
            int steps = HorizontalStepsBase;
            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float e = 1 - (1 - t) * (1 - t);
                el.X = sX + (toX - sX) * e;
                el.Y = sY + (toY - sY) * e;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            el.X = toX;
            el.Y = toY;
            InvalidateCanvas();
        }

        // ----------------------------------------------------------------
        // Публичные методы для обычных алгоритмов
        // ----------------------------------------------------------------
        public async Task FlyToComparisonAsync(int index1, int index2)
        {
            var orig1 = scene.Elements.First(e => e.ArrayIndex == index1 && !e.IsTemporary);
            var orig2 = scene.Elements.First(e => e.ArrayIndex == index2 && !e.IsTemporary);
            orig1.IsVisible = false;
            orig2.IsVisible = false;

            var fly1 = new VisualElement
            {
                Value = orig1.Value, X = orig1.X, Y = orig1.Y, IsVisible = true,
                BackgroundColor = Color.LightSkyBlue, IsTemporary = true, TargetArrayIndex = index1
            };
            var fly2 = new VisualElement
            {
                Value = orig2.Value, X = orig2.X, Y = orig2.Y, IsVisible = true,
                BackgroundColor = Color.LightSkyBlue, IsTemporary = true, TargetArrayIndex = index2
            };
            scene.Elements.Add(fly1);
            scene.Elements.Add(fly2);

            var (tx1, ty1, tx2, ty2) = GeometryHelper.GetComparisonTargetPosition(
                ctx.Array.Length, canvas.ClientSize, index1, index2);

            await AnimateLiftTwo(fly1, fly2, ty1, ty2);
            await AnimateApproachTwo(fly1, fly2, tx1, tx2);

            scene.Comparison = (fly1, fly2, "");
            InvalidateCanvas();
        }

        public async Task FlyBackAsync()
        {
            if (scene.Comparison == null) return;
            var (fly1, fly2, _) = scene.Comparison.Value;
            int idx1 = fly1.TargetArrayIndex.Value;
            int idx2 = fly2.TargetArrayIndex.Value;

            var orig1 = scene.Elements.First(e => e.ArrayIndex == idx1 && !e.IsTemporary);
            var orig2 = scene.Elements.First(e => e.ArrayIndex == idx2 && !e.IsTemporary);

            await AnimateApproachTwo(fly1, fly2, orig1.X, orig2.X);
            await AnimateLandTwo(fly1, fly2, orig1.Y, orig2.Y);

            // Обновляем значения основных элементов
            orig1.Value = fly1.Value;
            orig2.Value = fly2.Value;
            scene.Elements.Remove(fly1);
            scene.Elements.Remove(fly2);
            orig1.IsVisible = true;
            orig2.IsVisible = true;
            scene.Comparison = null;
            InvalidateCanvas();
        }

        public async Task SwapOnTopAsync(int index1, int index2)
        {
            if (scene.Comparison == null) return;
            var (fly1, fly2, _) = scene.Comparison.Value;
            if (fly1.TargetArrayIndex != index1 || fly2.TargetArrayIndex != index2) return;

            float fx1 = fly1.X, fy1 = fly1.Y;
            float fx2 = fly2.X, fy2 = fly2.Y;

            for (int st = 0; st <= SwapSteps; st++)
            {
                float t = (float)st / SwapSteps;
                fly1.X = fx1 + (fx2 - fx1) * t;
                fly1.Y = fy1 + (fy2 - fy1) * t;
                fly2.X = fx2 + (fx1 - fx2) * t;
                fly2.Y = fy2 + (fy1 - fy2) * t;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }

            await Task.Delay(200); //todo
            fly1.Value = ctx.Array[index1];
            fly2.Value = ctx.Array[index2];
            InvalidateCanvas();
        }

        public async Task SingleFlyToComparisonAsync(int index)
        {
            if (currentFlyingSingle != null) await FlyBackSingleAsync();

            var orig = scene.Elements.First(e => e.ArrayIndex == index && !e.IsTemporary);
            orig.IsVisible = false;
            var fly = new VisualElement
            {
                Value = orig.Value, X = orig.X, Y = orig.Y, IsVisible = true,
                BackgroundColor = Color.LightSkyBlue, IsTemporary = true, TargetArrayIndex = index
            };
            scene.Elements.Add(fly);
            float targetY = orig.Y - GeometryHelper.VerticalComparisonOffset;
            await AnimateLiftSingle(fly, targetY);
            currentFlyingSingle = fly;
            InvalidateCanvas();
        }

        public async Task FlyBackSingleAsync()
        {
            if (currentFlyingSingle == null) return;
            var fly = currentFlyingSingle;
            int idx = fly.TargetArrayIndex.Value;
            var orig = scene.Elements.First(e => e.ArrayIndex == idx && !e.IsTemporary);
            float homeY = orig.Y;
            await AnimateLandSingle(fly, homeY);
            orig.Value = fly.Value;
            scene.Elements.Remove(fly);
            orig.IsVisible = true;
            currentFlyingSingle = null;
            InvalidateCanvas();
        }

        // ----------------------------------------------------------------
        // Операции для слияния
        // ----------------------------------------------------------------
        public async Task BeginMergeVisualAsync(int left, int mid, int right, CancellationToken token)
        {
            ctx.IsMergeActive = true;
            // Скрыть основные элементы в диапазоне
            for (int i = left; i <= right; i++)
            {
                var elem = scene.Elements.First(e => e.ArrayIndex == i && !e.IsTemporary);
                elem.IsVisible = false;
            }

            ctx.MergeTempLeft.Clear();
            ctx.MergeTempRight.Clear();

            // Левая часть
            for (int i = left; i <= mid; i++)
            {
                var pos = GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, i);
                int tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var temp = new VisualElement
                {
                    Value = ctx.Array[i], X = pos.x, Y = pos.y, IsVisible = true,
                    BackgroundColor = Color.FromArgb(200, 200, 255), IsTemporary = true, ArrayIndex = i
                };
                scene.Elements.Add(temp);
                ctx.MergeTempLeft.Add(temp);
                await AnimateLiftSingle(temp, tempY);
                temp.Y = tempY;
                InvalidateCanvas();
                await Task.Delay(20, token);
            }
            // Правая часть
            for (int i = mid + 1; i <= right; i++)
            {
                var pos = GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, i);
                int tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var temp = new VisualElement
                {
                    Value = ctx.Array[i], X = pos.x, Y = pos.y, IsVisible = true,
                    BackgroundColor = Color.FromArgb(255, 200, 200), IsTemporary = true, ArrayIndex = i
                };
                scene.Elements.Add(temp);
                ctx.MergeTempRight.Add(temp);
                await AnimateLiftSingle(temp, tempY);
                temp.Y = tempY;
                InvalidateCanvas();
                await Task.Delay(20, token);
            }
        }

        public async Task AnimateTempToSlotAsync(VisualElement info, int slotX, int slotY)
        {
            await AnimateMoveTo(info, slotX, slotY);
        }

        public async Task AnimateSlotToMainAsync(int slotX, int slotY, int targetIndex, VisualElement info, CancellationToken token)
        {
            var targetPos = GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, targetIndex);
            await AnimateMoveTo(info, targetPos.x, targetPos.y);

            // Обновляем основной визуальный элемент
            var main = scene.Elements.First(e => e.ArrayIndex == targetIndex && !e.IsTemporary);
            main.Value = info.Value;
            main.IsVisible = true;
            ctx.SetElement(targetIndex, info.Value);
            ctx.MarkSorted(targetIndex);
            scene.Elements.Remove(info);
            InvalidateCanvas();
            await ctx.DelayAsync(token);
        }

        public async Task MoveRemainingTempElementAsync(VisualElement info, int targetIndex, CancellationToken token)
        {
            var targetPos = GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, targetIndex);
            await AnimateMoveTo(info, targetPos.x, targetPos.y);

            var main = scene.Elements.First(e => e.ArrayIndex == targetIndex && !e.IsTemporary);
            main.Value = info.Value;
            main.IsVisible = true;
            ctx.SetElement(targetIndex, info.Value);
            ctx.MarkSorted(targetIndex);
            scene.Elements.Remove(info);
            InvalidateCanvas();
            await ctx.DelayAsync(token);
        }

        public void EndMergeVisual()
        {
            ctx.IsMergeActive = false;
            foreach (var tmp in ctx.MergeTempLeft.Concat(ctx.MergeTempRight))
                scene.Elements.Remove(tmp);
            ctx.MergeTempLeft.Clear();
            ctx.MergeTempRight.Clear();
            // Показать все основные элементы
            foreach (var el in scene.Elements.Where(e => !e.IsTemporary && e.ArrayIndex.HasValue))
                el.IsVisible = true;
            InvalidateCanvas();
        }
    }
}