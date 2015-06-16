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
        SCENARIO_QUICKSTART
    }


    //  Material type of a physical object. This value determine sound of collision, decal type, explosion type, etc.
    public static class MyMaterialType
    {
        public static MyStringHash ROCK        = MyStringHash.GetOrCompute("Rock");
        public static MyStringHash METAL       = MyStringHash.GetOrCompute("Metal");
        public static MyStringHash GLASS       = MyStringHash.GetOrCompute("Glass");
        public static MyStringHash SHIP        = MyStringHash.GetOrCompute("Ship");
        public static MyStringHash AMMO        = MyStringHash.GetOrCompute("Ammo");
        public static MyStringHash CHARACTER   = MyStringHash.GetOrCompute("Character");
        public static MyStringHash RIFLEBULLET = MyStringHash.GetOrCompute("RifleBullet");
        public static MyStringHash GUNBULLET   = MyStringHash.GetOrCompute("GunBullet");
        public static MyStringHash EXPBULLET   = MyStringHash.GetOrCompute("ExpBullet");
    }

    

    public enum MyRelationsBetweenPlayerAndBlock
    {
        Owner,
        FactionShare,
        Neutral,
        Enemies
    }
}
