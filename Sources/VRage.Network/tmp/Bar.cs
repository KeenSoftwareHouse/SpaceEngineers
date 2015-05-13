using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;
using VRageMath;

namespace Server
{
    [Synchronized]
    public class Bar
    {
        public ulong EntityID;

        [StateData]
        public MySyncedVector3D Position;

        [StateData]
        public MySyncedQuaternionNorm Orientation;

        public Bar()
        {
            Position = new MySyncedVector3D();
            Orientation = new MySyncedQuaternionNorm();
        }
    }
}
