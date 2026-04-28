using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace РГР
{
    public partial class MainForm : Form
    {
        #region Поля

        private SortingContext sortingContext;
        private int[] originalArray;
        private int maxValue = 100;
        private bool isSorting = false;
        private bool isPaused = false;
        private CancellationTokenSource cancellationTokenSource;
        private HelpForm activeHelpForm = null;

        #endregion

        #region Конструктор и инициализация

        public MainForm()
        {
            InitializeComponent();

            // Настройки формы
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            this.Resize += MainForm_Resize;

            // Двойная буферизация панели
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(canvas, true, null);

            // Инициализация контекста сортировки
            sortingContext = new SortingContext(canvas, speedTrackBar, () => isPaused);

            GenerateArray();
            originalArray = sortingContext.Array.ToArray();
        }

        #endregion

        #region Обработчики формы (Help, Resize)

        protected override void OnHelpButtonClicked(CancelEventArgs e)
        {
            e.Cancel = true;
            ShowHelpForm();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                ShowHelpForm();
            }
        }

        private void ShowHelpForm()
        {
            if (activeHelpForm == null || activeHelpForm.IsDisposed)
            {
                activeHelpForm = new HelpForm();
                activeHelpForm.FormClosed += (s, args) => activeHelpForm = null;
                activeHelpForm.Show();
            }
            else
            {
                if (activeHelpForm.WindowState == FormWindowState.Minimized)
                    activeHelpForm.WindowState = FormWindowState.Normal;
                activeHelpForm.Activate();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (canvas != null && sidePanel != null)
            {
                int margin = 10;
                sidePanel.Left = this.ClientSize.Width - sidePanel.Width - margin;
                sidePanel.Height = this.ClientSize.Height - 2 * margin;

                int canvasWidth = this.ClientSize.Width - sidePanel.Width - 3 * margin;
                canvas.Width = canvasWidth;
                canvas.Left = margin;
                canvas.Top = 200;
            }
        }

        #endregion

        #region Генерация массива

        private void GenerateArray()
        {
            sortingContext.GenerateArray((int)sizeNumeric.Value, maxValue);
            canvas.Invalidate();
            originalArray = sortingContext.Array.ToArray();
        }

        #endregion

        #region Управление сортировкой

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            originalArray = sortingContext.Array.ToArray();
            isSorting = true;
            isPaused = false;
            SetControlsState(true, false);

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Сброс внешних элементов и пометок
            sortingContext.ExternalElement1 = null;
            sortingContext.ExternalElement2 = null;
            sortingContext.ExternalElementIndex1 = null;
            sortingContext.ExternalElementIndex2 = null;
            sortingContext.ComparisonSign = "";
            for (int i = 0; i < sortingContext.Array.Length; i++)
                sortingContext.IsSorted[i] = false;
            canvas.Invalidate();

            try
            {
                switch (algorithmCombo.SelectedIndex)
                {
                    case 0: await BubbleSort(token); break;
                    case 1: await SelectionSort(token); break;
                    case 2: await InsertionSort(token); break;
                    case 3: await MergeSort(0, sortingContext.Array.Length - 1, token); break;
                    case 4: await QuickSort(0, sortingContext.Array.Length - 1, token); break;
                    case 5: await TreeSort(token); break;
                }
                // Пометить все как отсортированные после успешной сортировки
                for (int i = 0; i < sortingContext.Array.Length; i++)
                    sortingContext.MarkSorted(i);
                canvas.Invalidate();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                isSorting = false;
                isPaused = false;
                SetControlsState(false, false);
                sortingContext.ExternalElement1 = null;
                sortingContext.ExternalElement2 = null;
                canvas.Invalidate();
            }
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (!isSorting) return;
            isPaused = !isPaused;
            pauseButton.Text = isPaused ? "Продолжить" : "Пауза";
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (isSorting)
            {
                cancellationTokenSource?.Cancel();
                isSorting = false;
                isPaused = false;
            }

            if (originalArray != null && originalArray.Length == sortingContext.Array.Length)
            {
                Array.Copy(originalArray, sortingContext.Array, sortingContext.Array.Length);
                for (int i = 0; i < sortingContext.Array.Length; i++)
                    sortingContext.IsSorted[i] = false;
                sortingContext.ExternalElement1 = sortingContext.ExternalElement2 = null;
                sortingContext.ExternalElementIndex1 = sortingContext.ExternalElementIndex2 = null;
                sortingContext.ComparisonSign = "";
                canvas.Invalidate();
            }
            else
            {
                GenerateArray();
                originalArray = sortingContext.Array.ToArray();
            }

            SetControlsState(false, false);
        }

        private void NewArrayButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;
            GenerateArray();
            originalArray = sortingContext.Array.ToArray();
        }

        private void SizeNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (!isSorting)
            {
                GenerateArray();
                originalArray = sortingContext.Array.ToArray();
            }
        }

        private void SetControlsState(bool sorting, bool paused)
        {
            algorithmCombo.Enabled = !sorting;
            sizeNumeric.Enabled = !sorting;
            newArrayButton.Enabled = !sorting;
            startButton.Enabled = !sorting;
            resetButton.Enabled = true;
            pauseButton.Enabled = sorting;
            speedTrackBar.Enabled = true;
            pauseButton.Text = (sorting && paused) ? "Продолжить" : "Пауза";
        }

        #endregion

        #region Отрисовка (делегирована ArrayRenderer)

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            ArrayRenderer.Draw(e.Graphics, sortingContext, canvas.ClientSize);
        }

        #endregion

        #region Алгоритмы сортировки (используют sortingContext)

        private async Task BubbleSort(CancellationToken token)
        {
            int n = sortingContext.Array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - 1 - i; j++)
                {
                    token.ThrowIfCancellationRequested();
                    await sortingContext.CompareAsync(j, j + 1, token);

                    if (sortingContext.Array[j] > sortingContext.Array[j + 1])
                    {
                        await sortingContext.SwapAsync(j, j + 1, token);
                        swapped = true;
                    }
                    else
                    {
                        await sortingContext.ClearExternalAsync();
                    }
                }
                sortingContext.MarkSorted(n - 1 - i);
                if (!swapped) break;
            }
        }

        private async Task SelectionSort(CancellationToken token)
        {
            int n = sortingContext.Array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                token.ThrowIfCancellationRequested();
                int minIdx = i;
                for (int j = i + 1; j < n; j++)
                {
                    await sortingContext.CompareAsync(minIdx, j, token);
                    if (sortingContext.Array[j] < sortingContext.Array[minIdx])
                        minIdx = j;
                    await sortingContext.ClearExternalAsync();
                }
                if (minIdx != i)
                {
                    await sortingContext.SwapAsync(i, minIdx, token);
                }
                sortingContext.MarkSorted(i);
            }
        }

        private async Task InsertionSort(CancellationToken token)
        {
            int n = sortingContext.Array.Length;
            sortingContext.MarkSorted(0);
            for (int i = 1; i < n; i++)
            {
                token.ThrowIfCancellationRequested();
                for (int j = i; j > 0; j--)
                {
                    await sortingContext.CompareAsync(j - 1, j, token);
                    if (sortingContext.Array[j - 1] > sortingContext.Array[j])
                    {
                        await sortingContext.SwapAsync(j - 1, j, token);
                    }
                    else
                    {
                        await sortingContext.ClearExternalAsync();
                        break;
                    }
                }
                // Пометить все до i включительно
                for (int k = 0; k <= i; k++)
                    sortingContext.MarkSorted(k);
            }
        }

        private async Task MergeSort(int left, int right, CancellationToken token)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;
                await MergeSort(left, mid, token);
                await MergeSort(mid + 1, right, token);
                await Merge(left, mid, right, token);
            }
        }

        private async Task Merge(int left, int mid, int right, CancellationToken token)
        {
            int n1 = mid - left + 1;
            int n2 = right - mid;
            int[] L = new int[n1];
            int[] R = new int[n2];

            for (int i = 0; i < n1; i++) L[i] = sortingContext.Array[left + i];
            for (int j = 0; j < n2; j++) R[j] = sortingContext.Array[mid + 1 + j];

            int iIdx = 0, jIdx = 0, k = left;

            // Убедимся, что внешние элементы убраны
            if (sortingContext.ExternalElement1.HasValue || sortingContext.ExternalElement2.HasValue)
                await sortingContext.ClearExternalAsync();

            while (iIdx < n1 && jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                await sortingContext.CompareAsync(left + iIdx, mid + 1 + jIdx, token);
                await sortingContext.ClearExternalAsync();

                int sourceIdx;
                if (L[iIdx] <= R[jIdx])
                    sourceIdx = left + iIdx;
                else
                    sourceIdx = mid + 1 + jIdx;

                int val = (L[iIdx] <= R[jIdx]) ? L[iIdx] : R[jIdx];
                // Получаем координаты целевой ячейки
                var targetPos = GetElementPositionOnCanvas(k);
                await sortingContext.MoveElementToAsync(sourceIdx, targetPos.x, targetPos.y, token);
                sortingContext.SetElement(k, val);
                canvas.Invalidate();
                await sortingContext.DelayAsync(token);

                if (L[iIdx] <= R[jIdx]) iIdx++; else jIdx++;
                k++;
            }

            while (iIdx < n1)
            {
                token.ThrowIfCancellationRequested();
                int sourceIdx = left + iIdx;
                var targetPos = GetElementPositionOnCanvas(k);
                await sortingContext.MoveElementToAsync(sourceIdx, targetPos.x, targetPos.y, token);
                sortingContext.SetElement(k, L[iIdx]);
                canvas.Invalidate();
                await sortingContext.DelayAsync(token);
                iIdx++; k++;
            }

            while (jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                int sourceIdx = mid + 1 + jIdx;
                var targetPos = GetElementPositionOnCanvas(k);
                await sortingContext.MoveElementToAsync(sourceIdx, targetPos.x, targetPos.y, token);
                sortingContext.SetElement(k, R[jIdx]);
                canvas.Invalidate();
                await sortingContext.DelayAsync(token);
                jIdx++; k++;
            }

            // Очищаем внешние элементы после слияния
            sortingContext.ExternalElement1 = sortingContext.ExternalElement2 = null;
            sortingContext.ExternalElementIndex1 = sortingContext.ExternalElementIndex2 = null;
            sortingContext.ComparisonSign = "";
            canvas.Invalidate();
        }

        // Вспомогательный метод для получения координат ячейки (используется только в Merge)
        private (int x, int y) GetElementPositionOnCanvas(int index)
        {
            int squareSize = 35;
            int spacing = 3;
            int totalWidth = sortingContext.Array.Length * (squareSize + spacing) - spacing;
            int startX = Math.Max(10, (canvas.Width - totalWidth) / 2);
            int startY = (canvas.Height - squareSize) / 2;
            int x = startX + index * (squareSize + spacing);
            return (x, startY);
        }

        private async Task QuickSort(int low, int high, CancellationToken token)
        {
            if (low < high)
            {
                int pi = await Partition(low, high, token);
                await QuickSort(low, pi - 1, token);
                await QuickSort(pi + 1, high, token);
            }
        }

        private async Task<int> Partition(int low, int high, CancellationToken token)
        {
            int pivot = sortingContext.Array[high];
            int i = low - 1;

            await sortingContext.ShowElementAsync(high, token);
            await sortingContext.ClearExternalAsync();

            for (int j = low; j < high; j++)
            {
                token.ThrowIfCancellationRequested();
                await sortingContext.CompareAsync(j, high, token);
                await sortingContext.ClearExternalAsync();
                if (sortingContext.Array[j] <= pivot)
                {
                    i++;
                    if (i != j)
                        await sortingContext.SwapAsync(i, j, token);
                }
            }
            if (i + 1 != high)
            {
                await sortingContext.SwapAsync(i + 1, high, token);
            }
            canvas.Invalidate();
            return i + 1;
        }

        private async Task TreeSort(CancellationToken token)
        {
            TreeNode root = null;
            for (int i = 0; i < sortingContext.Array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                await sortingContext.ShowElementAsync(i, token);
                root = Insert(root, sortingContext.Array[i]);
                await sortingContext.ClearExternalAsync();
                await sortingContext.DelayAsync(token);
            }

            var sortedList = new System.Collections.Generic.List<int>();
            await Inorder(root, sortedList, token);

            for (int i = 0; i < sortedList.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                sortingContext.SetElement(i, sortedList[i]);
                sortingContext.MarkSorted(i);
                canvas.Invalidate();
                await sortingContext.DelayAsync(token);
            }
        }

        private class TreeNode
        {
            public int Value;
            public TreeNode Left;
            public TreeNode Right;
            public TreeNode(int value) { Value = value; }
        }

        private TreeNode Insert(TreeNode root, int value)
        {
            if (root == null) return new TreeNode(value);
            if (value < root.Value)
                root.Left = Insert(root.Left, value);
            else
                root.Right = Insert(root.Right, value);
            return root;
        }

        private async Task Inorder(TreeNode node, System.Collections.Generic.List<int> result, CancellationToken ct)
        {
            if (node != null)
            {
                await Inorder(node.Left, result, ct);
                ct.ThrowIfCancellationRequested();
                result.Add(node.Value);
                await sortingContext.DelayAsync(ct);
                await Inorder(node.Right, result, ct);
            }
        }

        #endregion
    }
}