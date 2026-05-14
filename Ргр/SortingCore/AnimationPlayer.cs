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
        private const int SwapSteps = 20;
        private const int FrameDelayMs = 5;

        private readonly Panel canvas;
        private readonly SortingContext ctx;
        private Scene scene => ctx.Scene;

        private VisualElement currentFlyingSingle = null;

        public AnimationPlayer(Panel canvas, SortingContext ctx)
        {
            this.canvas = canvas;
            this.ctx = ctx;
        }

        public void InvalidateCanvas() => canvas.Invalidate();
        public Size GetCanvasSize() => canvas.ClientSize;

        // Получение позиции по индексу (всегда актуально при ресайзе)
        private (int x, int y) GetPos(int index) =>
            GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, index, ctx.ArrayYOffset);

        // Перестроить сцену по текущему массиву
        public void InitializeScene(int[] array)
        {
            scene.Clear();
            for (int i = 0; i < array.Length; i++)
            {
                var (x, y) = GetPos(i);
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

        // Синхронизировать ВСЕ элементы сцены с текущими позициями (вызывать при ресайзе)
        public void SyncAllPositions()
        {
            foreach (var el in scene.Elements.Where(e => e.ArrayIndex.HasValue && !e.IsTemporary))
            {
                var (x, y) = GetPos(el.ArrayIndex.Value);
                el.X = x;
                el.Y = y;
            }
            foreach (var el in scene.Elements.Where(e => e.IsTemporary && e.TargetArrayIndex.HasValue))
            {
                var (x, y) = GetPos(el.TargetArrayIndex.Value);
                el.X = x;
                el.Y = y;
            }
            InvalidateCanvas();
        }

        // Вспомогательные анимации движения (принимают ИНДЕКСЫ)
        private async Task AnimateLiftTwo(VisualElement e1, VisualElement e2, int targetIndex1, int targetIndex2)
        {
            float sY1 = e1.Y, sY2 = e2.Y;
            var (_, groundY1) = GetPos(targetIndex1);
            var (_, groundY2) = GetPos(targetIndex2);
            float toY1 = groundY1 - GeometryHelper.VerticalComparisonOffset;
            float toY2 = groundY2 - GeometryHelper.VerticalComparisonOffset;

            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                // Пересчитываем на каждом шаге (для ресайза)
                var (_, gy1) = GetPos(targetIndex1);
                var (_, gy2) = GetPos(targetIndex2);
                e1.Y = sY1 + ((gy1 - GeometryHelper.VerticalComparisonOffset) - sY1) * ease;
                e2.Y = sY2 + ((gy2 - GeometryHelper.VerticalComparisonOffset) - sY2) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateApproachTwo(VisualElement e1, VisualElement e2, int targetIndex1, int targetIndex2, float? customY1 = null, float? customY2 = null)
        {
            float sX1 = e1.X, sX2 = e2.X;
            float currentY1 = e1.Y, currentY2 = e2.Y;
            int steps = HorizontalStepsBase;

            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float ease = 1 - (1 - t) * (1 - t);
                var (tx1, _) = GetPos(targetIndex1);
                var (tx2, _) = GetPos(targetIndex2);
                e1.X = sX1 + (tx1 - sX1) * ease;
                e2.X = sX2 + (tx2 - sX2) * ease;
                e1.Y = customY1 ?? currentY1;
                e2.Y = customY2 ?? currentY2;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateLandTwo(VisualElement e1, VisualElement e2, int targetIndex1, int targetIndex2, float? customX1 = null, float? customX2 = null)
        {
            float sY1 = e1.Y, sY2 = e2.Y;

            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = t * t;
                var (_, ty1) = GetPos(targetIndex1);
                var (_, ty2) = GetPos(targetIndex2);
                e1.Y = sY1 + (ty1 - sY1) * ease;
                e2.Y = sY2 + (ty2 - sY2) * ease;
                if (customX1.HasValue) e1.X = customX1.Value;
                if (customX2.HasValue) e2.X = customX2.Value;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            e1.X = customX1 ?? GetPos(targetIndex1).x;
            e2.X = customX2 ?? GetPos(targetIndex2).x;
            e1.Y = GetPos(targetIndex1).y;
            e2.Y = GetPos(targetIndex2).y;
            InvalidateCanvas();
        }
        private async Task AnimateLiftVertical(VisualElement el, float toY)
        {
            float sY = el.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                el.Y = sY + (toY - sY) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }
        private async Task AnimateLiftSingle(VisualElement el, int targetIndex)
        {
            float sY = el.Y;
            var (_, groundY) = GetPos(targetIndex);
            float toY = groundY - GeometryHelper.VerticalComparisonOffset;

            for (int st = 0; st <= 2 * VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                var (tx, ty) = GetPos(targetIndex);
                el.X = tx;
                el.Y = sY + ((ty - GeometryHelper.VerticalComparisonOffset) - sY) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateLandSingle(VisualElement el, int targetIndex, float? customX = null)
        {
            float sY = el.Y;

            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = t * t;
                var (_, ty) = GetPos(targetIndex);
                el.Y = sY + (ty - sY) * ease;
                if (customX.HasValue) el.X = customX.Value;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            el.X = customX ?? GetPos(targetIndex).x;
            el.Y = GetPos(targetIndex).y;
            InvalidateCanvas();
        }

        private async Task AnimateMoveTo(VisualElement el, int targetIndex, float? customY = null)
        {
            float sX = el.X, sY = el.Y;
            int steps = HorizontalStepsBase;

            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float ease = 1 - (1 - t) * (1 - t);
                var (tx, ty) = GetPos(targetIndex);
                el.X = sX + (tx - sX) * ease;
                el.Y = sY + ((customY ?? ty) - sY) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            var (fx, fy) = GetPos(targetIndex);
            el.X = fx;
            el.Y = customY ?? fy;
            InvalidateCanvas();
        }

        private async Task AnimateLiftTwo(VisualElement e1, VisualElement e2, float toY1, float toY2)
        {
            float sY1 = e1.Y, sY2 = e2.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                e1.Y = sY1 + (toY1 - sY1) * ease;
                e2.Y = sY2 + (toY2 - sY2) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        private async Task AnimateApproachTwoToCoords(VisualElement e1, VisualElement e2, float toX1, float toX2)
        {
            float sX1 = e1.X, sX2 = e2.X;
            int steps = HorizontalStepsBase;
            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float ease = 1 - (1 - t) * (1 - t);
                e1.X = sX1 + (toX1 - sX1) * ease;
                e2.X = sX2 + (toX2 - sX2) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
        }

        // Публичные методы для обычных алгоритмов
        public async Task FlyToComparisonAsync(int index1, int index2)
        {
            var orig1 = scene.Elements.First(e => e.ArrayIndex == index1 && !e.IsTemporary);
            var orig2 = scene.Elements.First(e => e.ArrayIndex == index2 && !e.IsTemporary);
            orig1.IsVisible = false;
            orig2.IsVisible = false;

            var (ox1, oy1) = GetPos(index1);
            var (ox2, oy2) = GetPos(index2);

            var fly1 = new VisualElement
            {
                Value = orig1.Value,
                X = ox1,
                Y = oy1,
                IsVisible = true,
                BackgroundColor = Color.LightSkyBlue,
                IsTemporary = true,
                TargetArrayIndex = index1
            };
            var fly2 = new VisualElement
            {
                Value = orig2.Value,
                X = ox2,
                Y = oy2,
                IsVisible = true,
                BackgroundColor = Color.LightSkyBlue,
                IsTemporary = true,
                TargetArrayIndex = index2
            };
            scene.Elements.Add(fly1);
            scene.Elements.Add(fly2);

            // Вычисляем слоты сравнения
            var (tx1, ty1, tx2, ty2) = GeometryHelper.GetComparisonTargetPosition(
                ctx.Array.Length, canvas.ClientSize, index1, index2, ctx.ArrayYOffset);

            // Поднимаем на высоту слотов
            await AnimateLiftTwo(fly1, fly2, ty1, ty2);

            // Сближаем к X-координатам слотов (на текущей высоте)
            await AnimateApproachTwoToCoords(fly1, fly2, tx1, tx2);

            // Фиксируем в слотах
            fly1.X = tx1; fly1.Y = ty1;
            fly2.X = tx2; fly2.Y = ty2;

            scene.Comparison = (fly1, fly2, "");
            InvalidateCanvas();
        }

        public async Task FlyBackAsync()
        {
            if (scene.Comparison == null) return;
            var (fly1, fly2, _) = scene.Comparison.Value;
            int idx1 = fly1.TargetArrayIndex.Value;
            int idx2 = fly2.TargetArrayIndex.Value;

            float curY1 = fly1.Y, curY2 = fly2.Y;
            await AnimateApproachTwo(fly1, fly2, idx1, idx2, curY1, curY2);
            await AnimateLandTwo(fly1, fly2, idx1, idx2);

            var orig1 = scene.Elements.First(e => e.ArrayIndex == idx1 && !e.IsTemporary);
            var orig2 = scene.Elements.First(e => e.ArrayIndex == idx2 && !e.IsTemporary);
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

            fly1.Value = ctx.Array[index1];
            fly2.Value = ctx.Array[index2];
            InvalidateCanvas();
        }

        public async Task FallToNewPositions(int newIndex1, int newIndex2)
        {
            if (scene.Comparison == null) return;
            var (fly1, fly2, _) = scene.Comparison.Value;

            float currentX1 = fly1.X, currentY1 = fly1.Y;
            float currentX2 = fly2.X, currentY2 = fly2.Y;
            int val1 = fly1.Value;
            int val2 = fly2.Value;

            scene.Elements.Remove(fly1);
            scene.Elements.Remove(fly2);
            scene.Comparison = null;

            var newFly1 = new VisualElement
            {
                Value = val2,
                X = currentX1,
                Y = currentY1,
                IsVisible = true,
                BackgroundColor = Color.LightSkyBlue,
                IsTemporary = true,
                TargetArrayIndex = newIndex1
            };
            var newFly2 = new VisualElement
            {
                Value = val1,
                X = currentX2,
                Y = currentY2,
                IsVisible = true,
                BackgroundColor = Color.LightSkyBlue,
                IsTemporary = true,
                TargetArrayIndex = newIndex2
            };

            scene.Elements.Add(newFly1);
            scene.Elements.Add(newFly2);
            InvalidateCanvas();

            // Горизонтальное сближение (на текущей высоте)
            await AnimateApproachTwo(newFly1, newFly2, newIndex2, newIndex1, currentY1, currentY2);
            // Вертикальное падение
            await AnimateLandTwo(newFly1, newFly2, newIndex2, newIndex1);

            var orig1 = scene.Elements.First(e => e.ArrayIndex == newIndex1 && !e.IsTemporary);
            var orig2 = scene.Elements.First(e => e.ArrayIndex == newIndex2 && !e.IsTemporary);
            orig1.Value = val1;
            orig2.Value = val2;
            orig1.IsVisible = true;
            orig2.IsVisible = true;

            scene.Elements.Remove(newFly1);
            scene.Elements.Remove(newFly2);
            InvalidateCanvas();
        }

        public async Task SingleFlyToComparisonAsync(int index)
        {
            if (currentFlyingSingle != null) await FlyBackSingleAsync();

            var orig = scene.Elements.First(e => e.ArrayIndex == index && !e.IsTemporary);
            orig.IsVisible = false;
            var (ox, oy) = GetPos(index);
            var fly = new VisualElement
            {
                Value = orig.Value,
                X = ox,
                Y = oy,
                IsVisible = true,
                BackgroundColor = Color.LightSkyBlue,
                IsTemporary = true,
                TargetArrayIndex = index
            };
            scene.Elements.Add(fly);
            await AnimateLiftSingle(fly, index);
            currentFlyingSingle = fly;
            InvalidateCanvas();
        }

        public async Task FlyBackSingleAsync()
        {
            if (currentFlyingSingle == null) return;
            var fly = currentFlyingSingle;
            int idx = fly.TargetArrayIndex.Value;
            await AnimateLandSingle(fly, idx);

            var orig = scene.Elements.First(e => e.ArrayIndex == idx && !e.IsTemporary);
            orig.Value = fly.Value;
            scene.Elements.Remove(fly);
            orig.IsVisible = true;
            currentFlyingSingle = null;
            InvalidateCanvas();
        }

        // Операции для слияния
        public async Task BeginMergeVisualAsync(int left, int mid, int right, CancellationToken token)
        {
            ctx.IsMergeActive = true;
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
                var pos = GetPos(i);
                float tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var temp = new VisualElement
                {
                    Value = ctx.Array[i],
                    X = pos.x,
                    Y = pos.y,
                    IsVisible = true,
                    BackgroundColor = Color.FromArgb(200, 200, 255),
                    IsTemporary = true,
                    ArrayIndex = i,
                    TargetArrayIndex = i
                };
                scene.Elements.Add(temp);
                ctx.MergeTempLeft.Add(temp);
                await AnimateLiftVertical(temp, tempY);
                InvalidateCanvas();
                await Task.Delay(20, token);
            }
            // Правая часть
            for (int i = mid + 1; i <= right; i++)
            {
                var pos = GetPos(i);
                float tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var temp = new VisualElement
                {
                    Value = ctx.Array[i],
                    X = pos.x,
                    Y = pos.y,
                    IsVisible = true,
                    BackgroundColor = Color.FromArgb(255, 200, 200),
                    IsTemporary = true,
                    ArrayIndex = i,
                    TargetArrayIndex = i
                };
                scene.Elements.Add(temp);
                ctx.MergeTempRight.Add(temp);
                await AnimateLiftVertical(temp, tempY);
                InvalidateCanvas();
                await Task.Delay(20, token);
            }
        }

        public async Task AnimateTempToSlotAsync(VisualElement info, float targetX, float targetY)
        {
            float sX = info.X, sY = info.Y;
            int steps = HorizontalStepsBase;
            for (int st = 0; st <= steps; st++)
            {
                float t = (float)st / steps;
                float ease = 1 - (1 - t) * (1 - t);
                info.X = sX + (targetX - sX) * ease;
                info.Y = sY + (targetY - sY) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            info.X = targetX;
            info.Y = targetY;
            InvalidateCanvas();
        }

        public async Task AnimateSlotToMainAsync(int slotX, int slotY, int targetIndex, VisualElement info, CancellationToken token)
        {
            info.X = slotX;
            info.Y = slotY;
            InvalidateCanvas();

            var (fx, fy) = GetPos(targetIndex);
            float sY = info.Y;
            for (int st = 0; st <= VerticalSteps; st++)
            {
                float t = (float)st / VerticalSteps;
                float ease = t * t;
                info.X = slotX + (fx - slotX) * ease;
                info.Y = sY + (fy - sY) * ease;
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }
            info.X = fx;
            info.Y = fy;
            InvalidateCanvas();

            var main = scene.Elements.First(e => e.ArrayIndex == targetIndex && !e.IsTemporary);
            main.Value = info.Value;
            main.IsVisible = true;
            ctx.SetElement(targetIndex, info.Value);
            ctx.MarkSorted(targetIndex);
            scene.Elements.Remove(info);
            await ctx.DelayAsync(token);
        }

        public async Task MoveRemainingTempElementAsync(VisualElement info, int targetIndex, CancellationToken token)
        {
            await AnimateMoveTo(info, targetIndex);

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