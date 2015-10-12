using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using Buffer = SharpDX.Direct3D11.Buffer;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Color = VRageMath.Color;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRage;
using VRage.Utils;
using System.Diagnostics;

namespace VRageRender
{
    // not thread safe at all
    struct MySpritesBatch
    {
        internal int Count;
        internal int Start;
        internal RectangleF? ScissorRectangle;

        internal ShaderResourceView Texture;

        internal void AddSprite(Vector2 clipOffset, Vector2 clipScale, Vector2 texOffset, Vector2 texScale, 
            Vector2 origin, Vector2 tangent, Byte4 color)
        {
            MySpritesRenderer.StackTop().m_instances.Add(new MyVertexFormatSpritePositionTextureRotationColor(
                new HalfVector4(clipOffset.X, clipOffset.Y, clipScale.X, clipScale.Y),
                new HalfVector4(texOffset.X, texOffset.Y, texScale.X, texScale.Y),
                new HalfVector4(origin.X, origin.Y, tangent.X, tangent.Y),
                color));
        }

        internal void AddSprite(MyVertexFormatSpritePositionTextureRotationColor sprite)
        {
            MySpritesRenderer.StackTop().m_instances.Add(sprite);
            Count ++;
        }

        internal void Commit()
        {
            MySpritesRenderer.Commit(this);
        }
    };

    class MySpritesContext
    {
        internal Vector2? m_resolution;
        internal SpriteScissorStack m_scissorStack = new SpriteScissorStack();
        internal List<MyVertexFormatSpritePositionTextureRotationColor> m_instances = new List<MyVertexFormatSpritePositionTextureRotationColor>();
        internal List<MySpritesBatch> m_batches = new List<MySpritesBatch>();
        internal MySpritesBatch m_internalBatch;
    }

    class MySpritesRenderer : MyImmediateRC
    {
        static List<MySpritesContext> m_contextsStack = new List<MySpritesContext>();
        static int m_currentStackTop = 0;

        //internal static SpriteScissorStack m_scissorStack = new SpriteScissorStack();
        //internal static List<MyVertexFormatSpritePositionTextureRotationColor> m_instances = new List<MyVertexFormatSpritePositionTextureRotationColor>();
        //static List<MySpritesBatch> m_batches = new List<MySpritesBatch>();
        //static MySpritesBatch m_internalBatch;

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static InputLayoutId m_inputLayout = InputLayoutId.NULL;

        static int m_currentBufferSize;
        static VertexBufferId m_VB;

        internal unsafe static void Init()
        {
            m_vs = MyShaders.CreateVs("sprite.hlsl", "vs");
            m_ps = MyShaders.CreatePs("sprite.hlsl", "ps");

            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.CUSTOM_HALF4_0, MyVertexInputComponentFreq.PER_INSTANCE),
                new MyVertexInputComponent(MyVertexInputComponentType.CUSTOM_HALF4_1, MyVertexInputComponentFreq.PER_INSTANCE),
                new MyVertexInputComponent(MyVertexInputComponentType.CUSTOM_HALF4_2, MyVertexInputComponentFreq.PER_INSTANCE),
                new MyVertexInputComponent(MyVertexInputComponentType.COLOR4, MyVertexInputComponentFreq.PER_INSTANCE)
                ));

            m_currentBufferSize = 100000;
            m_VB = MyHwBuffers.CreateVertexBuffer(m_currentBufferSize, sizeof(MyVertexFormatSpritePositionTextureRotationColor), BindFlags.VertexBuffer, ResourceUsage.Dynamic);

            m_contextsStack.Add(new MySpritesContext());
        }

        internal static MySpritesContext StackTop()
        {
            return m_contextsStack[m_currentStackTop];
        }

        internal static void PushState(Vector2 ? targetResolution = null)
        {
            FlushInternalBatch();
            ++m_currentStackTop;
            if(m_contextsStack.Count == m_currentStackTop)
            {
                m_contextsStack.Add(new MySpritesContext());
            }
            StackTop().m_resolution = targetResolution;
        }

        internal static void PopState()
        {
            Debug.Assert(m_currentStackTop > 0);
            StackTop().m_instances.Clear();
            StackTop().m_batches.Clear();
            --m_currentStackTop;
        }

        static unsafe void CheckBufferSize(int requiredSize)
        {
            if (m_currentBufferSize < requiredSize)
            {
                m_currentBufferSize = (int)(requiredSize * 1.33f);
                MyHwBuffers.ResizeVertexBuffer(m_VB, m_currentBufferSize);
            }
        }

        static void FlushInternalBatch()
        {
            bool someStuffPending = StackTop().m_internalBatch.Texture != null && StackTop().m_internalBatch.Count > 0;
            if (someStuffPending)
            {
                StackTop().m_internalBatch.Commit();
            }

            StackTop().m_internalBatch.Start = StackTop().m_instances.Count;
            StackTop().m_internalBatch.Count = 0;
        }

        static MySpritesBatch CreateBatch()
        {
            return new MySpritesBatch { Start = StackTop().m_instances.Count };
        }

        static void AddSingleSprite(ShaderResourceView textureSrv, MyVertexFormatSpritePositionTextureRotationColor sprite)
        {
            if (StackTop().m_internalBatch.Texture != textureSrv && StackTop().m_internalBatch.Count > 0)
            {
                FlushInternalBatch();
            }
            StackTop().m_internalBatch.Texture = textureSrv;

            StackTop().m_internalBatch.AddSprite(sprite);
        }

        internal static void AddSingleSprite(TexId texId, Color color, Vector2 origin, Vector2 tangent, Rectangle? sourceRect, RectangleF destinationRect)
        {
            AddSingleSprite(MyTextures.Views[texId.Index], MyTextures.GetSize(texId), color, origin, tangent, sourceRect, destinationRect);
        }

        internal static void AddSingleSprite(ShaderResourceView view, Vector2 textureSize, Color color, Vector2 origin, Vector2 tangent, Rectangle? sourceRect, RectangleF destinationRect)
        {
            if (StackTop().m_internalBatch.ScissorRectangle.HasValue)
            {
                RectangleF intersection;
                var scissor = StackTop().m_internalBatch.ScissorRectangle.Value;
                RectangleF.Intersect(ref scissor, ref destinationRect, out intersection);
                if (intersection.Size.X * intersection.Size.Y == 0)
                {
                    return;
                }
            }

            Vector2 clipOffset;

            Vector2 targetResolution = StackTop().m_resolution ?? MyRender11.ResolutionF;
            clipOffset = destinationRect.Position / targetResolution * 2 - 1;
            clipOffset.Y = -clipOffset.Y;

            Vector2 texOffset = Vector2.Zero;
            Vector2 texScale = Vector2.One;

            Vector2 originOffset;
            if (sourceRect != null)
            {
                Vector2 leftTop = new Vector2(sourceRect.Value.Left, sourceRect.Value.Top);
                Vector2 size = new Vector2(sourceRect.Value.Width, sourceRect.Value.Height);
                texOffset = leftTop / textureSize;
                texScale = size / textureSize;

                originOffset = origin / new Vector2(sourceRect.Value.Width, sourceRect.Value.Height) - 0.5f;
                originOffset.Y *= -1;
            }
            else
            {
                originOffset = origin / textureSize - 0.5f;
                originOffset.Y *= -1;
            }

            AddSingleSprite(view, new MyVertexFormatSpritePositionTextureRotationColor(
                new HalfVector4(clipOffset.X, clipOffset.Y, destinationRect.Width, destinationRect.Height),
                new HalfVector4(texOffset.X, texOffset.Y, texScale.X, texScale.Y),
                new HalfVector4(originOffset.X, originOffset.Y, tangent.X, tangent.Y),
                new Byte4(color.R, color.G, color.B, color.A)));
        }


        internal static void ScissorStackPush(Rectangle rect)
        {
            StackTop().m_scissorStack.Push(rect);

            FlushInternalBatch();
            StackTop().m_internalBatch.ScissorRectangle = StackTop().m_scissorStack.Peek();
        }

        internal static void ScissorStackPop()
        {
            StackTop().m_scissorStack.Pop();

            FlushInternalBatch();
            StackTop().m_internalBatch.ScissorRectangle = StackTop().m_scissorStack.Peek();
        }

        internal static void Commit(MySpritesBatch batch)
        {
            batch.Count = StackTop().m_instances.Count - batch.Start;
            StackTop().m_batches.Add(batch);
        }

        // viewport, render target
        internal static void Draw(RenderTargetView rtv, MyViewport viewport)
        {
            if (StackTop().m_internalBatch.Texture != null && StackTop().m_internalBatch.Count > 0)
                StackTop().m_internalBatch.Commit();
            StackTop().m_internalBatch = new MySpritesBatch();

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            RC.SetIL(m_inputLayout);
            //RC.SetupScreenViewport();
            RC.Context.Rasterizer.SetViewport(viewport.OffsetX, viewport.OffsetY, viewport.Width, viewport.Height);

            RC.SetVS(m_vs);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.GetObjectCB(64));
            RC.SetPS(m_ps);
            RC.Context.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);

            //RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, MyRender11.Backbuffer);
            // to reset state
            RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, null);
            RC.Context.OutputMerger.SetRenderTargets(rtv);
            
            RC.SetBS(MyRender11.BlendGui);

            CheckBufferSize(StackTop().m_instances.Count);
            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);

            var mapping = MyMapping.MapDiscard(m_VB.Buffer);
            for (int i = 0; i < StackTop().m_instances.Count; i++)
            {
                mapping.stream.Write(StackTop().m_instances[i]);
            }
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(64));
            mapping.stream.Write(new Vector2(viewport.Width, viewport.Height));
            mapping.Unmap();

            foreach (var batch in StackTop().m_batches)
            {
                if(batch.ScissorRectangle.HasValue)
                {
                    RC.SetRS(MyRender11.m_scissorTestRasterizerState);

                    var scissor = batch.ScissorRectangle.Value;
                    RC.Context.Rasterizer.SetScissorRectangle((int)scissor.X, (int)scissor.Y, (int)(scissor.X + scissor.Width), (int)(scissor.Y + scissor.Height));
                }
                else
                {
                    RC.SetRS(MyRender11.m_nocullRasterizerState);
                }

                RC.BindRawSRV(0, batch.Texture);
                RC.Context.DrawInstanced(4, batch.Count, 0, batch.Start);
            }

            RC.SetBS(null);
            RC.SetRS(null);

            StackTop().m_instances.Clear();
            StackTop().m_batches.Clear();
        }

        internal static void Clear()
        {
            StackTop().m_instances.Clear();
            StackTop().m_batches.Clear();
        }

#region Helpers

        internal static void CalculateSpriteClipspace(RectangleF destination, Vector2 screenSize, out Vector2 clipOffset, out Vector2 clipScale)
        {
            Vector2 scale = destination.Size / screenSize;
            Vector2 translation = destination.Position / screenSize;

            clipScale = scale * 2;
            clipOffset = translation * 2 - 1;
            clipOffset.Y = -clipOffset.Y;
            //clipOffset += new Vector2(0.5f, -0.5f) * clipScale;
        }

        internal static Vector2 MeasureText(StringBuilder text, float scale)
        {
            return MyRender11.DebugFont.MeasureString(text, scale);
        }

        internal static float DrawText(Vector2 screenCoord, StringBuilder text, VRageMath.Color color, float scale, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            var font = MyRender11.DebugFont;

            return font.DrawString(
                MyUtils.GetCoordAligned(screenCoord, font.MeasureString(text, scale), align),
                color,
                text,
                scale);
        }

        internal static float DrawTextShadow(Vector2 screenCoord, StringBuilder text, VRageMath.Color color, float scale)
        {
            return MyRender11.DebugFont.DrawString(
                screenCoord,
                color,
                text,
                scale);
        }

#endregion
    }
}
