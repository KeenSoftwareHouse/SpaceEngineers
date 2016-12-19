using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    // Workaround because .NET XML serializer is stupid and does not like enum inside static class
    public /*static*/ class Base6Directions
    {
        private Base6Directions()
        {
        }

        public enum Direction : byte
        {
            Forward = 0,
            Backward = 1,
            Left = 2,
            Right = 3,
            Up = 4,
            Down = 5,
        }

        [Flags]
        public enum DirectionFlags : byte
        {
            Forward  = 1 << (int)Direction.Forward,
            Backward = 1 << (int)Direction.Backward,
            Left     = 1 << (int)Direction.Left,
            Right    = 1 << (int)Direction.Right,
            Up       = 1 << (int)Direction.Up,
            Down     = 1 << (int)Direction.Down,
            All = Forward | Backward | Left | Right | Up | Down,
        }

        public enum Axis : byte
        {
            ForwardBackward = 0,
            LeftRight = 1,
            UpDown = 2,
        }

        // Because Enum.GetValues(...) returns array of objects
        public static readonly Direction[] EnumDirections =
        {
            Direction.Forward,
            Direction.Backward,
            Direction.Left,
            Direction.Right,
            Direction.Up,
            Direction.Down,
        };

        public static readonly Vector3[] Directions = 
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right,
            Vector3.Up,
            Vector3.Down,
        };

        public static readonly Vector3I[] IntDirections =
        {
            Vector3I.Forward,
            Vector3I.Backward,
            Vector3I.Left,
            Vector3I.Right,
            Vector3I.Up,
            Vector3I.Down,
        };

        /// <summary>
        /// Pre-calculated left directions for given forward (index / 6) and up (index % 6) directions
        /// </summary>
        private static readonly Direction[] LeftDirections =
        {
            // Up ------->
            //     5 4 2 3 Forward
            //     4 5 3 2    |
            // 4 5     1 0    |
            // 5 4     0 1    |
            // 3 2 0 1        |
            // 2 3 1 0        V
            Direction.Forward, Direction.Forward, Direction.Down,     Direction.Up,       Direction.Left,     Direction.Right,
            Direction.Forward, Direction.Forward, Direction.Up,       Direction.Down,     Direction.Right,    Direction.Left,
            Direction.Up,      Direction.Down,    Direction.Left,     Direction.Left,     Direction.Backward, Direction.Forward,
            Direction.Down,    Direction.Up,      Direction.Left,     Direction.Left,     Direction.Forward,  Direction.Backward,
            Direction.Right,   Direction.Left,    Direction.Forward,  Direction.Backward, Direction.Left,     Direction.Right,
            Direction.Left,    Direction.Right,   Direction.Backward, Direction.Forward,  Direction.Left,     Direction.Right,
        };

        const float DIRECTION_EPSILON = 0.00001f;

        static readonly int[] ForwardBackward = new int[] { 0, 0, 1 };
        static readonly int[] LeftRight = new int[] { 2, 0, 3 };
        static readonly int[] UpDown = new int[] { 5, 0, 4 };

        public static bool IsBaseDirection(ref Vector3 vec)
        {
            return (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z) - 1 < DIRECTION_EPSILON;
        }

        public static bool IsBaseDirection(Vector3 vec)
        {
            return IsBaseDirection(ref vec);
        }

        public static bool IsBaseDirection(ref Vector3I vec)
        {
            return (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z) - 1 == 0;
        }

        public static Vector3 GetVector(int direction)
        {
            direction = direction % 6;
            return Directions[direction];
        }

        public static Vector3 GetVector(Direction dir)
        {
            return GetVector((int)dir);
        }

        public static Vector3I GetIntVector(int direction)
        {
            direction = direction % 6;
            return IntDirections[direction];
        }

        public static Vector3I GetIntVector(Direction dir)
        {
            int direction = (int)dir % 6;
            return IntDirections[direction];
        }

        public static void GetVector(Direction dir, out Vector3 result)
        {
            int direction = (int)dir % 6;
            result = Directions[direction];
        }

        public static DirectionFlags GetDirectionFlag(Direction dir)
        {
            return (DirectionFlags)(1 << (byte)dir);
        }

        public static Direction GetPerpendicular(Direction dir)
        {
            if (GetAxis(dir) == Axis.UpDown) return Direction.Right;
            else return Direction.Up;
        }

        public static Direction GetDirection(Vector3 vec)
        {
            return GetDirection(ref vec);
        }

        public static Direction GetDirection(ref Vector3 vec)
        {
            Debug.Assert(IsBaseDirection(ref vec));
            int value = 0;
            value += ForwardBackward[(int)Math.Round(vec.Z + 1)];
            value += LeftRight[(int)Math.Round(vec.X + 1)];
            value += UpDown[(int)Math.Round(vec.Y + 1)];
            return (Direction)value;
        }

        public static Direction GetDirection(Vector3I vec)
        {
            return GetDirection(ref vec);
        }

        public static Direction GetDirection(ref Vector3I vec)
        {
            Debug.Assert(IsBaseDirection(ref vec));
            int value = 0;
            value += ForwardBackward[vec.Z + 1];
            value += LeftRight[vec.X + 1];
            value += UpDown[vec.Y + 1];
            return (Direction)value;
        }

        public static Direction GetClosestDirection(Vector3 vec)
        {
            return GetClosestDirection(ref vec);
        }

        public static Direction GetClosestDirection(ref Vector3 vec)
        {
            Debug.Assert(vec != Vector3.Zero, "Cannot find direction for zero vector");
            Vector3 projection = Vector3.DominantAxisProjection(vec);
            projection = Vector3.Sign(projection);
            return GetDirection(ref projection);
        }

        public static Direction GetDirectionInAxis(Vector3 vec, Axis axis)
        {
            return GetDirectionInAxis(ref vec, axis);
        }

        public static Direction GetDirectionInAxis(ref Vector3 vec, Axis axis)
        {
            Direction baseDirection = GetBaseAxisDirection(axis);
            Vector3 v = IntDirections[(int)baseDirection];
            v = v * vec;
            if (v.X + v.Y + v.Z >= 1)
                return baseDirection;
            else
                return GetFlippedDirection(baseDirection);
        }

        public static Direction GetForward(Quaternion rot)
        {
            Vector3 rotatedForward;
            Vector3.Transform(ref Vector3.Forward, ref rot, out rotatedForward);
            return GetDirection(ref rotatedForward);
        }

        public static Direction GetForward(ref Quaternion rot)
        {
            Vector3 rotatedForward;
            Vector3.Transform(ref Vector3.Forward, ref rot, out rotatedForward);
            return GetDirection(ref rotatedForward);
        }

        public static Direction GetForward(ref Matrix rotation)
        {
            Debug.Assert(rotation.IsRotation());
            Vector3 rotatedForward;
            Vector3.TransformNormal(ref Vector3.Forward, ref rotation, out rotatedForward);
            return GetDirection(ref rotatedForward);
        }

        public static Direction GetUp(Quaternion rot)
        {
            Vector3 rotatedUp;
            Vector3.Transform(ref Vector3.Up, ref rot, out rotatedUp);
            return GetDirection(ref rotatedUp);
        }

        public static Direction GetUp(ref Quaternion rot)
        {
            Vector3 rotatedUp;
            Vector3.Transform(ref Vector3.Up, ref rot, out rotatedUp);
            return GetDirection(ref rotatedUp);
        }

        public static Direction GetUp(ref Matrix rotation)
        {
            Debug.Assert(rotation.IsRotation());
            Vector3 rotatedUp;
            Vector3.TransformNormal(ref Vector3.Up, ref rotation, out rotatedUp);
            return GetDirection(ref rotatedUp);
        }

        public static Axis GetAxis(Direction direction)
        {
            return (Axis)((byte)direction >> 1);
        }

        public static Direction GetBaseAxisDirection(Axis axis)
        {
            return (Direction)((byte)axis << 1);
        }

        public static Direction GetFlippedDirection(Direction toFlip)
        {
            return toFlip ^ Direction.Backward;
        }

        public static Direction GetCross(Direction dir1, Direction dir2)
        {
            return GetLeft(dir1, dir2);
        }

        public static Direction GetLeft(Direction up, Direction forward)
        {
            return LeftDirections[(int)forward*6 + (int)up];
        }

        public static Direction GetOppositeDirection(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.Forward:
                    return Direction.Backward;
                case Direction.Backward:
                    return Direction.Forward;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
            }
        }

        public static Quaternion GetOrientation(Direction forward, Direction up)
        {
            var vecForward = Base6Directions.GetVector(forward);
            var vecUp = Base6Directions.GetVector(up);
            return Quaternion.CreateFromForwardUp(vecForward, vecUp); // This can be replaced by lookup table
        }

        public static bool IsValidBlockOrientation(Direction forward, Direction up)
        {
            return forward <= Direction.Down && up <= Direction.Down && Vector3.Dot(Base6Directions.GetVector(forward), Base6Directions.GetVector(up)) == 0f;
        }

    }
}
