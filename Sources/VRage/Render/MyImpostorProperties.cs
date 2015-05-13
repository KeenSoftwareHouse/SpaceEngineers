using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public enum MyImpostorType
    {
        Billboards,
        Nebula
    }

    public struct MyImpostorProperties
    {
        public bool Enabled;
        public MyImpostorType ImpostorType;
        public VRageRender.MyTransparentMaterial Material;
        public int ImpostorsCount;
        public float MinDistance;
        public float MaxDistance;
        public float MinRadius;
        public float MaxRadius;
        public Vector4 AnimationSpeed;
        public Vector3 Color;
        public float Intensity;
        public float Contrast;

        // Gets or sets both MinRadius and MaxRadius
        // Always returns MaxRadius
        public float Radius;
        
        public float Anim1;
        public float Anim2;
        public float Anim3;
        public float Anim4;
    }

}
