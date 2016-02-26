using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX.Direct3D9;
using VRageMath;
using System.Diagnostics;


namespace VRageRender.Graphics
{
    /// <summary>
    /// Renders a group of sprites.
    /// </summary>
    internal class SpriteBatch //: GraphicsResource 
    {
        private const int MaxBatchSize = 2048;
        private const int MinBatchSize = 128;
        private const int InitialQueueSize = 64;
        private const int VerticesPerSprite = 4;
        private const int IndicesPerSprite = 6;
        private const int MaxVertexCount = MaxBatchSize * VerticesPerSprite;
        private const int MaxIndexCount = MaxBatchSize * IndicesPerSprite;

        private static readonly Vector2[] CornerOffsets = { Vector2.Zero, Vector2.UnitX, Vector2.UnitY, Vector2.One };
        private static readonly short[] m_indices;
        
        private readonly BackToFrontComparer m_backToFrontComparer = new BackToFrontComparer();
        private readonly FrontToBackComparer m_frontToBackComparer = new FrontToBackComparer();
        private IndexBuffer m_indexBuffer; //short
        private readonly TextureComparer m_textureComparer = new TextureComparer();
        private ResourceContext m_VBResourceContext;
        private readonly Dictionary<Int64, TextureInfo> m_textureInfos = new Dictionary<Int64, TextureInfo>(128);

        private BlendState m_blendState;
        private SamplerState m_samplerState;
        private RasterizerState m_rasterizerState;
        private DepthStencilState m_depthStencilState;
 
        private Effect m_customEffect;
        private EffectHandle m_customEffectMatrixTransform;
        private EffectHandle m_customEffectSampler;
        private EffectHandle m_customEffectTexture;

        private bool m_isBeginCalled;
        
        private int[] m_sortIndices;
        private SpriteInfo[] m_sortedSprites;
        private SpriteInfo[] m_spriteQueue;
        private int m_spriteQueueCount;
        private SpriteSortMode m_spriteSortMode;
        private TextureInfo[] m_spriteTextures;

        private Matrix m_transformMatrix;

        private Device m_graphicsDevice;

        public readonly SpriteScissorStack ScissorStack = new SpriteScissorStack();

        static SpriteBatch()
        {
            m_indices = new short[MaxIndexCount];
            int k = 0;
            for (int i = 0; i < MaxIndexCount; k += VerticesPerSprite)
            {
                m_indices[i++] = (short)(k + 0);
                m_indices[i++] = (short)(k + 1);
                m_indices[i++] = (short)(k + 2);
                m_indices[i++] = (short)(k + 1);
                m_indices[i++] = (short)(k + 3);
                m_indices[i++] = (short)(k + 2);
            }            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteBatch" /> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        public SpriteBatch(Device graphicsDevice, string debugName) //: base(graphicsDevice, debugName)
        {
            //graphicsDevice.DefaultEffectPool.RegisterBytecode(effectBytecode);

            m_graphicsDevice = graphicsDevice;

            m_spriteQueue = new SpriteInfo[MaxBatchSize];
            m_spriteTextures = new TextureInfo[MaxBatchSize];

            // Creates the vertex buffer (shared by within a device context).
            //resourceContext = GraphicsDevice.GetOrCreateSharedData(SharedDataType.PerContext, "SpriteBatch.VertexBuffer", () => new ResourceContext(GraphicsDevice));
            m_VBResourceContext = new ResourceContext(graphicsDevice, debugName);

            // Creates the index buffer (shared within a Direct3D11 Device)
            //indexBuffer =  GraphicsDevice.GetOrCreateSharedData(SharedDataType.PerDevice, "SpriteBatch.IndexBuffer", () => Buffer.Index.New(GraphicsDevice, indices));
            m_indexBuffer = new IndexBuffer(graphicsDevice, m_indices.Length * 2, Usage.WriteOnly, Pool.Default, true);
            m_indexBuffer.DebugName = "SpriteBatchIB(" + debugName + ")";
            m_indexBuffer.SetData(m_indices);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state. Other states are sets to default (DepthStencilState.None, SamplerState.LinearClamp, RasterizerState.CullCounterClockwise). If you pass a null blend state, the default is BlendState.AlphaBlend.
        /// </summary>
        /// <param name="sortMode">Sprite drawing order.</param>
        /// <param name="blendState">Blending options.</param>
        public void Begin(SpriteSortMode sortMode, BlendState blendState)
        {
            Begin(sortMode, blendState, null, null, null, null, Matrix.Identity);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil and rasterizer state objects. Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise).
        /// </summary>
        /// <param name="sortMode">Sprite drawing order.</param>
        /// <param name="blendState">Blending options.</param>
        /// <param name="samplerState">Texture sampling options.</param>
        /// <param name="depthStencilState">Depth and stencil options.</param>
        /// <param name="rasterizerState">Rasterization options.</param>
        public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
        {
            Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, null, Matrix.Identity);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil, rasterizer state objects, plus a custom effect and a 2D transformation matrix. Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.None, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp). Passing a null effect selects the default SpriteBatch Class shader. 
        /// </summary>
        /// <param name="sortMode">Sprite drawing order.</param>
        /// <param name="blendState">Blending options.</param>
        /// <param name="samplerState">Texture sampling options.</param>
        /// <param name="depthStencilState">Depth and stencil options.</param>
        /// <param name="rasterizerState">Rasterization options.</param>
        /// <param name="effect">Effect state options.</param>
        /// <param name="transformMatrix">Transformation matrix for scale, rotate, translate options.</param>
        public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
        {
            if (m_isBeginCalled)
            {
                throw new InvalidOperationException("End must be called before begin");
            }

            this.m_blendState = blendState;
            this.m_samplerState = samplerState;
            this.m_depthStencilState = depthStencilState;
            this.m_rasterizerState = rasterizerState;

            this.m_spriteSortMode = sortMode;
            this.m_customEffect = effect;
            this.m_transformMatrix = transformMatrix;

            // If custom effect is not null, get all its potential default parameters
            if (m_customEffect != null)
            {
                m_customEffectMatrixTransform = m_customEffect.GetParameter(null, "MatrixTransform");
                m_customEffectTexture = m_customEffect.GetParameter(null, "Texture");
                m_customEffectSampler = m_customEffect.GetParameter(null, "TextureSampler");
            }

            // Immediate mode, then prepare for rendering here instead of End()
            if (sortMode == SpriteSortMode.Immediate)
            {
                if (m_VBResourceContext.IsInImmediateMode)
                {
                    throw new InvalidOperationException("Only one SpriteBatch at a time can use SpriteSortMode.Immediate");
                }

                PrepareForRendering();

                m_VBResourceContext.IsInImmediateMode = true;
            }

            // Sets to true isBeginCalled
            m_isBeginCalled = true;
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, color, rotation, origin, scale, effects, and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="sourceRectangle">A rectangle that specifies (in texels) the source texels from a texture. Use null to draw the entire texture. </param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin; the default is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color, Vector2 rightVector, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            var destination = new RectangleF(position.X, position.Y, scale, scale);
            DrawSprite(texture, null, ref destination, true, sourceRectangle, color, rightVector, ref origin, effects, layerDepth);
        }

        /// <summary>
        /// Flushes the sprite batch and restores the device state to how it was before Begin was called. 
        /// </summary>
        public void End()
        {
            if (!m_isBeginCalled)
            {
                throw new InvalidOperationException("Begin must be called before End");
            }

            if (m_spriteSortMode == SpriteSortMode.Immediate)
            {
                m_VBResourceContext.IsInImmediateMode = false;
            }
            else if (m_spriteQueueCount > 0)
            {
                  // Draw the queued sprites now.
                if (m_VBResourceContext.IsInImmediateMode)
                {
                    throw new InvalidOperationException("Cannot end one SpriteBatch while another is using SpriteSortMode.Immediate");
                }

                // If not immediate, then setup and render all sprites
                PrepareForRendering();
                FlushBatch();
            }

            // Clear the custom effect so that it won't be used next Begin/End
            if (m_customEffect != null)
            {
                m_customEffectMatrixTransform = null;
                m_customEffectTexture = null;
                m_customEffectSampler = null;
                m_customEffect = null;
            }

            // Clear stored texture infos
            m_textureInfos.Clear();

            // We are with begin pair
            m_isBeginCalled = false;
        }

        private void FlushBatch()
        {
            SpriteInfo[] spriteQueueForBatch;

            // If Deferred, then sprites are displayed in the same order they arrived
            if (m_spriteSortMode == SpriteSortMode.Deferred)
            {
                spriteQueueForBatch = m_spriteQueue;
            }
            else
            {
                // Else Sort all sprites according to their sprite order mode.
                SortSprites();
                spriteQueueForBatch = m_sortedSprites;
            }

            // Iterate on all sprites and group batch per texture.
            int offset = 0;
            var previousTexture = default(TextureInfo);
            for (int i = 0; i < m_spriteQueueCount; i++)
            {
                TextureInfo texture;

                if (m_spriteSortMode == SpriteSortMode.Deferred)
                {
                    texture = m_spriteTextures[i];
                }
                else
                {
                    // Copy ordered sprites to the queue to batch
                    int index = m_sortIndices[i];
                    spriteQueueForBatch[i] = m_spriteQueue[index];

                    // Get the texture indirectly
                    texture = m_spriteTextures[index];
                }

                if (texture.Texture != previousTexture.Texture)
                {
                    if (i > offset)
                    {
                        DrawBatchPerTexture(ref previousTexture, spriteQueueForBatch, offset, i - offset);
                    }

                    offset = i;
                    previousTexture = texture;
                }
            }

            // Draw the last batch
            DrawBatchPerTexture(ref previousTexture, spriteQueueForBatch, offset, m_spriteQueueCount - offset);

            // Reset the queue.
            Array.Clear(m_spriteTextures, 0, m_spriteQueueCount);
            m_spriteQueueCount = 0;

            // When sorting is disabled, we persist mSortedSprites data from one batch to the next, to avoid
            // uneccessary work in GrowSortedSprites. But we never reuse these when sorting, because re-sorting
            // previously sorted items gives unstable ordering if some sprites have identical sort keys.
            if (m_spriteSortMode != SpriteSortMode.Deferred)
            {
                Array.Clear(m_sortedSprites, 0, m_sortedSprites.Length);
            }
        }

        private void SortSprites()
        {
            IComparer<int> comparer;

            switch (m_spriteSortMode)
            {
                case SpriteSortMode.Texture:
                    m_textureComparer.SpriteTextures = m_spriteTextures;
                    comparer = m_textureComparer;
                    break;

                case SpriteSortMode.BackToFront:
                    m_backToFrontComparer.SpriteQueue = m_spriteQueue;
                    comparer = m_backToFrontComparer;
                    break;

                case SpriteSortMode.FrontToBack:
                    m_frontToBackComparer.SpriteQueue = m_spriteQueue;
                    comparer = m_frontToBackComparer;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if ((m_sortIndices == null) || (m_sortIndices.Length < m_spriteQueueCount))
            {
                m_sortIndices = new int[m_spriteQueueCount];
                m_sortedSprites = new SpriteInfo[m_spriteQueueCount];
            }

            // Reset all indices to the original order
            for (int i = 0; i < m_spriteQueueCount; i++)
            {
                m_sortIndices[i] = i;
            }

            Array.Sort(m_sortIndices, 0, m_spriteQueueCount, comparer);
        }

        internal unsafe void DrawSprite(BaseTexture texture, CubeMapFace? face, ref RectangleF destination, bool scaleDestination, Rectangle? sourceRectangle, Color color, Vector2 rightVector, ref Vector2 origin, SpriteEffects effects, float depth)
        {
            // Check that texture is not null
            if (texture == null || texture.NativePointer == IntPtr.Zero)
            {
                Debug.Fail("Texture cannot be null.");
                return;
            }

            // Make sure that Begin was called
            if (!m_isBeginCalled)
            {
                throw new InvalidOperationException("Begin must be called before draw");
            }

            // Resize the buffer of SpriteInfo
            if (m_spriteQueueCount >= m_spriteQueue.Length)
            {
                Array.Resize(ref m_spriteQueue, m_spriteQueue.Length*2);
            }

            // Gets the resource information from the view (width, height).
            // Cache the result in order to avoid this request if the texture is reused 
            // inside a same Begin/End block.
            TextureInfo textureInfo;
            if (!m_textureInfos.TryGetValue(texture.NativePointer.ToInt64() + face.GetHashCode(), out textureInfo))
            {
                textureInfo.Texture = texture;
                textureInfo.Face = face;

                SurfaceDescription description2D;
                if (face.HasValue)
                {
                    Surface cubeSurface = ((CubeTexture)texture).GetCubeMapSurface(face.Value, 0);
                    description2D = cubeSurface.Description;
                    cubeSurface.Dispose();
                }
                else
                {
                    description2D = ((Texture)texture).GetLevelDescription(0);
                }

                textureInfo.Width = description2D.Width;
                textureInfo.Height = description2D.Height;

                m_textureInfos.Add(texture.NativePointer.ToInt64() + face.GetHashCode(), textureInfo);
            }

            // Put values in next SpriteInfo
            fixed (SpriteInfo* spriteInfo = &(m_spriteQueue[m_spriteQueueCount]))
            {
                float width;
                float height;

                // If the source rectangle has a value, then use it.
                if (sourceRectangle.HasValue)
                {
                    Rectangle rectangle = sourceRectangle.Value;
                    spriteInfo->Source.X = rectangle.X;
                    spriteInfo->Source.Y = rectangle.Y;
                    width = rectangle.Width;
                    height = rectangle.Height;
                }
                else
                {
                    // Else, use directly the size of the texture
                    spriteInfo->Source.X = 0.0f;
                    spriteInfo->Source.Y = 0.0f;
                    width = textureInfo.Width;
                    height = textureInfo.Height;
                }

                // Sets the width and height
                spriteInfo->Source.Width = width;
                spriteInfo->Source.Height = height;

                // Scale the destination box
                if (scaleDestination)
                {
                    destination.Width *= width;
                    destination.Height *= height;
                }

                // Sets the destination
                spriteInfo->Destination = destination;

                // Copy all other values.
                spriteInfo->Origin.X = origin.X;
                spriteInfo->Origin.Y = origin.Y;
                spriteInfo->RightVector = rightVector;
                spriteInfo->Depth = depth;
                spriteInfo->SpriteEffects = effects;
                spriteInfo->Color = color;

                if (spriteInfo->RightVector == Vector2.UnitX)
                { // Rotated sprites are not supported at the moment.
                    ScissorStack.Cut(ref spriteInfo->Destination,
                                     ref spriteInfo->Source);
                }

                if (spriteInfo->Destination.Size.X == 0 ||
                    spriteInfo->Destination.Size.Y == 0)
                    return; // Discard sprite as there is nothing left of it.
            }

            // If we are in immediate mode, render the sprite directly
            if (m_spriteSortMode == SpriteSortMode.Immediate)
            {
                DrawBatchPerTexture(ref textureInfo, m_spriteQueue, 0, 1);
            }
            else
            {
                if (m_spriteTextures.Length < m_spriteQueue.Length)
                {
                    Array.Resize(ref m_spriteTextures, m_spriteQueue.Length);
                }
                m_spriteTextures[m_spriteQueueCount] = textureInfo;
                m_spriteQueueCount++;
            }
        }

        private void DrawBatchPerTexture(ref TextureInfo texture, SpriteInfo[] sprites, int offset, int count)
        {
            if (m_customEffect != null)
            {
                var currentTechnique = m_customEffect.Technique;

                int passCount = m_customEffect.GetTechniqueDescription(currentTechnique).Passes;
                for (int i = 0; i < passCount; i++)
                {
                    // Sets the texture on the custom effect if the parameter exist
                    if (m_customEffectTexture != null)
                    {
                        m_customEffect.SetTexture(m_customEffectTexture, texture.Texture);
                    }


                    m_customEffect.Begin();
                    // Apply the current pass
                    m_customEffect.BeginPass(i);

                    // Draw the batch of sprites
                    DrawBatchPerTextureAndPass(ref texture, sprites, offset, count);

                    m_customEffect.EndPass();
                    m_customEffect.End();
                }
            }
            else
            {
                var spriteEffect = MyRender.GetEffect(MyEffects.SpriteBatch) as Effects.MyEffectSpriteBatchShader;

                if (texture.Face.HasValue)
                {
                    spriteEffect.SetCubeTexture(texture.Texture as CubeTexture);

                    switch (texture.Face.Value)
                    {
                        case CubeMapFace.PositiveX:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube0);
                            break;
                        case CubeMapFace.NegativeX:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube1);
                            break;
                        case CubeMapFace.PositiveY:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube2);
                            break;
                        case CubeMapFace.NegativeY:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube3);
                            break;
                        case CubeMapFace.PositiveZ:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube4);
                            break;
                        case CubeMapFace.NegativeZ:
                            spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.SpriteBatchCube5);
                            break;
                    }
                }
                else
                {
                    spriteEffect.SetTexture(texture.Texture as Texture);
                    spriteEffect.SetTechnique(Effects.MyEffectSpriteBatchShader.Technique.Sprite);
                }

                spriteEffect.Begin();

                DrawBatchPerTextureAndPass(ref texture, sprites, offset, count);

                spriteEffect.End();
            }
        }

        private unsafe void DrawBatchPerTextureAndPass(ref TextureInfo texture, SpriteInfo[] sprites, int offset, int count)
        {
            float deltaX = 1f/(texture.Width);
            float deltaY = 1f/(texture.Height);
            while (count > 0)
            {
                // How many sprites do we want to draw?
                int batchSize = count;

                // How many sprites does the D3D vertex buffer have room for?
                int remainingSpace = MaxBatchSize - m_VBResourceContext.VertexBufferPosition;
                if (batchSize > remainingSpace)
                {
                    if (remainingSpace < MinBatchSize)
                    {
                        m_VBResourceContext.VertexBufferPosition = 0;
                        batchSize = (count < MaxBatchSize) ? count : MaxBatchSize;
                    }
                    else
                    {
                        batchSize = remainingSpace;
                    }
                }

                // Sets the data directly to the buffer in memory
                int offsetInBytes = m_VBResourceContext.VertexBufferPosition * VerticesPerSprite * MyVertexFormatPositionTextureColor.Stride;

                var noOverwrite = m_VBResourceContext.VertexBufferPosition == 0 ? LockFlags.Discard : LockFlags.NoOverwrite;

                var ptr = m_VBResourceContext.VertexBuffer.LockToPointer(offsetInBytes, batchSize * VerticesPerSprite * MyVertexFormatPositionTextureColor.Stride, noOverwrite);
                   
                var vertexPtr = (MyVertexFormatPositionTextureColor*)ptr;
                    
                for (int i = 0; i < batchSize; i++)
                {
                    UpdateVertexFromSpriteInfo(ref sprites[offset + i], ref vertexPtr, deltaX, deltaY);
                } 

                m_VBResourceContext.VertexBuffer.Unlock();

                // Draw from the specified index
                int startIndex = m_VBResourceContext.VertexBufferPosition * IndicesPerSprite;
                int indexCount = batchSize * IndicesPerSprite;
                                  /*
                BlendState.Opaque.Apply();
                DepthStencilState.None.Apply();
                                */

                m_graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, MaxVertexCount, startIndex, indexCount / 3);

                // Update position, offset and remaining count
                m_VBResourceContext.VertexBufferPosition += batchSize;
                offset += batchSize;
                count -= batchSize;
            }
        }

        private unsafe void UpdateVertexFromSpriteInfo(ref SpriteInfo spriteInfo, ref MyVertexFormatPositionTextureColor* vertex, float deltaX, float deltaY)
        {
            //var rotation = spriteInfo.Rotation != 0f ? new Vector2((float) Math.Cos(spriteInfo.Rotation), (float) Math.Sin(spriteInfo.Rotation)) : Vector2.UnitX;
            var rotation = spriteInfo.RightVector;

            // Origin scale down to the size of the source texture 
            var origin = spriteInfo.Origin;
            origin.X /= spriteInfo.Source.Width == 0f ? float.Epsilon : spriteInfo.Source.Width;
            origin.Y /= spriteInfo.Source.Height == 0f ? float.Epsilon : spriteInfo.Source.Height;

            for (int j = 0; j < 4; j++)
            {
                // Gets the corner and take into account the Flip mode.
                var corner = CornerOffsets[j];
                // Calculate position on destination
                var position = new Vector2((corner.X - origin.X) * spriteInfo.Destination.Width, (corner.Y - origin.Y) * spriteInfo.Destination.Height);

                // Apply rotation and destination offset
                vertex->Position.X = spriteInfo.Destination.X + (position.X * rotation.X) - (position.Y * rotation.Y);
                vertex->Position.Y = spriteInfo.Destination.Y + (position.X * rotation.Y) + (position.Y * rotation.X);
                vertex->Position.Z = spriteInfo.Depth;
                vertex->Color = spriteInfo.Color.ToVector4();

                corner = CornerOffsets[j ^ (int)spriteInfo.SpriteEffects];
                vertex->TexCoord.X = (spriteInfo.Source.X + corner.X * spriteInfo.Source.Width) * deltaX;
                vertex->TexCoord.Y = (spriteInfo.Source.Y + corner.Y * spriteInfo.Source.Height) * deltaY;

                vertex++;
            }
        }

        private void PrepareForRendering()
        {
            // Setup states (Blend, DepthStencil, Rasterizer)
            if (m_blendState != null)
                m_blendState.Apply();

            if (m_rasterizerState != null)
                m_rasterizerState.Apply();

            if (m_depthStencilState != null)
                m_depthStencilState.Apply();

            if (m_samplerState != null)
                m_samplerState.Apply();

            // Build ortho-projection matrix
            SharpDX.ViewportF viewport = m_graphicsDevice.Viewport;
            float xRatio = (viewport.Width > 0) ? (1f/(viewport.Width)) : 0f;
            float yRatio = (viewport.Height > 0) ? (-1f/(viewport.Height)) : 0f;
            var matrix = new Matrix { M11 = xRatio * 2f, M22 = yRatio * 2f, M33 = 1f, M44 = 1f, M41 = -1f, M42 = 1f };

            Matrix finalMatrix;
            Matrix.Multiply(ref m_transformMatrix, ref matrix, out finalMatrix);

            // Use LinearClamp for sampler state
            //var localSamplerState = samplerState ?? GraphicsDevice.SamplerStates.LinearClamp;

            // Setup effect states and parameters: SamplerState and MatrixTransform
            // Sets the sampler state
            if (m_customEffect != null)
            {
                if (m_customEffect.Technique == null)
                    throw new InvalidOperationException("CurrentTechnique is not set on custom effect");

                //if (customEffectSampler != null)
                  //  customEffectSampler.SetResource(localSamplerState);

                if (m_customEffectMatrixTransform != null)
                    m_customEffect.SetValue(m_customEffectMatrixTransform, finalMatrix);
            }
            else
            {
                var spriteEffect = MyRender.GetEffect(MyEffects.SpriteBatch) as Effects.MyEffectSpriteBatchShader;
                spriteEffect.SetMatrixTransform(ref finalMatrix);
            }

            // Set VertexInputLayout
            m_graphicsDevice.VertexDeclaration = MyVertexFormatPositionTextureColor.VertexDeclaration;

            // VertexBuffer
            m_graphicsDevice.SetStreamSource(0, m_VBResourceContext.VertexBuffer, 0, MyVertexFormatPositionTextureColor.Stride);

            // Index buffer
            m_graphicsDevice.Indices = m_indexBuffer;

            // If this is a deferred D3D context, reset position so the first Map call will use D3D11_MAP_WRITE_DISCARD.
           /* if (GraphicsDevice.IsDeferred)
            {
                VBResourceContext.VertexBufferPosition = 0;
            } */
        }

        public void Dispose()
        {
            m_indexBuffer.Dispose();
            m_indexBuffer = null;

            m_VBResourceContext.Dispose();
            m_VBResourceContext = null;

            //base.Dispose(disposeManagedResources);
        }

        #region Nested type: BackToFrontComparer

        private class BackToFrontComparer : IComparer<int>
        {
            public SpriteInfo[] SpriteQueue;

            #region IComparer<int> Members

            public int Compare(int left, int right)
            {
                return SpriteQueue[right].Depth.CompareTo(SpriteQueue[left].Depth);
            }

            #endregion
        }

        #endregion

        #region Nested type: FrontToBackComparer

        private class FrontToBackComparer : IComparer<int>
        {
            public SpriteInfo[] SpriteQueue;

            #region IComparer<int> Members

            public int Compare(int left, int right)
            {
                return SpriteQueue[left].Depth.CompareTo(SpriteQueue[right].Depth);
            }

            #endregion
        }

        #endregion

        #region Nested type: TextureComparer

        private class TextureComparer : IComparer<int>
        {
            public TextureInfo[] SpriteTextures;

            #region IComparer<int> Members

            public int Compare(int left, int right)
            {
                return SpriteTextures[left].Texture.NativePointer.ToInt64().CompareTo(SpriteTextures[right].Texture.NativePointer.ToInt64());
            }

            #endregion
        }

        #endregion

        #region Nested type: SpriteInfo

        [StructLayout(LayoutKind.Sequential)]
        private struct SpriteInfo
        {
            public RectangleF Source;
            public RectangleF Destination;
            public Vector2 Origin;
            public Vector2 RightVector;
            public float Depth;
            public SpriteEffects SpriteEffects;
            public Color Color;
        }

        #endregion


        /// <summary>
        /// Use a ResourceContext per GraphicsDevice (DeviceContext)
        /// </summary>
        private class ResourceContext : SharpDX.Component
        {
            public readonly VertexBuffer VertexBuffer;

            public int VertexBufferPosition;

            public bool IsInImmediateMode;

            public ResourceContext(Device device, string debugName)
            {
                VertexBuffer = new VertexBuffer(device, MyVertexFormatPositionTextureColor.Stride * MaxVertexCount, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
                VertexBuffer.DebugName = "SpriteBatchVB(" + debugName + ")";
            }

            protected override void Dispose(bool disposeManagedResources)
            {
                VertexBuffer.Dispose();

                base.Dispose(disposeManagedResources);
            }
        }

        /// <summary>
        /// Internal structure used to store texture information.
        /// </summary>
        private struct TextureInfo
        {
            public BaseTexture Texture;
            public CubeMapFace? Face;

            public int Width;

            public int Height;
        }
    }
}