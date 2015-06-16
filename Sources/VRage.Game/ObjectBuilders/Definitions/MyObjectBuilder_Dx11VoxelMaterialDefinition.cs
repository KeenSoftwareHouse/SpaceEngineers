using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.Data;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Dx11VoxelMaterialDefinition : MyObjectBuilder_VoxelMaterialDefinition
    {
        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ColorMetalXZnY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ColorMetalY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossXZnY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtXZnY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ColorMetalXZnYFar1;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ColorMetalYFar1;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossXZnYFar1;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossYFar1;

        [ProtoMember]
        public float Scale = 8f;

        [ProtoMember]
        public float ScaleFar1 = 8f;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtXZnYFar1;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtYFar1;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string FoliageTextureArray1 = null;

        [ProtoMember]
        [ModdableContentFile("dds")]
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
        [ModdableContentFile("dds")]
        public string ColorMetalXZnYFar2;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ColorMetalYFar2;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossXZnYFar2;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalGlossYFar2;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtXZnYFar2;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ExtYFar2;

        [ProtoMember]
        public float ScaleFar2 = 8f;
    }
}
