using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D9;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender
{
    class MyPostProcessChromaticAberration : MyPostProcessBase
    {
        public float DistortionLens = -0.145f;
        public float DistortionCubic;
        public Vector3 DistortionWeights = new Vector3(1, 0.9f, 0.8f);

        public MyPostProcessChromaticAberration(bool enabled)
            : base(enabled)
        {
        }

        public override MyPostProcessEnum Name
        {
            get { return MyPostProcessEnum.ChromaticAberration; }
        }

        public override string DisplayName
        {
            get { return "ChromaticAberration"; }
        }

        public override Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget)
        {
            switch (postProcessStage)
            {
                case PostProcessStage.AlphaBlended:
                    {
                        BlendState.Opaque.Apply();
                        DepthStencilState.None.Apply();
                        RasterizerState.CullCounterClockwise.Apply();

                        MyRender.SetRenderTarget(availableRenderTarget, null);

                        MyEffectChromaticAberration effectChromaAberr = MyRender.GetEffect(MyEffects.ChromaticAberration) as MyEffectChromaticAberration;
                        effectChromaAberr.SetInputTexture(source);
                        effectChromaAberr.SetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height);
                        effectChromaAberr.SetAspectRatio((float)source.GetLevelDescription(0).Width / (float)source.GetLevelDescription(0).Height);
                        effectChromaAberr.SetDistortionLens(DistortionLens);
                        effectChromaAberr.SetDistortionCubic(DistortionCubic);
                        effectChromaAberr.SetDistortionWeights(ref DistortionWeights);

                        effectChromaAberr.Enable();

                        MyRender.GetFullscreenQuad().Draw(effectChromaAberr);
                        return availableRenderTarget;
                    }
            }
            return source;
        }
    }
}
