using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Game.World
{
    class MyParticleDustProperties
    {
        public bool Enabled = false;
        public float DustBillboardRadius = 3;
        public float DustFieldCountInDirectionHalf = 5;
        public float DistanceBetween = 180;
        public float AnimSpeed = 0.004f;
        public Color Color = Color.White;
        public int Texture = 0;

        /// <param name="interpolator">0 - use this object, 1 - use other object</param>
        public MyParticleDustProperties InterpolateWith(MyParticleDustProperties otherProperties, float interpolator)
        {
            var result = new MyParticleDustProperties();
            result.DustFieldCountInDirectionHalf = MathHelper.Lerp(DustFieldCountInDirectionHalf, otherProperties.DustFieldCountInDirectionHalf, interpolator);
            result.DistanceBetween = MathHelper.Lerp(DistanceBetween, otherProperties.DistanceBetween, interpolator);
            result.AnimSpeed = MathHelper.Lerp(AnimSpeed, otherProperties.AnimSpeed, interpolator);
            result.Color = Color.Lerp(Color, otherProperties.Color, interpolator);
            result.Enabled = MathHelper.Lerp(Enabled ? 1 : 0, otherProperties.Enabled ? 1 : 0, interpolator) > 0.5f;
            result.DustBillboardRadius = interpolator <= 0.5f ? DustBillboardRadius : otherProperties.DustBillboardRadius;
            result.Texture = interpolator <= 0.5f ? Texture : otherProperties.Texture;
            return result;
        }
    }
}
