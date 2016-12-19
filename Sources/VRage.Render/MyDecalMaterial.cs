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
        public MyDecalMaterial(MyDecalMaterialDesc materialDef, bool transparent, MyStringHash target, MyStringHash source,
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
            Transparent = transparent;
        }

        public string StringId
        {
            get;
            private set;
        }

        public bool Transparent;

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

    public struct MyDecalMaterialDesc
    {
        public string NormalmapTexture;
        public string ColorMetalTexture;
        public string AlphamaskTexture;
        public string ExtensionsTexture;
    }

    public struct MyDecalRenderInfo
    {
        public MyDecalFlags Flags;
        public Vector3D Position;
        public Vector3 Normal;
        public Vector4UByte BoneIndices;
        public Vector4 BoneWeights;
        public MyDecalBindingInfo? Binding;
        public int RenderObjectId;
        public MyStringHash Material;
    }

    // E.g. Position in binding/static pose
    public struct MyDecalBindingInfo
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Matrix Transformation; // Tranformation from binding to current pose
    }

    [Flags]
    public enum MyDecalFlags
    {
        None = 0,
        World = 1,                              // Position is in world coordinates
        Transparent = 2
    }
}
