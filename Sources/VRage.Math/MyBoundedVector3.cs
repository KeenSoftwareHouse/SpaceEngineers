
namespace VRageMath
{
    public struct MyBoundedVector3
    {
        public Vector3 Min;
        public Vector3 Max;
        public Vector3 Default;

        public MyBoundedVector3(Vector3 min, Vector3 max, Vector3 def)
        {
            Min = min;
            Max = max;
            Default = def;
        }

        /// <summary>
        /// Normalize value inside the bounds so that 0 is Min and 1 is Max.
        /// </summary>
        public Vector3 Normalize(float value)
        {
            return new Vector3(
                (value - Min.X) / (Max.X - Min.X),
                (value - Min.Y) / (Max.Y - Min.Y),
                (value - Min.Z) / (Max.Z - Min.Z));
        }

        public Vector3 Clamp(Vector3 value)
        {
            return new Vector3(
                MathHelper.Clamp(value.X, Min.X, Max.X),
                MathHelper.Clamp(value.Y, Min.Y, Max.Y),
                MathHelper.Clamp(value.Z, Min.Z, Max.Z));
        }

        public override string ToString()
        {
            return string.Format("Min={0}, Max={1}, Default={2}", Min, Max, Default);
        }
    }
}
