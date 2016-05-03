using System;
using System.Collections.Generic;
using VRage.Utils;

using SharpDX.Direct3D9;


//  This class loads voxel maps surrounding our sector, draws them to one large render target (texture atlas), create static
//  vertex buffer and then draws them every frame at background.
//  These aren't real impostors because we don't redraw them when camera moves. It's just a bunch of static billboards in the background.
//  Impostors must write to depth buffer, so then SunGlare and SunWind will work. In fact, impostors are objects too.

namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Graphics;
    using VRageRender.Utils;
    using VRageRender.Effects;
    using VRageRender.Textures;
    using VRageMath;

    class MyVoxelMapImpostors
    {
        class MyVoxelMapImpostor : IComparable
        {
            public Vector3 Position;
            public float Radius;
            public float Angle;
            float m_distance;

            public MyVoxelMapImpostor(Vector3 position, float radius, float angle)
            {
                Position = position;
                Radius = radius;
                Angle = angle;
                m_distance = position.Length();
            }

            //  For sorting impostors back-to-front (so bigger distance is first in the list)
            public int CompareTo(object compareToObject)
            {
                MyVoxelMapImpostor compareToImpostor = (MyVoxelMapImpostor)compareToObject;
                return compareToImpostor.m_distance.CompareTo(this.m_distance);
            }
        }

        class MyVoxelMapImpostorGroup
        {
            public MyImpostorProperties ImpostorProperties;

            //Texture2D m_texture;
            VertexBuffer m_vertexBuffer;
            int m_trianglesCount;
            MyVertexFormatPositionTextureColor[] m_vertices;
            Vector4 m_animationTime = Vector4.Zero;

            public void LoadContent()
            {
                CreateVertices(CreateFakeImpostors());
            }

            public void UnloadContent()
            {
                if (m_vertexBuffer != null)
                {
                    m_vertexBuffer.Dispose();
                    m_vertexBuffer = null;
                }
            }

            //  I used this method only when we didn't have real persistant sectors... for fly-through animations
            List<MyVoxelMapImpostor> CreateFakeImpostors()
            {
                List<MyVoxelMapImpostor> ret = new List<MyVoxelMapImpostor>(ImpostorProperties.ImpostorsCount);

                for (int i = 0; i < ImpostorProperties.ImpostorsCount; i++)
                {
                    Vector3 sectorCenter = Vector3.Zero;

                    float randomDistance = MyUtils.GetRandomFloat(ImpostorProperties.MinDistance, ImpostorProperties.MaxDistance);
                    Vector3 randomPositionWithinSector = MyUtils.GetRandomVector3Normalized() * randomDistance;

                    float radius = MyUtils.GetRandomFloat(ImpostorProperties.MinRadius, ImpostorProperties.MaxRadius);

                    Vector3 position = sectorCenter + randomPositionWithinSector;

                    float angle = MyUtils.GetRandomRadian();

                    ret.Add(new MyVoxelMapImpostor(position, radius, angle));
                }

                //  Sort by distance (back-to-front) so alpha won't make problems on overlapping quads
                ret.Sort();

                return ret;
            }

            void CreateVertices(List<MyVoxelMapImpostor> impostors)
            {
                if (impostors == null)
                    return;

                m_trianglesCount = impostors.Count * MyVoxelMapImpostorsConstants.TRIANGLES_PER_IMPOSTOR;
                if (m_trianglesCount <= 0) return;

                m_vertices = new MyVertexFormatPositionTextureColor[impostors.Count * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR];

                Vector2 texCoord0 = new Vector2(0, 0);
                Vector2 texCoord1 = new Vector2(1, 0);
                Vector2 texCoord2 = new Vector2(1, 1);
                Vector2 texCoord3 = new Vector2(0, 1);

                for (int i = 0; i < impostors.Count; i++)
                {
                    MyQuadD retQuad;
                    MyUtils.GetBillboardQuadAdvancedRotated(out retQuad, impostors[i].Position, impostors[i].Radius, impostors[i].Angle, Vector3.Zero);

                    Vector3 position0 = retQuad.Point0;
                    Vector3 position1 = retQuad.Point1;
                    Vector3 position2 = retQuad.Point2;
                    Vector3 position3 = retQuad.Point3;

                    //  Alpha must stay opaque (1.0)
                    Vector4 color = MyUtils.GetRandomFloat(MyVoxelMapImpostorsConstants.RANDOM_COLOR_MULTIPLIER_MIN, MyVoxelMapImpostorsConstants.RANDOM_COLOR_MULTIPLIER_MAX) * Vector4.One;
                    color.W = 1;

                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 0] = new MyVertexFormatPositionTextureColor(position0, texCoord0, color);
                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 1] = new MyVertexFormatPositionTextureColor(position1, texCoord1, color);
                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 2] = new MyVertexFormatPositionTextureColor(position3, texCoord3, color);
                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 3] = new MyVertexFormatPositionTextureColor(position1, texCoord1, color);
                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 4] = new MyVertexFormatPositionTextureColor(position2, texCoord2, color);
                    m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 5] = new MyVertexFormatPositionTextureColor(position3, texCoord3, color);
                }
            }

            public void LoadInDraw()
            {
                if (m_trianglesCount > 0)
                {
                    m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, MyVertexFormatPositionTextureColor.Stride * m_vertices.Length, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                    m_vertexBuffer.SetData(m_vertices);
                    m_vertexBuffer.DebugName = "VoxelMapImpostors";
                    m_vertexBuffer.Tag = this;
                }

                //  Don't need anymore
                m_vertices = null;
            }


            public void PrepareForDraw(MyEffectDistantImpostors effect)
            {            
                if (!ImpostorProperties.Enabled)
                    return;

                if (ImpostorProperties.ImpostorType == MyImpostorType.Nebula)
                {
                    Matrix mat = Matrix.CreateScale(ImpostorProperties.MinDistance) * Matrix.CreateTranslation((Vector3)MyRenderCamera.Position * 0.5f);

                    Matrix projection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, 1000, 10000000);

                    effect.SetWorldMatrix(mat);
                    effect.SetViewProjectionMatrix((Matrix)(MyRenderCamera.ViewMatrix * projection));
                    effect.SetScale(ImpostorProperties.MaxRadius * 0.00018f);
                    effect.SetColor(ImpostorProperties.Color);

                    effect.SetContrastAndIntensity(new Vector2(ImpostorProperties.Contrast, ImpostorProperties.Intensity));
                    effect.SetAnimation(m_animationTime);
                    effect.SetCameraPos((Vector3)MyRenderCamera.Position);
                    effect.SetSunDirection(MyRender.Sun.Direction);

                    effect.SetTechnique(MyEffectDistantImpostors.Technique.Textured3D);
                                    /* //todo missing texcoord0
                    effect.Begin();
                    MySimpleObjectDraw.ModelSphere.Render();
                    effect.End(); */
                }  
            }

            public void Draw(MyEffectDistantImpostors effect, MyImpostorType impostorType)
            {
                if (!ImpostorProperties.Enabled)
                    return;

                if (impostorType != ImpostorProperties.ImpostorType)
                    return;
                  
                if (ImpostorProperties.ImpostorType == MyImpostorType.Billboards)
                {
                    if (m_trianglesCount <= 0) return;

                    m_animationTime += ImpostorProperties.AnimationSpeed;

                    Device device = MyRender.GraphicsDevice;

                    Matrix worldMatrix = Matrix.Identity;

                    if (ImpostorProperties.AnimationSpeed.X > 0)
                        worldMatrix *= Matrix.CreateRotationX(m_animationTime.X);
                    if (ImpostorProperties.AnimationSpeed.Y > 0)
                        worldMatrix *= Matrix.CreateRotationX(m_animationTime.Y);
                    if (ImpostorProperties.AnimationSpeed.Z > 0)
                        worldMatrix *= Matrix.CreateRotationX(m_animationTime.Z);

                    worldMatrix.Translation = (Vector3)MyRenderCamera.Position * 0.5f;
                    effect.SetWorldMatrix(worldMatrix);

                    MyTexture2D texture = null;
                    if (ImpostorProperties.Material != null)
                    {
                        texture = MyTransparentGeometry.GetTexture(ImpostorProperties.Material);
                    }
                    effect.SetImpostorTexture(texture);
                    device.SetStreamSource(0, m_vertexBuffer, 0, MyVertexFormatPositionTextureColor.Stride);
                    device.VertexDeclaration = MyVertexFormatPositionTextureColor.VertexDeclaration;

                    effect.SetTechnique(MyEffectDistantImpostors.Technique.ColoredLit);

                    effect.Begin();
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, m_trianglesCount);
                    effect.End();
                    MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
                }
                else if (ImpostorProperties.ImpostorType == MyImpostorType.Nebula)
                {
                    m_animationTime += ImpostorProperties.AnimationSpeed * MyRenderConstants.RENDER_STEP_IN_MILLISECONDS / 20.0f;

                    BlendState.NonPremultiplied.Apply();
                    RasterizerState.CullCounterClockwise.Apply();
                    DepthStencilState.None.Apply();

                    MyRender.Blit(MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf0), true);
                }    
            }
        }

        List<MyVoxelMapImpostorGroup> m_voxelMapImpostorGroups = new List<MyVoxelMapImpostorGroup>();
        bool m_loaded = false; 

 
        public void LoadContent()
        {
            MyRender.Log.WriteLine("MyVoxelMapImpostors.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyVoxelMapImpostors::LoadContent");

            UnloadContent();
            m_voxelMapImpostorGroups.Clear();

            if (MyDistantImpostors.ImpostorProperties != null)
            {
                foreach (var i in MyDistantImpostors.ImpostorProperties)
                {
                    m_voxelMapImpostorGroups.Add(new MyVoxelMapImpostorGroup()
                    {
                        ImpostorProperties = i
                    });
                }
            }

            foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
            {
                group.LoadContent();
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyVoxelMapImpostors.LoadContent() - END");
        }

        public void UnloadContent()
        {
            foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
            {
                group.UnloadContent();
            }

            m_loaded = false; 
        }

        
       
        //	Special method that loads data into GPU, and can be called only from Draw method, never from LoadContent or from background thread.
        //	Because that would lead to empty vertex/index buffers if they are filled/created while game is minimized (remember the issue - alt-tab during loading screen)
        void LoadInDraw()
        {
            if (m_loaded == false)
            {
                foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
                {
                    group.LoadInDraw();
                }

                m_loaded = true; 
            }
        }

        public void Draw(MyEffectDistantImpostors effect)
        {
            LoadInDraw();

            Device device = MyRender.GraphicsDevice;
            RasterizerState.CullNone.Apply();
            MyStateObjects.NonPremultiplied_NoAlphaWrite_BlendState.Apply();
            MyStateObjects.DistantImpostorsDepthStencilState.Apply();


            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, 1000, 10000000);
            effect.SetViewProjectionMatrix((Matrix)(MyRenderCamera.ViewMatrix * projection));
            effect.SetSunDirection(MyRender.Sun.Direction);
            MyRenderCamera.SetupBaseEffect(effect, MyLodTypeEnum.LOD0);

            foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
            {
                group.Draw(effect, MyImpostorType.Billboards);
            }

            BlendState.NonPremultiplied.Apply();

            foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
            {
                group.Draw(effect, MyImpostorType.Nebula);
            }
        }

        public void PrepareForDraw(MyEffectDistantImpostors effect)
        {
            RasterizerState.CullClockwise.Apply();
            BlendState.Opaque.Apply();
            MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf0), null);

            foreach (MyVoxelMapImpostorGroup group in m_voxelMapImpostorGroups)
            {
                group.PrepareForDraw(effect);
            }     
        }
    }
}
