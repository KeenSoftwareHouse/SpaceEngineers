using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct Vector2B
    {
        byte X, Y;

        public Vector2B(byte x, byte y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Vector2I operator *(Vector2B op1, Vector2I op2)
        {
            return new Vector2I(op1.X * op2.X, op1.Y * op2.Y);
        }

        public static Vector2I operator *(Vector2B op1, int op2)
        {
            return new Vector2I(op1.X * op2, op1.Y * op2);
        }
    }
}
