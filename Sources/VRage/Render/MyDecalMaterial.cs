using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace VRageRender
{

    public class MyDecalMaterial
    {
        public MyDecalMaterial(MyDecalMaterialDesc materialDef, MyStringHash target, MyStringHash source,
            float minSize, float maxSize, float depth, float rotation)
        {
            Material = materialDef;
            Target = target;
            Source = source;
            MinSize = minSize;
            MaxSize = maxSize;
            Depth = depth;
            Rotation = rotation;
        }

        public string GetStringId()
        {
            return Target + "__" + (Source == MyStringHash.NullOrEmpty ? "NULL" : Source.String);
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

    public struct MyDecalMaterialId
    {
        public string Target;
        public string Source;
    }


    public enum MyScreenDecalType
    {
        NormalMap, // affects normalmap on whole surface
        ColorMap, // affects color and metallness on alphatested surface
        NormalColorMap // affects color and metallness on alphatested surface
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
    }
}
