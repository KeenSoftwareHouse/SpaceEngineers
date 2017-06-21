using System;
using System.Xml.Serialization;
using ProtoBuf;

namespace VRageMath
{
    [ProtoContract]
    public struct SerializableRange
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Min")]
        public float Min;
        [ProtoMember]
        [XmlAttribute(AttributeName = "Max")]
        public float Max;

        public SerializableRange(float min, float max)
        {
            Max = max;
            Min = min;
        }

        public bool ValueBetween(float value)
        {
            return value >= Min && value <= Max;
        }

        public override string ToString()
        {
            return String.Format("Range[{0}, {1}]", Min, Max);
        }

        /**
         * When the range is an angle this method changes it to the cosines of the angle.
         * 
         * The angle is expected to be in degrees.
         * 
         * Also beware that cosine is a decreasing function in [0,90], for that reason the minimum and maximum are swaped.
         * 
         */
        public SerializableRange ConvertToCosine()
        {
            float oldMax = Max;
            Max = (float)Math.Cos(Min * Math.PI / 180);
            Min = (float)Math.Cos(oldMax * Math.PI / 180);

            return this;
        }

        /**
         * When the range is an angle this method changes it to the sines of the angle.
         * 
         * The angle is expected to be in degrees.
         */
        public SerializableRange ConvertToSine()
        {
            Max = (float)Math.Sin(Max * Math.PI / 180);
            Min = (float)Math.Sin(Min * Math.PI / 180);

            return this;
        }

        public SerializableRange ConvertToCosineLongitude()
        {
            Max = MathHelper.MonotonicCosine((float)(Max * Math.PI / 180));
            Min = MathHelper.MonotonicCosine((float)(Min * Math.PI / 180));

            return this;
        }

        public string ToStringAsin()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Asin(Min)), MathHelper.ToDegrees(Math.Asin(Max)));
        }

        public string ToStringAcos()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Acos(Min)), MathHelper.ToDegrees(Math.Acos(Max)));
        }

        public string ToStringLongitude()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(MathHelper.MonotonicAcos(Min)), MathHelper.ToDegrees(MathHelper.MonotonicAcos(Max)));
        }
    }

    /**
     * Reflective because it can be reflected to the oposite range.
     * 
     * Structs not inheriting from structs is stupid.
     */
    public struct SymetricSerializableRange
    {
        [XmlAttribute(AttributeName = "Min")]
        public float Min;

        [XmlAttribute(AttributeName = "Max")]
        public float Max;

        // Need this to force true to default.
        private bool m_notMirror;

     
        [XmlAttribute(AttributeName = "Mirror")]
        public bool Mirror
        {
            get { return !m_notMirror; }
            set { m_notMirror = !value; }
        }

        public SymetricSerializableRange(float min, float max, bool mirror = true)
        {
            Max = max;
            Min = min;
            m_notMirror = !mirror;
        }

        public bool ValueBetween(float value)
        {
            if (!m_notMirror)
                value = Math.Abs(value);
            return value >= Min && value <= Max;
        }

        public override string ToString()
        {
            return String.Format("{0}[{1}, {2}]", Mirror ? "MirroredRange" : "Range", Min, Max);
        }

        /**
         * When the range is an angle this method changes it to the cosines of the angle.
         * 
         * The angle is expected to be in degrees.
         * 
         * Also beware that cosine is a decreasing function in [0,90], for that reason the minimum and maximum are swaped.
         * 
         */
        public SymetricSerializableRange ConvertToCosine()
        {
            float oldMax = Max;
            Max = (float)Math.Cos(Min * Math.PI / 180);
            Min = (float)Math.Cos(oldMax * Math.PI / 180);

            return this;
        }

        /**
         * When the range is an angle this method changes it to the sines of the angle.
         * 
         * The angle is expected to be in degrees.
         */
        public SymetricSerializableRange ConvertToSine()
        {
            Max = (float)Math.Sin(Max * Math.PI / 180);
            Min = (float)Math.Sin(Min * Math.PI / 180);

            return this;
        }

        public SymetricSerializableRange ConvertToCosineLongitude()
        {
            Max = CosineLongitude(Max);
            Min = CosineLongitude(Min);

            return this;
        }

        private static float CosineLongitude(float angle)
        {
            float val;
            if (angle > 0)
            {
                val = 2 - (float)Math.Cos(angle * Math.PI / 180);
            }
            else
            {
                val = (float)Math.Cos(angle * Math.PI / 180);
            }
            return val;
        }

        public string ToStringAsin()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Asin(Min)), MathHelper.ToDegrees(Math.Asin(Max)));
        }

        public string ToStringAcos()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Acos(Min)), MathHelper.ToDegrees(Math.Acos(Max)));
        }
    }
}
