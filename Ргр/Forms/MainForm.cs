using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RGR_TIMP_S4.Forms;
using RGR_TIMP_S4.Render;
using RGR_TIMP_S4.SortingCore;

namespace RGR_TIMP_S4
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
            // Создаём контекст сортировки, передавая canvas для отрисовки и isPaused для паузы
            sortingContext = new SortingContext(canvas, speedTrackBar, () => isPaused);
            GenerateArray();
            originalArray = sortingContext.Array.ToArray();
        }

        private void ConfigureForm()
        {
            HelpButton = true;
            MaximizeBox = true;
            MinimizeBox = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized; // Запуск в полный экран
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            Resize += MainForm_Resize;

            // Включаем двойную буферизацию для canvas, чтобы избежать мерцания при отрисовке
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
            // F1 — быстрый доступ к справке
            if (e.KeyCode == Keys.F1)
                ShowHelpForm();
        }

        private void ShowHelpForm()
        {
            // Окно справки создаётся один раз и не дублируется
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
            // При ресайзе синхронизируем позиции всех визуальных элементов с новым размером canvas
            sortingContext?.SyncAllPositions();
        }

        private void AdjustLayout()
        {
            if (canvas == null || sidePanel == null) return;
            const int margin = 10;
            // Боковая панель прижата к правому краю
            sidePanel.Left = ClientSize.Width - sidePanel.Width - margin;
            sidePanel.Height = ClientSize.Height - 2 * margin;

            // Canvas занимает всё оставшееся пространство
            int canvasWidth = ClientSize.Width - sidePanel.Width - 3 * margin;
            canvas.Width = canvasWidth;
            canvas.Left = margin;
            canvas.Top = 10;
            canvas.Height = ClientSize.Height - 2 * margin;
        }
        #endregion

        #region Генерация и восстановление массива
        private void GenerateArray()
        {
            // Генерируем новый случайный массив
            sortingContext.GenerateArray((int)sizeNumeric.Value, MaxValue);
            originalArray = sortingContext.Array.ToArray();
        }

        private void RestoreOriginalArray()
        {
            // Восстанавливаем массив до состояния перед началом сортировки
            if (originalArray != null && originalArray.Length == sortingContext.Array.Length)
                sortingContext.LoadArray(originalArray);
            else
                GenerateArray();
        }
        #endregion

        #region Управление сортировкой
        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isSorting) return;

            // Сохраняем исходный массив для возможности сброса
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
            catch (OperationCanceledException) { /* Отмена операции — штатное поведение при сбросе */ }
            finally
            {
                isSorting = false;
                isPaused = false;
                SetControlsState(false, false);
                sortingContext.ResetVisuals();
                SortingAlgorithms.TreeRoot = null; // Сброс дерева после древесной сортировки
                sortingContext.ArrayYOffset = 0; // Сброс смещения
            }
        }

        private void ResetSortingState()
        {
            sortingContext.ResetVisuals();
            SortingAlgorithms.TreeRoot = null;
            sortingContext.ArrayYOffset = 0;
            // Сбрасываем все пометки отсортированных элементов
            for (int i = 0; i < sortingContext.Array.Length; i++)
                sortingContext.IsSorted[i] = false;
            canvas.Invalidate();
        }

        private Task RunSelectedAlgorithm(CancellationToken token)
        {
            // Выбор алгоритма по индексу в ComboBox
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
            // После завершения сортировки помечаем все элементы как отсортированные (зелёные)
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
                cancellationTokenSource?.Cancel(); // Отменяем текущую сортировку
                isSorting = false;
                isPaused = false;
            }
            SortingAlgorithms.TreeRoot = null;
            sortingContext.ArrayYOffset = 0;
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
            // Изменение размера массива доступно только когда сортировка не идёт
            if (!isSorting)
            {
                GenerateArray();
                originalArray = sortingContext.Array.ToArray();
            }
        }

        private void SetControlsState(bool sorting, bool paused)
        {
            // Блокируем/разблокируем элементы управления в зависимости от состояния
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
            // Основной массив
            ArrayRenderer.Draw(e.Graphics, sortingContext.Scene);
            // Дерево для древесной сортировки (если активно)
            if (SortingAlgorithms.TreeRoot != null)
            {
                int treeWidth = GetTreeWidth(SortingAlgorithms.TreeRoot);
                int treeX = (canvas.Width - treeWidth) / 2;
                int treeY = canvas.Height / 2 - 50;
                TreeRenderer.Draw(e.Graphics, SortingAlgorithms.TreeRoot, new Point(treeX, treeY), 0);
            }
        }

        private int GetTreeWidth(TreeNodeVisual root)
        {
            // Вычисление ширины дерева для центрирования
            if (root == null) return 0;
            float minX = float.MaxValue, maxX = float.MinValue;
            FindBounds(root, ref minX, ref maxX);
            return (int)((maxX - minX) * 30) + 28;
        }

        private void FindBounds(TreeNodeVisual node, ref float minX, ref float maxX)
        {
            // Рекурсивный обход дерева для нахождения границ по X
            if (node == null) return;
            if (node.X < minX) minX = node.X;
            if (node.X > maxX) maxX = node.X;
            FindBounds(node.Left, ref minX, ref maxX);
            FindBounds(node.Right, ref minX, ref maxX);
        }
        #endregion
    }
}