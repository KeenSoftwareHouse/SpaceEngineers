using System;
using VRageMath;

using VRage.Utils;


namespace Sandbox.Engine.Platform.VideoMode
{
    //  IMPORTANT: Never change numeric values. When you need new enum item, just use new number!
    public enum MyAspectRatioEnum
    {
        Normal_4_3 = 0,
        Normal_16_9 = 1,
        Normal_16_10 = 2,

        Dual_4_3 = 3,
        Dual_16_9 = 4,
        Dual_16_10 = 5,

        Triple_4_3 = 6,
        Triple_16_9 = 7,
        Triple_16_10 = 8,

        Unsupported_5_4 = 9,
    }

    public struct MyAspectRatio
    {
        public readonly MyAspectRatioEnum AspectRatioEnum;
        public readonly float AspectRatioNumber;
        public readonly string TextShort;
        public readonly bool IsTripleHead;
        public readonly bool IsSupported;

        public MyAspectRatio(bool isTripleHead, MyAspectRatioEnum aspectRatioEnum, float aspectRatioNumber, string textShort, bool isSupported)
        {
            IsTripleHead = isTripleHead;
            AspectRatioEnum = aspectRatioEnum;
            AspectRatioNumber = aspectRatioNumber;
            TextShort = textShort;
            IsSupported = isSupported;
        }
    }
}
