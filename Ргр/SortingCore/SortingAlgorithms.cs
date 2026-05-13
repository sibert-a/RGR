using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RGR_TIMP_S4.Render;

namespace RGR_TIMP_S4.SortingCore
{
    public static class SortingAlgorithms
    {
        #region Публичное свойство для дерева
        public static Render.TreeNodeVisual TreeRoot { get; set; } // Изменено: добавлен публичный set
        #endregion

        #region BubbleSort
        public static async Task BubbleSort(SortingContext ctx, CancellationToken token)
        {
            int n = ctx.Array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - 1 - i; j++)
                {
                    token.ThrowIfCancellationRequested();
                    await ctx.CompareAsync(j, j + 1, token);
                    if (ctx.Array[j] > ctx.Array[j + 1])
                    {
                        await ctx.SwapAsync(j, j + 1, token);
                        swapped = true;
                    }
                    else
                        await ctx.ClearExternalAsync();
                }
                ctx.MarkSorted(n - 1 - i);
                if (!swapped) break;
            }
        }
        #endregion

        #region SelectionSort
        public static async Task SelectionSort(SortingContext ctx, CancellationToken token)
        {
            int n = ctx.Array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                token.ThrowIfCancellationRequested();
                int minIdx = i;
                for (int j = i + 1; j < n; j++)
                {
                    await ctx.CompareAsync(minIdx, j, token);
                    if (ctx.Array[j] < ctx.Array[minIdx]) minIdx = j;
                    await ctx.ClearExternalAsync();
                }
                if (minIdx != i)
                    await ctx.SwapAsync(i, minIdx, token);
                ctx.MarkSorted(i);
            }
        }
        #endregion

        #region InsertionSort
        public static async Task InsertionSort(SortingContext ctx, CancellationToken token)
        {
            int n = ctx.Array.Length;
            ctx.MarkSorted(0);
            for (int i = 1; i < n; i++)
            {
                token.ThrowIfCancellationRequested();
                for (int j = i; j > 0; j--)
                {
                    await ctx.CompareAsync(j - 1, j, token);
                    if (ctx.Array[j - 1] > ctx.Array[j])
                        await ctx.SwapAsync(j - 1, j, token);
                    else
                    {
                        await ctx.ClearExternalAsync();
                        break;
                    }
                }
                for (int k = 0; k <= i; k++)
                    ctx.MarkSorted(k);
            }
        }
        #endregion

        #region MergeSort
        public static async Task MergeSort(SortingContext ctx, int left, int right, CancellationToken token)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;
                await MergeSort(ctx, left, mid, token);
                await MergeSort(ctx, mid + 1, right, token);
                await Merge(ctx, left, mid, right, token);
            }
        }

        private static async Task Merge(SortingContext ctx, int left, int mid, int right, CancellationToken token)
        {
            int n1 = mid - left + 1;
            int n2 = right - mid;
            int[] L = new int[n1];
            int[] R = new int[n2];

            for (int i = 0; i < n1; i++) L[i] = ctx.Array[left + i];
            for (int j = 0; j < n2; j++) R[j] = ctx.Array[mid + 1 + j];

            await ctx.BeginMergeVisual(left, mid, right, token);

            int iIdx = 0, jIdx = 0, k = left;
            while (iIdx < n1 && jIdx < n2)
            {
                token.ThrowIfCancellationRequested();

                VisualElement leftInfo = ctx.MergeTempLeft[0];
                VisualElement rightInfo = ctx.MergeTempRight[0];

                ctx.GetMergeSlots(leftInfo, rightInfo, out int x1, out int y1, out int x2, out int y2);

                await ctx.AnimateTempToSlot(leftInfo, x1, y1, true, token);
                await ctx.AnimateTempToSlot(rightInfo, x2, y2, false, token);

                ctx.ComparisonSign = L[iIdx] <= R[jIdx] ? "<=" : ">";
                await ctx.DelayAsync(token);

                VisualElement smaller;
                int slotSmallerX, slotSmallerY;
                bool smallerIsLeft;
                if (L[iIdx] <= R[jIdx])
                {
                    smaller = leftInfo;
                    smallerIsLeft = true;
                    slotSmallerX = x1;
                    slotSmallerY = y1;
                    iIdx++;
                }
                else
                {
                    smaller = rightInfo;
                    smallerIsLeft = false;
                    slotSmallerX = x2;
                    slotSmallerY = y2;
                    jIdx++;
                }

                await ctx.AnimateSlotToMain(slotSmallerX, slotSmallerY, k, smaller, token);

                if (smallerIsLeft)
                    ctx.MergeTempLeft.RemoveAt(0);
                else
                    ctx.MergeTempRight.RemoveAt(0);

                if (smallerIsLeft && ctx.MergeTempLeft.Count > 0)
                {
                    var nextLeft = ctx.MergeTempLeft[0];
                    await ctx.AnimateTempToSlot(nextLeft, x1, y1, true, token);
                }
                else if (!smallerIsLeft && ctx.MergeTempRight.Count > 0)
                {
                    var nextRight = ctx.MergeTempRight[0];
                    await ctx.AnimateTempToSlot(nextRight, x2, y2, false, token);
                }

                k++;
            }

            while (iIdx < n1)
            {
                var info = ctx.MergeTempLeft[0];
                ctx.MergeTempLeft.RemoveAt(0);
                await ctx.MoveRemainingTempElement(info, k, token);
                iIdx++; k++;
            }
            while (jIdx < n2)
            {
                var info = ctx.MergeTempRight[0];
                ctx.MergeTempRight.RemoveAt(0);
                await ctx.MoveRemainingTempElement(info, k, token);
                jIdx++; k++;
            }

            ctx.EndMergeVisual();
        }
        #endregion

        #region QuickSort
        public static async Task QuickSort(SortingContext ctx, int low, int high, CancellationToken token)
        {
            if (low < high)
            {
                int pi = await Partition(ctx, low, high, token);
                await QuickSort(ctx, low, pi - 1, token);
                await QuickSort(ctx, pi + 1, high, token);
            }
        }

        private static async Task<int> Partition(SortingContext ctx, int low, int high, CancellationToken token)
        {
            int pivot = ctx.Array[high];
            int i = low - 1;

            await ctx.ShowElementAsync(high, token);
            await ctx.ClearExternalAsync();

            for (int j = low; j < high; j++)
            {
                token.ThrowIfCancellationRequested();
                await ctx.CompareAsync(j, high, token);
                await ctx.ClearExternalAsync();
                if (ctx.Array[j] <= pivot)
                {
                    i++;
                    if (i != j)
                        await ctx.SwapAsync(i, j, token);
                }
            }
            if (i + 1 != high)
                await ctx.SwapAsync(i + 1, high, token);

            return i + 1;
        }
        #endregion

        #region TreeSort
        public static async Task TreeSort(SortingContext ctx, CancellationToken token)
        {
            TreeNode root = null;
            TreeRoot = null;
            for (int i = 0; i < ctx.Array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                await ctx.ShowElementAsync(i, token);
                root = Insert(root, ctx.Array[i]);
                // Строим визуальное дерево на основе текущих вставленных элементов
                int[] currentSlice = new int[i + 1];
                System.Array.Copy(ctx.Array, 0, currentSlice, 0, i + 1);
                TreeRoot = Render.TreeRenderer.BuildAndLayout(currentSlice);
                ctx.InvalidateCanvas();
                await ctx.ClearExternalAsync();
                await ctx.DelayAsync(token);
            }

            List<int> sorted = new List<int>();
            await Inorder(root, sorted, token);
            for (int i = 0; i < sorted.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                ctx.SetElement(i, sorted[i]);
                ctx.MarkSorted(i);
                await ctx.DelayAsync(token);
            }
        }

        private class TreeNode
        {
            public int Value;
            public TreeNode Left, Right;
            public TreeNode(int v) => Value = v;
        }

        private static TreeNode Insert(TreeNode node, int val)
        {
            if (node == null) return new TreeNode(val);
            if (val < node.Value) node.Left = Insert(node.Left, val);
            else node.Right = Insert(node.Right, val);
            return node;
        }

        private static async Task Inorder(TreeNode node, List<int> res, CancellationToken ct)
        {
            if (node != null)
            {
                await Inorder(node.Left, res, ct);
                ct.ThrowIfCancellationRequested();
                res.Add(node.Value);
                await Task.CompletedTask;
                await Inorder(node.Right, res, ct);
            }
        }
        #endregion
    }
}