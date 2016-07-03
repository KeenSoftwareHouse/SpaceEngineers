using System;
using System.Runtime.InteropServices;

namespace VRageMath
{
    /// <summary>
    /// Structure using the same layout than <see cref="System.Drawing.RectangleF"/>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RectangleF : IEquatable<RectangleF>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleF"/> struct.
        /// </summary>
        /// <param name="position">The x-y position of this rectangle.</param>
        /// <param name="size">The x-y size of this rectangle.</param>
        public RectangleF(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleF"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public RectangleF(float x, float y, float width, float height)
        {
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }

        public bool Contains(int x, int y)
        {
            if (x >= X && x <= X + Width && y >= Y && y <= Y + Height)
            {
                return true;
            }
            return false;
        }

        public bool Contains(float x, float y)
        {
            if (x >= X && x <= X + Width && y >= Y && y <= Y + Height)
            {
                return true;
            }
            return false;
        }

        public bool Contains(Vector2 vector2D)
        {
            if (vector2D.X >= X && vector2D.X <= X + Width && vector2D.Y >= Y && vector2D.Y <= Y + Height)
            {
                return true;
            }
            return false;
        }

        public bool Contains(Point point)
        {
            if (point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// The Position.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The Size.
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// Left coordinate.
        /// </summary>
        public float X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        /// <summary>
        /// Top coordinate.
        /// </summary>
        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        /// <summary>
        /// Width of this rectangle.
        /// </summary>
        public float Width
        {
            get { return Size.X; }
            set { Size.X = value; }
        }

        /// <summary>
        /// Height of this rectangle.
        /// </summary>
        public float Height
        {
            get { return Size.Y; }
            set { Size.Y = value; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(RectangleF other)
        {
       		return (other.X == X) && (other.Y == Y) && (other.Width == Width) && (other.Height == Height);
        }

        /// <summary>
        /// Creates a Rectangle defining the area where one rectangle overlaps with another rectangle.
        /// </summary>
        /// <param name="value1">The first Rectangle to compare.</param>
        /// <param name="value2">The second Rectangle to compare.</param>
        /// <param name="result">[OutAttribute] The area where the two first parameters overlap.</param>
        public static void Intersect(ref RectangleF value1, ref RectangleF value2, out RectangleF result)
        {
            var num1 = value1.X + value1.Width;
            var num2 = value2.X + value2.Width;
            var num3 = value1.Y + value1.Height;
            var num4 = value2.Y + value2.Height;
            var num5 = value1.X > value2.X ? value1.X : value2.X;
            var num6 = value1.Y > value2.Y ? value1.Y : value2.Y;
            var num7 = num1 < num2 ? num1 : num2;
            var num8 = num3 < num4 ? num3 : num4;
            if (num7 > num5 && num8 > num6)
            {
                result = new RectangleF(
                    x: num5,
                    y: num6,
                    width: num7 - num5,
                    height: num8 - num6);
            }
            else
            {
                result = new RectangleF(
                    x: 0,
                    y: 0,
                    width: 0,
                    height: 0);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(RectangleF)) return false;
            return Equals((RectangleF)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Width.GetHashCode();
                result = (result * 397) ^ Height.GetHashCode();
                return result;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("(X: {0} Y: {1} W: {2} H: {3})", X, Y, Width, Height);
        }
    }
}
