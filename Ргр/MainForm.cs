using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace РГР
{
    public partial class MainForm : Form
    {
        private bool[] isSorted;

        private int[] array;
        private int[] originalArray;              // копия исходного массива для сброса
        private int arraySize = 30;
        private int maxValue = 100;
        private const int externalElementWidth = 40;
        private bool isSorting = false;
        private bool isPaused = false;
        private CancellationTokenSource cancellationTokenSource;

        // Для паузы используется простой флаг и цикл ожидания в Delay()
        private readonly object pauseLock = new object();

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
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            this.Resize += MainForm_Resize;

            // Включаем двойную буферизацию для устранения мерцания
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(canvas, true, null);
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(externalPanel, true, null);
            
           
            
            GenerateArray();
            originalArray = array.ToArray();
        }

        // Обработчик нажатия на кнопку "?" в заголовке окна
        protected override void OnHelpButtonClicked(CancelEventArgs e)
        {
            e.Cancel = true;  // Отключаем стандартное поведение
            HelpForm helpForm = new HelpForm();
            helpForm.Show(this);
        }

        // Обработчик нажатия клавиши F1
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                HelpForm helpForm = new HelpForm();
                helpForm.Show(this);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (canvas != null && externalPanel != null && controlPanel != null)
            {
                int margin = 10;
                externalPanel.Left = margin;
                externalPanel.Top = margin;
                externalPanel.Height = this.ClientSize.Height - controlPanel.Height - 3 * margin;
                canvas.Left = externalPanel.Right + margin;
                canvas.Top = margin;
                canvas.Width = this.ClientSize.Width - canvas.Left - margin;
                canvas.Height = externalPanel.Height;
                controlPanel.Top = canvas.Bottom + margin;
                controlPanel.Left = margin;
                controlPanel.Width = this.ClientSize.Width - 2 * margin;
            }
        }

        private void GenerateArray()
        {
            arraySize = (int)sizeNumeric.Value;
            Random rand = new Random();
            array = new int[arraySize];
            isSorted = new bool[arraySize];

            for (int i = 0; i < arraySize; i++)
            {
                array[i] = rand.Next(10, maxValue);
                isSorted[i] = false;
            }

            externalElement1 = null;
            externalElement2 = null;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            sortedCount = 0;

            canvas.Invalidate();
            externalPanel.Invalidate();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (array == null || array.Length == 0) return;

            Graphics g = e.Graphics;
            int barWidth = canvas.Width / array.Length - 2;
            if (barWidth < 1) barWidth = 1;

            for (int i = 0; i < array.Length; i++)
            {
                // Если элемент временно удалён для отображения снаружи – не рисуем его
                if (externalElementIndex1 == i || externalElementIndex2 == i)
                    continue;

                int barHeight = (int)((double)array[i] / maxValue * (canvas.Height - 50));
                int x = i * (barWidth + 2) + 2;
                int y = canvas.Height - barHeight - 20;

                Color barColor = isSorted[i] ? sortedColor : defaultColor;

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
        }

        private void ExternalPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(externalPanel.BackColor);

            int panelWidth = externalPanel.Width; // Объявляем переменную здесь

            if (externalElement1.HasValue && externalElement2.HasValue)
            {
                // Рисуем оба элемента параллельно
                int elementWidth = Math.Min(60, panelWidth / 3); // Адаптивная ширина
                int spacing = Math.Min(20, panelWidth / 10); // Адаптивный отступ

                // Вычисляем позиции так, чтобы элементы были по центру
                int totalWidth = elementWidth * 2 + spacing;
                int startX = Math.Max(0, (panelWidth - totalWidth) / 2);

                // Проверяем, чтобы элементы не выходили за границы
                if (startX + totalWidth > panelWidth)
                {
                    startX = 10; // Отступ от левого края
                    elementWidth = (panelWidth - 30) / 2; // Подгоняем ширину
                }

                // Рисуем первый элемент
                DrawExternalElement(g, externalElement1.Value, startX, 20, comparingColor, "Эл. 1");
                // Рисуем второй элемент
                DrawExternalElement(g, externalElement2.Value, startX + elementWidth + spacing, 20, swappingColor, "Эл.2");
            }
            else if (externalElement1.HasValue)
            {
                // Если только один элемент
                int elementWidth = Math.Min(60, panelWidth / 2);
                int startX = Math.Max(0, (externalPanel.Width - elementWidth) / 2);
                DrawExternalElement(g, externalElement1.Value, startX, 20, comparingColor, "Элемент");
            }
        }

        private void DrawExternalElement(Graphics g, int value, int x, int y, Color color, string label)
        {
            // Используем такую же высоту, как в основном массиве
            int maxBarHeight = canvas.Height - 50;
            int barHeight = (int)((double)value / maxValue * maxBarHeight);
            if (barHeight < 1) barHeight = 1;

            int barWidth = Math.Min(40, externalPanel.Width / 3); // Адаптивная ширина

            // Проверяем, чтобы элемент не выходил за правую границу
            if (x + barWidth > externalPanel.Width)
            {
                x = externalPanel.Width - barWidth - 5;
            }

            // Рисуем столбец
            using (Brush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y + maxBarHeight - barHeight, barWidth, barHeight);
            }
            g.DrawRectangle(Pens.Black, x, y + maxBarHeight - barHeight, barWidth, barHeight);

            // Рисуем значение над столбцом
            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            {
                string valueText = value.ToString();
                SizeF textSize = g.MeasureString(valueText, font);
                float textX = x + barWidth / 2 - textSize.Width / 2;
                g.DrawString(valueText, font, Brushes.Black,
                    textX, y + maxBarHeight - barHeight - 15);
            }

            // Рисуем подпись под столбцом
            using (Font font = new Font("Arial", 8))
            {
                SizeF textSize = g.MeasureString(label, font);
                float textX = x + barWidth / 2 - textSize.Width / 2;
                g.DrawString(label, font, Brushes.Black,
                    textX, y + maxBarHeight + 5);
            }
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            // Сохраняем исходное состояние массива
            originalArray = array.ToArray();
            isSorting = true;
            isPaused = false;
            SetControlsState(true, false);

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Сброс внешних элементов
            externalElement1 = null;
            externalElement2 = null;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            sortedCount = 0;
            for (int i = 0; i < array.Length; i++) isSorted[i] = false;
            canvas.Invalidate();
            externalPanel.Invalidate();

            try
            {
                switch (algorithmCombo.SelectedIndex)
                {
                    case 0: await BubbleSort(token); break;
                    case 1: await SelectionSort(token); break;
                    case 2: await InsertionSort(token); break;
                    case 3: await MergeSort(0, array.Length - 1, token); break;
                    case 4: await QuickSort(0, array.Length - 1, token); break;
                    case 5: await TreeSort(token); break;
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
                isPaused = false;
                SetControlsState(false, false);
                externalElement1 = null;
                externalElement2 = null;
                canvas.Invalidate();
                externalPanel.Invalidate();
            }
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (!isSorting) return;

            isPaused = !isPaused;
            pauseButton.Text = isPaused ? "Продолжить" : "Пауза";
            statusLabel.Text = isPaused ? "Пауза" : "Сортировка...";
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            // Если идёт сортировка – отменяем её
            if (isSorting)
            {
                cancellationTokenSource?.Cancel();
                isSorting = false;
                isPaused = false;
            }

            // Восстанавливаем исходный массив, если он есть
            if (originalArray != null && originalArray.Length == array.Length)
            {
                Array.Copy(originalArray, array, array.Length);
                for (int i = 0; i < array.Length; i++) isSorted[i] = false;
                sortedCount = 0;
                externalElement1 = externalElement2 = null;
                externalElementIndex1 = externalElementIndex2 = null;
                canvas.Invalidate();
                externalPanel.Invalidate();
                statusLabel.Text = "Сброшено к исходному массиву";
            }
            else
            {
                // Если по какой-то причине оригинал отсутствует – генерируем новый
                GenerateArray();
                originalArray = array.ToArray();
                statusLabel.Text = "Новый массив сгенерирован";
            }

            SetControlsState(false, false);
        }

        private void NewArrayButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            GenerateArray();
            originalArray = array.ToArray();
            statusLabel.Text = "Новый массив сгенерирован";
        }

        private void SizeNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (!isSorting)
            {
                GenerateArray();
                originalArray = array.ToArray();
            }
        }

        private void SetControlsState(bool sorting, bool paused)
        {
            algorithmCombo.Enabled = !sorting;
            sizeNumeric.Enabled = !sorting;
            newArrayButton.Enabled = !sorting;
            startButton.Enabled = !sorting;
            resetButton.Enabled = true;          // всегда доступна
            pauseButton.Enabled = sorting;       // только во время сортировки
            speedTrackBar.Enabled = true;

            pauseButton.Text = (sorting && paused) ? "Продолжить" : "Пауза";
        }

        private async Task Delay(CancellationToken token)
        {
            int delayMs = speedTrackBar.Value; // значение в мс (1..100)
            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException)
            {
                throw;
            }

            // Ожидание снятия паузы
            while (isPaused && !token.IsCancellationRequested)
            {
                await Task.Delay(50, token);
            }
            token.ThrowIfCancellationRequested();
        }

        private async Task ShowComparison(int index1, int index2, CancellationToken token)
        {
            externalElementIndex1 = index1;
            externalElementIndex2 = index2;
            externalElement1 = array[index1];
            if (index2 >= 0)
                externalElement2 = array[index2];
            else
                externalElement2 = null;

            canvas.Invalidate();
            externalPanel.Invalidate();
            await Delay(token);
        }

        private void ClearExternal()
        {
            externalElement1 = null;
            externalElement2 = null;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            canvas.Invalidate();
            externalPanel.Invalidate();
        }

        private async Task Swap(int i, int j, CancellationToken token)
        {
            await ShowComparison(i, j, token);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
            ClearExternal();
            canvas.Invalidate();
            await Delay(token);
        }

        // -------------------- Алгоритмы сортировки --------------------
        private async Task BubbleSort(CancellationToken token)
        {
            statusLabel.Text = "Пузырьковая сортировка";

            for (int i = 0; i < array.Length - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < array.Length - 1 - i; j++)
                {
                    token.ThrowIfCancellationRequested();
                    await ShowComparison(j, j + 1, token);
                    if (array[j] > array[j + 1])
                    {
                        await Swap(j, j + 1, token);
                        swapped = true;
                    }
                    ClearExternal();
                }
                isSorted[array.Length - 1 - i] = true;
                canvas.Invalidate();
                if (!swapped) break;
            }
            for (int i = 0; i < array.Length; i++) isSorted[i] = true;
        }

        private async Task SelectionSort(CancellationToken token)
        {
            statusLabel.Text = "Сортировка выбором";
            for (int i = 0; i < array.Length - 1; i++)
            {
                token.ThrowIfCancellationRequested();
                int minIdx = i;
                for (int j = i + 1; j < array.Length; j++)
                {
                    await ShowComparison(minIdx, j, token);
                    if (array[j] < array[minIdx])
                        minIdx = j;
                    ClearExternal();
                }
                if (minIdx != i)
                    await Swap(i, minIdx, token);
                isSorted[i] = true;
                canvas.Invalidate();
            }
            isSorted[array.Length - 1] = true;
        }

        private async Task InsertionSort(CancellationToken token)
        {
            statusLabel.Text = "Сортировка вставками";
            for (int i = 1; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                int key = array[i];
                int j = i - 1;

                externalElement1 = key;
                externalElementIndex1 = i;
                externalElement2 = null;
                externalElementIndex2 = null;
                canvas.Invalidate();
                externalPanel.Invalidate();
                await Delay(token);

                while (j >= 0 && array[j] > key)
                {
                    token.ThrowIfCancellationRequested();
                    // показываем сравнение с элементом j
                    externalElementIndex2 = j;
                    externalElement2 = array[j];
                    canvas.Invalidate();
                    externalPanel.Invalidate();
                    await Delay(token);

                    array[j + 1] = array[j];
                    canvas.Invalidate();
                    await Delay(token);
                    j--;
                }
                array[j + 1] = key;
                ClearExternal();
                isSorted[i] = true;
                canvas.Invalidate();
                await Delay(token);
            }
            for (int i = 0; i < array.Length; i++) isSorted[i] = true;
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
            for (int i = 0; i < n1; i++) L[i] = array[left + i];
            for (int j = 0; j < n2; j++) R[j] = array[mid + 1 + j];

            int iIdx = 0, jIdx = 0, k = left;
            while (iIdx < n1 && jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                externalElement1 = L[iIdx];
                externalElement2 = R[jIdx];
                externalPanel.Invalidate();
                await Delay(token);

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
                await Delay(token);
                k++;
            }
            while (iIdx < n1)
            {
                token.ThrowIfCancellationRequested();
                array[k] = L[iIdx];
                iIdx++; k++;
                canvas.Invalidate();
                await Delay(token);
            }
            while (jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                array[k] = R[jIdx];
                jIdx++; k++;
                canvas.Invalidate();
                await Delay(token);
            }
            ClearExternal();
            sortedCount = right + 1;
            canvas.Invalidate();
        }

        private async Task QuickSort(int low, int high, CancellationToken token)
        {
            if (low < high)
            {
                int pi = await Partition(low, high, token);
                await QuickSort(low, pi - 1, token);
                await QuickSort(pi + 1, high, token);
            }
            else if (low == high)
            {
                sortedCount = Math.Max(sortedCount, low + 1);
                canvas.Invalidate();
            }
        }

        private async Task<int> Partition(int low, int high, CancellationToken token)
        {
            int pivot = array[high];
            int i = low - 1;

            externalElement1 = pivot;
            externalElementIndex1 = high;
            externalPanel.Invalidate();
            await Delay(token);

            for (int j = low; j < high; j++)
            {
                token.ThrowIfCancellationRequested();
                await ShowComparison(j, high, token);
                if (array[j] <= pivot)
                {
                    i++;
                    await Swap(i, j, token);
                }
                ClearExternal();
            }
            await Swap(i + 1, high, token);
            ClearExternal();
            sortedCount = i + 2;
            canvas.Invalidate();
            return i + 1;
        }

        private async Task TreeSort(CancellationToken token)
        {
            statusLabel.Text = "Древесная сортировка (построение дерева)";
            TreeNode root = null;
            for (int i = 0; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                externalElement1 = array[i];
                externalElementIndex1 = i;
                externalPanel.Invalidate();
                await Delay(token);
                root = InsertIntoTree(root, array[i]);
                ClearExternal();
                await Delay(token);
            }

            statusLabel.Text = "Древесная сортировка (обход дерева)";
            List<int> sortedList = new List<int>();
            await InorderTraversal(root, sortedList, token);
            for (int i = 0; i < sortedList.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                array[i] = sortedList[i];
                sortedCount = i + 1;
                canvas.Invalidate();
                await Delay(token);
            }
        }

        private TreeNode InsertIntoTree(TreeNode root, int value)
        {
            if (root == null) return new TreeNode(value);
            if (value < root.Value)
                root.Left = InsertIntoTree(root.Left, value);
            else
                root.Right = InsertIntoTree(root.Right, value);
            return root;
        }

        private async Task InorderTraversal(TreeNode node, List<int> result, CancellationToken token)
        {
            if (node != null)
            {
                await InorderTraversal(node.Left, result, token);
                token.ThrowIfCancellationRequested();
                result.Add(node.Value);
                await Delay(token);
                await InorderTraversal(node.Right, result, token);
            }
        }

        private class TreeNode
        {
            public int Value { get; set; }
            public TreeNode Left { get; set; }
            public TreeNode Right { get; set; }
            public TreeNode(int value) { Value = value; }
        }
    }
}