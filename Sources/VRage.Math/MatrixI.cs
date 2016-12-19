using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct MatrixI
    {
        public Base6Directions.Direction Right;
        public Base6Directions.Direction Up;
        public Base6Directions.Direction Backward;

        public Vector3I Translation;

        public Base6Directions.Direction Left
        {
            get
            {
                return Base6Directions.GetFlippedDirection(Right);
            }
            set
            {
                Right = Base6Directions.GetFlippedDirection(value);
            }
        }

        public Base6Directions.Direction Down
        {
            get
            {
                return Base6Directions.GetFlippedDirection(Up);
            }
            set
            {
                Up = Base6Directions.GetFlippedDirection(value);
            }
        }

        public Base6Directions.Direction Forward
        {
            get
            {
                return Base6Directions.GetFlippedDirection(Backward);
            }
            set
            {
                Backward = Base6Directions.GetFlippedDirection(value);
            }
        }

        public Base6Directions.Direction GetDirection(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Right:
                    return Right;
                case Base6Directions.Direction.Left:
                    return Left;
                case Base6Directions.Direction.Up:
                    return Up;
                case Base6Directions.Direction.Down:
                    return Down;
                case Base6Directions.Direction.Backward:
                    return Backward;
                case Base6Directions.Direction.Forward:
                default:
                    return Forward;
            }
        }

        public void SetDirection(Base6Directions.Direction dirToSet, Base6Directions.Direction newDirection)
        {
            switch (dirToSet)
            {
                case Base6Directions.Direction.Right:
                    Right = newDirection;
                    break;
                case Base6Directions.Direction.Left:
                    Left = newDirection;
                    break;
                case Base6Directions.Direction.Up:
                    Up = newDirection;
                    break;
                case Base6Directions.Direction.Down:
                    Down = newDirection;
                    break;
                case Base6Directions.Direction.Backward:
                    Backward = newDirection;
                    break;
                case Base6Directions.Direction.Forward:
                    Forward = newDirection;
                    break;
            }
        }

        public Vector3I RightVector
        {
            get
            {
                return Base6Directions.GetIntVector(Right);
            }
            set
            {
                Right = Base6Directions.GetDirection(value);
            }
        }

        public Vector3I LeftVector
        {
            get
            {
                return Base6Directions.GetIntVector(Left);
            }
            set
            {
                Left = Base6Directions.GetDirection(value);
            }
        }

        public Vector3I UpVector
        {
            get
            {
                return Base6Directions.GetIntVector(Up);
            }
            set
            {
                Up = Base6Directions.GetDirection(value);
            }
        }

        public Vector3I DownVector
        {
            get
            {
                return Base6Directions.GetIntVector(Down);
            }
            set
            {
                Down = Base6Directions.GetDirection(value);
            }
        }

        public Vector3I BackwardVector
        {
            get
            {
                return Base6Directions.GetIntVector(Backward);
            }
            set
            {
                Backward = Base6Directions.GetDirection(value);
            }
        }

        public Vector3I ForwardVector
        {
            get
            {
                return Base6Directions.GetIntVector(Forward);
            }
            set
            {
                Forward = Base6Directions.GetDirection(value);
            }
        }

        public MatrixI(ref Vector3I position, Base6Directions.Direction forward, Base6Directions.Direction up)
        {
            Translation = position;
            Right = Base6Directions.GetFlippedDirection(Base6Directions.GetLeft(up, forward));
            Up = up;
            Backward = Base6Directions.GetFlippedDirection(forward);
        }

        public MatrixI(Vector3I position, Base6Directions.Direction forward, Base6Directions.Direction up)
        {
            Translation = position;
            Right = Base6Directions.GetFlippedDirection(Base6Directions.GetLeft(up, forward));
            Up = up;
            Backward = Base6Directions.GetFlippedDirection(forward);
        }

        public MatrixI(Base6Directions.Direction forward, Base6Directions.Direction up):
            this(Vector3I.Zero, forward, up)
        { }

        public MatrixI(ref Vector3I position, ref Vector3I forward, ref Vector3I up):
            this(ref position, Base6Directions.GetDirection(ref forward), Base6Directions.GetDirection(ref up))
        { }

        public MatrixI(ref Vector3I position, ref Vector3 forward, ref Vector3 up):
            this(ref position, Base6Directions.GetDirection(ref forward), Base6Directions.GetDirection(ref up))
        { }

        public MatrixI(MyBlockOrientation orientation):
            this(Vector3I.Zero, orientation.Forward, orientation.Up)
        { }

        public MyBlockOrientation GetBlockOrientation()
        {
            return new MyBlockOrientation(Forward, Up);
        }

        public Matrix GetFloatMatrix()
        {
            return Matrix.CreateWorld(new Vector3(Translation), Base6Directions.GetVector(Forward), Base6Directions.GetVector(Up));
        }

        public static MatrixI CreateRotation(Base6Directions.Direction oldA, Base6Directions.Direction oldB, Base6Directions.Direction newA, Base6Directions.Direction newB)
        {
            Debug.Assert(Base6Directions.GetAxis(oldA) != Base6Directions.GetAxis(oldB), "Original vectors must not lie in line!");
            Debug.Assert(Base6Directions.GetAxis(newA) != Base6Directions.GetAxis(newB), "Transformed vectors must not lie in line!");

            MatrixI newMatrix = new MatrixI();
            newMatrix.Translation = Vector3I.Zero;

            Base6Directions.Direction oldC = Base6Directions.GetCross(oldA, oldB);
            Base6Directions.Direction newC = Base6Directions.GetCross(newA, newB);

            newMatrix.SetDirection(oldA, newA);
            newMatrix.SetDirection(oldB, newB);
            newMatrix.SetDirection(oldC, newC);

            return newMatrix;
        }

        public static void Invert(ref MatrixI matrix, out MatrixI result)
        {
            result = new MatrixI();

            switch (matrix.Right)
            {
                case Base6Directions.Direction.Up: result.Up = Base6Directions.Direction.Right; break;
                case Base6Directions.Direction.Down: result.Up = Base6Directions.Direction.Left; break;
                case Base6Directions.Direction.Backward: result.Backward = Base6Directions.Direction.Right; break;
                case Base6Directions.Direction.Forward: result.Backward = Base6Directions.Direction.Left; break;
                default:
                    result.Right = matrix.Right;
                    break;
            }

            switch (matrix.Up)
            {
                case Base6Directions.Direction.Right: result.Right = Base6Directions.Direction.Up; break;
                case Base6Directions.Direction.Left: result.Right = Base6Directions.Direction.Down; break;
                case Base6Directions.Direction.Backward: result.Backward = Base6Directions.Direction.Up; break;
                case Base6Directions.Direction.Forward: result.Backward = Base6Directions.Direction.Down; break;
                default:
                    result.Up = matrix.Up;
                    break;
            }

            switch (matrix.Backward)
            {
                case Base6Directions.Direction.Right: result.Right = Base6Directions.Direction.Backward; break;
                case Base6Directions.Direction.Left: result.Right = Base6Directions.Direction.Forward; break;
                case Base6Directions.Direction.Up: result.Up = Base6Directions.Direction.Backward; break;
                case Base6Directions.Direction.Down: result.Up = Base6Directions.Direction.Forward; break;
                default:
                    result.Backward = matrix.Backward;
                    break;
            }

            Vector3I.TransformNormal(ref matrix.Translation, ref result, out result.Translation);
            result.Translation = -result.Translation;
        }

        public static void Multiply(ref MatrixI leftMatrix, ref MatrixI rightMatrix, out MatrixI result)
        {
            result = default(MatrixI);
            Vector3I right    = leftMatrix.RightVector;
            Vector3I up       = leftMatrix.UpVector;
            Vector3I backward = leftMatrix.BackwardVector;
            Vector3I newRight, newUp, newBackward;
            Vector3I.TransformNormal(ref right,    ref rightMatrix, out newRight);
            Vector3I.TransformNormal(ref up,       ref rightMatrix, out newUp);
            Vector3I.TransformNormal(ref backward, ref rightMatrix, out newBackward);
            Vector3I.Transform(ref leftMatrix.Translation, ref rightMatrix, out result.Translation);
            result.RightVector    = newRight;
            result.UpVector       = newUp;
            result.BackwardVector = newBackward;
        }

        public static MyBlockOrientation Transform(ref MyBlockOrientation orientation, ref MatrixI transform)
        {
            Base6Directions.Direction forward = transform.GetDirection(orientation.Forward);
            Base6Directions.Direction up = transform.GetDirection(orientation.Up);
            return new MyBlockOrientation(forward, up);
        }
    }
}
