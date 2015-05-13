using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Dx11VoxelMaterialDefinition : MyObjectBuilder_VoxelMaterialDefinition
    {
        [ProtoMember(1)]
        public string ColorMetalXZnY;

        [ProtoMember(2)]
        public string ColorMetalY;

        [ProtoMember(3)]
        public string NormalGlossXZnY;

        [ProtoMember(4)]
        public string NormalGlossY;

        [ProtoMember(5)]
        public string ExtXZnY;

        [ProtoMember(6)]
        public string ExtY;

        [ProtoMember(7)]
        public string ColorMetalXZnYFar1;

        [ProtoMember(8)]
        public string ColorMetalYFar1;

        [ProtoMember(9)]
        public string NormalGlossXZnYFar1;

        [ProtoMember(10)]
        public string NormalGlossYFar1;

        [ProtoMember(11)]
        public float Scale = 8f;

        [ProtoMember(12)]
        public float ScaleFar1 = 8f;

        [ProtoMember(13)]
        public string ExtXZnYFar1;

        [ProtoMember(14)]
        public string ExtYFar1;

        [ProtoMember(15)]
        public string FoliageTextureArray1 = null;

        [ProtoMember(16)]
        public string FoliageTextureArray2 = null;

        [ProtoMember(17)]
        public float FoliageDensity;

        [ProtoMember(18)]
        public Vector2 FoliageScale = Vector2.One;

        [ProtoMember(19)]
        public float FoliageRandomRescaleMult = 0;

        [ProtoMember(20)]
        public byte BiomeValueMin;

        [ProtoMember(21)]
        public byte BiomeValueMax;

        [ProtoMember(22)]
        public string ColorMetalXZnYFar2;

        [ProtoMember(23)]
        public string ColorMetalYFar2;

        [ProtoMember(24)]
        public string NormalGlossXZnYFar2;

        [ProtoMember(25)]
        public string NormalGlossYFar2;

        [ProtoMember(26)]
        public string ExtXZnYFar2;

        [ProtoMember(27)]
        public string ExtYFar2;

        [ProtoMember(28)]
        public float ScaleFar2 = 8f;
    }
}
