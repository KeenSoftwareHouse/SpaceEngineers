using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Common
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
		public static MyStringHash SNOW			= MyStringHash.GetOrCompute("Snow");
		public static MyStringHash ICE			= MyStringHash.GetOrCompute("Ice");
        public static MyStringHash ROCK			= MyStringHash.GetOrCompute("Rock");
		public static MyStringHash GRASS		= MyStringHash.GetOrCompute("Grass");
		public static MyStringHash GRASSDRY		= MyStringHash.GetOrCompute("GrassDry");
		public static MyStringHash SAND			= MyStringHash.GetOrCompute("Sand");
		public static MyStringHash SOIL			= MyStringHash.GetOrCompute("Soil");
		public static MyStringHash SOILDRY		= MyStringHash.GetOrCompute("SoilDry");
        public static MyStringHash METAL		= MyStringHash.GetOrCompute("Metal");
        public static MyStringHash GLASS		= MyStringHash.GetOrCompute("Glass");
        public static MyStringHash SHIP			= MyStringHash.GetOrCompute("Ship");
        public static MyStringHash AMMO			= MyStringHash.GetOrCompute("Ammo");
        public static MyStringHash CHARACTER	= MyStringHash.GetOrCompute("Character");
        public static MyStringHash RIFLEBULLET	= MyStringHash.GetOrCompute("RifleBullet");
        public static MyStringHash GUNBULLET	= MyStringHash.GetOrCompute("GunBullet");
        public static MyStringHash EXPBULLET	= MyStringHash.GetOrCompute("ExpBullet");
        public static MyStringHash BOLT			= MyStringHash.GetOrCompute("Bolt");
        public static MyStringHash DEER         = MyStringHash.GetOrCompute("Deer");
        public static MyStringHash SPIDER       = MyStringHash.GetOrCompute("Spider");
        public static MyStringHash WOOD         = MyStringHash.GetOrCompute("Wood");
        public static MyStringHash TREE         = MyStringHash.GetOrCompute("Tree");
    }
}
