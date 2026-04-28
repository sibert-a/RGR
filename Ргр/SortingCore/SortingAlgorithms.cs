using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RGR_TIMP_S4.SortingCore
{
    public static class SortingAlgorithms
    {
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

            int iIdx = 0, jIdx = 0, k = left;

            if (ctx.ExternalElement1.HasValue || ctx.ExternalElement2.HasValue)
                await ctx.ClearExternalAsync();

            while (iIdx < n1 && jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                await ctx.CompareAsync(left + iIdx, mid + 1 + jIdx, token);
                await ctx.ClearExternalAsync();

                int sourceIdx = L[iIdx] <= R[jIdx] ? left + iIdx : mid + 1 + jIdx;
                int val = L[iIdx] <= R[jIdx] ? L[iIdx] : R[jIdx];

                var targetPos = ctx.GetElementPositionOnCanvas(k);   // используем публичный метод контекста
                await ctx.MoveElementToAsync(sourceIdx, targetPos.x, targetPos.y, token);
                ctx.SetElement(k, val);
                await ctx.DelayAsync(token);

                if (L[iIdx] <= R[jIdx]) iIdx++; else jIdx++;
                k++;
            }

            while (iIdx < n1)
            {
                token.ThrowIfCancellationRequested();
                var targetPos = ctx.GetElementPositionOnCanvas(k);
                await ctx.MoveElementToAsync(left + iIdx, targetPos.x, targetPos.y, token);
                ctx.SetElement(k, L[iIdx]);
                await ctx.DelayAsync(token);
                iIdx++; k++;
            }

            while (jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                var targetPos = ctx.GetElementPositionOnCanvas(k);
                await ctx.MoveElementToAsync(mid + 1 + jIdx, targetPos.x, targetPos.y, token);
                ctx.SetElement(k, R[jIdx]);
                await ctx.DelayAsync(token);
                jIdx++; k++;
            }

            ctx.ExternalElement1 = ctx.ExternalElement2 = null;
            ctx.ExternalElementIndex1 = ctx.ExternalElementIndex2 = null;
            ctx.ComparisonSign = "";
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
            for (int i = 0; i < ctx.Array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                await ctx.ShowElementAsync(i, token);
                root = Insert(root, ctx.Array[i]);
                await ctx.ClearExternalAsync();
                await ctx.DelayAsync(token);
            }

            List<int> sortedList = new List<int>();
            await Inorder(root, sortedList, token);

            for (int i = 0; i < sortedList.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                ctx.SetElement(i, sortedList[i]);
                ctx.MarkSorted(i);
                await ctx.DelayAsync(token);
            }
        }

        private class TreeNode
        {
            public int Value;
            public TreeNode Left;
            public TreeNode Right;
            public TreeNode(int value) { Value = value; }
        }

        private static TreeNode Insert(TreeNode root, int value)
        {
            if (root == null) return new TreeNode(value);
            if (value < root.Value)
                root.Left = Insert(root.Left, value);
            else
                root.Right = Insert(root.Right, value);
            return root;
        }

        private static async Task Inorder(TreeNode node, List<int> result, CancellationToken ct)
        {
            if (node != null)
            {
                await Inorder(node.Left, result, ct);
                ct.ThrowIfCancellationRequested();
                result.Add(node.Value);
                await Task.CompletedTask; // задержка осуществляется через ctx.DelayAsync? Здесь можно ничего не делать, т.к. в TreeSort ожидание уже есть.
                await Inorder(node.Right, result, ct);
            }
        }
        #endregion
    }
}