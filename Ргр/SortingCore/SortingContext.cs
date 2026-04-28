using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RGR_TIMP_S4.Render;

namespace RGR_TIMP_S4.SortingCore
{
    // Класс состояния и управления. Не содержит сложной логики анимации.
    public class SortingContext
    {
        private readonly AnimationPlayer animator;
        private readonly TrackBar speedTrackBar;
        private readonly Func<bool> isPausedGetter;

        #region Состояние массива
        public int[] Array { get; private set; }
        public bool[] IsSorted { get; private set; }
        #endregion

        #region Визуальное состояние (используется рендерером и аниматором)
        public int? ExternalElement1 { get; set; }
        public int? ExternalElement2 { get; set; }
        public int? ExternalElementIndex1 { get; set; }
        public int? ExternalElementIndex2 { get; set; }
        public string ComparisonSign { get; set; } = "";

        public bool IsFlying1 { get; set; }
        public bool IsFlying2 { get; set; }
        public float FlyX1 { get; set; }
        public float FlyY1 { get; set; }
        public float FlyX2 { get; set; }
        public float FlyY2 { get; set; }
        public int FlyingValue1 { get; set; }
        public int FlyingValue2 { get; set; }
        #endregion

        public SortingContext(Panel canvas, TrackBar speedTrackBar, Func<bool> isPausedGetter)
        {
            this.speedTrackBar = speedTrackBar ?? throw new ArgumentNullException(nameof(speedTrackBar));
            this.isPausedGetter = isPausedGetter ?? throw new ArgumentNullException(nameof(isPausedGetter));
            this.animator = new AnimationPlayer(canvas, this);
        }

        // ==================== Управление массивом ====================

        public void GenerateArray(int size, int maxValue = 100)
        {
            Random rand = new Random();
            Array = new int[size];
            IsSorted = new bool[size];
            for (int i = 0; i < size; i++)
                Array[i] = rand.Next(10, maxValue);
            ResetVisuals();
        }

        public void LoadArray(int[] source)
        {
            Array = new int[source.Length];
            source.CopyTo(Array, 0);
            IsSorted = new bool[source.Length];
            animator.InvalidateCanvas();
        }

        public void SetElement(int index, int value)
        {
            Array[index] = value;
            animator.InvalidateCanvas();
        }

        public void MarkSorted(int index)
        {
            if (IsSorted != null && index >= 0 && index < IsSorted.Length)
            {
                IsSorted[index] = true;
                animator.InvalidateCanvas();
            }
        }

        public void MarkSortedRange(int start, int count)
        {
            if (IsSorted == null) return;
            for (int i = start; i < start + count && i < IsSorted.Length; i++)
                IsSorted[i] = true;
            animator.InvalidateCanvas();
        }

        // ==================== Управление паузой/задержкой ====================

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

        // ==================== Действия, видимые алгоритмам (тонкие обёртки над аниматором) ====================

        public (int x, int y) GetElementPositionOnCanvas(int index) =>
            animator.GetPositionOnCanvas(index);

        public async Task CompareAsync(int index1, int index2, CancellationToken token)
        {
            await animator.FlyToComparisonAsync(index1, index2);
            UpdateComparisonSign(index1, index2);
            await DelayAsync(token);
        }

        public async Task SwapAsync(int i, int j, CancellationToken token)
        {
            bool alreadyUp = ExternalElementIndex1 == i && ExternalElementIndex2 == j
                             && ExternalElement1.HasValue && ExternalElement2.HasValue;
            if (!alreadyUp)
            {
                await animator.FlyToComparisonAsync(i, j);
                await DelayAsync(token);
            }

            // Обмен значений
            int temp = Array[i];
            Array[i] = Array[j];
            Array[j] = temp;

            await animator.SwapOnTopAsync(i, j);
            ComparisonSign = "";
            await ClearExternalAsync();
            await DelayAsync(token);
        }

        public async Task ClearExternalAsync() => await animator.FlyBackAsync();

        public async Task ShowElementAsync(int index, CancellationToken token)
        {
            await animator.SingleFlyToComparisonAsync(index);
            await DelayAsync(token);
        }

        public async Task MoveElementToAsync(int sourceIndex, int targetX, int targetY, CancellationToken token)
        {
            await animator.FlyToPositionAsync(sourceIndex, targetX, targetY);
            await DelayAsync(token);
        }

        public void ResetVisuals()
        {
            ExternalElement1 = ExternalElement2 = null;
            ExternalElementIndex1 = ExternalElementIndex2 = null;
            ComparisonSign = "";
            IsFlying1 = IsFlying2 = false;
            animator.InvalidateCanvas();
        }

        // ==================== Приватные помощники ====================

        private void UpdateComparisonSign(int i, int j)
        {
            if (Array[i] < Array[j]) ComparisonSign = "<";
            else if (Array[i] > Array[j]) ComparisonSign = ">";
            else ComparisonSign = "=";
            animator.InvalidateCanvas();
        }
    }
}