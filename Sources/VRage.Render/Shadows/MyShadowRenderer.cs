#region Using

using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using ParallelTasks;

using SharpDX;
using SharpDX.Direct3D9;
using VRage;

#endregion

namespace VRageRender.Shadows
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using MathHelper = VRageMath.MathHelper;
    using VRageRender.Graphics;
    using VRageRender.Effects;
    using VRageRender.Utils;
    using VRageRender.Textures;
    using VRageMath;
    using VRage.Utils;


    class MyShadowRenderer : MyShadowRendererBase, IWork
    {
        public static bool RespectCastShadowsFlags = false;

        #region Members

        public const int NumSplits = MyShadowConstants.NumSplits;
        public static readonly float SHADOW_MAX_OFFSET = 1000.0f; //how far we are going to render objects behind cascade box
        private int m_shadowMapCascadeSize;
        private Vector2 m_shadowMapCascadeSizeInv;

        public int ShadowMapCascadeSize
        {
            get
            {
                return m_shadowMapCascadeSize;
            }
            private set
            {
                m_shadowMapCascadeSize = value;
                m_shadowMapCascadeSizeInv.X = 1.0f / ((float)NumSplits * value);
                m_shadowMapCascadeSizeInv.Y = 1.0f / value;
            }
        }

        

        Vector3D[] m_frustumCornersVS = new Vector3D[8];
        Vector3D[] m_frustumCornersWS = new Vector3D[8];
        Vector3D[] m_frustumCornersLS = new Vector3D[8];
        Vector3[] m_farFrustumCornersVS = new Vector3[4];
        Vector3D[] m_splitFrustumCornersVS = new Vector3D[8];
        MyOrthographicCamera[] m_lightCameras = new MyOrthographicCamera[NumSplits];
        Matrix[] m_lightViewProjectionMatrices = new Matrix[NumSplits];
        Vector2[] m_lightClipPlanes = new Vector2[NumSplits];
        List<MyOcclusionQueryIssue>[] m_occlusionQueriesLists = new List<MyOcclusionQueryIssue>[NumSplits];
        float[] m_splitDepths = new float[NumSplits + 1];

        MyPerspectiveCamera m_camera = new MyPerspectiveCamera();

        public MyOcclusionQueryIssue[] m_cascadeQueries = new MyOcclusionQueryIssue[NumSplits];

        int m_frameIndex;
        bool[] m_skip = new bool[NumSplits];
        bool[] m_interleave = new bool[NumSplits];
        bool[] m_visibility = new bool[NumSplits];

        MyRenderTargets m_shadowRenderTarget;
        MyRenderTargets m_shadowDepthTarget;

        /// <summary>
        /// Event that occures when we want prepare shadows for draw
        /// </summary>
        public bool MultiThreaded = false;
        Task m_prepareForDrawTask;


        #endregion

        public MyShadowRenderer(int shadowMapSize, MyRenderTargets renderTarget, MyRenderTargets depthTarget, bool multiThreaded)
        {
            ShadowMapCascadeSize = shadowMapSize;
            m_shadowRenderTarget = renderTarget;
            m_shadowDepthTarget = depthTarget;

            for (int i = 0; i < NumSplits; i++)
            {
                m_lightCameras[i] = new MyOrthographicCamera(1, 1, 1, 10);
                // Occ queries for shadows are disabled, so save memory by commenting this
                //m_occlusionQueriesLists[i] = new List<MyOcclusionQueryIssue>(1024);
                m_cascadeQueries[i] = new MyOcclusionQueryIssue(null);
                m_visibility[i] = true;
            }

            MultiThreaded = multiThreaded;
        }

        public void ChangeSize(int newSize)
        {
            ShadowMapCascadeSize = newSize;
        }

        public void UpdateFrustumCorners()
        {
            //Set camera data
            m_camera.FarClip = 1000.0f;
            m_camera.NearClip = 1.0f;
            m_camera.AspectRatio = MyRenderCamera.AspectRatio;
            m_camera.FieldOfView = MyRenderCamera.FieldOfView;

            //camera.WorldMatrix = Matrix.CreateWorld(MyCamera.Position, MyCamera.ForwardVector, MyCamera.UpVector);
            m_camera.ViewMatrix = MyRenderCamera.ViewMatrix;
            m_camera.ProjectionMatrix = MyRenderCamera.ProjectionMatrix;

            // Get corners of the main camera's bounding frustum
            MatrixD cameraTransform, viewMatrix;
            m_camera.GetWorldMatrix(out cameraTransform);
            MyUtils.AssertIsValid(cameraTransform);
            m_camera.GetViewMatrix(out viewMatrix);
            m_camera.BoundingFrustum.GetCorners(m_frustumCornersWS);
            Vector3D.Transform(m_frustumCornersWS, ref viewMatrix, m_frustumCornersVS);
            for (int i = 0; i < 4; i++)
                m_farFrustumCornersVS[i] = (Vector3)m_frustumCornersVS[i + 4];
        }

        public void PrepareFrame()
        {
            m_frameIndex++;
            m_frameIndex %= NumSplits;

            // Need both interleave and skip (interleave is required for occ queries

			for (int cascadeIndex = 0; cascadeIndex < NumSplits; ++cascadeIndex)
			{
				m_skip[cascadeIndex] = m_interleave[cascadeIndex] = MyRender.Settings.ShadowCascadeFrozen[cascadeIndex];
			}
            m_skip[1] = m_interleave[1] = m_skip[1] || (MyRender.Settings.ShadowInterleaving && m_frameIndex % 2 != 0); // on frames 1, 3
            m_skip[2] = m_interleave[2] = m_skip[2] || (MyRender.Settings.ShadowInterleaving && m_frameIndex % 2 == 0); // on frames 0, 2
            m_skip[3] = m_interleave[3] = m_skip[3] || (MyRender.Settings.ShadowInterleaving && m_frameIndex % 2 == 0); // on frames 0, 2

            PrepareForDraw();
        }

        void PrepareForDraw()
        {
            if (MultiThreaded)
            {
                if (m_prepareForDrawTask.IsComplete)
                {
                    m_prepareForDrawTask = ParallelTasks.Parallel.Start(this);
                }
            }
        }
 
        public void DoWork()
        {
            PrepareCascadesForDraw();
        }

        public WorkOptions Options
        {
            get { return new WorkOptions() { MaximumThreads = 1 }; }
        }

        public void WaitUntilPrepareForDrawCompleted()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("WaitUntilPrepareForDrawCompleted");

            m_prepareForDrawTask.Wait();

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        static int HiddenResolvedObjects = 0;

        void PrepareCascadesForDraw()
        {
            if (MyRender.Sun.Direction == Vector3.Zero)
                return;

            MyRender.GetRenderProfiler().StartProfilingBlock("UpdateFrustums");

            UpdateFrustums();

            MyRender.GetRenderProfiler().EndProfilingBlock();

            // Set casting shadows geometry

            MyRender.GetRenderProfiler().StartProfilingBlock("update entities");

            int frustumIndex = 0;

            foreach (MyOrthographicCamera lightCamera in m_lightCameras)
            {
                if (m_skip[frustumIndex])
                {
                    frustumIndex++;
                    continue;
                }

                m_renderElementsForShadows.Clear();
                m_transparentRenderElementsForShadows.Clear();
                m_castingRenderObjectsUnique.Clear();

                MyRender.GetRenderProfiler().StartProfilingBlock("OverlapAllBoundingBox");
         

                var castersBox = lightCamera.BoundingBox; //Cannot use unscaled - incorrect result because of different cascade viewport size
                var castersFrustum = lightCamera.BoundingFrustum;//Cannot use unscaled - incorrect result because of different cascade viewport size
                int occludedItemsStats = 0; //always 0 for shadows
                //MyRender.PrepareEntitiesForDraw(ref castersBox, (MyOcclusionQueryID)(frustumIndex + 1), m_castingRenderObjects, m_occlusionQueriesLists[frustumIndex], ref MyPerformanceCounter.PerCameraDrawWrite.ShadowEntitiesOccluded[frustumIndex]);
                MyRender.PrepareEntitiesForDraw(ref castersFrustum, lightCamera.Position, (MyOcclusionQueryID)(frustumIndex + 1), m_castingRenderObjects, null, m_castingCullObjects, m_castingManualCullObjects, null, ref occludedItemsStats);

                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("m_castingRenderObjects");

                int c = 0;
                int skipped = 0;
                     
                while (c < m_castingRenderObjects.Count)
                {
                    MyRenderObject renderObject = (MyRenderObject)m_castingRenderObjects[c];

                    if (RespectCastShadowsFlags)
                    {
                       // System.Diagnostics.Debug.Assert(!(entity is MyDummyPoint) && !(entity is AppCode.Game.Entities.WayPoints.MyWayPoint));

                        if ((renderObject.ShadowCastUpdateInterval > 0) && ((MyRender.RenderCounter % renderObject.ShadowCastUpdateInterval) == 0))
                        {
                            renderObject.NeedsResolveCastShadow = true;
                            //We have to leave last value, because true when not casting shadow make radiation to ship
                           // renderObject.CastShadow = true;
                        }

                        if (renderObject.NeedsResolveCastShadow)
                        { //Resolve raycast to sun
                            if (renderObject.CastShadowJob == null)
                            {
                                renderObject.CastShadowJob = new MyCastShadowJob(renderObject);
                                renderObject.CastShadowTask = ParallelTasks.Parallel.Start(renderObject.CastShadowJob);
                            }
                            else
                                if (renderObject.CastShadowTask.IsComplete)
                                {
                                    renderObject.CastShadows = renderObject.CastShadowJob.VisibleFromSun;
                                    renderObject.CastShadowTask = new ParallelTasks.Task();
                                    renderObject.CastShadowJob = null;
                                    renderObject.NeedsResolveCastShadow = false;

                                    if (renderObject.CastShadows == false)
                                        HiddenResolvedObjects++;
                                }
                        } 

                        if (!renderObject.NeedsResolveCastShadow && !renderObject.CastShadows)
                        {
                            if (renderObject is MyRenderVoxelCell)
                            {
                            }

                            m_castingRenderObjects.RemoveAtFast(c);
                            skipped++;
                            continue;
                        }
                    }
                    else
                    {
                        renderObject.NeedsResolveCastShadow = true;
                    }    
                              
                    if (!m_castingRenderObjectsUnique.Contains(renderObject))
                    {
                        m_castingRenderObjectsUnique.Add(renderObject);

                        if (frustumIndex < MyRenderConstants.RenderQualityProfile.ShadowCascadeLODTreshold)
                        {
                           renderObject.GetRenderElementsForShadowmap(MyLodTypeEnum.LOD0, m_renderElementsForShadows, m_transparentRenderElementsForShadows);
                        }
                        else
                        {
                           renderObject.GetRenderElementsForShadowmap(MyLodTypeEnum.LOD1, m_renderElementsForShadows, m_transparentRenderElementsForShadows);
                        }
                    }

                    c++;
                }      

                MyRender.GetRenderProfiler().EndProfilingBlock();
                           
                //Sorting VBs to minimize VB switches
                m_renderElementsForShadows.Sort(m_shadowElementsComparer);

                lightCamera.CastingRenderElements = m_renderElementsForShadows;

                MyPerformanceCounter.PerCameraDrawWrite.RenderElementsInShadows += m_renderElementsForShadows.Count;

                frustumIndex++;
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        void UpdateFrustums()
        {
            //Calculate cascade splits
            m_splitDepths[0] = 1;
            m_splitDepths[1] = 10;
            m_splitDepths[2] = 30;
            m_splitDepths[3] = 50;
            m_splitDepths[4] = 300;

            // Calculate data to each split of the cascade
            for (int i = 0; i < NumSplits; i++)
            {
                if (m_skip[i])
                {
                    continue;
                }

                float minZ = m_splitDepths[i];
                float maxZ = m_splitDepths[i + 1];

                if (CalculateFrustum(m_lightCameras[i], m_camera, minZ, maxZ) == null)
                {
                    //Shadow map caching
                    //m_skip[i] = true;                   
                }
            }

            // We'll use these clip planes to determine which split a pixel belongs to
            for (int i = 0; i < NumSplits; i++)
            {            
                m_lightClipPlanes[i].X = -m_splitDepths[i];
                m_lightClipPlanes[i].Y = -m_splitDepths[i + 1];

                MatrixD lv;
                m_lightCameras[i].GetViewProjMatrix(out lv);
                MatrixD lv2 = MatrixD.CreateWorld(MyRenderCamera.Position, MyRenderCamera.ForwardVector, MyRenderCamera.UpVector) * lv;
                m_lightViewProjectionMatrices[i] = (Matrix)lv2;
                //m_lightViewProjectionMatrices[i] = Matrix.CreateWorld((Vector3)MyRenderCamera.Position, MyRenderCamera.ForwardVector, MyRenderCamera.UpVector) * m_lightViewProjectionMatrices[i];
            }
        }

        /// <summary>
        /// Renders a list of models to the shadow map, and returns a surface 
        /// containing the shadow occlusion factor
        /// </summary>
        public void Render()
        {
            if (MyRender.Sun.Direction == Vector3.Zero)
                return;

            MyRender.GetRenderProfiler().StartProfilingBlock("MyShadowRenderer::Render");

            if (MultiThreaded)
            {
                WaitUntilPrepareForDrawCompleted();
            }
            else
            {
                //PrepareFrame();
                PrepareCascadesForDraw();
            }

            IssueQueriesForCascades();

            MyRender.GetRenderProfiler().StartProfilingBlock("Set & Clear RT");

            // Set our targets
            MyRender.SetRenderTarget(MyRender.GetRenderTarget(m_shadowRenderTarget), MyRender.GetRenderTarget(m_shadowDepthTarget));
            MyRender.GraphicsDevice.Clear(ClearFlags.ZBuffer, new ColorBGRA(0.0f), 1.0f, 0);

            DepthStencilState.Default.Apply();
            RasterizerState.CullNone.Apply();
            //RasterizerState.CullCounterClockwise.Apply();
            BlendState.Opaque.Apply();

            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.GetRenderProfiler().StartProfilingBlock("Render 4 ShadowMaps");

            // Render our scene geometry to each split of the cascade
            for (int i = 0; i < NumSplits; i++)
            {
                if (m_skip[i]) continue;
                if (!m_visibility[i]) continue;

                RenderShadowMap(i);
                //IssueQueriesForShadowMap(i);
            }

            //Restore viewport 
            MyRenderCamera.UpdateCamera();
            MyRender.SetDeviceViewport(MyRenderCamera.Viewport);

            MyRender.GetRenderProfiler().EndProfilingBlock();

            //   MyGuiManager.TakeScreenshot();
            MyRender.TakeScreenshot("ShadowMap", MyRender.GetRenderTarget(m_shadowRenderTarget), MyEffectScreenshot.ScreenshotTechniqueEnum.Color);

          // Texture.ToFile(MyRender.GetRenderTarget(m_shadowRenderTarget), "c:\\test.dds", ImageFileFormat.Dds);

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        /// <summary>
        /// Determines the size of the frustum needed to cover the viewable area,
        /// then creates an appropriate orthographic projection.
        /// </summary>
        /// <param name="light">The directional light to use</param>
        /// <param name="mainCamera">The camera viewing the scene</param>
        protected MyOrthographicCamera CalculateFrustum(MyOrthographicCamera lightCamera, MyPerspectiveCamera mainCamera, float minZ, float maxZ)
        {
            MatrixD cameraMatrix;
            mainCamera.GetWorldMatrix(out cameraMatrix);

            MatrixD cameraMatrixAtZero = cameraMatrix;
            cameraMatrixAtZero.Translation = Vector3D.Zero;

       
            for (int i = 0; i < 4; i++)
                m_splitFrustumCornersVS[i] = m_frustumCornersVS[i + 4] * (minZ / mainCamera.FarClip);

            for (int i = 4; i < 8; i++)
                m_splitFrustumCornersVS[i] = m_frustumCornersVS[i] * (maxZ / mainCamera.FarClip);

            Vector3D.Transform(m_splitFrustumCornersVS, ref cameraMatrix, m_frustumCornersWS);

            // Position the shadow-caster camera so that it's looking at the centroid,
            // and backed up in the direction of the sunlight
            Vector3 viewUp = Math.Abs(Vector3.UnitY.Dot(MyRender.Sun.Direction)) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
            MatrixD viewMatrix = MatrixD.CreateLookAt(MyRenderCamera.Position - (MyRender.Sun.Direction * (float)mainCamera.FarClip), MyRenderCamera.Position, viewUp);

            // Determine the position of the frustum corners in light space
            Vector3D.Transform(m_frustumCornersWS, ref viewMatrix, m_frustumCornersLS);

            // Calculate an orthographic projection by sizing a bounding box
            // to the frustum coordinates in light space
            Vector3D mins = m_frustumCornersLS[0];
            Vector3D maxes = m_frustumCornersLS[0];
            for (int i = 0; i < 8; i++)
            {
                if (m_frustumCornersLS[i].X > maxes.X)
                    maxes.X = m_frustumCornersLS[i].X;
                else if (m_frustumCornersLS[i].X < mins.X)
                    mins.X = m_frustumCornersLS[i].X;
                if (m_frustumCornersLS[i].Y > maxes.Y)
                    maxes.Y = m_frustumCornersLS[i].Y;
                else if (m_frustumCornersLS[i].Y < mins.Y)
                    mins.Y = m_frustumCornersLS[i].Y;
                if (m_frustumCornersLS[i].Z > maxes.Z)
                    maxes.Z = m_frustumCornersLS[i].Z;
                else if (m_frustumCornersLS[i].Z < mins.Z)
                    mins.Z = m_frustumCornersLS[i].Z;
            }

            // Update an orthographic camera for collision detection
            lightCamera.UpdateUnscaled(mins.X, maxes.X, mins.Y, maxes.Y, -maxes.Z - SHADOW_MAX_OFFSET, -mins.Z);
            lightCamera.SetViewMatrixUnscaled(ref viewMatrix);

            // We snap the camera to 1 pixel increments so that moving the camera does not cause the shadows to jitter.
            // This is a matter of integer dividing by the world space size of a texel
            var diagonalLength = (m_frustumCornersWS[0] - m_frustumCornersWS[6]).Length();
                                                                                     
            //Make bigger box - ensure rotation and movement stabilization
            diagonalLength = MathHelper.GetNearestBiggerPowerOfTwo(diagonalLength);

            var worldsUnitsPerTexel = diagonalLength / (float)ShadowMapCascadeSize;

            Vector3D vBorderOffset = (new Vector3D(diagonalLength, diagonalLength, diagonalLength) - (maxes - mins)) * 0.5f;
            maxes += vBorderOffset;
            mins -= vBorderOffset;

            mins /= worldsUnitsPerTexel;
            mins.X = Math.Floor(mins.X);
            mins.Y = Math.Floor(mins.Y);
            mins.Z = Math.Floor(mins.Z);
            mins *= worldsUnitsPerTexel;

            maxes /= worldsUnitsPerTexel;
            maxes.X = Math.Floor(maxes.X);
            maxes.Y = Math.Floor(maxes.Y);
            maxes.Z = Math.Floor(maxes.Z);
            maxes *= worldsUnitsPerTexel;


            /*
            Matrix proj;
            Matrix.CreateOrthographicOffCenter(mins.X, maxes.X, mins.Y, maxes.Y, -maxes.Z - SHADOW_MAX_OFFSET, -mins.Z, out proj);
            
            if (MyUtils.IsEqual(lightCamera.ProjectionMatrix, proj))
            {   //cache
                return null;
            } */


            // Update an orthographic camera for use as a shadow caster
            lightCamera.Update(mins.X, maxes.X, mins.Y, maxes.Y, -maxes.Z - SHADOW_MAX_OFFSET, -mins.Z);


            lightCamera.SetViewMatrix(ref viewMatrix);

            return lightCamera;
        }


        void PrepareViewportForCascade(int splitIndex)
        {
            // Set the viewport for the current split   
            Viewport splitViewport = new Viewport();
            splitViewport.MinDepth = 0;
            splitViewport.MaxDepth = 1;
            splitViewport.Width = ShadowMapCascadeSize;
            splitViewport.Height = ShadowMapCascadeSize;
            splitViewport.X = splitIndex * ShadowMapCascadeSize;
            splitViewport.Y = 0;
            //Must be here because otherwise it crasher after resolution change
            MyRender.SetDeviceViewport(splitViewport);
        }


        /// <summary>
        /// Renders the shadow map using the orthographic camera created in
        /// CalculateFrustum.
        /// </summary>
        /// <param name="modelList">The list of models to be rendered</param>        
        protected void RenderShadowMap(int splitIndex)
        {
            PrepareViewportForCascade(splitIndex);

            // Set up the effect
            MyEffectShadowMap shadowMapEffect = MyRender.GetEffect(MyEffects.ShadowMap) as Effects.MyEffectShadowMap;
            shadowMapEffect.SetDitheringTexture((SharpDX.Direct3D9.Texture)MyTextureManager.GetTexture<MyTexture2D>(@"Textures\Models\Dither.png"));
            shadowMapEffect.SetHalfPixel(MyShadowRenderer.NumSplits * MyRender.GetShadowCascadeSize(), MyRender.GetShadowCascadeSize());

            // Clear shadow map
            shadowMapEffect.SetTechnique(MyEffectShadowMap.ShadowTechnique.Clear);
            
            MyRender.GetFullscreenQuad().Draw(shadowMapEffect);

            shadowMapEffect.SetViewProjMatrix((Matrix)m_lightCameras[splitIndex].ViewProjMatrixAtZero);

            MyRender.GetRenderProfiler().StartProfilingBlock("draw elements");
            // Draw the models
            DrawElements(m_lightCameras[splitIndex].CastingRenderElements, shadowMapEffect, true, m_lightCameras[splitIndex].WorldMatrix.Translation, splitIndex, true);

            m_lightCameras[splitIndex].CastingRenderElements.Clear();

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        Vector3[] frustum = new Vector3[8];
        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);

        public void IssueQueriesForCascades()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyShadowRenderer::IssueQueriesForCascades");

            bool useOccQueries = MyRender.EnableHWOcclusionQueriesForShadows && MyRender.CurrentRenderSetup.EnableOcclusionQueries;

            if (!useOccQueries)
            {
                for (int i = 0; i < NumSplits; i++)
                {
                    m_visibility[i] = true;
                }
                MyRender.GetRenderProfiler().EndProfilingBlock();
                return;
            }
                /*
            Device device = MyRender.GraphicsDevice;
            BlendState oldBlendState = BlendState.Current;
            MyStateObjects.DisabledColorChannels_BlendState.Apply();

            //generate and draw bounding box of our renderCell in occlusion query 
            //device.BlendState = MyStateObjects.DisabledColorChannels_BlendState;
            MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary0), null);

            Vector3 campos = MyRenderCamera.Position;

            RasterizerState.CullNone.Apply();

            if (MyRenderConstants.RenderQualityProfile.ForwardRender)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            for (int i = 1; i < NumSplits; i++)
            {
                if (m_interleave[i]) continue;

                MyPerformanceCounter.PerCameraDrawWrite.QueriesCount++;

                var queryIssue = m_cascadeQueries[i];

                if (queryIssue.OcclusionQueryIssued)
                {
                    if (queryIssue.OcclusionQuery.IsComplete)
                    {
                        m_visibility[i] = queryIssue.OcclusionQuery.PixelCount > 0;
                        queryIssue.OcclusionQueryIssued = false;
                    }
                    continue;
                }

                queryIssue.OcclusionQueryIssued = true;

                if (queryIssue.OcclusionQuery == null) 
                    queryIssue.OcclusionQuery = new MyOcclusionQuery(device);

                cameraFrustum.Matrix = m_lightCameras[i].CameraSubfrustum;

                cameraFrustum.GetCorners(frustum);

                var tmp = frustum[3];
                frustum[3] = frustum[2];
                frustum[2] = tmp;

                queryIssue.OcclusionQuery.Begin();
                MySimpleObjectDraw.OcclusionPlaneDraw(frustum);
                queryIssue.OcclusionQuery.End();
            }

            oldBlendState.Apply();
                        */
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }


        public void SetupShadowBaseEffect(MyEffectShadowBase effect)
        {
            effect.SetLightViewProjMatrices(m_lightViewProjectionMatrices);
            effect.SetClipPlanes(m_lightClipPlanes);

            effect.SetShadowMapSize(new Vector4(ShadowMapCascadeSize * NumSplits, ShadowMapCascadeSize, m_shadowMapCascadeSizeInv.X, m_shadowMapCascadeSizeInv.Y));
            effect.SetShadowMap(MyRender.GetRenderTarget(m_shadowRenderTarget));
        }

        public Vector3[] GetFrustumCorners()
        {
            return m_farFrustumCornersVS;
        }

        void DebugDrawFrustum(MatrixD camera, Color color)
        {
            BoundingFrustumD frustum = new BoundingFrustumD(camera);
            MyDebugDraw.DrawBoundingFrustum(frustum, color);
        }

        static readonly Color[] frustumColors = new Color[]
        {
            new Color(1.0f,0.0f,0.0f),
            new Color(0.0f,1.0f,0.0f),
            new Color(0.0f,0.0f,1.0f),
            new Color(1.0f,1.0f,0.0f),
        };

        Matrix[] frustumMatrices = new Matrix[4];
        //Matrix mainCamera;

        public void DebugDraw()
        {        
            return;
            //MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            //for (int i = 0; i < NumSplits; i++)
            //{
            //    cameraFrustum.Matrix = m_lightCameras[i].CameraSubfrustum;
            //    cameraFrustum.GetCorners(frustum);

            //    var tmp = frustum[3];
            //    frustum[3] = frustum[2];
            //    frustum[2] = tmp;

            //    //MyDebugDraw.DrawBoundingFrustum(cameraFrustum, frustumColors[i]);
            //    MyDebugDraw.OcclusionPlaneDraw(frustum);
            //    MyDebugDraw.DrawTriangle(frustum[0], frustum[1], frustum[2], frustumColors[i], frustumColors[i], frustumColors[i], false, false);
            //    MyDebugDraw.DrawTriangle(frustum[1], frustum[2], frustum[3], frustumColors[i], frustumColors[i], frustumColors[i], false, false);
            //}

            //return;
                   
            //bool update = false;

            //if (MyRender.CurrentRenderSetup.CallerID.Value == MyRenderCallerEnum.Main)
            //{
            //    if (update)
            //    {
            //        mainCamera = MyRenderCamera.GetBoundingFrustum().Matrix;
            //    }

            //    for (int i = 0; i < NumSplits; i++)
            //    {
            //        if (update)
            //        {
            //            Vector4 c = frustumColors[i].ToVector4();

            //            //MyDebugDraw.DrawAABBLowRes(ref box, ref c, 1);
            //            //BoundingFrustum bf = new BoundingFrustum();

            //            //frustumMatrices[i] = m_lightCameras[i].CameraSubfrustum;
            //            frustumMatrices[i] = m_lightCameras[i].BoundingFrustum.Matrix;
            //        }

            //        DebugDrawFrustum(frustumMatrices[i], frustumColors[i]);


            //        Vector4 cc = frustumColors[i].ToVector4();

            //        BoundingFrustum frma = new BoundingFrustum(frustumMatrices[i]);
            //        int occludedItemsStats = 0;
            //        MyRender.PrepareEntitiesForDraw(ref frma, Vector3.Zero, (MyOcclusionQueryID)(i + 1), m_castingRenderObjects, null, m_castingCullObjects, m_castingManualCullObjects, m_occlusionQueriesLists[i], ref occludedItemsStats);
            //        BoundingBox aabbFr = BoundingBox.CreateInvalid();
            //        foreach (MyRenderObject ro in m_castingRenderObjects)
            //        {
            //            BoundingBox vv = ro.WorldAABB;
            //            //MyDebugDraw.DrawAABBLowRes(ref vv, ref cc, 1);
            //            aabbFr = aabbFr.Include(ref vv);
            //        }


            //        //MyDebugDraw.DrawAABBLowRes(ref aabbFr, ref cc, 1);
            //    }

            //    // DebugDrawFrustum(mainCamera, new Color(1.0f, 1.0f, 1.0f));
            //}

        }
    }
}
