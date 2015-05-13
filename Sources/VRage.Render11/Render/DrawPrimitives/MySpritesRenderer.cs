using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Render11.Shaders;
using VRageRender.Resources;
using Buffer = SharpDX.Direct3D11.Buffer;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Color = VRageMath.Color;
using VRageRender.Vertex;
using VRageMath.PackedVector;

namespace VRageRender
{
    // not thread safe at all
    struct MySpritesBatch
    {
        internal int instanceCount;
        internal int startInstance;

        internal ShaderResourceView texture;

        internal void SetTexture(MyTexture2D texture)
        {
            this.texture = texture.ShaderView;
        }

        internal void AddSprite(Vector2 clipOffset, Vector2 clipScale, Vector2 texOffset, Vector2 texScale, Byte4 color)
        {
            MySpritesRenderer.m_spriteInstanceList.Add(new MyVertexFormatSpritePositionTextureColor(
                new HalfVector4(clipOffset.X, clipOffset.Y, clipScale.X, clipScale.Y),
                new HalfVector4(texOffset.X, texOffset.Y, texScale.X, texScale.Y),
                color));
        }

        internal void AddSprite(MyVertexFormatSpritePositionTextureColor sprite)
        {
            MySpritesRenderer.m_spriteInstanceList.Add(sprite);
        }

        internal void Commit()
        {
            instanceCount = MySpritesRenderer.m_spriteInstanceList.Count - startInstance;
            MySpritesRenderer.m_spriteBatches.Add(this);
        }
    };

    static class MySpritesRenderer
    {
        internal static List<MyVertexFormatSpritePositionTextureColor> m_spriteInstanceList = new List<MyVertexFormatSpritePositionTextureColor>();
        internal static List<MySpritesBatch> m_spriteBatches = new List<MySpritesBatch>();

        internal static MySpritesBatch m_currentInnerBatch;

        internal static MyShader m_vertexShader = MyShaderCache.Create("sprite.hlsl", "vs", MyShaderProfile.VS_5_0);
        internal static MyShader m_pixelShader = MyShaderCache.Create("sprite.hlsl", "ps", MyShaderProfile.PS_5_0);
        internal static InputLayout m_inputLayout;

        internal static MyVertexBuffer m_spritesInstanceBuffer;

        internal unsafe static void Init()
        {
            m_spritesInstanceBuffer = MyRender.WrapResource("sprites instance buffer", new MyVertexBuffer(MyRenderConstants.MAX_SPRITES * sizeof(MyVertexFormatSpritePositionTextureColor), ResourceUsage.Dynamic));
        }

        internal static void AddSingleSprite(MyTexture2D texture, MyVertexFormatSpritePositionTextureColor sprite)
        {
            if (m_currentInnerBatch.texture != texture.ShaderView)
            {
                if (m_currentInnerBatch.texture != null)
                    m_currentInnerBatch.Commit();

                m_currentInnerBatch = CreateSpritesBatch();
                m_currentInnerBatch.startInstance = m_spriteInstanceList.Count;
                m_currentInnerBatch.SetTexture(texture);
            }

            m_currentInnerBatch.AddSprite(sprite);
        }

        internal static void AddSingleSprite(string textureStr, Color color, Vector2 origin, Rectangle? sourceRect, RectangleF destinationRect)
        {
            Vector2 clipOffset;
            Vector2 clipScale;

            CalculateSpriteClipspace(destinationRect,
                MyRender.ViewportResolution,
                out clipOffset, out clipScale);

            var texture = MyTextureManager.GetTexture(textureStr);

            AddSingleSprite(texture, new MyVertexFormatSpritePositionTextureColor(
                new HalfVector4(clipOffset.X, clipOffset.Y, clipScale.X, clipScale.Y),
                new HalfVector4(0, 0, 1, 1),
                new Byte4(color.R, color.G, color.B, color.A)));
        }

        internal static void AddSingleSprite(MyTexture2D texture, Vector2 position, Rectangle? sourceRectangle, float scale, Color color)
        {
            Vector2 clipOffset;
            Vector2 clipScale;

            RectangleF destination;
            if (sourceRectangle != null)
            {
                destination = new RectangleF(position.X, position.Y, scale * sourceRectangle.Value.Width,
                    scale * sourceRectangle.Value.Height);
            }
            else
            {
                destination = new RectangleF(position.X, position.Y, scale, scale);
            }

            CalculateSpriteClipspace(destination,
                MyRender.ViewportResolution,
                out clipOffset, out clipScale);

            Vector2 texOffset = Vector2.Zero;
            Vector2 texScale = Vector2.One;

            if (sourceRectangle != null)
            {
                Vector2 textureSize = texture != null ? texture.Size : Vector2.Zero;

                Vector2 leftTop = new Vector2(sourceRectangle.Value.Left, sourceRectangle.Value.Top);
                Vector2 size = new Vector2(sourceRectangle.Value.Width, sourceRectangle.Value.Height);
                texOffset = leftTop / textureSize;
                texScale = size / textureSize;
            }

            AddSingleSprite(texture, new MyVertexFormatSpritePositionTextureColor(
                new HalfVector4(clipOffset.X, clipOffset.Y, clipScale.X, clipScale.Y),
                new HalfVector4(texOffset.X, texOffset.Y, texScale.X, texScale.Y),
                new Byte4(color.R, color.G, color.B, color.A)));
        }

        internal static MySpritesBatch CreateSpritesBatch()
        {
            var batch = new MySpritesBatch();
            batch.startInstance = m_spriteInstanceList.Count;
            return batch;
        }

        internal static void Draw()
        {
            // flush inner sprites batcher
            if (m_currentInnerBatch.texture != null)
                m_currentInnerBatch.Commit();
            m_currentInnerBatch = new MySpritesBatch();

            if (m_inputLayout == null)
            {
                var spritesInput = MyVertexInput.Empty()
                    .Append(MyVertexInputComponentType.CUSTOM_HALF4_0, 0, MyVertexInputComponentFreq.PER_INSTANCE)
                    .Append(MyVertexInputComponentType.CUSTOM_HALF4_1, 0, MyVertexInputComponentFreq.PER_INSTANCE)
                    .Append(MyVertexInputComponentType.COLOR4, 0, MyVertexInputComponentFreq.PER_INSTANCE);

                m_inputLayout = MyVertexInput.CreateLayout(spritesInput.Hash, MyShaderCache.CompileFromFile("sprite.hlsl", "vs", MyShaderProfile.VS_5_0).Bytecode);
            }

            //

            var context = MyRender.Context;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_spritesInstanceBuffer.Buffer, MyVertexFormatSpritePositionTextureColor.STRIDE, 0));
            context.InputAssembler.InputLayout = m_inputLayout;

            context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            context.VertexShader.Set(m_vertexShader.VertexShader);

            context.PixelShader.Set(m_pixelShader.PixelShader);
            context.PixelShader.SetSamplers(0, MyRender.StandardSamplers);

            context.OutputMerger.SetTargets(MyRender.Backbuffer.RenderTarget);
            context.OutputMerger.SetBlendState(MyRender.BlendStateGui);

            DataStream stream;
            context.MapSubresource(m_spritesInstanceBuffer.Buffer, MapMode.WriteDiscard, MapFlags.None, out stream);
            for (int i = 0; i < m_spriteInstanceList.Count; i++)
                stream.Write(m_spriteInstanceList[i]);
            context.UnmapSubresource(m_spritesInstanceBuffer.Buffer, 0);
            stream.Dispose();

            for (int b = 0; b < m_spriteBatches.Count; b++)
            {
                context.PixelShader.SetShaderResource(0, m_spriteBatches[b].texture);
                context.DrawInstanced(4, m_spriteBatches[b].instanceCount, 0, m_spriteBatches[b].startInstance);
            }

            m_spriteInstanceList.Clear();
            m_spriteBatches.Clear();
        }

        internal static void CalculateSpriteClipspace(RectangleF destination, Vector2 screenSize, out Vector2 clipOffset, out Vector2 clipScale)
        {
            Vector2 scale = destination.Size / screenSize;
            Vector2 translation = destination.Position / screenSize;

            clipScale = scale * 2;
            clipOffset = translation * 2 - 1;
            clipOffset.Y = -clipOffset.Y;
            clipOffset += new Vector2(0.5f, -0.5f) * clipScale;
        }

        internal static float DrawText(Vector2 screenCoord, StringBuilder text, VRageMath.Color color, float scale)
        {
            return MyRender.DebugFont.DrawString(
                screenCoord,
                color,
                text,
                scale);
        }

        internal static float DrawTextShadow(Vector2 screenCoord, StringBuilder text, VRageMath.Color color, float scale)
        {
            return MyRender.DebugFont.DrawString(
                screenCoord,
                color,
                text,
                scale);
        }
    }
}
