using SharpDX;
using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
	[Unsharper.UnsharperDisableReflection()]
    public sealed class DistanceCurve : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Native
        {
            public IntPtr PointsPointer;
            public int PointCount;
        }

        internal unsafe Native* DataPointer;

        public int MaxCount { get; private set; }

        public DistanceCurve(params CurvePoint[] points)
        {
            if (points.Length < 2)
                throw new InvalidOperationException("Data curve must have at least two points");
            CreateNative(points.Length);
            Write(points);
        }

        /// <summary>
        /// Sets new data, resizes native memory if needed.
        /// </summary>
        public void SetData(params CurvePoint[] points)
        {
            if (points.Length < 2)
                throw new InvalidOperationException("Data curve must have at least two points");

            if (points.Length > MaxCount)
            {
                ReleaseNative();
                CreateNative(points.Length);
            }
            Write(points);
        }

        private unsafe void CreateNative(int pointCount)
        {
            DataPointer = (Native*)(void*)Marshal.AllocHGlobal(Utilities.SizeOf<Native>() + Utilities.SizeOf<CurvePoint>() * pointCount);
            DataPointer->PointsPointer = new IntPtr(DataPointer + 1);
            MaxCount = pointCount;
        }

        private unsafe void ReleaseNative()
        {
            Marshal.FreeHGlobal(new IntPtr(DataPointer));
            DataPointer = null;
            MaxCount = 0;
        }

        private unsafe void Write(CurvePoint[] points)
        {
            Utilities.Write(DataPointer->PointsPointer, points, 0, points.Length);
            DataPointer->PointCount = points.Length;
        }

        public void Dispose()
        {
            ReleaseNative();
            GC.SuppressFinalize(this);
        }

        ~DistanceCurve()
        {
            ReleaseNative();
        }
    }
}
