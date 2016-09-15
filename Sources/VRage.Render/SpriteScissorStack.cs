using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    #region Nested type: SpriteScissorStack

    /// <summary>
    /// Stores stack of scissor rectangles where top rectangle has already
    /// been cut using all the rectangles below it, so that only one
    /// rectangle is checked during scissor test.
    /// </summary>
    public class SpriteScissorStack
    {
        private Stack<Rectangle> m_rectangleStack = new Stack<Rectangle>();

        public bool Empty
        {
            get { return m_rectangleStack.Count == 0; }
        }

        public void Push(Rectangle scissorRect)
        {
            if (!Empty)
            {
                var topRect = m_rectangleStack.Peek();
                Rectangle.Intersect(ref scissorRect, ref topRect, out scissorRect);
            }
            m_rectangleStack.Push(scissorRect);
        }

        public void Pop()
        {
            if (!Empty)
                m_rectangleStack.Pop();
        }

        public RectangleF? Peek()
        {
            if(!Empty)
            {
                Rectangle top = m_rectangleStack.Peek();
                return new RectangleF(top.X, top.Y, top.Width, top.Height);
            }
            return null;
        }

        /// <summary>
        /// Cuts the destination rectangle using top of the scissor stack.
        /// Source rectangle is modified using scaled change of destination
        /// as well.
        /// </summary>
        public void Cut(ref RectangleF destination, ref RectangleF source)
        {
            if (Empty)
                return;
            var originalDestination = destination;
            Rectangle top = m_rectangleStack.Peek();
            var topF = new RectangleF(top.X, top.Y, top.Width, top.Height);
            RectangleF.Intersect(ref destination, ref topF, out destination);
            if (destination.Equals(originalDestination))
                return;
            var scale = source.Size / originalDestination.Size;
            var sizeChange = destination.Size - originalDestination.Size;
            var positionChange = destination.Position - originalDestination.Position;
            var originalSource = source;
            source.Position += positionChange * scale;
            source.Size += sizeChange * scale;
        }
    }

    #endregion
}
