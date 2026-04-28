using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace РГР
{
    public class SortingContext
    {
        private readonly Panel canvas;
        private readonly TrackBar speedTrackBar;
        private readonly Func<bool> isPausedGetter;

        // Состояние массива
        public int[] Array { get; private set; }
        public bool[] IsSorted { get; private set; }

        // Внешние элементы
        public int? ExternalElement1 { get; set; }
        public int? ExternalElement2 { get; set; }
        public int? ExternalElementIndex1 { get; set; }
        public int? ExternalElementIndex2 { get; set; }
        public string ComparisonSign { get; set; } = "";

        // Анимация полёта
        public bool IsFlying1 { get; set; }
        public bool IsFlying2 { get; set; }
        public float FlyX1 { get; set; }
        public float FlyY1 { get; set; }
        public float FlyX2 { get; set; }
        public float FlyY2 { get; set; }
        public int FlyingValue1 { get; set; }
        public int FlyingValue2 { get; set; }

        public SortingContext(Panel canvas, TrackBar speedTrackBar, Func<bool> isPausedGetter)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.speedTrackBar = speedTrackBar ?? throw new ArgumentNullException(nameof(speedTrackBar));
            this.isPausedGetter = isPausedGetter ?? throw new ArgumentNullException(nameof(isPausedGetter));
        }

        public void GenerateArray(int size, int maxValue = 100)
        {
            Random rand = new Random();
            Array = new int[size];
            IsSorted = new bool[size];
            for (int i = 0; i < size; i++)
                Array[i] = rand.Next(10, maxValue);

            ExternalElement1 = ExternalElement2 = null;
            ExternalElementIndex1 = ExternalElementIndex2 = null;
            ComparisonSign = "";
            IsFlying1 = IsFlying2 = false;
            canvas.Invalidate();
        }

        // Основные операции для алгоритмов

        public async Task DelayAsync(CancellationToken token)
        {
            int delayMs = speedTrackBar.Value;
            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            while (isPausedGetter() && !token.IsCancellationRequested)
            {
                await Task.Delay(50, token);
            }
            token.ThrowIfCancellationRequested();
        }

        public async Task CompareAsync(int index1, int index2, CancellationToken token)
        {
            await AnimateFlyToComparison(index1, index2);
            if (Array[index1] < Array[index2])
                ComparisonSign = "<";
            else if (Array[index1] > Array[index2])
                ComparisonSign = ">";
            else
                ComparisonSign = "=";
            canvas.Invalidate();
            await DelayAsync(token);
        }

        public async Task SwapAsync(int i, int j, CancellationToken token)
        {
            bool needLift = !(ExternalElementIndex1 == i && ExternalElementIndex2 == j
                              && ExternalElement1.HasValue && ExternalElement2.HasValue);
            if (needLift)
            {
                await AnimateFlyToComparison(i, j);
                canvas.Invalidate();
                await DelayAsync(token);
            }

            int temp = Array[i];
            Array[i] = Array[j];
            Array[j] = temp;

            await AnimateSwapOnTop(i, j);
            ComparisonSign = "";
            canvas.Invalidate();
            await ClearExternalAsync();
            canvas.Invalidate();
            await DelayAsync(token);
        }

        public async Task ClearExternalAsync()
        {
            await AnimateFlyBack();
        }

        public async Task ShowElementAsync(int index, CancellationToken token)
        {
            await AnimateSingleFlyToComparison(index);
            canvas.Invalidate();
            await DelayAsync(token);
        }

        public async Task MoveElementToAsync(int sourceIndex, int targetX, int targetY, CancellationToken token)
        {
            await AnimateFlyToPosition(sourceIndex, targetX, targetY);
            canvas.Invalidate();
            await DelayAsync(token);
        }

        // Прямое изменение элемента (для сортировки слиянием)
        public void SetElement(int index, int value)
        {
            Array[index] = value;
            canvas.Invalidate();
        }

        public void MarkSorted(int index)
        {
            if (IsSorted != null && index >= 0 && index < IsSorted.Length)
            {
                IsSorted[index] = true;
                canvas.Invalidate();
            }
        }

        public void MarkSortedRange(int start, int count)
        {
            if (IsSorted == null) return;
            for (int i = start; i < start + count && i < IsSorted.Length; i++)
                IsSorted[i] = true;
            canvas.Invalidate();
        }

        // ----------------- Приватная геометрия и анимации ------------------

        private void GetElementScreenPosition(int index, out int x, out int y)
        {
            int squareSize = 35;
            int spacing = 3;
            int totalWidth = Array.Length * (squareSize + spacing) - spacing;
            int startX = Math.Max(10, (canvas.Width - totalWidth) / 2);
            int startY = (canvas.Height - squareSize) / 2;
            x = startX + index * (squareSize + spacing);
            y = startY;
        }

        private void GetComparisonTargetPosition(int index1, int index2,
            out int x1, out int y1, out int x2, out int y2)
        {
            int squareSize = 35;
            int verticalOffset = 70;
            GetElementScreenPosition(index1, out int elemX1, out int elemY1);
            GetElementScreenPosition(index2, out int elemX2, out int elemY2);
            float centerX1 = elemX1 + squareSize / 2f;
            float centerX2 = elemX2 + squareSize / 2f;
            float midX = (centerX1 + centerX2) / 2f;
            float halfGap = 40f;
            float targetCenterX1 = midX - halfGap;
            float targetCenterX2 = midX + halfGap;
            x1 = (int)(targetCenterX1 - squareSize / 2f);
            x2 = (int)(targetCenterX2 - squareSize / 2f);
            y1 = elemY1 - verticalOffset;
            y2 = elemY2 - verticalOffset;
            x1 = Math.Max(5, x1);
            x2 = Math.Min(canvas.Width - squareSize - 5, x2);
        }

        private async Task AnimateFlyToComparison(int index1, int index2)
        {
            ExternalElementIndex1 = index1;
            ExternalElementIndex2 = index2;
            GetElementScreenPosition(index1, out int startX1, out int startY1);
            GetElementScreenPosition(index2, out int startX2, out int startY2);
            GetComparisonTargetPosition(index1, index2,
                out int targetX1, out int targetY1, out int targetX2, out int targetY2);

            FlyingValue1 = Array[index1];
            FlyingValue2 = Array[index2];
            IsFlying1 = true;
            IsFlying2 = true;

            int vertSteps = 8;
            for (int step = 0; step <= vertSteps; step++)
            {
                float t = (float)step / vertSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                FlyX1 = startX1;
                FlyY1 = startY1 + (targetY1 - startY1) * easeT;
                FlyX2 = startX2;
                FlyY2 = startY2 + (targetY2 - startY2) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int diff = Math.Abs(index1 - index2);
            int horizSteps = Math.Max(diff, 10);
            for (int step = 0; step <= horizSteps; step++)
            {
                float t = (float)step / horizSteps;
                float easeT = 1 - (1 - t) * (1 - t);
                FlyX1 = startX1 + (targetX1 - startX1) * easeT;
                FlyY1 = targetY1;
                FlyX2 = startX2 + (targetX2 - startX2) * easeT;
                FlyY2 = targetY2;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            IsFlying1 = false;
            IsFlying2 = false;
            ExternalElement1 = Array[index1];
            ExternalElement2 = Array[index2];
            canvas.Invalidate();
        }

        private async Task AnimateSingleFlyToComparison(int index)
        {
            ExternalElementIndex1 = index;
            ExternalElementIndex2 = null;
            GetElementScreenPosition(index, out int startX, out int startY);
            int targetY = startY - 70;
            FlyingValue1 = Array[index];
            IsFlying1 = true;

            int steps = 8;
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                FlyX1 = startX;
                FlyY1 = startY + (targetY - startY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            IsFlying1 = false;
            ExternalElement1 = Array[index];
            ExternalElement2 = null;
            canvas.Invalidate();
        }

        private async Task AnimateFlyBack()
        {
            if (!ExternalElement1.HasValue && !ExternalElement2.HasValue) return;
            int? idx1 = ExternalElementIndex1;
            int? idx2 = ExternalElementIndex2;
            int? val1 = ExternalElement1;
            int? val2 = ExternalElement2;

            if (idx1.HasValue && idx2.HasValue)
            {
                GetComparisonTargetPosition(idx1.Value, idx2.Value,
                    out int curX1, out int curY1, out int curX2, out int curY2);
                GetElementScreenPosition(idx1.Value, out int tgtX1, out int tgtY1);
                GetElementScreenPosition(idx2.Value, out int tgtX2, out int tgtY2);

                FlyingValue1 = val1.Value;
                FlyingValue2 = val2.Value;
                IsFlying1 = true;
                IsFlying2 = true;
                ExternalElement1 = ExternalElement2 = null;
                ComparisonSign = "";
                canvas.Invalidate();

                int diff = Math.Abs(idx1.Value - idx2.Value);
                int horizSteps = Math.Max(diff, 10);
                for (int step = 0; step <= horizSteps; step++)
                {
                    float t = (float)step / horizSteps;
                    float easeT = 1 - (1 - t) * (1 - t);
                    FlyX1 = curX1 + (tgtX1 - curX1) * easeT;
                    FlyY1 = curY1;
                    FlyX2 = curX2 + (tgtX2 - curX2) * easeT;
                    FlyY2 = curY2;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }

                int vertSteps = 8;
                for (int step = 0; step <= vertSteps; step++)
                {
                    float t = (float)step / vertSteps;
                    float easeT = t * t;
                    FlyX1 = tgtX1;
                    FlyY1 = curY1 + (tgtY1 - curY1) * easeT;
                    FlyX2 = tgtX2;
                    FlyY2 = curY2 + (tgtY2 - curY2) * easeT;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }
            }
            else if (idx1.HasValue)
            {
                GetElementScreenPosition(idx1.Value, out int tgtX, out int tgtY);
                int curY = tgtY - 70;
                FlyingValue1 = val1.Value;
                IsFlying1 = true;
                ExternalElement1 = null;
                ComparisonSign = "";
                canvas.Invalidate();

                int steps = 8;
                for (int step = 0; step <= steps; step++)
                {
                    float t = (float)step / steps;
                    float easeT = t * t;
                    FlyX1 = tgtX;
                    FlyY1 = curY + (tgtY - curY) * easeT;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }
            }

            IsFlying1 = false;
            IsFlying2 = false;
            ExternalElementIndex1 = null;
            ExternalElementIndex2 = null;
            canvas.Invalidate();
        }

        private async Task AnimateSwapOnTop(int index1, int index2)
        {
            GetComparisonTargetPosition(index1, index2,
                out int x1, out int y1, out int x2, out int y2);
            int val1 = Array[index1];
            int val2 = Array[index2];

            ExternalElement1 = ExternalElement2 = null;
            ComparisonSign = "";
            FlyingValue1 = val1;
            FlyingValue2 = val2;
            IsFlying1 = true;
            IsFlying2 = true;
            FlyX1 = x1; FlyY1 = y1;
            FlyX2 = x2; FlyY2 = y2;
            canvas.Invalidate();

            float startX1 = x1, startY1 = y1;
            float startX2 = x2, startY2 = y2;
            int steps = 12;
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                FlyX1 = startX1 + (startX2 - startX1) * t;
                FlyY1 = startY1 + (startY2 - startY1) * t;
                FlyX2 = startX2 + (startX1 - startX2) * t;
                FlyY2 = startY2 + (startY1 - startY2) * t;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            IsFlying1 = false;
            IsFlying2 = false;
            ExternalElement1 = val1;
            ExternalElement2 = val2;
            ExternalElementIndex1 = index1;
            ExternalElementIndex2 = index2;
            canvas.Invalidate();
        }

        private async Task AnimateFlyToPosition(int sourceIndex, int targetX, int targetY)
        {
            GetElementScreenPosition(sourceIndex, out int startX, out int startY);
            ExternalElementIndex1 = sourceIndex;
            FlyingValue1 = Array[sourceIndex];
            IsFlying1 = true;

            int liftY = startY - 70;
            int vertSteps = 8;
            for (int step = 0; step <= vertSteps; step++)
            {
                float t = (float)step / vertSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                FlyX1 = startX;
                FlyY1 = startY + (liftY - startY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int horizSteps = 10;
            for (int step = 0; step <= horizSteps; step++)
            {
                float t = (float)step / horizSteps;
                float easeT = 1 - (1 - t) * (1 - t);
                FlyX1 = startX + (targetX - startX) * easeT;
                FlyY1 = liftY;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int downSteps = 8;
            for (int step = 0; step <= downSteps; step++)
            {
                float t = (float)step / downSteps;
                float easeT = t * t;
                FlyX1 = targetX;
                FlyY1 = liftY + (targetY - liftY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            IsFlying1 = false;
            ExternalElement1 = null;
            ExternalElementIndex1 = null;
            canvas.Invalidate();
        }
    }
}