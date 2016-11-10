using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Messages;
using ImageFileFormat = SharpDX.Direct3D9.ImageFileFormat;


namespace VRage.Render11.PostprocessStage
{
    class MySaveExportedTextures : MyScreenPass
    {
        static PixelShaderId m_ps;
        static IConstantBuffer m_cb;

        static bool m_initialized;

        private unsafe static void Init()
        {
            m_ps = MyShaders.CreatePs("Postprocess/PostprocessColorizeExportedEexture.hlsl");
            m_cb = MyManagers.Buffers.CreateConstantBuffer("ExportedTexturesColor", sizeof(Vector4), usage: ResourceUsage.Dynamic);
            m_initialized = true;
        }

        internal static void RenderColoredTextures(List<renderColoredTextureProperties> texturesToRender)
        {
            if (texturesToRender.Count == 0)
                return;

            if (!m_initialized)
                Init();

            const int RENDER_TEXTURE_RESOLUTION = 512;

            RC.SetBlendState(null);
            RC.SetInputLayout(null);

            RC.PixelShader.Set(m_ps);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.AllShaderStages.SetConstantBuffer(1, m_cb);

            MyBorrowedRwTextureManager rwTexManager = MyManagers.RwTexturesPool;
            MyFileTextureManager fileTexManager = MyManagers.FileTextures;
            foreach (var texture in texturesToRender)
            {
                ISrvBindable tex = fileTexManager.GetTexture(texture.TextureName, MyFileTextureEnum.COLOR_METAL, true);
                if (tex == null)
                    continue;

                Vector2 texSize = tex.Size;
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

                IBorrowedRtvTexture renderTexture = rwTexManager.BorrowRtv("MySaveExportedTextures.RenderColoredTextures",
                        renderTargetResolution.X, renderTargetResolution.Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, 1, 0);

                RC.SetRtv(renderTexture);

                // Set color
                var mapping = MyMapping.MapDiscard(m_cb);
                Vector4 color = new Vector4(texture.ColorMaskHSV, 1);
                mapping.WriteAndPosition(ref color);
                mapping.Unmap();

                // Set texture
                RC.PixelShader.SetSrv(0, tex);

                // Draw
                MyScreenPass.DrawFullscreenQuad(viewport);

                // Save to file
                MyTextureData.ToFile(renderTexture, texture.PathToSave, ImageFileFormat.Png);
                
                renderTexture.Release();
            }

            texturesToRender.Clear();

            RC.SetRtv(null);
            RC.PixelShader.SetSrvs(0, MyGBuffer.Main);
        }
    }
}
