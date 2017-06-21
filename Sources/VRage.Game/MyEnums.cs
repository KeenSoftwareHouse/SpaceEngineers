using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Game
{
    //  What game type we start when quick launch is enabled
    public enum MyQuickLaunchType : byte
    {
        NEW_SANDBOX,
        LAST_SANDBOX,
    }


    //  Material type of a physical object. This value determine sound of collision, decal type, explosion type, etc.
    public static class MyMaterialType
    {
        public static MyStringHash ROCK         = MyStringHash.GetOrCompute("Rock");
        public static MyStringHash METAL        = MyStringHash.GetOrCompute("Metal");
        public static MyStringHash AMMO         = MyStringHash.GetOrCompute("Ammo");
        public static MyStringHash CHARACTER    = MyStringHash.GetOrCompute("Character");
        public static MyStringHash WOOD         = MyStringHash.GetOrCompute("Wood");
        public static MyStringHash THRUSTER_LARGE = MyStringHash.GetOrCompute("Thruster_large");
        public static MyStringHash THRUSTER_SMALL = MyStringHash.GetOrCompute("Thruster_small");
        public static MyStringHash MISSILE = MyStringHash.GetOrCompute("Missile");
    }
}
