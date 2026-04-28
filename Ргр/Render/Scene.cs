using System.Collections.Generic;

namespace RGR_TIMP_S4.Render
{
    public class Scene
    {
        public List<VisualElement> Elements { get; set; } = new List<VisualElement>();
        public (VisualElement Left, VisualElement Right, string Sign)? Comparison { get; set; }

        public void Clear()
        {
            Elements.Clear();
            Comparison = null;
        }
    }
}
