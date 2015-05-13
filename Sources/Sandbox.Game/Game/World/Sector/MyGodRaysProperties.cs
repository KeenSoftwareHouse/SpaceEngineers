using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Game.World
{
    class MyGodRaysProperties
    {
        public bool Enabled = false;
        public float Density = 0.34f;
        public float Weight = 1.27f;
        public float Decay = 0.97f;
        public float Exposition = 0.077f;

        /// <param name="interpolator">0 - use this object, 1 - use other object</param>
        public MyGodRaysProperties InterpolateWith(MyGodRaysProperties otherProperties, float interpolator)
        {
            var result = new MyGodRaysProperties();
            result.Density = MathHelper.Lerp(Density, otherProperties.Density, interpolator);
            result.Weight = MathHelper.Lerp(Weight, otherProperties.Weight, interpolator);
            result.Decay = MathHelper.Lerp(Decay, otherProperties.Decay, interpolator);
            result.Exposition = MathHelper.Lerp(Exposition, otherProperties.Exposition, interpolator);
            result.Enabled = MathHelper.Lerp(Enabled ? 1 : 0, otherProperties.Enabled ? 1 : 0, interpolator) > 0.5f;
            return result;
        }
    }   
}
