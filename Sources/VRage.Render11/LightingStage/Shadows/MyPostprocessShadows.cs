using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    class MyPostprocessShadows: IManagerDevice
    {
        public enum Type
        {
            HARD,
            SIMPLE,
        }

        PixelShaderId m_ps = PixelShaderId.NULL;
        Type m_type;

        public MyPostprocessShadows(Type type)
        {
            m_type = type;
        }

        void IManagerDevice.OnDeviceInit()
        {
            if (m_ps == PixelShaderId.NULL)
            {
                switch (m_type)
                {
                    case Type.HARD:
                        m_ps = MyShaders.CreatePs("Shadows\\PostprocessHardShadows.hlsl");
                        break;
                    case Type.SIMPLE:
                        m_ps = MyShaders.CreatePs("Shadows\\PostprocessSimpleShadows.hlsl");
                        break;
                    default:
                        MyRenderProxy.Assert(false, "Unknown type of postproces!");
                        break;
                }
            }
        }

        void IManagerDevice.OnDeviceReset()
        {
            
        }

        void IManagerDevice.OnDeviceEnd()
        {
            
        }

        unsafe IConstantBuffer GetShadowConstants(ICascadeShadowMap csm, ref MyShadowsSettings settings)
        {
            const int MAX_SLICES_COUNT = 8;
            MyRenderProxy.Assert(csm.SlicesCount <= MAX_SLICES_COUNT, "It is not supported more than 8 slices per cascade shadow map");
            int size = sizeof(Matrix)*MAX_SLICES_COUNT + sizeof(Vector4)*MAX_SLICES_COUNT;
            IConstantBuffer cb = MyCommon.GetObjectCB(size);
            var mapping = MyMapping.MapDiscard(cb);

            for (int i = 0; i < csm.SlicesCount; i++)
            {
                // Set matrices:
                Matrix matrix = csm.GetSlice(i).MatrixWorldAt0ToShadowSpace;
                matrix = matrix*Matrix.CreateTranslation(1, -1, 0);
                
                Vector2 scalingFactor = new Vector2(0.5f, -0.5f);
                matrix = matrix * Matrix.CreateScale(scalingFactor.X, scalingFactor.Y, 1);
                matrix = Matrix.Transpose(matrix);
                mapping.WriteAndPosition(ref matrix);

                // Set normal offsets:
                mapping.WriteAndPosition(ref settings.Cascades[i].ShadowNormalOffset);
                float zero = 0;
                for (int j = 1; j < 4; j++)
                    mapping.WriteAndPosition(ref zero);
            }
            
            mapping.Unmap();
            return cb;
        }

        public void Draw(IRtvTexture outTex, IDepthStencil stencil, ICascadeShadowMap csm, ref MyShadowsSettings settings)
        {
            MyRenderContext RC = MyRender11.RC;
            RC.SetBlendState(null);
            RC.SetRtv(outTex);

            RC.PixelShader.Set(m_ps);
            RC.PixelShader.SetSrv(0, stencil.SrvDepth);
            RC.PixelShader.SetSrv(1, stencil.SrvStencil);
            RC.PixelShader.SetSrv(2, csm.DepthArrayTexture);
            RC.PixelShader.SetSrv(3, MyGBuffer.Main.GBuffer1);
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.PixelShader.SetConstantBuffer(1, GetShadowConstants(csm, ref settings));
            RC.PixelShader.SetSampler(6, MySamplerStateManager.Shadowmap);

            MyScreenPass.DrawFullscreenQuad();
            RC.ResetTargets();
        }
    }
}
