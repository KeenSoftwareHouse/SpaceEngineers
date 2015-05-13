using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    /// <summary>
    /// Base 26 directions and Vector3.Zero
    /// Each component is only 0,-1 or 1;
    /// </summary>
    public class Base27Directions
    {
        [Flags]
        public enum Direction : byte
        {
            Forward = 1,
            Backward = 2,
            Left = 4,
            Right = 8,
            Up = 16,
            Down = 32,
        }

        public static readonly Vector3[] Directions = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(-0.7071068f, 0f, -0.7071068f),
            new Vector3(-0.7071068f, 0f, 0.7071068f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0.7071068f, 0f, -0.7071068f),
            new Vector3(0.7071068f, 0f, 0.7071068f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0.7071068f, -0.7071068f),
            new Vector3(0f, 0.7071068f, 0.7071068f),
            new Vector3(0f, 1f, 0f),
            new Vector3(-0.7071068f, 0.7071068f, 0f),
            new Vector3(-0.5773503f, 0.5773503f, -0.5773503f),
            new Vector3(-0.5773503f, 0.5773503f, 0.5773503f),
            new Vector3(-0.7071068f, 0.7071068f, 0f),
            new Vector3(0.7071068f, 0.7071068f, 0f),
            new Vector3(0.5773503f, 0.5773503f, -0.5773503f),
            new Vector3(0.5773503f, 0.5773503f, 0.5773503f),
            new Vector3(0.7071068f, 0.7071068f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0.7071068f, -0.7071068f),
            new Vector3(0f, 0.7071068f, 0.7071068f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, -0.7071068f, -0.7071068f),
            new Vector3(0f, -0.7071068f, 0.7071068f),
            new Vector3(0f, -1f, 0f),
            new Vector3(-0.7071068f, -0.7071068f, 0f),
            new Vector3(-0.5773503f, -0.5773503f, -0.5773503f),
            new Vector3(-0.5773503f, -0.5773503f, 0.5773503f),
            new Vector3(-0.7071068f, -0.7071068f, 0f),
            new Vector3(0.7071068f, -0.7071068f, 0f),
            new Vector3(0.5773503f, -0.5773503f, -0.5773503f),
            new Vector3(0.5773503f, -0.5773503f, 0.5773503f),
            new Vector3(0.7071068f, -0.7071068f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, -0.7071068f, -0.7071068f),
            new Vector3(0f, -0.7071068f, 0.7071068f),
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(-0.7071068f, 0f, -0.7071068f),
            new Vector3(-0.7071068f, 0f, 0.7071068f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0.7071068f, 0f, -0.7071068f),
            new Vector3(0.7071068f, 0f, 0.7071068f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
        };

        public static readonly Vector3I[] DirectionsInt = new Vector3I[]
        {
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, -1),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 0, 0),
            new Vector3I(-1, 0, 0),
            new Vector3I(-1, 0, -1),
            new Vector3I(-1, 0, 1),
            new Vector3I(-1, 0, 0),
            new Vector3I(1, 0, 0),
            new Vector3I(1, 0, -1),
            new Vector3I(1, 0, 1),
            new Vector3I(1, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, -1),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 1, 0),
            new Vector3I(0, 1, -1),
            new Vector3I(0, 1, 1),
            new Vector3I(0, 1, 0),
            new Vector3I(-1, 1, 0),
            new Vector3I(-1, 1, -1),
            new Vector3I(-1, 1, 1),
            new Vector3I(-1, 1, 0),
            new Vector3I(1, 1, 0),
            new Vector3I(1, 1, -1),
            new Vector3I(1, 1, 1),
            new Vector3I(1, 1, 0),
            new Vector3I(0, 1, 0),
            new Vector3I(0, 1, -1),
            new Vector3I(0, 1, 1),
            new Vector3I(0, 1, 0),
            new Vector3I(0, -1, 0),
            new Vector3I(0, -1, -1),
            new Vector3I(0, -1, 1),
            new Vector3I(0, -1, 0),
            new Vector3I(-1, -1, 0),
            new Vector3I(-1, -1, -1),
            new Vector3I(-1, -1, 1),
            new Vector3I(-1, -1, 0),
            new Vector3I(1, -1, 0),
            new Vector3I(1, -1, -1),
            new Vector3I(1, -1, 1),
            new Vector3I(1, -1, 0),
            new Vector3I(0, -1, 0),
            new Vector3I(0, -1, -1),
            new Vector3I(0, -1, 1),
            new Vector3I(0, -1, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, -1),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 0, 0),
            new Vector3I(-1, 0, 0),
            new Vector3I(-1, 0, -1),
            new Vector3I(-1, 0, 1),
            new Vector3I(-1, 0, 0),
            new Vector3I(1, 0, 0),
            new Vector3I(1, 0, -1),
            new Vector3I(1, 0, 1),
            new Vector3I(1, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, -1),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 0, 0),
        };

        const float DIRECTION_EPSILON = 0.00001f;

        static readonly int[] ForwardBackward = new int[] { 1, 0, 2 };
        static readonly int[] LeftRight = new int[] { 4, 0, 8 };
        static readonly int[] UpDown = new int[] { 32, 0, 16 };

        public static bool IsBaseDirection(ref Vector3 vec)
        {
            return (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z) - 1 < DIRECTION_EPSILON;
        }

        public static bool IsBaseDirection(ref Vector3I vec)
        {
            return vec.X >= -1 && vec.X <= 1
                && vec.Y >= -1 && vec.Y <= 1
                && vec.Z >= -1 && vec.Z <= 1;
        }

        public static bool IsBaseDirection(Vector3 vec)
        {
            return IsBaseDirection(ref vec);
        }
        
        public static Vector3 GetVector(int direction)
        {
            return Directions[direction];
        }

        public static Vector3I GetVectorInt(int direction)
        {
            return DirectionsInt[direction];
        }

        public static Vector3 GetVector(Direction dir)
        {
            return Directions[(int)dir];
        }

        public static Vector3I GetVectorInt(Direction dir)
        {
            return DirectionsInt[(int)dir];
        }

        /// <summary>
        /// Vector must be normalized, allowed values for components are: 0, 1, -1, 0.707, -0.707, 0.577, -0.577
        /// </summary>
        public static Direction GetDirection(Vector3 vec)
        {
            return GetDirection(ref vec);
        }

        public static Direction GetDirection(Vector3I vec)
        {
            return GetDirection(ref vec);
        }

        public static Direction GetDirection(ref Vector3 vec)
        {
            Debug.Assert(IsBaseDirection(ref vec), "Vector must be normalized and one of 27 directions");
            int value = 0;
            value += ForwardBackward[(int)Math.Round(vec.Z + 1)];
            value += LeftRight[(int)Math.Round(vec.X + 1)];
            value += UpDown[(int)Math.Round(vec.Y + 1)];
            return (Direction)value;
        }

        public static Direction GetDirection(ref Vector3I vec)
        {
            Debug.Assert(IsBaseDirection(ref vec), "Vector must be have component (-1, 0 or 1) and one of 27 directions");
            int value = 0;
            value += ForwardBackward[vec.Z + 1];
            value += LeftRight[vec.X + 1];
            value += UpDown[vec.Y + 1];
            return (Direction)value;
        }

        public static Direction GetForward(ref Quaternion rot)
        {
            Vector3 rotatedForward;
            Vector3.Transform(ref Vector3.Forward, ref rot, out rotatedForward);
            return GetDirection(ref rotatedForward);
        }

        public static Direction GetUp(ref Quaternion rot)
        {
            Vector3 rotatedUp;
            Vector3.Transform(ref Vector3.Up, ref rot, out rotatedUp);
            return GetDirection(ref rotatedUp);
        }
    }
}
