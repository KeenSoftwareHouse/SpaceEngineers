using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;
using VRageMath;

namespace Server
{
    [Synchronized]
    public class Foo : Bar
    {
        [StateData]
        public MySyncedString Name;

        public Foo()
            : base()
        {
            Name = new MySyncedString();
            Name.Set("Foo");
        }
    }
}
