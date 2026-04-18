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

            this.Text = "Визуализация алгоритмов сортировки";
            this.ClientSize = new System.Drawing.Size(1300, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.White;
            this.DoubleBuffered = true;

            // Панель для массива - убрана белая область, уменьшена высота
            this.canvas = new Panel();
            this.canvas.Location = new System.Drawing.Point(10, 170);
            this.canvas.Size = new System.Drawing.Size(1050, 80);
            this.canvas.BackColor = System.Drawing.Color.White;
            this.canvas.BorderStyle = BorderStyle.None;
            this.canvas.Paint += new PaintEventHandler(this.Canvas_Paint);

            // Правая панель с кнопками (прижата вправо)
            this.sidePanel = new Panel();
            this.sidePanel.Location = new System.Drawing.Point(1070, 10);
            this.sidePanel.Size = new System.Drawing.Size(210, 640);
            this.sidePanel.BackColor = System.Drawing.Color.LightGray;
            this.sidePanel.BorderStyle = BorderStyle.FixedSingle;

            // Кнопка Старт
            this.startButton = new Button();
            this.startButton.Text = "Старт";
            this.startButton.Location = new System.Drawing.Point(15, 15);
            this.startButton.Size = new System.Drawing.Size(180, 35);
            this.startButton.BackColor = System.Drawing.Color.LightGreen;
            this.startButton.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            this.startButton.Click += new EventHandler(this.StartButton_Click);

            // Кнопка Пауза
            this.pauseButton = new Button();
            this.pauseButton.Text = "Пауза";
            this.pauseButton.Location = new System.Drawing.Point(15, 60);
            this.pauseButton.Size = new System.Drawing.Size(180, 35);
            this.pauseButton.BackColor = System.Drawing.Color.LightYellow;
            this.pauseButton.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            this.pauseButton.Click += new EventHandler(this.PauseButton_Click);

            // Кнопка Сброс
            this.resetButton = new Button();
            this.resetButton.Text = "Сброс";
            this.resetButton.Location = new System.Drawing.Point(15, 105);
            this.resetButton.Size = new System.Drawing.Size(180, 35);
            this.resetButton.BackColor = System.Drawing.Color.LightCoral;
            this.resetButton.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            this.resetButton.Click += new EventHandler(this.ResetButton_Click);

            // Метка Скорость
            Label speedLabel = new Label();
            speedLabel.Text = "Скорость (мс):";
            speedLabel.Location = new System.Drawing.Point(15, 155);
            speedLabel.Size = new System.Drawing.Size(180, 20);
            speedLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // TrackBar скорости
            this.speedTrackBar = new TrackBar();
            this.speedTrackBar.Location = new System.Drawing.Point(15, 175);
            this.speedTrackBar.Size = new System.Drawing.Size(180, 45);
            this.speedTrackBar.Minimum = 1;
            this.speedTrackBar.Maximum = 500;
            this.speedTrackBar.Value = 50;
            this.speedTrackBar.TickFrequency = 50;

            // Метка Выбор алгоритма
            Label algoLabel = new Label();
            algoLabel.Text = "Выбор алгоритма:";
            algoLabel.Location = new System.Drawing.Point(15, 230);
            algoLabel.Size = new System.Drawing.Size(180, 20);
            algoLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // ComboBox алгоритмов
            this.algorithmCombo = new ComboBox();
            this.algorithmCombo.Location = new System.Drawing.Point(15, 253);
            this.algorithmCombo.Size = new System.Drawing.Size(180, 24);
            this.algorithmCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.algorithmCombo.Font = new System.Drawing.Font("Arial", 9);
            this.algorithmCombo.Items.AddRange(new object[] {
                "Пузырьковая",
                "Сортировка выбором",
                "Сортировка вставками",
                "Сортировка слиянием",
                "Быстрая сортировка",
                "Древесная"
            });
            this.algorithmCombo.SelectedIndex = 0;

            // Заголовок НАСТРОЙКА МАССИВА
            Label sizeLabel1 = new Label();
            sizeLabel1.Text = "НАСТРОЙКА МАССИВА";
            sizeLabel1.Location = new System.Drawing.Point(15, 300);
            sizeLabel1.Size = new System.Drawing.Size(180, 25);
            sizeLabel1.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            sizeLabel1.TextAlign = ContentAlignment.MiddleCenter;

            // Метка Размер
            Label sizeLabel = new Label();
            sizeLabel.Text = "Размер (5-30):";
            sizeLabel.Location = new System.Drawing.Point(15, 340);
            sizeLabel.Size = new System.Drawing.Size(180, 20);
            sizeLabel.Font = new System.Drawing.Font("Arial", 9);

            // NumericUpDown размера
            this.sizeNumeric = new NumericUpDown();
            this.sizeNumeric.Location = new System.Drawing.Point(15, 363);
            this.sizeNumeric.Size = new System.Drawing.Size(180, 23);
            this.sizeNumeric.Minimum = 5;
            this.sizeNumeric.Maximum = 30;
            this.sizeNumeric.Value = 15;
            this.sizeNumeric.Font = new System.Drawing.Font("Arial", 9);
            this.sizeNumeric.ValueChanged += new EventHandler(this.SizeNumeric_ValueChanged);

            // Кнопка Новая выборка
            this.newArrayButton = new Button();
            this.newArrayButton.Text = "Новая выборка";
            this.newArrayButton.Location = new System.Drawing.Point(15, 400);
            this.newArrayButton.Size = new System.Drawing.Size(180, 35);
            this.newArrayButton.BackColor = System.Drawing.Color.LightBlue;
            this.newArrayButton.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            this.newArrayButton.Click += new EventHandler(this.NewArrayButton_Click);

            // Добавление контролов на панель
            this.sidePanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.startButton,
                this.pauseButton,
                this.resetButton,
                speedLabel,
                this.speedTrackBar,
                algoLabel,
                this.algorithmCombo,
                sizeLabel1,
                sizeLabel,
                this.sizeNumeric,
                this.newArrayButton
            });

            this.Controls.Add(this.canvas);
            this.Controls.Add(this.sidePanel);

            this.components = new System.ComponentModel.Container();
        }

        private Panel sidePanel;
        private Panel canvas;
        private ComboBox algorithmCombo;
        private Button startButton;
        private Button resetButton;
        private Button pauseButton;
        private Button newArrayButton;
        private TrackBar speedTrackBar;
        private NumericUpDown sizeNumeric;
    }
}