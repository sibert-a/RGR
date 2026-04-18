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
        private bool[] isSorted;
        private int[] array;
        private int[] originalArray;
        private int arraySize = 15;
        private int maxValue = 100;
        private bool isSorting = false;
        private bool isPaused = false;
        private CancellationTokenSource cancellationTokenSource;

        // Цвета для отображения (голубой)
        private Color defaultColor = Color.LightSkyBlue;
        private Color sortedColor = Color.LightGreen;

        // Для отображения элементов вне массива
        private int? externalElement1 = null;
        private int? externalElement2 = null;
        private int? externalElementIndex1 = null;
        private int? externalElementIndex2 = null;
        private string comparisonSign = "";

        // Для анимации полета
        private float flyX1 = 0, flyY1 = 0;
        private float flyX2 = 0, flyY2 = 0;
        private bool isFlying1 = false;
        private bool isFlying2 = false;
        private int flyingValue1 = 0;
        private int flyingValue2 = 0;
        private int startX1, startY1, startX2, startY2;
        private int targetX1, targetY1, targetX2, targetY2;
        private const int verticalSteps = 10;
        private int currentStep = 0;

        public MainForm()
        {
            InitializeComponent();
            this.Resize += MainForm_Resize;
            this.Paint += MainForm_Paint;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(canvas, true, null);

            GenerateArray();
            originalArray = array.ToArray();
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            DrawComparisonPanel(e.Graphics);
            DrawFlyingElements(e.Graphics);
        }

        private void DrawFlyingElements(Graphics g)
        {
            int squareSize = 35;

            if (isFlying1)
            {
                float x = flyX1;
                float y = flyY1;

                using (Brush brush = new SolidBrush(defaultColor))
                {
                    g.FillRectangle(brush, x, y, squareSize, squareSize);
                }
                g.DrawRectangle(Pens.Black, x, y, squareSize, squareSize);
                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                {
                    string valueText = flyingValue1.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x + (squareSize - textSize.Width) / 2;
                    float textY = y + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }

            if (isFlying2)
            {
                float x = flyX2;
                float y = flyY2;

                using (Brush brush = new SolidBrush(defaultColor))
                {
                    g.FillRectangle(brush, x, y, squareSize, squareSize);
                }
                g.DrawRectangle(Pens.Black, x, y, squareSize, squareSize);
                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                {
                    string valueText = flyingValue2.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x + (squareSize - textSize.Width) / 2;
                    float textY = y + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
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
            comparisonSign = "";
            isFlying1 = false;
            isFlying2 = false;

            canvas.Invalidate();
        }

        private void GetElementScreenPosition(int index, out int x, out int y)
        {
            int squareSize = 35;
            int spacing = 3;

            int totalWidth = array.Length * (squareSize + spacing) - spacing;
            int startX = canvas.Left + Math.Max(10, (canvas.Width - totalWidth) / 2);
            int startY = canvas.Top + (canvas.Height - squareSize) / 2;

            x = startX + index * (squareSize + spacing);
            y = startY;
        }

        private void GetComparisonTargetPosition(int index1, int index2, out int x1, out int y1, out int x2, out int y2)
        {
            int squareSize = 35;

            GetElementScreenPosition(index1, out int elementX1, out int elementY1);
            GetElementScreenPosition(index2, out int elementX2, out int elementY2);

            int distance = Math.Abs(elementX2 - elementX1);
            int offset1, offset2;

            if (distance > 180)
            {
                offset1 = 60;
                offset2 = 60;
            }
            else if (distance > 100)
            {
                offset1 = 50;
                offset2 = 50;
            }
            else
            {
                offset1 = 40;
                offset2 = 40;
            }

            x1 = elementX1 - offset1;
            x2 = elementX2 + offset2;
            y1 = elementY1 - 70;
            y2 = elementY2 - 70;
        }

        private async Task AnimateFlyToComparison(int index1, int index2)
        {
            GetElementScreenPosition(index1, out startX1, out startY1);
            GetElementScreenPosition(index2, out startX2, out startY2);
            GetComparisonTargetPosition(index1, index2, out targetX1, out targetY1, out targetX2, out targetY2);

            flyingValue1 = array[index1];
            flyingValue2 = array[index2];
            isFlying1 = true;
            isFlying2 = true;

            for (currentStep = 0; currentStep <= verticalSteps; currentStep++)
            {
                float t = (float)currentStep / verticalSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);

                flyX1 = startX1 + (targetX1 - startX1) * easeT;
                flyY1 = startY1 + (targetY1 - startY1) * easeT;
                flyX2 = startX2 + (targetX2 - startX2) * easeT;
                flyY2 = startY2 + (targetY2 - startY2) * easeT;

                canvas.Invalidate();
                this.Invalidate();
                await Task.Delay(8);
            }

            isFlying1 = false;
            isFlying2 = false;

            externalElement1 = array[index1];
            externalElement2 = array[index2];
            externalElementIndex1 = index1;
            externalElementIndex2 = index2;

            canvas.Invalidate();
            this.Invalidate();
        }

        private async Task AnimateSingleFlyToComparison(int index)
        {
            GetElementScreenPosition(index, out startX1, out startY1);
            targetX1 = startX1;
            targetY1 = startY1 - 70;

            flyingValue1 = array[index];
            isFlying1 = true;

            for (currentStep = 0; currentStep <= verticalSteps; currentStep++)
            {
                float t = (float)currentStep / verticalSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);

                flyX1 = startX1 + (targetX1 - startX1) * easeT;
                flyY1 = startY1 + (targetY1 - startY1) * easeT;

                canvas.Invalidate();
                this.Invalidate();
                await Task.Delay(8);
            }

            isFlying1 = false;
            externalElement1 = array[index];
            externalElementIndex1 = index;
            externalElement2 = null;
            externalElementIndex2 = null;

            canvas.Invalidate();
            this.Invalidate();
        }

        private async Task AnimateFlyBack()
        {
            if (!externalElement1.HasValue && !externalElement2.HasValue) return;

            // Сохраняем значения перед анимацией
            int? savedIndex1 = externalElementIndex1;
            int? savedIndex2 = externalElementIndex2;
            int? savedValue1 = externalElement1;
            int? savedValue2 = externalElement2;

            if (savedIndex1.HasValue && savedIndex2.HasValue)
            {
                GetComparisonTargetPosition(savedIndex1.Value, savedIndex2.Value, out startX1, out startY1, out startX2, out startY2);
                GetElementScreenPosition(savedIndex1.Value, out targetX1, out targetY1);
                GetElementScreenPosition(savedIndex2.Value, out targetX2, out targetY2);
                flyingValue1 = savedValue1.Value;
                flyingValue2 = savedValue2.Value;
                isFlying1 = true;
                isFlying2 = true;
            }
            else if (savedIndex1.HasValue)
            {
                startX1 = targetX1;
                startY1 = targetY1 - 70;
                GetElementScreenPosition(savedIndex1.Value, out targetX1, out targetY1);
                flyingValue1 = savedValue1.Value;
                isFlying1 = true;
                isFlying2 = false;
            }
            else
            {
                return;
            }

            // Очищаем внешние элементы, но сохраняем индексы для анимации
            externalElement1 = null;
            externalElement2 = null;
            comparisonSign = "";
            canvas.Invalidate();
            this.Invalidate();

            for (currentStep = 0; currentStep <= verticalSteps; currentStep++)
            {
                float t = (float)currentStep / verticalSteps;
                float easeT = t;

                if (isFlying1)
                {
                    flyX1 = startX1 + (targetX1 - startX1) * easeT;
                    flyY1 = startY1 + (targetY1 - startY1) * easeT;
                }
                if (isFlying2)
                {
                    flyX2 = startX2 + (targetX2 - startX2) * easeT;
                    flyY2 = startY2 + (targetY2 - startY2) * easeT;
                }

                canvas.Invalidate();
                this.Invalidate();
                await Task.Delay(8);
            }

            isFlying1 = false;
            isFlying2 = false;
            externalElementIndex1 = null;
            externalElementIndex2 = null;

            canvas.Invalidate();
            this.Invalidate();
        }

        private async Task AnimateSingleFlyBack()
        {
            if (!externalElement1.HasValue) return;

            int? savedIndex = externalElementIndex1;
            int? savedValue = externalElement1;

            if (savedIndex.HasValue)
            {
                startX1 = targetX1;
                startY1 = targetY1 - 70;
                GetElementScreenPosition(savedIndex.Value, out targetX1, out targetY1);
                flyingValue1 = savedValue.Value;
            }

            externalElement1 = null;
            comparisonSign = "";
            isFlying1 = true;

            for (currentStep = 0; currentStep <= verticalSteps; currentStep++)
            {
                float t = (float)currentStep / verticalSteps;
                float easeT = t;

                if (isFlying1)
                {
                    flyX1 = startX1 + (targetX1 - startX1) * easeT;
                    flyY1 = startY1 + (targetY1 - startY1) * easeT;
                }

                canvas.Invalidate();
                this.Invalidate();
                await Task.Delay(8);
            }

            isFlying1 = false;
            externalElementIndex1 = null;

            canvas.Invalidate();
            this.Invalidate();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (array == null || array.Length == 0) return;

            Graphics g = e.Graphics;

            int squareSize = 35;
            int spacing = 3;

            int totalWidth = array.Length * (squareSize + spacing) - spacing;
            int startX = Math.Max(10, (canvas.Width - totalWidth) / 2);
            int startY = (canvas.Height - squareSize) / 2;

            for (int i = 0; i < array.Length; i++)
            {
                int x = startX + i * (squareSize + spacing);
                int y = startY;

                if ((externalElementIndex1 == i && isFlying1) || (externalElementIndex2 == i && isFlying2))
                {
                    continue;
                }

                Color backColor = isSorted[i] ? sortedColor : defaultColor;

                using (Brush brush = new SolidBrush(backColor))
                {
                    g.FillRectangle(brush, x, y, squareSize, squareSize);
                }
                g.DrawRectangle(Pens.Black, x, y, squareSize, squareSize);

                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                {
                    string valueText = array[i].ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x + (squareSize - textSize.Width) / 2;
                    float textY = y + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }
        }

        private void DrawComparisonPanel(Graphics g)
        {
            if (externalElement1.HasValue && externalElement2.HasValue && !isFlying1 && !isFlying2 && externalElementIndex1.HasValue && externalElementIndex2.HasValue)
            {
                int squareSize = 35;

                GetComparisonTargetPosition(externalElementIndex1.Value, externalElementIndex2.Value, out int x1, out int y1, out int x2, out int y2);

                // Рисуем первый элемент
                using (Brush brush = new SolidBrush(defaultColor))
                {
                    g.FillRectangle(brush, x1, y1, squareSize, squareSize);
                }
                g.DrawRectangle(Pens.Black, x1, y1, squareSize, squareSize);
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                {
                    string valueText = externalElement1.Value.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x1 + (squareSize - textSize.Width) / 2;
                    float textY = y1 + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }

                // Рисуем знак сравнения между элементами
                int centerX = (x1 + x2) / 2 + squareSize / 2;
                int centerY = y1 + squareSize / 2;

                using (Font font = new Font("Arial", 18, FontStyle.Bold))
                {
                    SizeF signSize = g.MeasureString(comparisonSign, font);
                    float signX = centerX - signSize.Width / 2;
                    float signY = centerY - signSize.Height / 2;
                    g.DrawString(comparisonSign, font, Brushes.DarkRed, signX, signY);
                }

                // Рисуем второй элемент
                using (Brush brush = new SolidBrush(defaultColor))
                {
                    g.FillRectangle(brush, x2, y2, squareSize, squareSize);
                }
                g.DrawRectangle(Pens.Black, x2, y2, squareSize, squareSize);
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                {
                    string valueText = externalElement2.Value.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x2 + (squareSize - textSize.Width) / 2;
                    float textY = y2 + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            originalArray = array.ToArray();
            isSorting = true;
            isPaused = false;
            SetControlsState(true, false);

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            externalElement1 = null;
            externalElement2 = null;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            comparisonSign = "";
            for (int i = 0; i < array.Length; i++) isSorted[i] = false;
            canvas.Invalidate();
            this.Invalidate();

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
                for (int i = 0; i < array.Length; i++) isSorted[i] = true;
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
                externalElement1 = null;
                externalElement2 = null;
                canvas.Invalidate();
                this.Invalidate();
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

            if (originalArray != null && originalArray.Length == array.Length)
            {
                Array.Copy(originalArray, array, array.Length);
                for (int i = 0; i < array.Length; i++) isSorted[i] = false;
                externalElement1 = externalElement2 = null;
                externalElementIndex1 = externalElementIndex2 = null;
                comparisonSign = "";
                canvas.Invalidate();
                this.Invalidate();
            }
            else
            {
                GenerateArray();
                originalArray = array.ToArray();
            }

            SetControlsState(false, false);
        }

        private void NewArrayButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;
            GenerateArray();
            originalArray = array.ToArray();
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
            resetButton.Enabled = true;
            pauseButton.Enabled = sorting;
            speedTrackBar.Enabled = true;
            pauseButton.Text = (sorting && paused) ? "Продолжить" : "Пауза";
        }

        private async Task Delay(CancellationToken token)
        {
            int delayMs = speedTrackBar.Value;
            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException)
            {
                throw;
            }

            while (isPaused && !token.IsCancellationRequested)
            {
                await Task.Delay(50, token);
            }
            token.ThrowIfCancellationRequested();
        }

        private async Task ShowComparison(int index1, int index2, CancellationToken token)
        {
            await AnimateFlyToComparison(index1, index2);

            if (index2 >= 0 && index2 < array.Length)
            {
                if (array[index1] < array[index2])
                    comparisonSign = "<";
                else if (array[index1] > array[index2])
                    comparisonSign = ">";
                else
                    comparisonSign = "=";
            }

            this.Invalidate();
            await Delay(token);
        }

        private async Task ClearExternal()
        {
            await AnimateFlyBack();
        }

        private async Task ClearSingleExternal()
        {
            await AnimateSingleFlyBack();
        }

        private async Task Swap(int i, int j, CancellationToken token)
        {
            await ShowComparison(i, j, token);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
            await ClearExternal();
            canvas.Invalidate();
            await Delay(token);
        }

        // -------------------- Пузырьковая сортировка --------------------
        private async Task BubbleSort(CancellationToken token)
        {
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
                    await ClearExternal();
                }
                isSorted[array.Length - 1 - i] = true;
                canvas.Invalidate();
                if (!swapped) break;
            }
        }

        // -------------------- Сортировка выбором --------------------
        private async Task SelectionSort(CancellationToken token)
        {
            for (int i = 0; i < array.Length - 1; i++)
            {
                token.ThrowIfCancellationRequested();
                int minIdx = i;
                for (int j = i + 1; j < array.Length; j++)
                {
                    await ShowComparison(minIdx, j, token);
                    if (array[j] < array[minIdx])
                        minIdx = j;
                    await ClearExternal();
                }
                if (minIdx != i)
                {
                    await Swap(i, minIdx, token);
                }
                isSorted[i] = true;
                canvas.Invalidate();
            }
        }

        // -------------------- Сортировка вставками --------------------
        private async Task InsertionSort(CancellationToken token)
        {
            for (int i = 1; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                int key = array[i];
                int j = i - 1;

                await AnimateSingleFlyToComparison(i);
                this.Invalidate();
                await Delay(token);

                while (j >= 0 && array[j] > key)
                {
                    token.ThrowIfCancellationRequested();
                    await ShowComparison(j, i, token);
                    array[j + 1] = array[j];
                    canvas.Invalidate();
                    await Delay(token);
                    await ClearExternal();
                    j--;
                }
                array[j + 1] = key;
                await ClearSingleExternal();
                isSorted[i] = true;
                canvas.Invalidate();
                await Delay(token);
            }
        }

        // -------------------- Сортировка слиянием --------------------
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

                await ShowComparison(left + iIdx, mid + 1 + jIdx, token);

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

                await ClearExternal();
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

            canvas.Invalidate();
        }

        // -------------------- Быстрая сортировка --------------------
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
            int pivot = array[high];
            int i = low - 1;

            await AnimateSingleFlyToComparison(high);
            this.Invalidate();
            await Delay(token);
            await ClearSingleExternal();

            for (int j = low; j < high; j++)
            {
                token.ThrowIfCancellationRequested();
                await ShowComparison(j, high, token);
                if (array[j] <= pivot)
                {
                    i++;
                    await Swap(i, j, token);
                }
                await ClearExternal();
            }
            await Swap(i + 1, high, token);
            await ClearExternal();
            canvas.Invalidate();
            return i + 1;
        }

        // -------------------- Древесная сортировка --------------------
        private async Task TreeSort(CancellationToken token)
        {
            TreeNode root = null;
            for (int i = 0; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                await AnimateSingleFlyToComparison(i);
                root = InsertIntoTree(root, array[i]);
                await ClearSingleExternal();
                await Delay(token);
            }

            List<int> sortedList = new List<int>();
            await InorderTraversal(root, sortedList, token);
            for (int i = 0; i < sortedList.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                array[i] = sortedList[i];
                isSorted[i] = true;
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