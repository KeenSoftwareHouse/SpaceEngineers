using System;
using System.ComponentModel;

namespace VRageMath
{
    /// <summary>
    /// Represents a point in a multi-point curve.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
    {
        internal float position;
        internal float internalValue;
        internal float tangentOut;
        internal float tangentIn;
        internal CurveContinuity continuity;

        /// <summary>
        /// Position of the CurveKey in the curve.
        /// </summary>
        public float Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Describes the value of this point.
        /// </summary>
        public float Value
        {
            get
            {
                return this.internalValue;
            }
            set
            {
                this.internalValue = value;
            }
        }

        /// <summary>
        /// Describes the tangent when approaching this point from the previous point in the curve.
        /// </summary>
        public float TangentIn
        {
            get
            {
                return this.tangentIn;
            }
            set
            {
                this.tangentIn = value;
            }
        }

        /// <summary>
        /// Describes the tangent when leaving this point to the next point in the curve.
        /// </summary>
        public float TangentOut
        {
            get
            {
                return this.tangentOut;
            }
            set
            {
                this.tangentOut = value;
            }
        }

        /// <summary>
        /// Describes whether the segment between this point and the next point in the curve is discrete or continuous.
        /// </summary>
        public CurveContinuity Continuity
        {
            get
            {
                return this.continuity;
            }
            set
            {
                this.continuity = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of CurveKey.
        /// </summary>
        /// <param name="position">Position in the curve.</param><param name="value">Value of the control point.</param>
        public CurveKey(float position, float value)
        {
            this.position = position;
            this.internalValue = value;
        }

        /// <summary>
        /// Initializes a new instance of CurveKey.
        /// </summary>
        /// <param name="position">Position in the curve.</param><param name="value">Value of the control point.</param><param name="tangentIn">Tangent approaching point from the previous point in the curve.</param><param name="tangentOut">Tangent leaving point toward next point in the curve.</param>
        public CurveKey(float position, float value, float tangentIn, float tangentOut)
        {
            this.position = position;
            this.internalValue = value;
            this.tangentIn = tangentIn;
            this.tangentOut = tangentOut;
        }

        /// <summary>
        /// Initializes a new instance of CurveKey.
        /// </summary>
        /// <param name="position">Position in the curve.</param><param name="value">Value of the control point.</param><param name="tangentIn">Tangent approaching point from the previous point in the curve.</param><param name="tangentOut">Tangent leaving point toward next point in the curve.</param><param name="continuity">Enum indicating whether the curve is discrete or continuous.</param>
        public CurveKey(float position, float value, float tangentIn, float tangentOut, CurveContinuity continuity)
        {
            this.position = position;
            this.internalValue = value;
            this.tangentIn = tangentIn;
            this.tangentOut = tangentOut;
            this.continuity = continuity;
        }

        /// <summary>
        /// Determines whether two CurveKey instances are equal.
        /// </summary>
        /// <param name="a">CurveKey on the left of the equal sign.</param><param name="b">CurveKey on the right of the equal sign.</param>
        public static bool operator ==(CurveKey a, CurveKey b)
        {
#if UNSHARPER_TMP
			bool flag1 = null == a;
			bool flag2 = null == b;
#else
            bool flag1 = null == (object)a;
            bool flag2 = null == (object)b;
#endif
            return flag1 || flag2 ? flag1 == flag2 : a.Equals(b);
        }

        /// <summary>
        /// Determines whether two CurveKey instances are not equal.
        /// </summary>
        /// <param name="a">CurveKey on the left of the equal sign.</param><param name="b">CurveKey on the right of the equal sign.</param>
        public static bool operator !=(CurveKey a, CurveKey b)
        {
            bool flag1 = a == (CurveKey)null;
            bool flag2 = b == (CurveKey)null;
            return flag1 || flag2 ? flag1 != flag2 : (double)a.position != (double)b.position || (double)a.internalValue != (double)b.internalValue || ((double)a.tangentIn != (double)b.tangentIn || (double)a.tangentOut != (double)b.tangentOut) || a.continuity != b.continuity;
        }

        /// <summary>
        /// Creates a copy of the CurveKey.
        /// </summary>
        public CurveKey Clone()
        {
            return new CurveKey(this.position, this.internalValue, this.tangentIn, this.tangentOut, this.continuity);
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the CurveKey.
        /// </summary>
        /// <param name="other">The Object to compare with the current CurveKey.</param>
        public bool Equals(CurveKey other)
        {
            if (other != (CurveKey)null && (double)other.position == (double)this.position && ((double)other.internalValue == (double)this.internalValue && (double)other.tangentIn == (double)this.tangentIn) && (double)other.tangentOut == (double)this.tangentOut)
                return other.continuity == this.continuity;
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as CurveKey);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.position.GetHashCode() + this.internalValue.GetHashCode() + this.tangentIn.GetHashCode() + this.tangentOut.GetHashCode() + this.continuity.GetHashCode();
        }

        /// <summary>
        /// Compares this instance to another CurveKey and returns an indication of their relative values.
        /// </summary>
        /// <param name="other">CurveKey to compare to.</param>
        public int CompareTo(CurveKey other)
        {
            if ((double)this.position == (double)other.position)
                return 0;
            return (double)this.position >= (double)other.position ? 1 : -1;
        }
    }
}
