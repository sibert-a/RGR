using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace РГР
{
    public partial class MainForm : Form
    {
        private Panel canvas;
        private ComboBox algorithmCombo;
        private Button startButton;
        private Button resetButton;
        private TrackBar speedTrackBar;
        private NumericUpDown sizeNumeric;
        private Label statusLabel;

        private int[] array;
        private int arraySize = 30;
        private int maxValue = 100;
        private bool isSorting = false;
        private CancellationTokenSource cancellationTokenSource;

        // Цвета для отображения
        private Color defaultColor = Color.SteelBlue;
        private Color comparingColor = Color.Orange;
        private Color swappingColor = Color.Red;
        private Color sortedColor = Color.LightGreen;
        private Color pivotColor = Color.Purple;

        // Для отображения элементов вне массива
        private int? externalElement1 = null;
        private int? externalElement2 = null;
        private int? externalElementIndex1 = null;
        private int? externalElementIndex2 = null;
        private int sortedCount = 0;

        public MainForm()
        {
            InitializeComponent();
            this.Resize += MainForm_Resize;
            GenerateArray();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Адаптация размеров при изменении окна
            if (canvas != null)
            {
                canvas.Width = this.ClientSize.Width - 40;
                canvas.Height = 400;
                controlPanel.Top = canvas.Bottom + 10;
                controlPanel.Width = this.ClientSize.Width - 40;
            }
        }

        private void GenerateArray()
        {
            arraySize = (int)sizeNumeric.Value;
            Random rand = new Random();
            array = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                array[i] = rand.Next(10, maxValue + 10);
            }
            externalElement1 = null;
            externalElement2 = null;
            sortedCount = 0;
            canvas.Invalidate();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (array == null || array.Length == 0) return;

            Graphics g = e.Graphics;
            int barWidth = canvas.Width / array.Length - 2;
            if (barWidth < 1) barWidth = 1;

            for (int i = 0; i < array.Length; i++)
            {
                int barHeight = (int)((double)array[i] / maxValue * (canvas.Height - 50));
                int x = i * (barWidth + 2) + 2;
                int y = canvas.Height - barHeight - 20;

                Color barColor = defaultColor;

                if (externalElementIndex1 == i || externalElementIndex2 == i)
                {
                    barColor = Color.Transparent;
                }
                else if (i < sortedCount)
                {
                    barColor = sortedColor;
                }
                else
                {
                    barColor = defaultColor;
                }

                using (Brush brush = new SolidBrush(barColor))
                {
                    g.FillRectangle(brush, x, y, barWidth, barHeight);
                }

                g.DrawRectangle(Pens.Black, x, y, barWidth, barHeight);

                using (Font font = new Font("Arial", 8))
                {
                    string valueText = array[i].ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    g.DrawString(valueText, font, Brushes.Black,
                        x + barWidth / 2 - textSize.Width / 2, y - 15);
                }
            }

            if (externalElement1.HasValue)
            {
                DrawExternalElement(g, externalElement1.Value, 50, 20, comparingColor, "Элемент 1");
            }
            if (externalElement2.HasValue)
            {
                DrawExternalElement(g, externalElement2.Value, 50, 70, swappingColor, "Элемент 2");
            }
        }

        private void DrawExternalElement(Graphics g, int value, int x, int y, Color color, string label)
        {
            int barWidth = 40;
            int barHeight = (int)((double)value / maxValue * 80);

            using (Brush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y + 50 - barHeight, barWidth, barHeight);
            }
            g.DrawRectangle(Pens.Black, x, y + 50 - barHeight, barWidth, barHeight);
            g.DrawString(value.ToString(), new Font("Arial", 10, FontStyle.Bold), Brushes.Black,
                x + barWidth / 2 - 8, y + 50 - barHeight - 15);
            g.DrawString(label, new Font("Arial", 8), Brushes.Black, x, y);
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            isSorting = true;
            startButton.Enabled = false;
            resetButton.Enabled = false;
            algorithmCombo.Enabled = false;
            sizeNumeric.Enabled = false;
            sortedCount = 0;
            externalElement1 = null;
            externalElement2 = null;

            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                switch (algorithmCombo.SelectedIndex)
                {
                    case 0:
                        await BubbleSort();
                        break;
                    case 1:
                        await SelectionSort();
                        break;
                    case 2:
                        await InsertionSort();
                        break;
                    case 3:
                        await MergeSort(0, array.Length - 1);
                        break;
                    case 4:
                        await QuickSort(0, array.Length - 1);
                        break;
                    case 5:
                        await TreeSort();
                        break;
                }
                sortedCount = array.Length;
                canvas.Invalidate();
                statusLabel.Text = "Сортировка завершена!";
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Сортировка прервана";
            }
            finally
            {
                isSorting = false;
                startButton.Enabled = true;
                resetButton.Enabled = true;
                algorithmCombo.Enabled = true;
                sizeNumeric.Enabled = true;
                externalElement1 = null;
                externalElement2 = null;
                canvas.Invalidate();
            }
        }

        private async Task Delay()
        {
            await Task.Delay(speedTrackBar.Value);
        }

        private async Task ShowComparison(int index1, int index2, bool showExternally = true)
        {
            if (showExternally && index2 >= 0)
            {
                externalElementIndex1 = index1;
                externalElementIndex2 = index2;
                externalElement1 = array[index1];
                externalElement2 = array[index2];
            }
            canvas.Invalidate();
            await Delay();
        }

        private void ClearExternal()
        {
            externalElement1 = null;
            externalElement2 = null;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            canvas.Invalidate();
        }

        private async Task Swap(int i, int j)
        {
            await ShowComparison(i, j);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
            ClearExternal();
            canvas.Invalidate();
            await Delay();
        }

        private async Task BubbleSort()
        {
            statusLabel.Text = "Пузырьковая сортировка";
            for (int i = 0; i < array.Length - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < array.Length - 1 - i; j++)
                {
                    await ShowComparison(j, j + 1);
                    if (array[j] > array[j + 1])
                    {
                        await Swap(j, j + 1);
                        swapped = true;
                    }
                    ClearExternal();
                }
                sortedCount = array.Length - 1 - i;
                canvas.Invalidate();
                if (!swapped) break;
            }
            sortedCount = array.Length;
        }

        private async Task SelectionSort()
        {
            statusLabel.Text = "Сортировка выбором";
            for (int i = 0; i < array.Length - 1; i++)
            {
                int minIdx = i;
                for (int j = i + 1; j < array.Length; j++)
                {
                    await ShowComparison(minIdx, j);
                    if (array[j] < array[minIdx])
                    {
                        minIdx = j;
                    }
                    ClearExternal();
                }
                if (minIdx != i)
                {
                    await Swap(i, minIdx);
                }
                sortedCount = i + 1;
                canvas.Invalidate();
            }
        }

        private async Task InsertionSort()
        {
            statusLabel.Text = "Сортировка вставками";
            for (int i = 1; i < array.Length; i++)
            {
                int key = array[i];
                int j = i - 1;

                externalElement1 = key;
                externalElementIndex1 = i;
                canvas.Invalidate();
                await Delay();

                while (j >= 0 && array[j] > key)
                {
                    await ShowComparison(j, -1, false);
                    array[j + 1] = array[j];
                    canvas.Invalidate();
                    await Delay();
                    j--;
                }
                array[j + 1] = key;
                ClearExternal();
                sortedCount = i + 1;
                canvas.Invalidate();
                await Delay();
            }
        }

        private async Task MergeSort(int left, int right)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;
                await MergeSort(left, mid);
                await MergeSort(mid + 1, right);
                await Merge(left, mid, right);
            }
        }

        private async Task Merge(int left, int mid, int right)
        {
            int n1 = mid - left + 1;
            int n2 = right - mid;

            int[] L = new int[n1];
            int[] R = new int[n2];

            for (int i = 0; i < n1; i++) L[i] = array[left + i];
            for (int j = 0; j < n2; j++) R[j] = array[mid + 1 + j];

            int iIdx = 0, jIdx = 0, k = left;

            while (iIdx < n1 && jIdx < n2)
            {
                externalElement1 = L[iIdx];
                externalElement2 = R[jIdx];
                canvas.Invalidate();
                await Delay();

                if (L[iIdx] <= R[jIdx])
                {
                    array[k] = L[iIdx];
                    iIdx++;
                }
                else
                {
                    array[k] = R[jIdx];
                    jIdx++;
                }
                canvas.Invalidate();
                await Delay();
                k++;
            }

            while (iIdx < n1)
            {
                array[k] = L[iIdx];
                iIdx++;
                k++;
                canvas.Invalidate();
                await Delay();
            }

            while (jIdx < n2)
            {
                array[k] = R[jIdx];
                jIdx++;
                k++;
                canvas.Invalidate();
                await Delay();
            }

            ClearExternal();
            sortedCount = right + 1;
            canvas.Invalidate();
        }

        private async Task QuickSort(int low, int high)
        {
            if (low < high)
            {
                int pi = await Partition(low, high);
                await QuickSort(low, pi - 1);
                await QuickSort(pi + 1, high);
            }
            else if (low == high)
            {
                sortedCount = Math.Max(sortedCount, low + 1);
                canvas.Invalidate();
            }
        }

        private async Task<int> Partition(int low, int high)
        {
            int pivot = array[high];
            int i = low - 1;

            externalElement1 = pivot;
            externalElementIndex1 = high;
            canvas.Invalidate();
            await Delay();

            for (int j = low; j < high; j++)
            {
                await ShowComparison(j, high);
                if (array[j] <= pivot)
                {
                    i++;
                    await Swap(i, j);
                }
                ClearExternal();
            }
            await Swap(i + 1, high);
            ClearExternal();
            sortedCount = i + 2;
            canvas.Invalidate();
            return i + 1;
        }

        private async Task TreeSort()
        {
            statusLabel.Text = "Древесная сортировка (построение дерева)";

            TreeNode root = null;

            for (int i = 0; i < array.Length; i++)
            {
                externalElement1 = array[i];
                externalElementIndex1 = i;
                canvas.Invalidate();
                await Delay();

                root = InsertIntoTree(root, array[i]);
                ClearExternal();
                await Delay();
            }

            statusLabel.Text = "Древесная сортировка (обход дерева)";

            List<int> sortedList = new List<int>();
            await InorderTraversal(root, sortedList);

            for (int i = 0; i < sortedList.Count; i++)
            {
                array[i] = sortedList[i];
                sortedCount = i + 1;
                canvas.Invalidate();
                await Delay();
            }
        }

        private TreeNode InsertIntoTree(TreeNode root, int value)
        {
            if (root == null)
                return new TreeNode(value);

            if (value < root.Value)
                root.Left = InsertIntoTree(root.Left, value);
            else
                root.Right = InsertIntoTree(root.Right, value);

            return root;
        }

        private async Task InorderTraversal(TreeNode node, List<int> result)
        {
            if (node != null)
            {
                await InorderTraversal(node.Left, result);
                result.Add(node.Value);
                await Delay();
                await InorderTraversal(node.Right, result);
            }
        }

        private class TreeNode
        {
            public int Value { get; set; }
            public TreeNode Left { get; set; }
            public TreeNode Right { get; set; }

            public TreeNode(int value)
            {
                Value = value;
                Left = null;
                Right = null;
            }
        }

        // Панель управления (добавляем как поле класса)
        private Panel controlPanel;
    }
}