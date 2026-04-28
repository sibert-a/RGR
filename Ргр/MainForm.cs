using System;
using System.ComponentModel;
using System.Drawing;
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
        private const int MaxValue = 100;
        private bool isSorting = false;
        private bool isPaused = false;
        private CancellationTokenSource cancellationTokenSource;
        private HelpForm activeHelpForm = null;
        #endregion

        #region Конструктор и инициализация
        public MainForm()
        {
            InitializeComponent();
            ConfigureForm();
            sortingContext = new SortingContext(canvas, speedTrackBar, () => isPaused);
            GenerateArray();
            originalArray = sortingContext.Array.ToArray();
        }

        private void ConfigureForm()
        {
            HelpButton = true;
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            Resize += MainForm_Resize;

            // Двойная буферизация панели
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(canvas, true, null);
        }
        #endregion

        #region Обработчики формы
        protected override void OnHelpButtonClicked(CancelEventArgs e)
        {
            e.Cancel = true;
            ShowHelpForm();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
                ShowHelpForm();
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
            AdjustLayout();
        }

        private void AdjustLayout()
        {
            if (canvas == null || sidePanel == null) return;
            const int margin = 10;
            sidePanel.Left = ClientSize.Width - sidePanel.Width - margin;
            sidePanel.Height = ClientSize.Height - 2 * margin;

            int canvasWidth = ClientSize.Width - sidePanel.Width - 3 * margin;
            canvas.Width = canvasWidth;
            canvas.Left = margin;
            canvas.Top = 200;
        }
        #endregion

        #region Генерация и восстановление массива
        private void GenerateArray()
        {
            sortingContext.GenerateArray((int)sizeNumeric.Value, MaxValue);
            originalArray = sortingContext.Array.ToArray();
        }

        private void RestoreOriginalArray()
        {
            if (originalArray != null && originalArray.Length == sortingContext.Array.Length)
            {
                sortingContext.LoadArray(originalArray);
            }
            else
            {
                GenerateArray();
            }
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

            ResetSortingState();

            try
            {
                await RunSelectedAlgorithm(token);
                MarkAllSorted();
            }
            catch (OperationCanceledException) { }
            finally
            {
                isSorting = false;
                isPaused = false;
                SetControlsState(false, false);
                sortingContext.ResetVisuals();
            }
        }

        private void ResetSortingState()
        {
            sortingContext.ResetVisuals();
            for (int i = 0; i < sortingContext.Array.Length; i++)
                sortingContext.IsSorted[i] = false;
            canvas.Invalidate();
        }

        private Task RunSelectedAlgorithm(CancellationToken token)
        {
            return algorithmCombo.SelectedIndex switch
            {
                0 => SortingAlgorithms.BubbleSort(sortingContext, token),
                1 => SortingAlgorithms.SelectionSort(sortingContext, token),
                2 => SortingAlgorithms.InsertionSort(sortingContext, token),
                3 => SortingAlgorithms.MergeSort(sortingContext, 0, sortingContext.Array.Length - 1, token),
                4 => SortingAlgorithms.QuickSort(sortingContext, 0, sortingContext.Array.Length - 1, token),
                5 => SortingAlgorithms.TreeSort(sortingContext, token),
                _ => Task.CompletedTask
            };
        }

        private void MarkAllSorted()
        {
            for (int i = 0; i < sortingContext.Array.Length; i++)
                sortingContext.MarkSorted(i);
            canvas.Invalidate();
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

            RestoreOriginalArray();
            sortingContext.ResetVisuals();
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

        #region Отрисовка
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            ArrayRenderer.Draw(e.Graphics, sortingContext, canvas.ClientSize);
        }
        #endregion
    }
}