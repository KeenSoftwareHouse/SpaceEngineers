#region Using

using System.Collections.Generic;
using VRage.Generics;
using ParallelTasks;
using VRageMath.PackedVector;
using System.Runtime.InteropServices;
using System;
using VRage.Utils;
using System.Diagnostics;

using VRageRender.Textures;
using VRageRender.Graphics;
using VRageRender.Utils;
using VRageRender.Effects;
using VRageRender.Lights;
using VRageMath;

using SharpDX;
using SharpDX.Direct3D9;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Rectangle = VRageMath.Rectangle;
using Matrix = VRageMath.Matrix;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Animations;

#endregion

//  Use this STATIC class to create new particle and draw all living particles.
//  Particle is drawn as billboard or poly-line facing the camera. All particles lie on same texture atlas.
//  We use only pre-multiplied alpha particles. Here is the principle:
//      texture defines:
//          RGB - how much color adds/contributes the object to the scene
//          A   - how much it obscures whatever is behind it (0 = opaque, 1 = transparent ... but RGB is also important because it's additive)
//
//  Pre-multiplied alpha ----> blend(source, dest)  =  source.rgb + (dest.rgb * (1 - source.a))
//
//  In this world, RGB and alpha are linked. To make an object transparent you must reduce both its RGB (to contribute less color) and also its 
//  alpha (to obscure less of whatever is behind it). Fully transparent objects no longer have any RGB color, so there is only one value that 
//  represents 100% transparency (RGB and alpha all zero).
//
//  Billboards can be affected (lighted, attenuated) by other lights (player reflector, dynamic lights, etc) if 'CanBeAffectedByOtherLights = true'.
//  This is usefull for example when you want to have dust particle lighted by reflector, but don't want it for explosions or ship engine thrusts.
//  Drawing is always in back-to-front order.
//  If you set particle color higher than 1.0, it will shine more. It's cheap HDR effect.

namespace VRageRender
{
    class MyTransparentGeometry : MyRenderComponentBase
    {
        #region Fields

        //static List<MyBillboard> m_unsortedTransparentGeometry = new List<MyBillboard>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT); 
        static List<MyBillboard> m_sortedTransparentGeometry = new List<MyBillboard>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT); //  Used for drawing sorted particles
        static List<MyBillboard> m_preparedTransparentGeometry = new List<MyBillboard>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT);
        static List<MyBillboard> m_lowresTransparentGeometry = new List<MyBillboard>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT);

        // Billboards only for this frame
        static List<MyBillboard> m_billboardsOnce = new List<MyBillboard>();
        static MyObjectsPoolSimple<MyBillboard> m_billboardOncePool = new MyObjectsPoolSimple<MyBillboard>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT / 4);

        const int RENDER_BUFFER_SIZE = 4096;

        //  For drawing particle billboards using vertex buffer
        static MyVertexFormatTransparentGeometry[] m_vertices = new MyVertexFormatTransparentGeometry[MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT * MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY];

        // This can be freed, but it would create holes in LOH, so let is allocated
        static int[] m_indices = new int[MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_INDICES];

        static MyTexture2D m_atlasTexture;

        static VertexBuffer m_vertexBuffer;
        static IndexBuffer m_indexBuffer;
        static int m_startOffsetInVertexBuffer;
        static int m_endOffsetInVertexBuffer;

        //interpolated property used to calculate color for particles overdraw layer
        static MyAnimatedPropertyVector4 m_overDrawColorsAnim;
        const int PARTICLES_OVERDRAW_MAX = 100;
        static Viewport m_halfViewport = new Viewport();

        public static Color ColorizeColor { get; set; }
        public static Vector3 ColorizePlaneNormal { get; set; }
        public static float ColorizePlaneDistance { get; set; }
        public static bool EnableColorize { get; set; }

        static bool IsEnabled
        {
            get
            {
                return MyRender.IsModuleEnabled(MyRenderStage.AlphaBlend, MyRenderModuleEnum.TransparentGeometry)
                    || MyRender.IsModuleEnabled(MyRenderStage.AlphaBlendPreHDR, MyRenderModuleEnum.TransparentGeometry);
            }
        }

        #endregion

        #region Constructor

        static MyTransparentGeometry()
        {
            MyRender.RegisterRenderModule(MyRenderModuleEnum.TransparentGeometry, "Transparent geometry", Draw, MyRenderStage.AlphaBlendPreHDR, 150, true);

            int v = 0;
            for (int i = 0; i < m_indices.Length; i += 6)
            {
                m_indices[i + 0] = v + 0;
                m_indices[i + 1] = v + 1;
                m_indices[i + 2] = v + 2;

                m_indices[i + 3] = v + 0;
                m_indices[i + 4] = v + 2;
                m_indices[i + 5] = v + 3;

                v += MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY;
            }

            MyTransparentMaterials.OnUpdate += delegate
            {
                PrepareAtlasMaterials();
            };
        }

        #endregion

        #region Add billboards

        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddPointBillboard(string material,
            Color color, Vector3D origin, float radius, float angle, int priority = 0, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1,bool cullwithStencil = false)
        {
            Debug.Assert(material != null);

            if (!IsEnabled) return;

            origin.AssertIsValid();
            angle.AssertIsValid();

            MyQuadD quad;
            if (MyUtils.GetBillboardQuadAdvancedRotated(out quad, origin, radius, angle, MyRenderCamera.Position) != false)
            {
                VRageRender.MyBillboard billboard = m_billboardOncePool.Allocate();
                if (billboard == null)
                    return;

                billboard.CullWithStencil = cullwithStencil;
                billboard.Priority = priority;
                billboard.CustomViewProjection = customViewProjection;
                CreateBillboard(billboard, ref quad, material, ref color, ref origin, colorize, near, lowres);

                // TODO: OP! Nothing should add into BillboardsRead, especially when it may be used for more than one rendering frame
                m_billboardsOnce.Add(billboard);
            }
        }

        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  This billboard isn't facing the camera. It's always oriented in specified direction. May be used as thrusts, or inner light of reflector.
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddBillboardOriented(string material,
            Color color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float radius, int priority = 0, bool colorize = false, int customViewProjection = -1)
        {
            Debug.Assert(material != null);

            if (!IsEnabled) return;

            origin.AssertIsValid();
            leftVector.AssertIsValid();
            upVector.AssertIsValid();
            radius.AssertIsValid();
            MyDebug.AssertDebug(radius > 0);


            MyBillboard billboard = m_billboardOncePool.Allocate();
            if (billboard == null)
                return;

            billboard.Priority = priority;
            billboard.CustomViewProjection = customViewProjection;

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, radius, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, colorize);

            m_billboardsOnce.Add(billboard);
        }


        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddLineBillboard(string material,
            Color color, Vector3D origin, Vector3 directionNormalized, float length, float thickness, int priority = 0, bool near = false, int customViewProjection = -1)
        {
            Debug.Assert(material != null);

            if (!IsEnabled) return;

            origin.AssertIsValid();
            length.AssertIsValid();
            MyDebug.AssertDebug(length > 0);
            MyDebug.AssertDebug(thickness > 0);

            VRageRender.MyBillboard billboard = m_billboardOncePool.Allocate();
            if (billboard == null)
                return;

            billboard.Priority = priority;
            billboard.CustomViewProjection = customViewProjection;

            MyPolyLineD polyLine;
            polyLine.LineDirectionNormalized = directionNormalized;
            polyLine.Point0 = origin;
            polyLine.Point1 = origin + directionNormalized * length;
            polyLine.Thickness = thickness;

            MyQuadD quad;
            MyUtilsRender9.GetPolyLineQuad(out quad, ref polyLine);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, false, near);

            m_billboardsOnce.Add(billboard);
        }


        public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material,
            ref Color color, ref Vector3D origin, bool colorize = false, bool near = false, bool lowres = false)
        {
            Debug.Assert(material != null);
            CreateBillboard(billboard, ref quad, material, null, 0, ref color, ref origin, colorize, near, lowres);
        }

        public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material,
            ref Color color, ref Vector3D origin, Vector2 uvOffset, bool colorize = false, bool near = false, bool lowres = false)
        {
            Debug.Assert(material != null);
            CreateBillboard(billboard, ref quad, material, null, 0, ref color, ref origin, uvOffset, colorize, near, lowres);
        }

        public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, string blendMaterial, float textureBlendRatio,
            ref Color color, ref Vector3D origin, bool colorize = false, bool near = false, bool lowres = false)
        {
            Debug.Assert(material != null);
            CreateBillboard(billboard, ref quad, material, blendMaterial, textureBlendRatio, ref color, ref origin, Vector2.Zero, colorize, near, lowres);
        }

        //  This method is like a constructor (which we can't use because billboards are allocated from a pool).
        //  It starts/initializes a billboard. Refs used only for optimalization
        public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, string blendMaterial, float textureBlendRatio,
            ref Color color, ref Vector3D origin, Vector2 uvOffset, bool colorize = false, bool near = false, bool lowres = false, float reflectivity = 0)
        {
            Debug.Assert(material != null);
            
            if (string.IsNullOrEmpty(material) || !MyTransparentMaterials.ContainsMaterial(material))
            {
                material = "ErrorMaterial";
                color = Vector4.One;
            }

            billboard.Material = material;
            billboard.BlendMaterial = blendMaterial;
            billboard.BlendTextureRatio = textureBlendRatio;

            quad.Point0.AssertIsValid();
            quad.Point1.AssertIsValid();
            quad.Point2.AssertIsValid();
            quad.Point3.AssertIsValid();


            //  Billboard vertexes
            billboard.Position0 = quad.Point0;
            billboard.Position1 = quad.Point1;
            billboard.Position2 = quad.Point2;
            billboard.Position3 = quad.Point3;

            billboard.UVOffset = uvOffset;

            EnableColorize = colorize;

            if (EnableColorize)
                billboard.Size = (float)(billboard.Position0 - billboard.Position2).Length();

            //  Distance for sorting
            //  IMPORTANT: Must be calculated before we do color and alpha misting, because we need distance there
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyRenderCamera.Position, origin);

            //  Color
            billboard.Color = color;
            billboard.ColorIntensity = 1;
            billboard.Reflectivity = reflectivity;

            billboard.Near = near;
            billboard.Lowres = lowres;
            billboard.ParentID = -1;

            //  Alpha depends on distance to camera. Very close bilboards are more transparent, so player won't see billboard errors or rotating billboards
            var mat = MyTransparentMaterials.GetMaterial(billboard.Material);
            if (mat.AlphaMistingEnable)
                billboard.Color *= MathHelper.Clamp(((float)Math.Sqrt(billboard.DistanceSquared) - mat.AlphaMistingStart) / (mat.AlphaMistingEnd - mat.AlphaMistingStart), 0, 1);

            billboard.Color *= mat.Color;

            billboard.ContainedBillboards.Clear();
        }

        #endregion

        #region Load/unload content

        public override int GetID()
        {
            return (int)MyRenderComponentID.TransparentGeometry;
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        public override void LoadContent()
        {
            MyRender.Log.WriteLine("TransparentGeometry.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("TransparentGeometry.LoadContent");

            MyRender.Log.WriteLine(string.Format("MyTransparentGeometry.LoadData - START"));

            m_sortedTransparentGeometry.Clear();

            m_preparedTransparentGeometry.Clear();
            m_lowresTransparentGeometry.Clear();

            //  Max count of all particles should be less or equal than max count of billboards
            MyDebug.AssertRelease(MyTransparentGeometryConstants.MAX_PARTICLES_COUNT <= MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT);
            MyDebug.AssertRelease(MyTransparentGeometryConstants.MAX_COCKPIT_PARTICLES_COUNT <= MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT);

            PrepareAtlasMaterials();

            m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, MyVertexFormatTransparentGeometry.Stride * MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT * MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY, Usage.WriteOnly | Usage.Dynamic, VertexFormat.None, Pool.Default);
            m_startOffsetInVertexBuffer = 0;
            m_endOffsetInVertexBuffer = 0;
            m_vertexBuffer.DebugName = "TransparentGeometry";

            m_indexBuffer = new IndexBuffer(MyRender.GraphicsDevice, MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_INDICES * sizeof(int), Usage.WriteOnly, Pool.Default, false);
            m_indexBuffer.SetData(m_indices);

            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("TransparentGeometry.LoadContent() - END");
        }

        public static void PrepareAtlasMaterials()
        {
            //Prepare data for atlas
            List<string> atlasTextures = new List<string>();
            Dictionary<string, int> atlasMaterials = new Dictionary<string, int>();
            foreach (var material in MyTransparentMaterials.Materials)
            {
                if (material.UseAtlas)
                {
                    string texturePath = "Textures\\" + System.IO.Path.GetFileName(material.Texture);
                    if (!atlasTextures.Contains(texturePath))
                        atlasTextures.Add(texturePath);
                    atlasMaterials.Add(material.Name, atlasTextures.IndexOf(texturePath));
                }
            }
            string[] atlasTexturesArray = atlasTextures.ToArray();
            MyAtlasTextureCoordinate[] m_textureCoords;
            //Load atlas
            MyUtilsRender9.LoadTextureAtlas(atlasTexturesArray, "Textures\\Particles\\", "Textures\\Particles\\ParticlesAtlas.tai", out m_atlasTexture, out m_textureCoords);

            //Assign atlas coordinates to materials UV
            foreach (KeyValuePair<string, int> pair in atlasMaterials)
            {
                string materialName = pair.Key;
                MyTransparentMaterial materialProperties = MyTransparentMaterials.GetMaterial(materialName);

                materialProperties.UVOffset = m_textureCoords[pair.Value].Offset;
                materialProperties.UVSize = m_textureCoords[pair.Value].Size;
            }
        }

        /// <summary>
        /// Unloads the content.
        /// </summary>
        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("TransparentGeometry.UnloadContent - START");
            MyRender.Log.IncreaseIndent();


            if (m_indexBuffer != null)
            {
                m_indexBuffer.Dispose();
                m_indexBuffer = null;
            }

            if (m_vertexBuffer != null)
            {
                m_vertexBuffer.Dispose();
                m_vertexBuffer = null;
            }

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("TransparentGeometry.UnloadContent - END");

        }


        #endregion

        #region PrepareVertexBuffer

        static void PrepareVertexBuffer()
        {
            //  Billboards by distance to camera (back-to-front)
            m_sortedTransparentGeometry.Clear();

            MyRender.GetRenderProfiler().StartProfilingBlock("CopyBillboardsToSortingList sorted");
            CopyBillboardsToSortingList(MyRenderProxy.BillboardsRead, true);
            CopyBillboardsToSortingList(m_billboardsOnce, true);
            MyRender.GetRenderProfiler().EndProfilingBlock();

            /*
            MyRender.GetRenderProfiler().StartProfilingBlock("CopyAnimatedParticlesToSortingList");
            CopyAnimatedParticlesToSortingList(m_animatedParticles);
            MyRender.GetRenderProfiler().EndProfilingBlock();
              */

            MyRender.GetRenderProfiler().StartProfilingBlock("Sort");
            m_sortedTransparentGeometry.Sort();

            MyPerformanceCounter.PerCameraDrawWrite.BillboardsSorted = m_sortedTransparentGeometry.Count;

            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.GetRenderProfiler().StartProfilingBlock("CopyBillboardsToSortingList unsorted");
            CopyBillboardsToSortingList(MyRenderProxy.BillboardsRead, false);
            CopyBillboardsToSortingList(m_billboardsOnce, false);
            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.GetRenderProfiler().StartProfilingBlock("ProcessSortedBillboards");
            ProcessSortedBillboards();
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region Draw

        public static void Draw()
        {
            MyStateObjects.AlphaBlend_NoAlphaWrite_BlendState.Apply();

            MyTransparentGeometry.Draw(MyRender.GetRenderTarget(MyRenderTargets.Depth));

            MyRender.GetRenderProfiler().ProfileCustomValue("Particles count", MyPerformanceCounter.PerCameraDrawWrite.NewParticlesCount);
            MyRender.GetRenderProfiler().ProfileCustomValue("Billboard drawcalls", MyPerformanceCounter.PerCameraDrawWrite.BillboardsDrawCalls);
        }


        //  Draws and updates active particles. If particle dies/timeouts, remove it from the list.
        //  This method is in fact update+draw.
        //  If drawNormalParticles = true, then normal particles are drawn. If drawNormalParticles=false, then in-cockpit particles are drawn.
        static void Draw(Texture depthForParticlesRT)
        {
            PrepareVertexBuffer();

            bool setClipPlanes = MyRender.CurrentRenderSetup.CallerID.Value == MyRenderCallerEnum.Main;
            if (setClipPlanes)
                MyRenderCamera.SetParticleClipPlanes(true);

            if (!MyRender.Settings.VisualizeOverdraw)
            {
                MyRender.GetRenderProfiler().StartProfilingBlock("CopyToVertexBuffer Lowres");
                CopyToVertexBuffer(m_lowresTransparentGeometry);
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("DrawVertexBuffer Lowres");

                MyRenderTargets lowresTarget = MyRenderTargets.AuxiliaryHalf0;
                MyRender.SetRenderTarget(MyRender.GetRenderTarget(lowresTarget), null);

                //Must be here otherwise mixes with godrays
                MyRender.GraphicsDevice.Clear(ClearFlags.Target, new ColorBGRA(0), 1, 0);

                m_halfViewport.Width = MyRender.GetRenderTarget(lowresTarget).GetLevelDescription(0).Width;
                m_halfViewport.Height = MyRender.GetRenderTarget(lowresTarget).GetLevelDescription(0).Height;
                MyRender.SetDeviceViewport(m_halfViewport);

                //  Pre-multiplied alpha
                BlendState.AlphaBlend.Apply();
                DrawVertexBuffer(MyRender.GetRenderTarget(MyRenderTargets.Depth), m_lowresTransparentGeometry);
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.TakeScreenshot("LowresParticles", MyRender.GetRenderTarget(lowresTarget), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);

                MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary1), null);
                MyRender.SetDeviceViewport(MyRenderCamera.Viewport);

                MyStateObjects.AlphaBlend_NoAlphaWrite_BlendState.Apply();
                MyRender.Blit(MyRender.GetRenderTarget(lowresTarget), false, MyEffectScreenshot.ScreenshotTechniqueEnum.LinearScale);
            }

            //  Render
            MyRender.GetRenderProfiler().StartProfilingBlock("CopyToVertexBuffer");
            CopyToVertexBuffer(m_preparedTransparentGeometry);
            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.GetRenderProfiler().StartProfilingBlock("DrawVertexBuffer");
            //  Pre-multiplied alpha with disabled alpha write
            MyStateObjects.AlphaBlend_NoAlphaWrite_BlendState.Apply();
            DrawVertexBuffer(depthForParticlesRT, m_preparedTransparentGeometry);
            MyRender.GetRenderProfiler().EndProfilingBlock();



            BlendState.Opaque.Apply();
            if (setClipPlanes)
                MyRenderCamera.ResetClipPlanes(true);

            m_billboardOncePool.ClearAllAllocated();
            m_billboardsOnce.Clear();
            //Now we dont need billboards anymore and we can clear them
            //ClearBillboards();  
        }


        static void ProcessSortedBillboards()
        {
            //replace contained billboards, move lowres billboards
            int c = 0;
            m_lowresTransparentGeometry.Clear();
            m_preparedTransparentGeometry.Clear();
            while (c < m_sortedTransparentGeometry.Count)
            {
                MyBillboard billboard = m_sortedTransparentGeometry[c];

                if (billboard.Lowres)
                {
                    if (billboard.ContainedBillboards.Count > 0)
                    {
                        m_lowresTransparentGeometry.AddList(billboard.ContainedBillboards);
                    }
                    else
                    {
                        m_lowresTransparentGeometry.Add(billboard);
                    }
                }
                else
                {
                    if (billboard.ContainedBillboards.Count > 0)
                    {
                        m_preparedTransparentGeometry.AddList(billboard.ContainedBillboards);
                    }
                    else
                    {
                        m_preparedTransparentGeometry.Add(billboard);
                    }
                }

                c++;
            }
        }

        static void CopyBillboardsToSortingList(List<MyBillboard> billboards, bool sortedMaterials)
        {
            for (int i = 0; i < billboards.Count; i++)
            {
                MyBillboard billboard = billboards[i];
                MyTransparentMaterial materialProperties = billboard.Material != null ? MyTransparentMaterials.GetMaterial(billboard.Material) : null;

                if (materialProperties == null && (billboard.ContainedBillboards.Count == 0 || sortedMaterials))
                    continue;

                bool needSort = billboard.ContainedBillboards.Count > 0 || materialProperties.NeedSort;

                if (needSort == sortedMaterials)
                {
                    if (m_sortedTransparentGeometry.Count < MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT)
                    {
                        m_sortedTransparentGeometry.Add(billboard);
                    }
                }
            }
        }

        /// <summary>
        /// Copies to vertex buffer.
        /// </summary>
        static void CopyToVertexBuffer(List<MyBillboard> billboards)
        {
            // Loop over in parallel tasks
            //Parallel.For(0, m_sortedTransparentGeometry.Count, m_copyGeometryToVertexBuffer);

            int verticesCount = billboards.Count * MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY;

            LockFlags lockFlags = LockFlags.NoOverwrite;

            if (verticesCount + m_endOffsetInVertexBuffer > MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_VERTICES)
            {
                m_startOffsetInVertexBuffer = 0;
                lockFlags = LockFlags.Discard;
            }
            else
                m_startOffsetInVertexBuffer = m_endOffsetInVertexBuffer;

            for (int i = 0; i < billboards.Count; i++)
            {
                var billboard = billboards[i];
                MyTriangleBillboard triBillboard = billboard as MyTriangleBillboard;
                if (triBillboard != null)
                    CopyTriBillboardToVertices(i, triBillboard);
                else
                    CopyBillboardToVertices(i, billboard);
            }

            if (billboards.Count > 0)
            {
                int offset = m_startOffsetInVertexBuffer * MyVertexFormatTransparentGeometry.Stride;
                int size = verticesCount * MyVertexFormatTransparentGeometry.Stride;
                m_vertexBuffer.LockAndWrite(offset, size, lockFlags, m_vertices, 0, verticesCount);
            }
            m_endOffsetInVertexBuffer = m_startOffsetInVertexBuffer + verticesCount;
            //m_startOffsetInVertexBuffer = 0;
        }

        /// <summary>
        /// Copies the billboard to vertex buffer.
        /// </summary>
        /// <param name="billboarIdx">The billboar idx.</param>
        static void CopyBillboardToVertices(int billboarIdx, MyBillboard billboard)
        {
            int startIndex = (billboarIdx) * MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY;
            HalfVector4 colorHalf = new HalfVector4(billboard.Color);
            MyTransparentMaterial materialProperties = MyTransparentMaterials.GetMaterial(billboard.Material);

            billboard.Position0.AssertIsValid();
            billboard.Position1.AssertIsValid();
            billboard.Position2.AssertIsValid();
            billboard.Position3.AssertIsValid();

            
            m_vertices[startIndex + 0].Color = colorHalf;
            m_vertices[startIndex + 0].TexCoord = new HalfVector4(materialProperties.UVOffset.X + billboard.UVOffset.X, materialProperties.UVOffset.Y + billboard.UVOffset.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            
            m_vertices[startIndex + 1].Color = colorHalf;
            m_vertices[startIndex + 1].TexCoord = new HalfVector4(materialProperties.UVOffset.X + materialProperties.UVSize.X + billboard.UVOffset.X, materialProperties.UVOffset.Y + billboard.UVOffset.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            
            m_vertices[startIndex + 2].Color = colorHalf;
            m_vertices[startIndex + 2].TexCoord = new HalfVector4(materialProperties.UVOffset.X + materialProperties.UVSize.X + billboard.UVOffset.X, materialProperties.UVOffset.Y + materialProperties.UVSize.Y + billboard.UVOffset.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            m_vertices[startIndex + 3].Color = colorHalf;
            m_vertices[startIndex + 3].TexCoord = new HalfVector4(materialProperties.UVOffset.X + billboard.UVOffset.X, materialProperties.UVOffset.Y + materialProperties.UVSize.Y + billboard.UVOffset.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            Vector3D pos0 = billboard.Position0;
            Vector3D pos1 = billboard.Position1;
            Vector3D pos2 = billboard.Position2;
            Vector3D pos3 = billboard.Position3;

            if (billboard.ParentID != -1)
            {
                MyRenderObject renderObject;
                if (MyRender.m_renderObjects.TryGetValue((uint)billboard.ParentID, out renderObject))
                {
                    MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                    MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                    if (transformObject != null)
                    {
                        var worldMatrix = transformObject.WorldMatrix;

                        Vector3D.Transform(ref pos0, ref worldMatrix, out pos0);
                        Vector3D.Transform(ref pos1, ref worldMatrix, out pos1);
                        Vector3D.Transform(ref pos2, ref worldMatrix, out pos2);
                        Vector3D.Transform(ref pos3, ref worldMatrix, out pos3);
                    }
                    else if (cullableObject != null)
                    {
                        var worldMatrix = cullableObject.WorldMatrix;

                        Vector3D.Transform(ref pos0, ref worldMatrix, out pos0);
                        Vector3D.Transform(ref pos1, ref worldMatrix, out pos1);
                        Vector3D.Transform(ref pos2, ref worldMatrix, out pos2);
                        Vector3D.Transform(ref pos3, ref worldMatrix, out pos3);
                    }
                }
            }

            if (billboard.CustomViewProjection != -1)
            {
                System.Diagnostics.Debug.Assert(MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(billboard.CustomViewProjection));

                if (MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(billboard.CustomViewProjection))
                {
                    var billboardViewProjection = MyRenderProxy.BillboardsViewProjectionRead[billboard.CustomViewProjection];

                    pos0 -= billboardViewProjection.CameraPosition;
                    pos1 -= billboardViewProjection.CameraPosition;
                    pos2 -= billboardViewProjection.CameraPosition;
                    pos3 -= billboardViewProjection.CameraPosition;
                }
                else
                {
                    pos0 -= MyRenderCamera.Position;
                    pos1 -= MyRenderCamera.Position;
                    pos2 -= MyRenderCamera.Position;
                    pos3 -= MyRenderCamera.Position;
                }
            }
            else
            {
                pos0 -= MyRenderCamera.Position;
                pos1 -= MyRenderCamera.Position;
                pos2 -= MyRenderCamera.Position;
                pos3 -= MyRenderCamera.Position;
            }

            m_vertices[startIndex + 0].Position = pos0;
            m_vertices[startIndex + 1].Position = pos1;
            m_vertices[startIndex + 2].Position = pos2;
            m_vertices[startIndex + 3].Position = pos3;

            pos0.AssertIsValid();
            pos1.AssertIsValid();
            pos2.AssertIsValid();
            pos3.AssertIsValid();

            if (materialProperties.Reflectivity > 0)
            {
                var normal = Vector3.Cross(billboard.Position1 - billboard.Position0, billboard.Position2 - billboard.Position0);

                var NormalRefl = new HalfVector4(normal.X, normal.Y, normal.Z, billboard.Reflectivity);
                m_vertices[startIndex + 0].TexCoord2 = NormalRefl;
                m_vertices[startIndex + 1].TexCoord2 = NormalRefl;
                m_vertices[startIndex + 2].TexCoord2 = NormalRefl;
                m_vertices[startIndex + 3].TexCoord2 = NormalRefl;
            }
            else if (billboard.BlendTextureRatio > 0)
            {
                MyTransparentMaterial blendMaterialProperties = MyTransparentMaterials.GetMaterial(billboard.BlendMaterial);
                m_vertices[startIndex + 0].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 1].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + blendMaterialProperties.UVSize.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 2].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + blendMaterialProperties.UVSize.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + blendMaterialProperties.UVSize.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 3].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + blendMaterialProperties.UVSize.Y + billboard.UVOffset.Y, 0, 0);
            }

        }


        /// <summary>
        /// Copies the billboard to vertex buffer.
        /// </summary>
        /// <param name="billboarIdx">The billboar idx.</param>
        static void CopyTriBillboardToVertices(int billboarIdx, MyTriangleBillboard billboard)
        {
            int startIndex = (billboarIdx) * MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY;
            HalfVector4 colorHalf = new HalfVector4(billboard.Color);
            MyTransparentMaterial materialProperties = MyTransparentMaterials.GetMaterial(billboard.Material);

            var position0 = billboard.Position0;
            var position1 = billboard.Position1;
            var position2 = billboard.Position2;

            var normal0 = (Vector3D)billboard.Normal0;
            var normal1 = (Vector3D)billboard.Normal1;
            var normal2 = (Vector3D)billboard.Normal2;
            
            if (billboard.ParentID != -1)
            {
                MyRenderObject renderObject;
                if (MyRender.m_renderObjects.TryGetValue((uint)billboard.ParentID, out renderObject))
                {
                    MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                    if (transformObject != null)
                    {
                        var worldMatrix = transformObject.WorldMatrix;
                        Vector3D.Transform(ref billboard.Position0, ref worldMatrix, out position0);
                        Vector3D.Transform(ref billboard.Position1, ref worldMatrix, out position1);
                        Vector3D.Transform(ref billboard.Position2, ref worldMatrix, out position2);

                        Vector3D.TransformNormal(ref billboard.Normal0, ref worldMatrix, out normal0);
                        Vector3D.TransformNormal(ref billboard.Normal1, ref worldMatrix, out normal1);
                        Vector3D.TransformNormal(ref billboard.Normal2, ref worldMatrix, out normal2);
                    }
                }
            }

            if (billboard.CustomViewProjection != -1)
            {
                var billboardViewProjection = MyRenderProxy.BillboardsViewProjectionRead[billboard.CustomViewProjection];

                position0 -= billboardViewProjection.CameraPosition;
                position1 -= billboardViewProjection.CameraPosition;
                position2 -= billboardViewProjection.CameraPosition;
            }
            else
            {
                position0 -= MyRenderCamera.Position;
                position1 -= MyRenderCamera.Position;
                position2 -= MyRenderCamera.Position;
            }

            billboard.Position0.AssertIsValid();
            billboard.Position1.AssertIsValid();
            billboard.Position2.AssertIsValid();
            billboard.Position3.AssertIsValid();

            m_vertices[startIndex + 0].Position = position0;
            m_vertices[startIndex + 0].Color = colorHalf;
            m_vertices[startIndex + 0].TexCoord = new HalfVector4(billboard.UV0.X, billboard.UV0.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            m_vertices[startIndex + 1].Position = position1;
            m_vertices[startIndex + 1].Color = colorHalf;
            m_vertices[startIndex + 1].TexCoord = new HalfVector4(billboard.UV1.X, billboard.UV1.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            m_vertices[startIndex + 2].Position = position2;
            m_vertices[startIndex + 2].Color = colorHalf;
            m_vertices[startIndex + 2].TexCoord = new HalfVector4(billboard.UV2.X, billboard.UV2.Y, billboard.BlendTextureRatio, materialProperties.Emissivity);

            m_vertices[startIndex + 3] = m_vertices[startIndex + 0];

            if (materialProperties.Reflectivity > 0)
            {
                m_vertices[startIndex + 0].TexCoord2 = new HalfVector4((float)normal0.X, (float)normal0.Y, (float)normal0.Z, billboard.Reflectivity);
                m_vertices[startIndex + 1].TexCoord2 = new HalfVector4((float)normal1.X, (float)normal1.Y, (float)normal1.Z, billboard.Reflectivity);
                m_vertices[startIndex + 2].TexCoord2 = new HalfVector4((float)normal2.X, (float)normal2.Y, (float)normal2.Z, billboard.Reflectivity);
                m_vertices[startIndex + 3].TexCoord2 = m_vertices[startIndex + 0].TexCoord2;
            }
            else if (billboard.BlendTextureRatio > 0)
            {
                MyTransparentMaterial blendMaterialProperties = MyTransparentMaterials.GetMaterial(billboard.BlendMaterial);
                m_vertices[startIndex + 0].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 1].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + blendMaterialProperties.UVSize.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 2].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + blendMaterialProperties.UVSize.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + blendMaterialProperties.UVSize.Y + billboard.UVOffset.Y, 0, 0);
                m_vertices[startIndex + 3].TexCoord2 = new HalfVector4(blendMaterialProperties.UVOffset.X + billboard.UVOffset.X, blendMaterialProperties.UVOffset.Y + blendMaterialProperties.UVSize.Y + billboard.UVOffset.Y, 0, 0);
            }

        }

        static void DrawBuffer(int firstIndex, int primitivesCount)
        {
            //baseVertexIndex - start of part VB which ATI copy to internal buffer
            //minVertexIndex - relative value each index is decremented do get correct index to internal VB buffer part
            MyRender.GraphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, m_startOffsetInVertexBuffer, firstIndex / 3 * 2, primitivesCount * MyTransparentGeometryConstants.VERTICES_PER_TRIANGLE, firstIndex, primitivesCount);
            MyPerformanceCounter.PerCameraDrawWrite["Billboard draw calls"]++;
            MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
        }



        static void DrawVertexBuffer(Texture depthForParticlesRT, List<MyBillboard> billboards)
        {
            //  This is important for optimalization (although I don't know when it can happen to have zero billboards), but
            //  also that loop below needs it - as it assumes we are rendering at least one billboard.
            if (billboards.Count == 0)
                return;

            Device device = MyRender.GraphicsDevice;
            Surface oldTargets = null;

            DepthStencilState previousState = null;
            if (MyRender.Settings.VisualizeOverdraw)
            {
                oldTargets = device.GetRenderTarget(0);

                //We borrow lod0normals to render stencil
                MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary0), null);
                device.Clear(ClearFlags.Target | ClearFlags.Stencil, new ColorBGRA(0), 1.0f, 0);

                previousState = MyStateObjects.StencilMask_AlwaysIncrement_DepthStencilState;
                MyStateObjects.StencilMask_AlwaysIncrement_DepthStencilState.Apply();
            }
            else
            {
                previousState = DepthStencilState.None;
                DepthStencilState.None.Apply();
            }

            //  Draw particles without culling. It's because how we calculate left/up vector, we can have problems in back camera. (yes, that can be solved, but why bother...)
            //  Also I guess that drawing without culling may be faster - as GPU doesn't have to check it
            // No it's not, correct culling is faster: http://msdn.microsoft.com/en-us/library/windows/desktop/bb204882(v=vs.85).aspx
            RasterizerState.CullNone.Apply();

            MyEffectTransparentGeometry effect = MyRender.GetEffect(MyEffects.TransparentGeometry) as MyEffectTransparentGeometry;

            effect.SetWorldMatrix(Matrix.Identity);

            Matrix viewMatrix = MyRenderCamera.ViewMatrixAtZero;
            effect.SetViewMatrix(ref viewMatrix);

            effect.SetProjectionMatrix(ref MyRenderCamera.ProjectionMatrix);

            Viewport originalViewport = MyRender.GraphicsDevice.Viewport;

            effect.SetDepthsRT(depthForParticlesRT);
            effect.SetHalfPixel(depthForParticlesRT.GetLevelDescription(0).Width, depthForParticlesRT.GetLevelDescription(0).Height);
            effect.SetScale(MyRender.GetScaleForViewport(depthForParticlesRT));

            // Later we can interpolate between Main and Aux
            effect.SetEnvironmentMap(MyRender.GetRenderTargetCube(MyRenderTargets.EnvironmentCube));

            //For struct size checks
            //int stride = MyVertexFormatTransparentGeometry.VertexDeclaration.VertexStride;
            //int s = Marshal.SizeOf(new MyVertexFormatTransparentGeometry());


            //  We iterate over all sorted billboards, and seach for when texture/shader has changed.
            //  We try to draw as many billboards as possible (using the same texture), but because we are rendering billboards
            //  sorted by depth, we still need to switch sometimes. Btw: I have observed, that most time consuming when drawing particles
            //  is device.DrawUserPrimitives(), even if I call it for the whole list of billboards (without this optimization). I think, it's
            //  because particles are pixel-bound (I do a lot of light calculation + there is blending, which is always slow).
            MyTransparentMaterial lastMaterial = MyTransparentMaterials.GetMaterial(billboards[0].Material);
            MyBillboard lastBillboard = billboards[0];
            MyTransparentMaterial lastBlendMaterial = lastBillboard.BlendMaterial != null ? MyTransparentMaterials.GetMaterial(lastBillboard.BlendMaterial) : null;


            bool ignoreDepth = false;
            Matrix projectionMatrix = MyRenderCamera.ProjectionMatrix;
            Matrix invProjectionMatrix = Matrix.Invert(projectionMatrix);
            effect.SetInverseDefaultProjectionMatrix(ref invProjectionMatrix);

            if (lastBillboard.CustomViewProjection != -1)
            {
                SetupCustomViewProjection(effect, ref originalViewport, lastBillboard, ref ignoreDepth, ref projectionMatrix);
            }

            // 0.05% of billboard is blended
            const float softColorizeSize = 0.05f;

            device.VertexDeclaration = MyVertexFormatTransparentGeometry.VertexDeclaration;
            device.SetStreamSource(0, m_vertexBuffer, 0, MyVertexFormatTransparentGeometry.Stride);
            device.Indices = m_indexBuffer;

            MyRender.GetShadowRenderer().SetupShadowBaseEffect(effect);

            MyEffectTransparentGeometry effect2 = MyRender.GetEffect(MyEffects.TransparentGeometry) as MyEffectTransparentGeometry;
            effect2.SetShadowBias(0.001f);

            MyLights.UpdateEffectReflector(effect2.Reflector, false);
            MyLights.UpdateEffect(effect2, false);


            int geomCount = billboards.Count;
            int it = 0;
            int cnt = 0;

            while (geomCount > 0)
            {
                if (geomCount > RENDER_BUFFER_SIZE)
                {
                    geomCount -= RENDER_BUFFER_SIZE;
                    cnt = RENDER_BUFFER_SIZE;
                }
                else
                {
                    cnt = geomCount;
                    geomCount = 0;
                }

                int indexFrom = it * RENDER_BUFFER_SIZE + 1;
                cnt = cnt + indexFrom - 1;
                for (int i = indexFrom; i <= cnt; i++)
                {
                    //  We need texture from billboard that's before the current billboard (because we always render "what was")
                    MyBillboard billboard = billboards[i - 1];
                    MyTransparentMaterial blendMaterialProperties = billboard.BlendMaterial != null ? MyTransparentMaterials.GetMaterial(billboard.BlendMaterial) : MyTransparentMaterials.GetMaterial(billboard.Material);
                    MyTransparentMaterial lastBlendMaterialProperties = lastBlendMaterial == null ? blendMaterialProperties : lastBlendMaterial;

                    bool colorizeChanged = EnableColorize && lastBillboard.EnableColorize != billboard.EnableColorize;
                    bool nearChanged = lastBillboard.Near != billboard.Near;
                    bool sizeChanged = EnableColorize && billboard.EnableColorize && lastBillboard.Size != billboard.Size;
                    bool blendTextureChanged = false;
                    bool projectionChanged = lastBillboard.CustomViewProjection != billboard.CustomViewProjection;
                    bool cullStencilChanges = lastBillboard.CullWithStencil != billboard.CullWithStencil;

                    if (lastBlendMaterial != (billboard.BlendMaterial != null ? MyTransparentMaterials.GetMaterial(billboard.BlendMaterial) : null) && billboard.BlendTextureRatio > 0)
                    {
                        if ((lastBlendMaterialProperties.UseAtlas) && (blendMaterialProperties.UseAtlas))
                            blendTextureChanged = false;
                        else
                            blendTextureChanged = true;
                    }

                    //bool blendTextureChanged = lastBlendTexture != billboard.BlendTexture;
                    bool billboardChanged = colorizeChanged || sizeChanged || blendTextureChanged || nearChanged || projectionChanged || cullStencilChanges;

                    MyTransparentMaterial actMaterialProperties = MyTransparentMaterials.GetMaterial(billboard.Material);
                    MyTransparentMaterial lastMaterialProperties = lastMaterial;

                    billboardChanged |= (actMaterialProperties.CanBeAffectedByOtherLights != lastMaterialProperties.CanBeAffectedByOtherLights)
                                    || (actMaterialProperties.IgnoreDepth != lastMaterialProperties.IgnoreDepth);


                    if (projectionChanged)
                    {
                        SetupCustomViewProjection(effect, ref originalViewport, lastBillboard, ref ignoreDepth, ref projectionMatrix);
                    }

                    if (!billboardChanged)
                    {
                        if (MyTransparentMaterials.GetMaterial(billboard.Material) != lastMaterial)
                        {
                            if (actMaterialProperties.UseAtlas && lastMaterialProperties.UseAtlas)
                                billboardChanged = false;
                            else
                                billboardChanged = true;
                        }
                    }

                    //  If texture is different than the last one, or if we reached end of billboards
                    if ((i == cnt) || billboardChanged)
                    {
                        //  We don't need to do this when we reach end of billboards - it's needed only if we do next iteration of possible billboards
                        if ((i != cnt) || billboardChanged)
                        {
                            if ((i - indexFrom) > 0)
                            {
                                int firstIndex = (indexFrom - 1) * MyTransparentGeometryConstants.INDICES_PER_TRANSPARENT_GEOMETRY; //MyTransparentGeometryConstants.VERTICES_PER_TRANSPARENT_GEOMETRY;

                                SetupCustomViewProjection(effect, ref originalViewport, lastBillboard, ref ignoreDepth, ref projectionMatrix);

                                SetupEffect(ref lastMaterial, lastBlendMaterialProperties, EnableColorize && lastBillboard.EnableColorize, lastBillboard.Size * softColorizeSize, lastBillboard.Near, ignoreDepth, ref projectionMatrix);

                                effect.Begin();
                                DrawBuffer(firstIndex, (i - indexFrom) * MyTransparentGeometryConstants.TRIANGLES_PER_TRANSPARENT_GEOMETRY);
                                effect.End();

                                MyPerformanceCounter.PerCameraDrawWrite.BillboardsDrawCalls++;
                            }

                            lastMaterial = MyTransparentMaterials.GetMaterial(billboard.Material);
                            lastBillboard = billboard;
                            lastBlendMaterial = billboard.BlendMaterial != null ? MyTransparentMaterials.GetMaterial(billboard.BlendMaterial) : null;
                            indexFrom = i;
                        }


                        if ((i == cnt) && (i - indexFrom + 1 != 0))
                        {
                            lastMaterial = MyTransparentMaterials.GetMaterial(lastBillboard.Material);
                            blendMaterialProperties = lastBillboard.BlendMaterial == null ? MyTransparentMaterials.GetMaterial(lastBillboard.Material) : MyTransparentMaterials.GetMaterial(lastBillboard.BlendMaterial);
                            int firstIndex = (indexFrom - 1) * MyTransparentGeometryConstants.INDICES_PER_TRANSPARENT_GEOMETRY;

                            SetupCustomViewProjection(effect, ref originalViewport, lastBillboard, ref ignoreDepth, ref projectionMatrix);

                            SetupEffect(ref lastMaterial, blendMaterialProperties, EnableColorize && billboard.EnableColorize, lastBillboard.Size * softColorizeSize, lastBillboard.Near, ignoreDepth, ref projectionMatrix);

                            if (billboard.CullWithStencil)
                            {
                                DepthStencilState.BackgroundObjects.Apply();
                            }
                            effect.Begin();
                            DrawBuffer(firstIndex, (i - indexFrom + 1) * MyTransparentGeometryConstants.TRIANGLES_PER_TRANSPARENT_GEOMETRY);
                            effect.End();

                            if (billboard.CullWithStencil)
                            {
                                previousState.Apply();
                            }
                            MyPerformanceCounter.PerCameraDrawWrite.BillboardsDrawCalls++;
                        }
                    }
                }

                it++;
            }

            device.SetStreamSource(0, null, 0, 0);

            MyPerformanceCounter.PerCameraDrawWrite.BillboardsInFrustum += billboards.Count;

            // Visualize overdraw of particles. More overdraws = bigger performance issue.
            if (MyRender.Settings.VisualizeOverdraw)
            {
                if (m_overDrawColorsAnim == null)
                {
                    m_overDrawColorsAnim = new MyAnimatedPropertyVector4();
                    m_overDrawColorsAnim.AddKey(0.0f, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    m_overDrawColorsAnim.AddKey(0.25f, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                    m_overDrawColorsAnim.AddKey(0.75f, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
                    m_overDrawColorsAnim.AddKey(1.0f, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                }

                //Space without particles is black
                device.Clear(ClearFlags.Target, new ColorBGRA(0), 1.0f, 0);


                for (int referenceStencil = 1; referenceStencil < PARTICLES_OVERDRAW_MAX; referenceStencil++)
                {
                    DepthStencilState ds = new DepthStencilState()
                    {
                        StencilEnable = true,
                        ReferenceStencil = referenceStencil,
                        StencilFunction = Compare.LessEqual,
                    };

                    ds.Apply(false);


                    float diff = (float)(referenceStencil - 1) / (PARTICLES_OVERDRAW_MAX - 1);
                    Vector4 referenceColorV4;
                    m_overDrawColorsAnim.GetInterpolatedValue<Vector4>(diff, out referenceColorV4);
                    Color referenceColor = new Color(referenceColorV4);

                    MyRender.BeginSpriteBatch(BlendState.Opaque, null, null);
                    MyRender.DrawSprite(MyRender.BlankTexture, new Rectangle(0, 0, MyRenderCamera.Viewport.Width, MyRenderCamera.Viewport.Height), referenceColor);
                    MyRender.EndSpriteBatch();
                }

                DepthStencilState.None.Apply();

                int leftStart = MyRenderCamera.Viewport.Width / 4;
                int topStart = (int)(MyRenderCamera.Viewport.Height * 0.75f);

                int size = MyRenderCamera.Viewport.Width - 2 * leftStart;
                int sizeY = (int)(MyRenderCamera.Viewport.Width / 32.0f);
                int sizeStep = size / PARTICLES_OVERDRAW_MAX;

                MyRender.BeginSpriteBatch(null, null, null);

                for (int i = 0; i < PARTICLES_OVERDRAW_MAX; i++)
                {
                    float diff = (float)(i - 1) / (PARTICLES_OVERDRAW_MAX - 1);
                    Vector4 referenceColorV4;
                    m_overDrawColorsAnim.GetInterpolatedValue<Vector4>(diff, out referenceColorV4);
                    Color referenceColor = new Color(referenceColorV4);

                    MyRender.DrawSprite(MyRender.BlankTexture, new Rectangle(leftStart + i * sizeStep, topStart, sizeStep, sizeY), referenceColor);
                }

                MyDebugDraw.DrawText(new Vector2((float)leftStart, (float)(topStart + sizeY)), new System.Text.StringBuilder("1"), Color.White, 1.0f, false);
                MyDebugDraw.DrawText(new Vector2((float)leftStart + size, (float)(topStart + sizeY)), new System.Text.StringBuilder(">" + PARTICLES_OVERDRAW_MAX.ToString()), Color.White, 1.0f, false);

                MyRender.EndSpriteBatch();

                device.SetRenderTarget(0, oldTargets);
                oldTargets.Dispose();

                MyRender.Blit(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary0), false);
            }

            //  Restore to 'opaque', because that's the usual blend state
            BlendState.Opaque.Apply();
        }

        private static void SetupCustomViewProjection(MyEffectTransparentGeometry effect, ref Viewport originalViewport, MyBillboard lastBillboard, ref bool ignoreDepth, ref Matrix projectionMatrix)
        {
            if (lastBillboard.CustomViewProjection != -1 && MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(lastBillboard.CustomViewProjection))
            {
                var billboardViewProjection = MyRenderProxy.BillboardsViewProjectionRead[lastBillboard.CustomViewProjection];
                effect.SetViewMatrix(ref billboardViewProjection.ViewAtZero);
                effect.SetProjectionMatrix(ref billboardViewProjection.Projection);


                Matrix invProjectionMatrix = Matrix.Invert((MyRenderCamera.m_backupMatrix.HasValue ? MyRenderCamera.m_backupMatrix.Value : MyRenderCamera.ProjectionMatrix));
                effect.SetInverseDefaultProjectionMatrix(ref invProjectionMatrix);

                Viewport viewport = new Viewport((int)billboardViewProjection.Viewport.OffsetX, (int)billboardViewProjection.Viewport.OffsetY,
                    (int)billboardViewProjection.Viewport.Width, (int)billboardViewProjection.Viewport.Height);
                if (MyRender.GetScreenshot() != null)
                    viewport = new Viewport((int)(billboardViewProjection.Viewport.OffsetX * MyRender.GetScreenshot().SizeMultiplier.X),
                                            (int)(billboardViewProjection.Viewport.OffsetY * MyRender.GetScreenshot().SizeMultiplier.Y),
                                            (int)(billboardViewProjection.Viewport.Width * MyRender.GetScreenshot().SizeMultiplier.X),
                                            (int)(billboardViewProjection.Viewport.Height * MyRender.GetScreenshot().SizeMultiplier.Y));
                MyRender.SetDeviceViewport(viewport);
                ignoreDepth = !billboardViewProjection.DepthRead;
                projectionMatrix = billboardViewProjection.Projection;
            }
            else
            {
                var viewMatrix = MyRenderCamera.ViewMatrixAtZero;
                effect.SetViewMatrix(ref viewMatrix);
                effect.SetProjectionMatrix(ref MyRenderCamera.ProjectionMatrix);
                MyRender.SetDeviceViewport(originalViewport);
                ignoreDepth = false;
                projectionMatrix = MyRenderCamera.ProjectionMatrix;
            }
        }

        public static MyTexture2D GetTexture(MyTransparentMaterial material)
        {
            if (material.RenderTexture == null)
            {
                material.RenderTexture = material.UseAtlas ? m_atlasTexture : MyTextureManager.GetTexture<MyTexture2D>(material.Texture);
                if (material.RenderTexture == null)
                {
                    Debug.Fail("Null particle texture: " + material.Texture);
                    material.RenderTexture = new object(); // We don't want to try loading every call
                }
            }
            return material.RenderTexture as MyTexture2D;
        }

        private static void SetupEffect(ref MyTransparentMaterial materialProperties, MyTransparentMaterial blendMaterialProperties, bool colorize, float colorizeSoftDist, bool near, bool ignoreDepth, ref Matrix projectionMatrix)
        {
            MyEffectTransparentGeometry effect = MyRender.GetEffect(MyEffects.TransparentGeometry) as MyEffectTransparentGeometry;

            effect.SetBillboardTexture(GetTexture(materialProperties));
            effect.SetBillboardBlendTexture(GetTexture(blendMaterialProperties));

            effect.SetSoftParticleDistanceScale(materialProperties.SoftParticleDistanceScale);

            effect.SetAlphaMultiplierAndSaturation(1, materialProperties.AlphaSaturation);

            if (near)
            {
                effect.SetProjectionMatrix(ref MyRenderCamera.ProjectionMatrixForNearObjects);
                Matrix invProjectionMatrix = Matrix.Invert(MyRenderCamera.ProjectionMatrixForNearObjects);
                effect.SetInverseDefaultProjectionMatrix(ref invProjectionMatrix);
            }
            else
            {
                effect.SetProjectionMatrix(ref projectionMatrix);
                Matrix invProjectionMatrix = Matrix.Invert((MyRenderCamera.m_backupMatrix.HasValue ? MyRenderCamera.m_backupMatrix.Value : MyRenderCamera.ProjectionMatrix));
                effect.SetInverseDefaultProjectionMatrix(ref invProjectionMatrix);
            }

            if (MyRender.Settings.VisualizeOverdraw)
            {
                effect.SetTechnique(MyEffectTransparentGeometry.Technique.VisualizeOverdraw);
            }
            else
            {
                if (colorize)
                {
                    effect.SetColorizeSoftDistance(colorizeSoftDist);
                    effect.SetColorizeColor(ColorizeColor);
                    effect.SetColorizePlane(ColorizePlaneNormal, ColorizePlaneDistance);
                    effect.SetTechnique(MyEffectTransparentGeometry.Technique.ColorizeHeight);
                }
                else if (materialProperties.IgnoreDepth || ignoreDepth)
                {
                    effect.SetTechnique(MyEffectTransparentGeometry.Technique.IgnoreDepth);
                }
                else if (materialProperties.CanBeAffectedByOtherLights)
                {
                    effect.SetTechnique(MyEffectTransparentGeometry.Technique.Lit);
                }
                else if (materialProperties.Reflectivity > 0)
                {
                    effect.SetTechnique(MyEffectTransparentGeometry.Technique.Reflection);
                }
                else
                {
                    effect.SetTechnique(MyEffectTransparentGeometry.Technique.Unlit);
                }
            }
        }

        [Conditional("PARTICLE_PROFILING")]
        public static void StartParticleProfilingBlock(string name)
        {
            MyRender.GetRenderProfiler().StartProfilingBlock(name);
        }

        [Conditional("PARTICLE_PROFILING")]
        public static void EndParticleProfilingBlock()
        {
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion
    }
}