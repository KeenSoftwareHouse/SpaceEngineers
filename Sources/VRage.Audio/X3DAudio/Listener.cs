using SharpDX;
using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    public class Listener
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Native
        {
            public Vector3 OrientFront;
            public Vector3 OrientTop;
            public Vector3 Position;
            public Vector3 Velocity;
            public IntPtr ConePointer;
        }

        public Vector3 OrientFront;
        public Vector3 OrientTop;
        public Vector3 Position;
        public Vector3 Velocity;
        public Cone? Cone;
    }
}
