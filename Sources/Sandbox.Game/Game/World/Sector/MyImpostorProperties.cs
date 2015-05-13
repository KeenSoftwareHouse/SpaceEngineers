using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Game.World
{
   
    class MyImpostorProperties
    {
        public bool Enabled = true;
        public int ImpostorType;
        public int? Material;
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
        public float Radius
        {
            get
            {
                return MaxRadius;
            }
            set
            {
                MinRadius = value;
                MaxRadius = value;
            }
        }

        public float Anim1 { get { return AnimationSpeed.X; } set { AnimationSpeed.X = value; } }
        public float Anim2 { get { return AnimationSpeed.Y; } set { AnimationSpeed.Y = value; } }
        public float Anim3 { get { return AnimationSpeed.Z; } set { AnimationSpeed.Z = value; } }
        public float Anim4 { get { return AnimationSpeed.W; } set { AnimationSpeed.W = value; } }
    }
}
