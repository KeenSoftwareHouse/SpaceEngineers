using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using VRageMath;
using VRageRender;
using VRageRender.Resources;

namespace VRage.Render11.PostprocessStage
{
    class MySaveExportedTextures : MyScreenPass
    {
        static PixelShaderId m_ps;
        static ConstantsBufferId m_cb;

        static bool m_initialized;

        private unsafe static void Init()
        {
            m_ps = MyShaders.CreatePs("postprocess_colorize_exported_texture.hlsl");
            m_cb = MyHwBuffers.CreateConstantsBuffer(sizeof(Vector4), "ExportedTexturesColor");
            m_initialized = true;
        }

        internal static void RenderColoredTextures(List<renderColoredTextureProperties> texturesToRender)
        {
            if (texturesToRender.Count == 0)
                return;

            if (!m_initialized)
                Init();

            const int RENDER_TEXTURE_RESOLUTION = 512;

            RC.DeviceContext.OutputMerger.BlendState = null;
            RC.SetIL(null);

            RC.SetPS(m_ps);
            RC.SetCB(0, MyCommon.FrameConstants);
            RC.SetCB(1, m_cb);

            Dictionary<Vector2I, MyRenderTarget> createdRenderTextureTargets = new Dictionary<Vector2I, MyRenderTarget>();

            foreach (var texture in texturesToRender)
            {
                TexId texId = MyTextures.GetTexture(texture.TextureName, MyTextureEnum.COLOR_METAL, true);
                if (texId == TexId.NULL) 
                    continue;

                Vector2 texSize = MyTextures.GetSize(texId);
                Vector2I renderTargetResolution = new Vector2I(RENDER_TEXTURE_RESOLUTION, RENDER_TEXTURE_RESOLUTION);
                if (texSize.Y > 0)
                {
                    if (texSize.Y < RENDER_TEXTURE_RESOLUTION)
                    {
                        renderTargetResolution.X = (int)texSize.X;
                        renderTargetResolution.Y = (int)texSize.Y;
                    }
                    else
                    {
                        renderTargetResolution.X *= (int)(texSize.X / texSize.Y);
                    }
                }

                MyViewport viewport = new MyViewport(renderTargetResolution.X, renderTargetResolution.Y);

                MyRenderTarget renderTexture = null;
                if (!createdRenderTextureTargets.TryGetValue(renderTargetResolution, out renderTexture))
                {
                    renderTexture = new MyRenderTarget(renderTargetResolution.X, renderTargetResolution.Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, 1, 0);
                    createdRenderTextureTargets[renderTargetResolution] = renderTexture;
                }

                RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, renderTexture);

                // Set color
                var mapping = MyMapping.MapDiscard(m_cb);
                Vector4 color = new Vector4(texture.ColorMaskHSV, 1);
                mapping.WriteAndPosition(ref color);
                mapping.Unmap();

                // Set texture
                RC.DeviceContext.PixelShader.SetShaderResource(0, MyTextures.GetView(texId));

                // Draw
                MyScreenPass.DrawFullscreenQuad(viewport);

                // Save to file
                MyTextureData.ToFile(renderTexture.GetHWResource(), texture.PathToSave, ImageFileFormat.Png);
            }

            texturesToRender.Clear();

            foreach (var texture in createdRenderTextureTargets)
            {
                texture.Value.Release();
            }
            createdRenderTextureTargets.Clear();

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, null);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
        }
    }
}
