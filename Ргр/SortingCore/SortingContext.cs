using RGR_TIMP_S4.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RGR_TIMP_S4.SortingCore
{
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

        #region Состояние слияния (merge)
        public bool IsMergeActive { get; set; }
        public int MergeLeftStart { get; set; }
        public int MergeSplit { get; set; }         // mid
        public int MergeRightEnd { get; set; }
        public List<TempElementInfo> MergeTempLeft { get; } = new List<TempElementInfo>();
        public List<TempElementInfo> MergeTempRight { get; } = new List<TempElementInfo>();

        public class TempElementInfo
        {
            public int Value;
            public int OriginalIndex;
            public PointF Position;
        }
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
            catch (OperationCanceledException) { throw; }

            while (isPausedGetter() && !token.IsCancellationRequested)
            {
                await Task.Delay(50, token);
            }
            token.ThrowIfCancellationRequested();
        }

        // ==================== Обычные действия алгоритмов ====================
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
            IsMergeActive = false;
            MergeTempLeft.Clear();
            MergeTempRight.Clear();
            animator.InvalidateCanvas();
        }

        // ==================== Действия для слияния ====================
        public async Task BeginMergeVisual(int left, int mid, int right, CancellationToken token)
        {
            IsMergeActive = true;
            MergeLeftStart = left;
            MergeSplit = mid;
            MergeRightEnd = right;
            MergeTempLeft.Clear();
            MergeTempRight.Clear();

            // Поднимаем элементы из основного массива в верхние временные строки
            for (int i = left; i <= mid; i++)
            {
                var pos = GetElementPositionOnCanvas(i);
                int tempX = pos.x;
                int tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var info = new TempElementInfo { Value = Array[i], OriginalIndex = i, Position = new PointF(tempX, pos.y) }; // начальная позиция = основная
                MergeTempLeft.Add(info);
                await animator.AnimateLiftSingle(tempX, pos.y, tempY, info); // анимация подъёма
                info.Position = new PointF(tempX, tempY); // фиксируем конечную позицию
                animator.InvalidateCanvas();
                await Task.Delay(20, token); // небольшая пауза между элементами
            }
            for (int i = mid + 1; i <= right; i++)
            {
                var pos = GetElementPositionOnCanvas(i);
                int tempX = pos.x;
                int tempY = pos.y - GeometryHelper.TempRowVerticalOffset;
                var info = new TempElementInfo { Value = Array[i], OriginalIndex = i, Position = new PointF(tempX, pos.y) };
                MergeTempRight.Add(info);
                await animator.AnimateLiftSingle(tempX, pos.y, tempY, info);
                info.Position = new PointF(tempX, tempY);
                animator.InvalidateCanvas();
                await Task.Delay(20, token);
            }
        }

        public async Task MergeCompareAndAdvance(CancellationToken token)
        {
            if (MergeTempLeft.Count == 0 || MergeTempRight.Count == 0)
                throw new InvalidOperationException("Both temp arrays must be non-empty for compare.");

            var leftInfo = MergeTempLeft[0];
            var rightInfo = MergeTempRight[0];

            // Получаем координаты слотов сравнения (используем оригинальные индексы для горизонтального позиционирования)
            var (slotX1, slotY1, slotX2, slotY2) = GeometryHelper.GetComparisonTargetPosition(
                Array.Length, animator.GetCanvasSize(), leftInfo.OriginalIndex, rightInfo.OriginalIndex);

            // Анимируем спуск из временных позиций в слоты
            ClearExternalElements(); // на всякий случай
            SetFlyingState(true, false);
            // Левый элемент летит в левый слот
            await animator.AnimateLandSingle(leftInfo.Position.X, leftInfo.Position.Y, slotY1, leftInfo);
            SetFlyingState(false, false);
            SetExternalSingle(leftInfo.Value, leftInfo.OriginalIndex, slotX1, slotY1);

            SetFlyingState(false, true);
            await animator.AnimateLandSingle(rightInfo.Position.X, rightInfo.Position.Y, slotY2, rightInfo);
            SetFlyingState(false, false);
            SetExternalSingle(rightInfo.Value, rightInfo.OriginalIndex, slotX2, slotY2, true);

            ExternalElement2 = rightInfo.Value;
            ExternalElementIndex2 = rightInfo.OriginalIndex;
            ComparisonSign = leftInfo.Value <= rightInfo.Value ? "<=" : ">";
            animator.InvalidateCanvas();
            await DelayAsync(token);

            // Определяем меньший
            TempElementInfo smaller;
            bool smallerIsLeft;
            if (leftInfo.Value <= rightInfo.Value)
            {
                smaller = leftInfo;
                smallerIsLeft = true;
                ComparisonSign = "<=";
            }
            else
            {
                smaller = rightInfo;
                smallerIsLeft = false;
                ComparisonSign = ">";
            }

            // Анимация падения меньшего из его слота в основную позицию k (передаётся через параметр или вычисляется)
            // Здесь k нужно получить из алгоритма, поэтому метод должен принимать ref int k.
            // Изменяем сигнатуру: public async Task MergeCompareAndAdvance(ref int k, CancellationToken token)
            // Но для простоты пока оставим так: алгоритм сам будет вызывать этот метод и дальше двигать k.
            // Временно вернём меньший элемент и его сторону.
        }

        // Вспомогательные методы для слияния (будут использоваться в алгоритме)
        public void GetMergeSlots(TempElementInfo left, TempElementInfo right, out int x1, out int y1, out int x2, out int y2)
        {
            var (slotX1, slotY1, slotX2, slotY2) = GeometryHelper.GetComparisonTargetPosition(
                Array.Length, animator.GetCanvasSize(), left.OriginalIndex, right.OriginalIndex);
            x1 = slotX1;
            y1 = slotY1;
            x2 = slotX2;
            y2 = slotY2;
        }

        public async Task AnimateTempToSlot(TempElementInfo info, int slotX, int slotY, bool isLeftSlot, CancellationToken token)
        {
            SetFlyingState(true, false);
            ClearExternalSingle();
            await animator.AnimateLandSingle(info.Position.X, info.Position.Y, slotY, info);
            info.Position = new PointF(slotX, slotY);
            SetFlyingState(false, false);
            SetExternalSingle(info.Value, info.OriginalIndex, slotX, slotY, !isLeftSlot);
        }

        public async Task AnimateSlotToMain(int slotX, int slotY, int targetIndex, TempElementInfo info, CancellationToken token)
        {
            // Очищаем внешний элемент слота
            if (ExternalElementIndex1 == info.OriginalIndex) ExternalElement1 = null;
            if (ExternalElementIndex2 == info.OriginalIndex) ExternalElement2 = null;
            ExternalElementIndex1 = ExternalElementIndex2 = null;
            ComparisonSign = "";

            var targetPos = GetElementPositionOnCanvas(targetIndex);
            // Анимация: горизонтальное перемещение на высоте слота, затем падение
            await animator.AnimateHorizontalMove(slotX, targetPos.x, slotY, info);
            await animator.AnimateLandSingle(targetPos.x, slotY, targetPos.y, info);
            info.Position = new PointF(targetPos.x, targetPos.y);

            // Обновляем массив (алгоритм его уже изменил)
            SetElement(targetIndex, info.Value);
            MarkSorted(targetIndex);
            animator.InvalidateCanvas();
            await DelayAsync(token);
        }

        public async Task MoveRemainingTempElement(TempElementInfo info, int targetIndex, CancellationToken token)
        {
            var targetPos = GetElementPositionOnCanvas(targetIndex);
            await animator.FlyToPositionAsync(info.Position, targetPos.x, targetPos.y, info);
            SetElement(targetIndex, info.Value);
            MarkSorted(targetIndex);
            animator.InvalidateCanvas();
            await DelayAsync(token);
        }

        public void EndMergeVisual()
        {
            IsMergeActive = false;
            MergeTempLeft.Clear();
            MergeTempRight.Clear();
            ResetVisuals();
        }

        // ==================== Приватные хелперы ====================
        private void UpdateComparisonSign(int i, int j)
        {
            if (Array[i] < Array[j]) ComparisonSign = "<";
            else if (Array[i] > Array[j]) ComparisonSign = ">";
            else ComparisonSign = "=";
            animator.InvalidateCanvas();
        }

        private void SetExternalSingle(int value, int index, float x, float y, bool isSecond = false)
        {
            if (!isSecond)
            {
                ExternalElement1 = value;
                ExternalElementIndex1 = index;
                FlyX1 = x; FlyY1 = y;
            }
            else
            {
                ExternalElement2 = value;
                ExternalElementIndex2 = index;
                FlyX2 = x; FlyY2 = y;
            }
        }

        private void ClearExternalElements()
        {
            ExternalElement1 = ExternalElement2 = null;
            ExternalElementIndex1 = ExternalElementIndex2 = null;
        }

        private void SetFlyingState(bool f1, bool f2)
        {
            IsFlying1 = f1;
            IsFlying2 = f2;
        }

        private void ClearExternalSingle()
        {
            ExternalElement1 = null;
            ExternalElementIndex1 = null;
        }
    }
}