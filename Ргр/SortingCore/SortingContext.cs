using RGR_TIMP_S4.Render;
using RGR_TIMP_S4.SortingCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public int[] Array { get; private set; }
        public bool[] IsSorted { get; private set; }
        public Scene Scene { get; private set; }

        public List<VisualElement> MergeTempLeft { get; } = new List<VisualElement>();
        public List<VisualElement> MergeTempRight { get; } = new List<VisualElement>();
        public bool IsMergeActive { get; set; }

        private string _comparisonSign;
        public string ComparisonSign
        {
            get => _comparisonSign;
            set
            {
                _comparisonSign = value;
                if (Scene.Comparison != null)
                {
                    var (l, r, _) = Scene.Comparison.Value;
                    Scene.Comparison = (l, r, value);
                }
                animator.InvalidateCanvas();
            }
        }

        public SortingContext(Panel canvas, TrackBar speedTrackBar, Func<bool> isPausedGetter)
        {
            this.speedTrackBar = speedTrackBar ?? throw new ArgumentNullException(nameof(speedTrackBar));
            this.isPausedGetter = isPausedGetter ?? throw new ArgumentNullException(nameof(isPausedGetter));
            Scene = new Scene();
            animator = new AnimationPlayer(canvas, this);
        }

        // Управление массивом
        public void GenerateArray(int size, int maxValue = 100)
        {
            var rand = new Random();
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
            animator.InitializeScene(Array);
        }

        public void SetElement(int index, int value)
        {
            Array[index] = value;
            var el = Scene.Elements.FirstOrDefault(e => e.ArrayIndex == index && !e.IsTemporary);
            if (el != null) el.Value = value;
            animator.InvalidateCanvas();
        }

        public void MarkSorted(int index)
        {
            if (IsSorted != null && index >= 0 && index < IsSorted.Length)
            {
                IsSorted[index] = true;
                var el = Scene.Elements.FirstOrDefault(e => e.ArrayIndex == index && !e.IsTemporary);
                if (el != null) el.BackgroundColor = Color.LightGreen;
                animator.InvalidateCanvas();
            }
        }
        public void InvalidateCanvas()
        {
            animator.InvalidateCanvas();
        }

        public void ResetVisuals()
        {
            IsMergeActive = false;
            MergeTempLeft.Clear();
            MergeTempRight.Clear();
            animator.InitializeScene(Array);
            animator.InvalidateCanvas();
        }

        public async Task DelayAsync(CancellationToken token)
        {
            int delayMs = speedTrackBar.Value;
            try { await Task.Delay(delayMs, token); }
            catch (OperationCanceledException) { throw; }
            while (isPausedGetter() && !token.IsCancellationRequested)
                await Task.Delay(50, token);
            token.ThrowIfCancellationRequested();
        }

        // Высокоуровневые действия для алгоритмов
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
            bool alreadyUp = Scene.Comparison != null &&
                             Scene.Comparison.Value.Left.TargetArrayIndex == i &&
                             Scene.Comparison.Value.Right.TargetArrayIndex == j;
            if (!alreadyUp)
            {
                await animator.FlyToComparisonAsync(i, j);
                await DelayAsync(token);
            }

            int tmp = Array[i];
            Array[i] = Array[j];
            Array[j] = tmp;

            await animator.SwapOnTopAsync(i, j);
            ComparisonSign = "";
            await ClearExternalAsync();
            //await DelayAsync(token);
        }

        public async Task ClearExternalAsync()
        {
            if (Scene.Comparison != null)
                await animator.FlyBackAsync();
            else
                await animator.FlyBackSingleAsync();
        }

        public async Task ShowElementAsync(int index, CancellationToken token)
        {
            await animator.SingleFlyToComparisonAsync(index);
            await DelayAsync(token);
        }

        public async Task MoveElementToAsync(int sourceIndex, int targetX, int targetY, CancellationToken token)
        {
            // заглушка – не используется основными алгоритмами
            await Task.CompletedTask;
        }

        // Методы для слияния
        public async Task BeginMergeVisual(int left, int mid, int right, CancellationToken token)
        {
            await animator.BeginMergeVisualAsync(left, mid, right, token);
        }

        public void GetMergeSlots(VisualElement leftInfo, VisualElement rightInfo,
            out int x1, out int y1, out int x2, out int y2)
        {
            var (sx1, sy1, sx2, sy2) = GeometryHelper.GetComparisonTargetPosition(
                Array.Length, animator.GetCanvasSize(),
                leftInfo.ArrayIndex.Value, rightInfo.ArrayIndex.Value);
            x1 = sx1; y1 = sy1; x2 = sx2; y2 = sy2;
        }

        public async Task AnimateTempToSlot(VisualElement info, int slotX, int slotY, bool isLeftSlot, CancellationToken token)
        {
            await animator.AnimateTempToSlotAsync(info, slotX, slotY);
        }

        public async Task AnimateSlotToMain(int slotX, int slotY, int targetIndex, VisualElement info, CancellationToken token)
        {
            await animator.AnimateSlotToMainAsync(slotX, slotY, targetIndex, info, token);
        }

        public async Task MoveRemainingTempElement(VisualElement info, int targetIndex, CancellationToken token)
        {
            await animator.MoveRemainingTempElementAsync(info, targetIndex, token);
        }

        public void EndMergeVisual()
        {
            animator.EndMergeVisual();
        }

        private void UpdateComparisonSign(int i, int j)
        {
            if (Array[i] < Array[j]) ComparisonSign = "<";
            else if (Array[i] > Array[j]) ComparisonSign = ">";
            else ComparisonSign = "=";
        }
    }
}