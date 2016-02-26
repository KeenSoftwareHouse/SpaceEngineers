using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Effects;

namespace VRageRender.Techniques
{
    class MyDrawTechniquePlanetSurface : MyDrawTechniqueAtmosphere
    {
        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectAtmosphere)MyRender.GetEffect(MyEffects.Atmosphere);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetSurfaceTechnique();
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            base.SetupEntity(shader, renderElement);

            var atmosphere = renderElement.RenderObject as MyRenderAtmosphere;
            if (atmosphere.IsInside(MyRenderCamera.Position))
            {
                MyEffectAtmosphere effectAtmosphere = shader as MyEffectAtmosphere;
                Matrix optProjection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, MyRenderCamera.NEAR_PLANE_DISTANCE, atmosphere.AtmosphereRadius * 2.0f / MyRenderAtmosphere.ATMOSPHERE_SCALE);
                effectAtmosphere.SetProjectionMatrix(ref optProjection);
            }

        }

    }
}
