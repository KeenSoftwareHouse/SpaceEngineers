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

    public enum MyFontEnum
    {
        Debug, // First font will be used as debug by engine, so we put Debug font here.
        Red,
        Green,
        Blue,
        White,
        DarkBlue,

        UrlNormal,
        UrlHighlight,
        ErrorMessageBoxCaption,
        ErrorMessageBoxText,
        InfoMessageBoxCaption,
        InfoMessageBoxText,
        ScreenCaption,
        GameCredits,
        LoadingScreen,

        BuildInfo,
        BuildInfoHighlight,
    }

    //  Material type of a physical object. This value determine sound of collision, decal type, explosion type, etc.
    public static class MyMaterialType
    {
        public static MyStringId ROCK = MyStringId.GetOrCompute("Rock");
        public static MyStringId METAL = MyStringId.GetOrCompute("Metal");
        public static MyStringId GLASS = MyStringId.GetOrCompute("Glass");
        public static MyStringId SHIP = MyStringId.GetOrCompute("Ship");
        public static MyStringId AMMO = MyStringId.GetOrCompute("Ammo");
        public static MyStringId CHARACTER = MyStringId.GetOrCompute("Character");
        public static MyStringId RIFLEBULLET = MyStringId.GetOrCompute("RifleBullet");
        public static MyStringId GUNBULLET = MyStringId.GetOrCompute("GunBullet");
        public static MyStringId EXPBULLET = MyStringId.GetOrCompute("ExpBullet");
    }

    public enum MyRelationsBetweenFactions
    {
        Neutral,
        Enemies
    }

    public enum MyRelationsBetweenPlayerAndBlock
    {
        Owner,
        FactionShare,
        Neutral,
        Enemies
    }
}
