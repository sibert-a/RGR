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
        #region Поля и константы

        private bool[] isSorted;
        private int[] array;
        private int[] originalArray;
        private int arraySize = 15;
        private int maxValue = 100;
        private bool isSorting = false;
        private bool isPaused = false;
        private CancellationTokenSource cancellationTokenSource;

        // Цвета для отображения
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

        #endregion

        #region Конструктор и инициализация

        public MainForm()
        {
            InitializeComponent();

            // ========== ДЛЯ КНОПКИ СПРАВКИ ==========
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            this.Resize += MainForm_Resize;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(canvas, true, null);

            GenerateArray();
            originalArray = array.ToArray();
        }

        #endregion

        #region Обработчики событий формы (Help, Resize)

        protected override void OnHelpButtonClicked(CancelEventArgs e)
        {
            e.Cancel = true;
            HelpForm helpForm = new HelpForm();
            helpForm.Show(this);
        }

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

        #endregion

        #region Вспомогательные методы для координат

        private void GetElementScreenPosition(int index, out int x, out int y)
        {
            int squareSize = 35;
            int spacing = 3;

            int totalWidth = array.Length * (squareSize + spacing) - spacing;
            int startX = Math.Max(10, (canvas.Width - totalWidth) / 2);
            int startY = (canvas.Height - squareSize) / 2;

            x = startX + index * (squareSize + spacing);
            y = startY;
        }

        private void GetComparisonTargetPosition(int index1, int index2, out int x1, out int y1, out int x2, out int y2)
        {
            int squareSize = 35;
            int verticalOffset = 70;

            GetElementScreenPosition(index1, out int elemX1, out int elemY1);
            GetElementScreenPosition(index2, out int elemX2, out int elemY2);

            float centerX1 = elemX1 + squareSize / 2f;
            float centerX2 = elemX2 + squareSize / 2f;
            float midX = (centerX1 + centerX2) / 2f;

            int desiredGap = 80;
            float halfGap = desiredGap / 2f;

            float targetCenterX1 = midX - halfGap;
            float targetCenterX2 = midX + halfGap;

            x1 = (int)(targetCenterX1 - squareSize / 2f);
            x2 = (int)(targetCenterX2 - squareSize / 2f);

            y1 = elemY1 - verticalOffset;
            y2 = elemY2 - verticalOffset;

            x1 = Math.Max(5, x1);
            x2 = Math.Min(canvas.Width - squareSize - 5, x2);
        }

        #endregion

        #region Анимации

        private async Task AnimateFlyToComparison(int index1, int index2)
        {
            externalElementIndex1 = index1;
            externalElementIndex2 = index2;

            GetElementScreenPosition(index1, out int startX1, out int startY1);
            GetElementScreenPosition(index2, out int startX2, out int startY2);
            GetComparisonTargetPosition(index1, index2, out int targetX1, out int targetY1, out int targetX2, out int targetY2);

            flyingValue1 = array[index1];
            flyingValue2 = array[index2];
            isFlying1 = true;
            isFlying2 = true;

            int verticalSteps = 8;
            for (int step = 0; step <= verticalSteps; step++)
            {
                float t = (float)step / verticalSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                flyX1 = startX1;
                flyY1 = startY1 + (targetY1 - startY1) * easeT;
                flyX2 = startX2;
                flyY2 = startY2 + (targetY2 - startY2) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int diff = Math.Abs(index1 - index2);
            int horizontalSteps = Math.Max(diff, 10);
            for (int step = 0; step <= horizontalSteps; step++)
            {
                float t = (float)step / horizontalSteps;
                float easeT = 1 - (1 - t) * (1 - t);
                flyX1 = startX1 + (targetX1 - startX1) * easeT;
                flyY1 = targetY1;
                flyX2 = startX2 + (targetX2 - startX2) * easeT;
                flyY2 = targetY2;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            isFlying1 = false;
            isFlying2 = false;
            externalElement1 = array[index1];
            externalElement2 = array[index2];
            canvas.Invalidate();
        }

        private async Task AnimateSingleFlyToComparison(int index)
        {
            externalElementIndex1 = index;

            GetElementScreenPosition(index, out int startX, out int startY);
            int targetY = startY - 70;

            flyingValue1 = array[index];
            isFlying1 = true;

            int steps = 8;
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                flyX1 = startX;
                flyY1 = startY + (targetY - startY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            isFlying1 = false;
            externalElement1 = array[index];
            externalElement2 = null;
            externalElementIndex2 = null;
            canvas.Invalidate();
        }

        private async Task AnimateFlyBack()
        {
            if (!externalElement1.HasValue && !externalElement2.HasValue) return;

            int? savedIndex1 = externalElementIndex1;
            int? savedIndex2 = externalElementIndex2;
            int? savedValue1 = externalElement1;
            int? savedValue2 = externalElement2;

            if (savedIndex1.HasValue && savedIndex2.HasValue)
            {
                GetComparisonTargetPosition(savedIndex1.Value, savedIndex2.Value, out int currentX1, out int currentY1, out int currentX2, out int currentY2);
                GetElementScreenPosition(savedIndex1.Value, out int targetX1, out int targetY1);
                GetElementScreenPosition(savedIndex2.Value, out int targetX2, out int targetY2);

                flyingValue1 = savedValue1.Value;
                flyingValue2 = savedValue2.Value;
                isFlying1 = true;
                isFlying2 = true;

                externalElement1 = null;
                externalElement2 = null;
                comparisonSign = "";
                canvas.Invalidate();

                int diff = Math.Abs((int)savedIndex1 - (int)savedIndex2);
                int horizontalSteps = Math.Max(diff, 10);
                for (int step = 0; step <= horizontalSteps; step++)
                {
                    float t = (float)step / horizontalSteps;
                    float easeT = 1 - (1 - t) * (1 - t);
                    flyX1 = currentX1 + (targetX1 - currentX1) * easeT;
                    flyY1 = currentY1;
                    flyX2 = currentX2 + (targetX2 - currentX2) * easeT;
                    flyY2 = currentY2;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }

                int verticalSteps = 8;
                for (int step = 0; step <= verticalSteps; step++)
                {
                    float t = (float)step / verticalSteps;
                    float easeT = t * t;
                    flyX1 = targetX1;
                    flyY1 = currentY1 + (targetY1 - currentY1) * easeT;
                    flyX2 = targetX2;
                    flyY2 = currentY2 + (targetY2 - currentY2) * easeT;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }
            }
            else if (savedIndex1.HasValue)
            {
                GetElementScreenPosition(savedIndex1.Value, out int targetX, out int targetY);
                int currentY = targetY - 70;
                flyingValue1 = savedValue1.Value;
                isFlying1 = true;
                externalElement1 = null;
                comparisonSign = "";
                canvas.Invalidate();

                int steps = 8;
                for (int step = 0; step <= steps; step++)
                {
                    float t = (float)step / steps;
                    float easeT = t * t;
                    flyX1 = targetX;
                    flyY1 = currentY + (targetY - currentY) * easeT;
                    canvas.Invalidate();
                    await Task.Delay(5);
                }
            }

            isFlying1 = false;
            isFlying2 = false;
            externalElementIndex1 = null;
            externalElementIndex2 = null;
            canvas.Invalidate();
        }

        private async Task AnimateSingleFlyBack()
        {
            await AnimateFlyBack();
        }

        private async Task AnimateSwapOnTop(int index1, int index2)
        {
            GetComparisonTargetPosition(index1, index2, out int x1, out int y1, out int x2, out int y2);

            int val1 = array[index1];
            int val2 = array[index2];

            externalElement1 = null;
            externalElement2 = null;
            comparisonSign = "";
            flyingValue1 = val1;
            flyingValue2 = val2;
            isFlying1 = true;
            isFlying2 = true;
            flyX1 = x1; flyY1 = y1;
            flyX2 = x2; flyY2 = y2;
            canvas.Invalidate();

            float startX1 = x1, startY1 = y1;
            float startX2 = x2, startY2 = y2;
            int steps = 12;
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                flyX1 = startX1 + (startX2 - startX1) * t;
                flyY1 = startY1 + (startY2 - startY1) * t;
                flyX2 = startX2 + (startX1 - startX2) * t;
                flyY2 = startY2 + (startY1 - startY2) * t;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            isFlying1 = false;
            isFlying2 = false;
            externalElement1 = val1;
            externalElement2 = val2;
            externalElementIndex1 = index1;
            externalElementIndex2 = index2;
            canvas.Invalidate();
        }

        /// <summary>
        /// Анимация перемещения одного элемента с позиции sourceIndex на заданные координаты (targetX, targetY)
        /// </summary>
        private async Task AnimateFlyToPosition(int sourceIndex, int targetX, int targetY)
        {
            GetElementScreenPosition(sourceIndex, out int startX, out int startY);

            externalElementIndex1 = sourceIndex;
            flyingValue1 = array[sourceIndex];
            isFlying1 = true;

            int verticalSteps = 8;
            int liftY = startY - 70;
            for (int step = 0; step <= verticalSteps; step++)
            {
                float t = (float)step / verticalSteps;
                float easeT = 1 - (float)Math.Pow(1 - t, 2);
                flyX1 = startX;
                flyY1 = startY + (liftY - startY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int horizontalSteps = 10;
            for (int step = 0; step <= horizontalSteps; step++)
            {
                float t = (float)step / horizontalSteps;
                float easeT = 1 - (1 - t) * (1 - t);
                flyX1 = startX + (targetX - startX) * easeT;
                flyY1 = liftY;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            int downSteps = 8;
            for (int step = 0; step <= downSteps; step++)
            {
                float t = (float)step / downSteps;
                float easeT = t * t;
                flyX1 = targetX;
                flyY1 = liftY + (targetY - liftY) * easeT;
                canvas.Invalidate();
                await Task.Delay(5);
            }

            isFlying1 = false;
            externalElement1 = null;
            externalElementIndex1 = null;
            canvas.Invalidate();
        }

        #endregion

        #region Отрисовка на Canvas

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
                    continue;
                if (externalElementIndex1 == i && externalElement1.HasValue)
                    continue;
                if (externalElementIndex2 == i && externalElement2.HasValue)
                    continue;

                Color backColor = isSorted[i] ? sortedColor : defaultColor;
                using (Brush brush = new SolidBrush(backColor))
                    g.FillRectangle(brush, x, y, squareSize, squareSize);
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

            DrawFlyingElementsOnCanvas(g, squareSize);
            DrawComparisonPanelOnCanvas(g, squareSize);
        }

        private void DrawFlyingElementsOnCanvas(Graphics g, int squareSize)
        {
            if (isFlying1)
            {
                using (Brush brush = new SolidBrush(defaultColor))
                    g.FillRectangle(brush, flyX1, flyY1, squareSize, squareSize);
                g.DrawRectangle(Pens.Black, flyX1, flyY1, squareSize, squareSize);
                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                {
                    string valueText = flyingValue1.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = flyX1 + (squareSize - textSize.Width) / 2;
                    float textY = flyY1 + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }

            if (isFlying2)
            {
                using (Brush brush = new SolidBrush(defaultColor))
                    g.FillRectangle(brush, flyX2, flyY2, squareSize, squareSize);
                g.DrawRectangle(Pens.Black, flyX2, flyY2, squareSize, squareSize);
                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                {
                    string valueText = flyingValue2.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = flyX2 + (squareSize - textSize.Width) / 2;
                    float textY = flyY2 + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }
        }

        private void DrawComparisonPanelOnCanvas(Graphics g, int squareSize)
        {
            if (isFlying1 || isFlying2) return;

            if (externalElement1.HasValue && externalElement2.HasValue &&
                externalElementIndex1.HasValue && externalElementIndex2.HasValue)
            {
                GetComparisonTargetPosition(externalElementIndex1.Value, externalElementIndex2.Value,
                    out int x1, out int y1, out int x2, out int y2);

                using (Brush brush = new SolidBrush(defaultColor))
                    g.FillRectangle(brush, x1, y1, squareSize, squareSize);
                g.DrawRectangle(Pens.Black, x1, y1, squareSize, squareSize);
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                {
                    string valueText = externalElement1.Value.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x1 + (squareSize - textSize.Width) / 2;
                    float textY = y1 + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }

                int centerX = (x1 + x2) / 2 + squareSize / 2;
                int centerY = y1 + squareSize / 2;
                using (Font font = new Font("Arial", 18, FontStyle.Bold))
                {
                    SizeF signSize = g.MeasureString(comparisonSign, font);
                    float signX = centerX - signSize.Width / 2;
                    float signY = centerY - signSize.Height / 2;
                    g.DrawString(comparisonSign, font, Brushes.DarkRed, signX, signY);
                }

                using (Brush brush = new SolidBrush(defaultColor))
                    g.FillRectangle(brush, x2, y2, squareSize, squareSize);
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
            else if (externalElement1.HasValue && externalElementIndex1.HasValue)
            {
                GetElementScreenPosition(externalElementIndex1.Value, out int x, out int y);
                int targetY = y - 70;
                using (Brush brush = new SolidBrush(defaultColor))
                    g.FillRectangle(brush, x, targetY, squareSize, squareSize);
                g.DrawRectangle(Pens.Black, x, targetY, squareSize, squareSize);
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                {
                    string valueText = externalElement1.Value.ToString();
                    SizeF textSize = g.MeasureString(valueText, font);
                    float textX = x + (squareSize - textSize.Width) / 2;
                    float textY = targetY + (squareSize - textSize.Height) / 2;
                    g.DrawString(valueText, font, Brushes.Black, textX, textY);
                }
            }
        }

        #endregion

        #region Управление сортировкой

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

        #endregion

        #region Вспомогательные методы сортировки

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

            canvas.Invalidate();
            await Delay(token);
        }

        private async Task ClearExternal()
        {
            await AnimateFlyBack();
        }

        private async Task ClearSingleExternal()
        {
            await AnimateFlyBack();
        }

        private async Task Swap(int i, int j, CancellationToken token)
        {
            bool needLift = !(externalElementIndex1 == i && externalElementIndex2 == j && externalElement1.HasValue && externalElement2.HasValue);
            if (needLift)
            {
                await AnimateFlyToComparison(i, j);
                canvas.Invalidate();
                await Delay(token);
            }

            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;

            await AnimateSwapOnTop(i, j);

            comparisonSign = "";
            canvas.Invalidate();

            await ClearExternal();
            canvas.Invalidate();
            await Delay(token);
        }

        #endregion

        #region Алгоритмы сортировки

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
                    else
                    {
                        await ClearExternal();
                    }
                }
                isSorted[array.Length - 1 - i] = true;
                canvas.Invalidate();
                if (!swapped) break;
            }
        }

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

        private async Task InsertionSort(CancellationToken token)
        {
            isSorted[0] = true;
            for (int i = 1; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                for (int j = i; j > 0; j--)
                {
                    await ShowComparison(j - 1, j, token);
                    if (array[j - 1] > array[j])
                    {
                        await Swap(j - 1, j, token);
                    }
                    else
                    {
                        await ClearExternal();
                        break;
                    }
                }
                for (int k = 0; k <= i; k++)
                    isSorted[k] = true;
                canvas.Invalidate();
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

            for (int i = 0; i < n1; i++) L[i] = array[left + i];
            for (int j = 0; j < n2; j++) R[j] = array[mid + 1 + j];

            int iIdx = 0, jIdx = 0, k = left;

            if (externalElement1.HasValue || externalElement2.HasValue)
                await ClearExternal();

            while (iIdx < n1 && jIdx < n2)
            {
                token.ThrowIfCancellationRequested();

                await ShowComparison(left + iIdx, mid + 1 + jIdx, token);
                await ClearExternal();

                int sourceIdx;
                if (L[iIdx] <= R[jIdx])
                    sourceIdx = left + iIdx;
                else
                    sourceIdx = mid + 1 + jIdx;

                GetElementScreenPosition(k, out int targetX, out int targetY);
                await AnimateFlyToPosition(sourceIdx, targetX, targetY);

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
                int sourceIdx = left + iIdx;
                GetElementScreenPosition(k, out int targetX, out int targetY);
                await AnimateFlyToPosition(sourceIdx, targetX, targetY);
                array[k] = L[iIdx];
                iIdx++; k++;
                canvas.Invalidate();
                await Delay(token);
            }

            while (jIdx < n2)
            {
                token.ThrowIfCancellationRequested();
                int sourceIdx = mid + 1 + jIdx;
                GetElementScreenPosition(k, out int targetX, out int targetY);
                await AnimateFlyToPosition(sourceIdx, targetX, targetY);
                array[k] = R[jIdx];
                jIdx++; k++;
                canvas.Invalidate();
                await Delay(token);
            }

            externalElement1 = externalElement2 = null;
            externalElementIndex1 = externalElementIndex2 = null;
            comparisonSign = "";
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
        }

        private async Task<int> Partition(int low, int high, CancellationToken token)
        {
            int pivot = array[high];
            int i = low - 1;

            await AnimateSingleFlyToComparison(high);
            canvas.Invalidate();
            await Delay(token);
            await ClearSingleExternal();

            for (int j = low; j < high; j++)
            {
                token.ThrowIfCancellationRequested();
                await ShowComparison(j, high, token);
                await ClearExternal();
                if (array[j] <= pivot)
                {
                    i++;
                    if (i != j)
                        await Swap(i, j, token);
                }
            }
            if (i + 1 != high)
            {
                await Swap(i + 1, high, token);
            }
            canvas.Invalidate();
            return i + 1;
        }

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

        #endregion

        #region Вспомогательный класс TreeNode

        private class TreeNode
        {
            public int Value { get; set; }
            public TreeNode Left { get; set; }
            public TreeNode Right { get; set; }
            public TreeNode(int value) { Value = value; }
        }

        #endregion
    }
}