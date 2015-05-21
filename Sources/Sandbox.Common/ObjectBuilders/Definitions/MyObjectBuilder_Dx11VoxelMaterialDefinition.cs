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
        [ProtoMember]
        public string ColorMetalXZnY;

        [ProtoMember]
        public string ColorMetalY;

        [ProtoMember]
        public string NormalGlossXZnY;

        [ProtoMember]
        public string NormalGlossY;

        [ProtoMember]
        public string ExtXZnY;

        [ProtoMember]
        public string ExtY;

        [ProtoMember]
        public string ColorMetalXZnYFar1;

        [ProtoMember]
        public string ColorMetalYFar1;

        [ProtoMember]
        public string NormalGlossXZnYFar1;

        [ProtoMember]
        public string NormalGlossYFar1;

        [ProtoMember]
        public float Scale = 8f;

        [ProtoMember]
        public float ScaleFar1 = 8f;

        [ProtoMember]
        public string ExtXZnYFar1;

        [ProtoMember]
        public string ExtYFar1;

        [ProtoMember]
        public string FoliageTextureArray1 = null;

        [ProtoMember]
        public string FoliageTextureArray2 = null;

        [ProtoMember]
        public float FoliageDensity;

        [ProtoMember]
        public Vector2 FoliageScale = Vector2.One;

        [ProtoMember]
        public float FoliageRandomRescaleMult = 0;

        [ProtoMember]
        public byte BiomeValueMin;

        [ProtoMember]
        public byte BiomeValueMax;

        [ProtoMember]
        public string ColorMetalXZnYFar2;

        [ProtoMember]
        public string ColorMetalYFar2;

        [ProtoMember]
        public string NormalGlossXZnYFar2;

        [ProtoMember]
        public string NormalGlossYFar2;

        [ProtoMember]
        public string ExtXZnYFar2;

        [ProtoMember]
        public string ExtYFar2;

        [ProtoMember]
        public float ScaleFar2 = 8f;
    }
}
