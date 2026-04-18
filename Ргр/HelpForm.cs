using System;
using System.Drawing;
using System.Windows.Forms;

namespace РГР
{
    public class HelpForm : Form
    {
        public HelpForm()
        {
            SetupHelpForm();
        }

        private void SetupHelpForm()
        {
            this.Text = "Справка — Визуализация алгоритмов сортировки";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            RichTextBox richTextBox = new RichTextBox();
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.ReadOnly = true;
            richTextBox.Font = new Font("Segoe UI", 10);
            richTextBox.BackColor = Color.White;
            richTextBox.Text = GetHelpText();

            // Форматируем жирным все заголовки
            FormatBold(richTextBox, "СПРАВКА – ВИЗУАЛИЗАЦИЯ АЛГОРИТМОВ СОРТИРОВКИ");
            FormatBold(richTextBox, "1. НАЗНАЧЕНИЕ ПРОГРАММЫ");
            FormatBold(richTextBox, "2. КАК ПОЛЬЗОВАТЬСЯ ПРОГРАММОЙ");
            FormatBold(richTextBox, "3. ДОСТУПНЫЕ АЛГОРИТМЫ СОРТИРОВКИ");
            FormatBold(richTextBox, "4. ЧАСТЫЕ ВОПРОСЫ (FAQ)");
            FormatBold(richTextBox, "5. ГОРЯЧИЕ КЛАВИШИ");
            FormatBold(richTextBox, "Дополнительный функционал:");
            FormatBold(richTextBox, "Совет:");
            FormatText(richTextBox, "Сортировка пузырьком (обменами)", FontStyle.Bold, Color.DarkBlue);
            FormatText(richTextBox, "Сортировка выбором (извлечением)", FontStyle.Bold, Color.DarkBlue);
            FormatText(richTextBox, "Сортировка вставками", FontStyle.Bold, Color.DarkBlue);
            FormatText(richTextBox, "Сортировка слиянием", FontStyle.Bold, Color.DarkBlue);
            FormatText(richTextBox, "Древесная сортировка (пирамидальная / Tree sort)", FontStyle.Bold, Color.DarkBlue);
            FormatText(richTextBox, "Быстрая сортировка", FontStyle.Bold, Color.DarkBlue);
            FormatBold(richTextBox, "F1");


            // 1. Снимаем выделение 
            richTextBox.Select(0, 0);
            richTextBox.SelectionLength = 0;

            // 2. Прокручиваем в самое начало документа
            richTextBox.ScrollToCaret();

            // 3. Убираем фокус с RichTextBox 
            this.ActiveControl = null;


            this.Controls.Add(richTextBox);
        }
        private void FormatBold(RichTextBox rtb, string textToBold)
        {
            int startIndex = rtb.Text.IndexOf(textToBold);
            if (startIndex >= 0)
            {
                rtb.Select(startIndex, textToBold.Length);
                rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                rtb.SelectionColor = Color.Black;
            }
        }
        private void FormatText(RichTextBox rtb, string textToFormat, FontStyle style, Color color, float fontSize = 0)
        {
            int startIndex = rtb.Text.IndexOf(textToFormat);
            if (startIndex >= 0)
            {
                rtb.Select(startIndex, textToFormat.Length);

                // Шрифт: используем текущий размер или заданный
                float currentSize = (fontSize > 0) ? fontSize : rtb.Font.Size;
                rtb.SelectionFont = new Font(rtb.Font.FontFamily, currentSize, style);
                rtb.SelectionColor = color;
            }
        }
        private string GetHelpText()
        {
            return
"                                       СПРАВКА – ВИЗУАЛИЗАЦИЯ АЛГОРИТМОВ СОРТИРОВКИ\n" +
"\n" +
"1. НАЗНАЧЕНИЕ ПРОГРАММЫ\n" +
"Данная программа предназначена для наглядной демонстрации работы основных алгоритмов\n" +
"сортировки целочисленных массивов.\n" +
"Вы можете видеть, как элементы сравниваются, перемещаются и постепенно выстраиваются\n" +
"по возрастанию или убыванию.\n" +
"\n" +
"2. КАК ПОЛЬЗОВАТЬСЯ ПРОГРАММОЙ\n" +
"1. Сгенерируйте массив – выберите размер массива (количество чисел выборки), затем\n" +
"   нажмите кнопку «Новая выборка».\n" +
"2. Выберите алгоритм из списка (например, «Пузырёк», «Быстрая», «Слиянием» и т.д.).\n" +
"3. Запустите сортировку – нажмите кнопку «Старт».\n" +
"4. Наблюдайте за анимацией:\n" +
"   - Сравниваемые элементы временно отображаются вне массива (сверху).\n" +
"   - Затем они меняются местами или вставляются в правильную позицию.\n" +
"5. При необходимости можно остановить (нажав на кнопку «Пауза») или сбросить\n" +
"   (кнопка «Сброс») анимацию.\n" +
"\n" +
"💡 Совет: для повторного запуска с тем же массивом используйте кнопку «Сброс»\n" +
"   (данная кнопка сбрасывает сортировку).\n" +
"\n" +
"Дополнительный функционал:\n" +
"• Можно изменить скорость сортировки, потянув ползунок «Скорость».\n" +
"\n" +
"3. ДОСТУПНЫЕ АЛГОРИТМЫ СОРТИРОВКИ\n" +
"\n" +
"• Сортировка пузырьком (обменами)\n" +
"  Как работает: последовательно сравниваются соседние элементы. Если порядок нарушен –\n" +
"  они меняются местами. Самый большой элемент «всплывает» в конец.\n" +
"  Особенность: простой, но медленный на больших массивах (O(n²)).\n" +
"\n" +
"• Сортировка выбором (извлечением)\n" +
"  Как работает: на каждом шаге ищется минимальный элемент в неотсортированной части\n" +
"  и меняется с первым неотсортированным.\n" +
"  Особенность: делает мало обменов, но много сравнений.\n" +
"\n" +
"• Сортировка вставками\n" +
"  Как работает: массив постепенно строится из отсортированной части. Каждый новый\n" +
"  элемент вставляется в нужное место среди уже отсортированных.\n" +
"  Особенность: очень эффективна на почти отсортированных данных.\n" +
"\n" +
"• Сортировка слиянием\n" +
"  Как работает: массив рекурсивно делится пополам, затем отсортированные половинки\n" +
"  сливаются воедино.\n" +
"  Особенность: быстрая (O(n log n)), но требует дополнительной памяти.\n" +
"\n" +
"• Древесная сортировка (пирамидальная / Tree sort)\n" +
"  Как работает: элементы вставляются в двоичное дерево поиска, затем обходятся по порядку.\n" +
"  Особенность: наглядно показывает древовидную структуру, но может быть неустойчивой.\n" +
"\n" +
"• Быстрая сортировка\n" +
"  Как работает: выбирается опорный элемент, массив делится на две части (меньше опорного\n" +
"  и больше), затем каждая часть сортируется рекурсивно.\n" +
"  Особенность: один из самых быстрых на практике (O(n log n) в среднем).\n" +
"\n" +
"4. ЧАСТЫЕ ВОПРОСЫ (FAQ)\n" +
"Вопрос: Почему массив не сортируется за один шаг?\n" +
"Ответ: Программа показывает пошаговую анимацию, чтобы вы видели логику алгоритма.\n" +
"       Вы можете ускорить анимацию ползунком скорости.\n" +
"\n" +
"Вопрос: Что делать, если анимация зависла?\n" +
"Ответ: Нажмите «Сброс», затем «Старт» и запустите сортировку заново.\n" +
"\n" +
"5. ГОРЯЧИЕ КЛАВИШИ\n" +
"F1 – вызов этой справки\n" +
"\n" +
"═══════════════════════════════════════════════════════════════\n" +
"                      © Визуализация сортировки v1.0\n" +
"═══════════════════════════════════════════════════════════════";
        }
    }
}