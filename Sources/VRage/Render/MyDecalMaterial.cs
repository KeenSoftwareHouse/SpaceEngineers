using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{

    public class MyDecalMaterial
    {
        public MyDecalMaterial(MyDecalMaterialDesc materialDef, MyStringHash target, MyStringHash source,
            float minSize, float maxSize, float depth, float rotation)
        {
            StringId = MyDecalMaterials.GetStringId(source, target);
            Material = materialDef;
            Target = target;
            Source = source;
            MinSize = minSize;
            MaxSize = maxSize;
            Depth = depth;
            Rotation = rotation;
        }

        public string StringId
        {
            get;
            private set;
        }

        public MyDecalMaterialDesc Material
        {
            get;
            private set;
        }

        public MyStringHash Target
        {
            get;
            private set;
        }

        public MyStringHash Source
        {
            get;
            private set;
        }

        public float MinSize
        {
            get;
            private set;
        }

        public float MaxSize
        {
            get;
            private set;
        }

        public float Depth
        {
            get;
            private set;
        }

        /// <summary>
        /// Positive infinity for random rotation
        /// </summary>
        public float Rotation
        {
            get;
            private set;
        }
    }

    public enum MyScreenDecalType
    {
        NormalMap,          // Normals, Gloss
        ColorMap,           // Color, Metal
        NormalColorMap,     // Color, Metal
        NormalColorExtMap   // Color, Metal, Normals, Gloss, AO, Emissivity
    }

    [ProtoContract]
    public struct MyDecalMaterialDesc
    {
        [ProtoMember]
        public MyScreenDecalType DecalType;
        [ProtoMember]
        public string NormalmapTexture;
        [ProtoMember]
        public string ColorMetalTexture;
        [ProtoMember]
        public string AlphamaskTexture;
        [ProtoMember]
        public string ExtensionsTexture;
    }

    public struct MyDecalRenderInfo
    {
        public MyDecalFlags Flags;
        public Vector3D Position;
        public Vector3 Normal;
        public int RenderObjectId;
        public MyStringHash Material;
    }

    [Flags]
    public enum MyDecalFlags
    {
        None = 0,
        World = 1,                              // Position is in world coordinates
        Transparent = 2
    }
}
