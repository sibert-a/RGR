using System.Drawing;

namespace RGR_TIMP_S4.Render
{
    public class VisualElement
    {
        public int Value { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsVisible { get; set; } = true;
        public Color BackgroundColor { get; set; } = Color.LightSkyBlue;
        public int? ArrayIndex { get; set; }          // индекс в массиве для основных элементов
        public int? TargetArrayIndex { get; set; }    // для временных: куда вернуться
        public bool IsTemporary { get; set; }         // временный элемент сцены
    }
}
