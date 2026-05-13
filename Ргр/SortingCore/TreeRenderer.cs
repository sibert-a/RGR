using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RGR_TIMP_S4.Render
{
    public class TreeNodeVisual
    {
        public int Value;
        public TreeNodeVisual Left, Right;
        public float X, Y;
        public const int NodeRadius = 14; // Было 18, уменьшили
    }

    public static class TreeRenderer
    {
        private static readonly Font NodeFont = new Font("Arial", 7, FontStyle.Bold); // Было 9, уменьшили
        private static readonly Pen LinePen = new Pen(Color.DarkGreen, 1.5f); // Было 2, уменьшили

        public static TreeNodeVisual BuildAndLayout(int[] array)
        {
            TreeNodeVisual root = null;
            foreach (int val in array)
                root = Insert(root, val);

            if (root != null)
                CalculatePositions(root, 0, 0, new Dictionary<TreeNodeVisual, float>());

            return root;
        }

        private static TreeNodeVisual Insert(TreeNodeVisual node, int val)
        {
            if (node == null) return new TreeNodeVisual { Value = val };
            if (val < node.Value) node.Left = Insert(node.Left, val);
            else node.Right = Insert(node.Right, val);
            return node;
        }

        private static float CalculatePositions(TreeNodeVisual node, int depth, float leftBound, Dictionary<TreeNodeVisual, float> subtreeWidths)
        {
            if (node == null) return leftBound;

            float leftWidth = CalculatePositions(node.Left, depth + 1, leftBound, subtreeWidths);
            node.X = leftWidth;
            float rightBound = CalculatePositions(node.Right, depth + 1, leftWidth + 1, subtreeWidths);
            node.Y = depth;

            subtreeWidths[node] = rightBound;
            return rightBound;
        }

        public static void Draw(Graphics g, TreeNodeVisual root, Point offset, int treeHeight)
        {
            if (root == null) return;

            float horizontalSpacing = 30; // Было 40, уменьшили
            float verticalSpacing = 28;   // Было 50, уменьшили
            float centerX = offset.X;
            float startY = offset.Y;

            DrawTree(g, root, centerX, startY, horizontalSpacing, verticalSpacing);
        }

        private static void DrawTree(Graphics g, TreeNodeVisual node, float x, float y, float hSpacing, float vSpacing)
        {
            if (node == null) return;

            float nodeX = x + node.X * hSpacing;
            float nodeY = y + node.Y * vSpacing;

            if (node.Left != null)
            {
                float childX = x + node.Left.X * hSpacing;
                float childY = y + node.Left.Y * vSpacing;
                g.DrawLine(LinePen, nodeX, nodeY + TreeNodeVisual.NodeRadius, childX, childY - TreeNodeVisual.NodeRadius);
                DrawTree(g, node.Left, x, y, hSpacing, vSpacing);
            }

            if (node.Right != null)
            {
                float childX = x + node.Right.X * hSpacing;
                float childY = y + node.Right.Y * vSpacing;
                g.DrawLine(LinePen, nodeX, nodeY + TreeNodeVisual.NodeRadius, childX, childY - TreeNodeVisual.NodeRadius);
                DrawTree(g, node.Right, x, y, hSpacing, vSpacing);
            }

            var rect = new RectangleF(nodeX - TreeNodeVisual.NodeRadius, nodeY - TreeNodeVisual.NodeRadius,
                                      2 * TreeNodeVisual.NodeRadius, 2 * TreeNodeVisual.NodeRadius);
            using (var brush = new SolidBrush(Color.LightGoldenrodYellow))
                g.FillEllipse(brush, rect);
            g.DrawEllipse(Pens.Black, rect);

            string text = node.Value.ToString();
            var sz = g.MeasureString(text, NodeFont);
            float textX = nodeX - sz.Width / 2;
            float textY = nodeY - sz.Height / 2;
            g.DrawString(text, NodeFont, Brushes.Black, textX, textY);
        }
    }
}