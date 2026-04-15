namespace РГР
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Настройка формы
            this.Text = "Визуализация алгоритмов сортировки";
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.SystemColors.Control;

            // Canvas панель
            this.canvas = new Panel();
            this.canvas.Location = new System.Drawing.Point(10, 10);
            this.canvas.Size = new System.Drawing.Size(1160, 400);
            this.canvas.BackColor = System.Drawing.Color.White;
            this.canvas.BorderStyle = BorderStyle.FixedSingle;
            this.canvas.Paint += new PaintEventHandler(this.Canvas_Paint);

            // Панель управления
            this.controlPanel = new Panel();
            this.controlPanel.Location = new System.Drawing.Point(10, 420);
            this.controlPanel.Size = new System.Drawing.Size(1160, 230);
            this.controlPanel.BackColor = System.Drawing.Color.LightGray;

            // Метка "Алгоритм"
            Label algoLabel = new Label();
            algoLabel.Text = "Алгоритм:";
            algoLabel.Location = new System.Drawing.Point(10, 15);
            algoLabel.Size = new System.Drawing.Size(70, 25);

            // ComboBox алгоритмов
            this.algorithmCombo = new ComboBox();
            this.algorithmCombo.Location = new System.Drawing.Point(90, 12);
            this.algorithmCombo.Size = new System.Drawing.Size(180, 25);
            this.algorithmCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.algorithmCombo.Items.AddRange(new object[] {
                "Пузырьковая (Bubble Sort)",
                "Сортировка выбором (Selection Sort)",
                "Сортировка вставками (Insertion Sort)",
                "Сортировка слиянием (Merge Sort)",
                "Быстрая сортировка (Quick Sort)",
                "Древесная (Tree Sort)"
            });
            this.algorithmCombo.SelectedIndex = 0;

            // Метка "Размер"
            Label sizeLabel = new Label();
            sizeLabel.Text = "Размер:";
            sizeLabel.Location = new System.Drawing.Point(300, 15);
            sizeLabel.Size = new System.Drawing.Size(50, 25);

            // NumericUpDown размера
            this.sizeNumeric = new NumericUpDown();
            this.sizeNumeric.Location = new System.Drawing.Point(355, 12);
            this.sizeNumeric.Size = new System.Drawing.Size(60, 25);
            this.sizeNumeric.Minimum = 5;
            this.sizeNumeric.Maximum = 100;
            this.sizeNumeric.Value = 30;
            this.sizeNumeric.ValueChanged += new EventHandler(this.SizeNumeric_ValueChanged);

            // Метка "Скорость"
            Label speedLabel = new Label();
            speedLabel.Text = "Скорость (мс):";
            speedLabel.Location = new System.Drawing.Point(450, 15);
            speedLabel.Size = new System.Drawing.Size(80, 25);

            // TrackBar скорости
            this.speedTrackBar = new TrackBar();
            this.speedTrackBar.Location = new System.Drawing.Point(535, 5);
            this.speedTrackBar.Size = new System.Drawing.Size(200, 40);
            this.speedTrackBar.Minimum = 1;
            this.speedTrackBar.Maximum = 100;
            this.speedTrackBar.Value = 20;
            this.speedTrackBar.TickFrequency = 10;

            // Кнопка Старт
            this.startButton = new Button();
            this.startButton.Text = "Старт";
            this.startButton.Location = new System.Drawing.Point(760, 10);
            this.startButton.Size = new System.Drawing.Size(100, 35);
            this.startButton.BackColor = System.Drawing.Color.LightGreen;
            this.startButton.Click += new EventHandler(this.StartButton_Click);

            // Кнопка Сброс
            this.resetButton = new Button();
            this.resetButton.Text = "Сброс";
            this.resetButton.Location = new System.Drawing.Point(870, 10);
            this.resetButton.Size = new System.Drawing.Size(100, 35);
            this.resetButton.BackColor = System.Drawing.Color.LightCoral;
            this.resetButton.Click += new EventHandler(this.ResetButton_Click);

            // Статусная метка
            this.statusLabel = new Label();
            this.statusLabel.Text = "Готов";
            this.statusLabel.Location = new System.Drawing.Point(10, 180);
            this.statusLabel.Size = new System.Drawing.Size(300, 25);
            this.statusLabel.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);

            // Группа легенды
            GroupBox legendBox = new GroupBox();
            legendBox.Location = new System.Drawing.Point(990, 10);
            legendBox.Size = new System.Drawing.Size(160, 180);

            // Добавление элементов легенды
            AddLegendItem(legendBox, System.Drawing.Color.SteelBlue, "Обычный элемент", 0);
            AddLegendItem(legendBox, System.Drawing.Color.Orange, "Сравниваемый", 1);
            AddLegendItem(legendBox, System.Drawing.Color.Red, "Перемещаемый", 2);
            AddLegendItem(legendBox, System.Drawing.Color.LightGreen, "Отсортированный", 3);
            AddLegendItem(legendBox, System.Drawing.Color.Purple, "Опорный элемент", 4);

            // Добавление всех контролов на панель управления
            this.controlPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                algoLabel, this.algorithmCombo, sizeLabel, this.sizeNumeric,
                speedLabel, this.speedTrackBar, this.startButton, this.resetButton,
                this.statusLabel, legendBox
            });

            // Добавление панелей на форму
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.controlPanel);

            // Настройка компонентов
            this.components = new System.ComponentModel.Container();
        }

        private void AddLegendItem(GroupBox box, System.Drawing.Color color, string text, int index)
        {
            Panel colorPanel = new Panel();
            colorPanel.Location = new System.Drawing.Point(10, index * 25 + 25);
            colorPanel.Size = new System.Drawing.Size(20, 20);
            colorPanel.BackColor = color;
            colorPanel.BorderStyle = BorderStyle.FixedSingle;

            Label label = new Label();
            label.Text = text;
            label.Location = new System.Drawing.Point(35, index * 25 + 25);
            label.Size = new System.Drawing.Size(120, 20);
            label.Font = new System.Drawing.Font("Arial", 8);

            box.Controls.Add(colorPanel);
            box.Controls.Add(label);
        }

        // Обработчики событий
        private void SizeNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (!isSorting) GenerateArray();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (!isSorting) GenerateArray();
        }
    }
}