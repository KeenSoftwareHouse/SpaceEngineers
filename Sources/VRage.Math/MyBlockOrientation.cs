using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    [ProtoContract]
    public struct MyBlockOrientation
    {
        public static readonly MyBlockOrientation Identity = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        [ProtoMember]
        public Base6Directions.Direction Forward;
        
        [ProtoMember]
        public Base6Directions.Direction Up;

        public Base6Directions.Direction Left
        {
            get
            {
                return Base6Directions.GetLeft(Up, Forward);
            }
        }

        public bool IsValid
        {
            get
            {
                return Base6Directions.IsValidBlockOrientation(Forward, Up);
            }
        }

        public MyBlockOrientation(Base6Directions.Direction forward, Base6Directions.Direction up)
        {
            Forward = forward;
            Up = up;
            Debug.Assert(IsValid);
        }

        public MyBlockOrientation(ref Quaternion q)
        {
            Forward = Base6Directions.GetForward(q);
            Up      = Base6Directions.GetUp(q);
            Debug.Assert(IsValid);
        }

        public MyBlockOrientation(ref Matrix m)
        {
            Forward = Base6Directions.GetForward(ref m);
            Up      = Base6Directions.GetUp(ref m);
            //Debug.Assert(IsValid);
        }

        public void GetQuaternion(out Quaternion result)
        {
            //Debug.Assert(IsValid);

            Matrix matrix;
            GetMatrix(out matrix);

            Quaternion.CreateFromRotationMatrix(ref matrix, out result);
        }

        public void GetMatrix(out Matrix result)
        {
            //Debug.Assert(IsValid);
            
            Vector3 vecForward, vecUp;
            Base6Directions.GetVector(Forward, out vecForward);
            Base6Directions.GetVector(Up, out vecUp);

            Matrix.CreateWorld(ref Vector3.Zero, ref vecForward, ref vecUp, out result);
        }

        public override int GetHashCode()
        {
            return ((int)Forward << 16) | ((int)Up);
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var rhs = obj as MyBlockOrientation?;
                if (rhs.HasValue)
                    return this == rhs.Value;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("[Forward:{0}, Up:{1}]", Forward, Up);
        }

        /// <summary>
        /// Returns the direction baseDirection will point to after transformation
        /// </summary>
        public Base6Directions.Direction TransformDirection(Base6Directions.Direction baseDirection)
        {
            Base6Directions.Axis axis = Base6Directions.GetAxis(baseDirection);
            int flip = ((int)baseDirection % 2);

            if (axis == Base6Directions.Axis.ForwardBackward)
            {
                return flip == 1 ? Base6Directions.GetFlippedDirection(Forward) : Forward;
            }
            if (axis == Base6Directions.Axis.LeftRight)
            {
                return flip == 1 ? Base6Directions.GetFlippedDirection(Left) : Left;            
            }
            Debug.Assert(axis == Base6Directions.Axis.UpDown, "Axis invalid in MyBlockOrientation");
            return flip == 1 ? Base6Directions.GetFlippedDirection(Up) : Up;
        }

        /// <summary>
        /// Returns the direction that this orientation transforms to baseDirection
        /// </summary>
        public Base6Directions.Direction TransformDirectionInverse(Base6Directions.Direction baseDirection)
        {
            Base6Directions.Axis axis = Base6Directions.GetAxis(baseDirection);

            if (axis == Base6Directions.GetAxis(Forward))
            {
                return baseDirection == Forward ? Base6Directions.Direction.Forward : Base6Directions.Direction.Backward;
            }
            if (axis == Base6Directions.GetAxis(Left))
            {
                return baseDirection == Left ? Base6Directions.Direction.Left : Base6Directions.Direction.Right;
            }
            Debug.Assert(axis == Base6Directions.GetAxis(Up), "Direction invalid in MyBlockOrientation");
            return baseDirection == Up ? Base6Directions.Direction.Up : Base6Directions.Direction.Down;
        }

        public static bool operator ==(MyBlockOrientation orientation1, MyBlockOrientation orientation2)
        {
            return orientation1.Forward == orientation2.Forward && orientation1.Up == orientation2.Up;
        }

        public static bool operator !=(MyBlockOrientation orientation1, MyBlockOrientation orientation2)
        {
            return orientation1.Forward != orientation2.Forward || orientation1.Up != orientation2.Up;
        }
    }
}
