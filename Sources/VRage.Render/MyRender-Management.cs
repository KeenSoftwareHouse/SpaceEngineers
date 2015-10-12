#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Threading;
using VRageMath;
using VRageRender.Graphics;
using VRageRender.Utils;

using VRage.Utils;
using ParallelTasks;

using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace VRageRender
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


    internal static partial class MyRender
    {

        #region Render object management

        internal static void PushRenderSetupAndApply(MyRenderSetup setup, ref MyRenderSetup storePreviousSetup)
        {
            PushRenderSetup(setup);
            ApplySetupStack(storePreviousSetup);
        }

        internal static void PopRenderSetupAndRevert(MyRenderSetup previousSetup)
        {
            PopRenderSetup();
            ApplySetup(previousSetup);
        }

        internal static void PushRenderSetup(MyRenderSetup setup)
        {
            m_renderSetupStack.Add(setup);
        }

        internal static void PopRenderSetup()
        {
            m_renderSetupStack.RemoveAt(m_renderSetupStack.Count - 1);
        }

        internal static void ApplyBackupSetup()
        {
            ApplySetup(m_backupSetup);
        }

        internal static void ApplySetup(MyRenderSetup setup)
        {
            if (setup.CameraPosition.HasValue)
            {
                MyRenderCamera.SetPosition(setup.CameraPosition.Value);
            }
            if (setup.AspectRatio.HasValue)
            {
                MyRenderCamera.AspectRatio = setup.AspectRatio.Value;
            }
            if (setup.Fov.HasValue && MyRenderCamera.FieldOfView != setup.Fov.Value)
            {
                MyRenderCamera.FieldOfView = setup.Fov.Value;
            }
            if (setup.ViewMatrix.HasValue && setup.ViewMatrix != MatrixD.Zero)
            {
                MyRenderCamera.SetViewMatrix(setup.ViewMatrix.Value, null);
            }
            if (setup.Viewport.HasValue)
            {
                MyRenderCamera.Viewport = setup.Viewport.Value;
            }
            if (setup.Fov.HasValue && MyRenderCamera.FieldOfView != setup.Fov.Value)
            {
                // When custom FOV set, zoom will be disabled
                MyRenderCamera.ChangeFov(setup.Fov.Value);
            }
            if (setup.ProjectionMatrix.HasValue)
            {
                MyRenderCamera.SetCustomProjection(setup.ProjectionMatrix.Value);
            }

            if (setup.RenderTargets != null && setup.RenderTargets.Length > 0)
            {
                Texture rt = setup.RenderTargets[0];
                if (rt != null)
                {
                    MyRender.SetRenderTarget(rt, setup.DepthTarget);
                }
                else
                    MyRender.SetRenderTarget(null, null);
            }
            else
                MyRender.SetRenderTarget(null, null);

            m_currentSetup.RenderTargets = setup.RenderTargets;

            MyRenderCamera.UpdateCamera();
            MyRender.SetDeviceViewport(MyRenderCamera.Viewport);
        }

        private static void ApplySetupStack(MyRenderSetup storeBackup)
        {
            if (storeBackup != null)
            {
                if (MyRenderCamera.ViewMatrix.Left != Vector3D.Zero)
                {
                    storeBackup.CameraPosition = MyRenderCamera.Position;
                    storeBackup.ViewMatrix = MyRenderCamera.ViewMatrix;
                }

                storeBackup.ProjectionMatrix = MyRenderCamera.ProjectionMatrix;
                storeBackup.AspectRatio = MyRenderCamera.AspectRatio;
                storeBackup.Fov = MyRenderCamera.FieldOfView;
                storeBackup.RenderTargets = m_currentSetup.RenderTargets;
                storeBackup.Viewport = MyRenderCamera.Viewport;
            }

            if (MyRenderCamera.ViewMatrix.Left != Vector3D.Zero)
            {
                m_currentSetup.ViewMatrix = MyRenderCamera.ViewMatrix;
            }

            // Set default values
            m_currentSetup.CallerID = MyRenderCallerEnum.Main;

            m_currentSetup.RenderTargets = null;

            m_currentSetup.CameraPosition = MyRenderCamera.Position;
            m_currentSetup.AspectRatio = MyRenderCamera.AspectRatio;
            m_currentSetup.Fov = null;
            m_currentSetup.Viewport = MyRenderCamera.Viewport;
            m_currentSetup.ProjectionMatrix = null;
            m_currentSetup.FogMultiplierMult = 1;
            m_currentSetup.DepthToAlpha = false;
            m_currentSetup.DepthCopy = false;

            m_currentSetup.LodTransitionNear = MyRenderCamera.GetLodTransitionDistanceNear();
            m_currentSetup.LodTransitionFar = MyRenderCamera.GetLodTransitionDistanceFar();
            m_currentSetup.LodTransitionBackgroundStart = MyRenderCamera.GetLodTransitionDistanceBackgroundStart();
            m_currentSetup.LodTransitionBackgroundEnd = MyRenderCamera.GetLodTransitionDistanceBackgroundEnd();

            m_currentSetup.EnableHDR = true;
            m_currentSetup.EnableLights = true;
            m_currentSetup.EnableSun = true;
            m_currentSetup.ShadowRenderer = m_shadowRenderer; // Default shadow render
            m_currentSetup.EnableShadowInterleaving = Settings.ShadowInterleaving;
            m_currentSetup.EnableSmallLights = true;
            m_currentSetup.EnableSmallLightShadows = true;
            m_currentSetup.EnableDebugHelpers = true;
            m_currentSetup.EnableEnvironmentMapping = true;
            m_currentSetup.EnableNear = true;
            m_currentSetup.EnableOcclusionQueries = true;

            m_currentSetup.BackgroundColor = null;

            m_currentSetup.EnabledModules = null;
            m_currentSetup.EnabledPostprocesses = null;
            m_currentSetup.EnabledRenderStages = null;

            //m_currentSetup.LightsToUse = null;
            m_currentSetup.RenderElementsToDraw = null;
            m_currentSetup.TransparentRenderElementsToDraw = null;

            foreach (var setup in m_renderSetupStack)
            {
                AggregateSetup(setup);
            }

            ApplySetup(m_currentSetup);
        }

        private static void AggregateSetup(MyRenderSetup setup)
        {
            if (setup.CallerID != null)
            {
                m_currentSetup.CallerID = setup.CallerID;
            }
            else
            {
                Debug.Assert(false, "CallerID has to be set in render setup.");
            }

            if (setup.RenderTargets != null)
            {
                m_currentSetup.RenderTargets = setup.RenderTargets;
            }

            if (setup.CameraPosition.HasValue)
            {
                m_currentSetup.CameraPosition = setup.CameraPosition;
            }

            if (setup.ViewMatrix.HasValue)
            {
                m_currentSetup.ViewMatrix = setup.ViewMatrix;
            }

            if (setup.ProjectionMatrix.HasValue)
            {
                m_currentSetup.ProjectionMatrix = setup.ProjectionMatrix;
            }

            if (setup.Fov.HasValue)
            {
                m_currentSetup.Fov = setup.Fov;
            }

            if (setup.AspectRatio.HasValue)
            {
                m_currentSetup.AspectRatio = setup.AspectRatio;
            }

            if (setup.Viewport.HasValue)
            {
                m_currentSetup.Viewport = setup.Viewport;
            }

            if (setup.LodTransitionNear.HasValue)
            {
                m_currentSetup.LodTransitionNear = setup.LodTransitionNear;
            }

            if (setup.LodTransitionFar.HasValue)
            {
                m_currentSetup.LodTransitionFar = setup.LodTransitionFar;
            }

            if (setup.LodTransitionBackgroundStart.HasValue)
            {
                m_currentSetup.LodTransitionBackgroundStart = setup.LodTransitionBackgroundStart;
            }

            if (setup.LodTransitionBackgroundEnd.HasValue)
            {
                m_currentSetup.LodTransitionBackgroundEnd = setup.LodTransitionBackgroundEnd;
            }

            if (setup.EnableHDR.HasValue)
            {
                m_currentSetup.EnableHDR = setup.EnableHDR;
            }

            if (setup.EnableLights.HasValue)
            {
                m_currentSetup.EnableLights = setup.EnableLights;
            }

            if (setup.EnableSun.HasValue)
            {
                m_currentSetup.EnableSun = setup.EnableSun;
            }

            // Special case...when no shadow render specified, no shadows are rendered
            m_currentSetup.ShadowRenderer = setup.ShadowRenderer;
            m_currentSetup.FogMultiplierMult = setup.FogMultiplierMult;
            m_currentSetup.DepthToAlpha = setup.DepthToAlpha;
            m_currentSetup.DepthCopy = setup.DepthCopy;

            if (setup.EnableShadowInterleaving.HasValue)
            {
                m_currentSetup.EnableShadowInterleaving = setup.EnableShadowInterleaving;
            }

            if (setup.EnableSmallLights.HasValue)
            {
                m_currentSetup.EnableSmallLights = setup.EnableSmallLights;
            }

            if (setup.EnableSmallLightShadows.HasValue)
            {
                m_currentSetup.EnableSmallLightShadows = setup.EnableSmallLightShadows;
            }

            if (setup.EnableDebugHelpers.HasValue)
            {
                m_currentSetup.EnableDebugHelpers = setup.EnableDebugHelpers;
            }

            if (setup.EnableEnvironmentMapping.HasValue)
            {
                m_currentSetup.EnableEnvironmentMapping = setup.EnableEnvironmentMapping;
            }

            if (setup.EnableNear.HasValue)
            {
                m_currentSetup.EnableNear = setup.EnableNear;
            }

            if (setup.BackgroundColor.HasValue)
            {
                m_currentSetup.BackgroundColor = setup.BackgroundColor;
            }

            if (setup.RenderElementsToDraw != null)
            {
                m_currentSetup.RenderElementsToDraw = setup.RenderElementsToDraw;
            }

            if (setup.TransparentRenderElementsToDraw != null)
            {
                m_currentSetup.TransparentRenderElementsToDraw = setup.TransparentRenderElementsToDraw;
            }

            m_currentSetup.EnableOcclusionQueries = setup.EnableOcclusionQueries;

            if (setup.EnabledModules != null)
            {
                if (m_currentSetup.EnabledModules == null)
                {
                    m_currentSetup.EnabledModules = setup.EnabledModules;
                }
                else
                {
                    m_currentSetup.EnabledModules.IntersectWith(setup.EnabledModules);
                }
            }

            if (setup.EnabledPostprocesses != null)
            {
                if (m_currentSetup.EnabledPostprocesses == null)
                {
                    m_currentSetup.EnabledPostprocesses = setup.EnabledPostprocesses;
                }
                else
                {
                    m_currentSetup.EnabledPostprocesses.IntersectWith(setup.EnabledPostprocesses);
                }
            }

            if (setup.EnabledRenderStages != null)
            {
                if (m_currentSetup.EnabledRenderStages == null)
                {
                    m_currentSetup.EnabledRenderStages = setup.EnabledRenderStages;
                }
                else
                {
                    m_currentSetup.EnabledRenderStages.IntersectWith(setup.EnabledRenderStages);
                }
            }

            //m_currentSetup.RenderTargets = setup.RenderTargets;
        }

        #endregion

        #region Prunning structure

        static int m_renderObjectIncrementalCounter = 0;

        internal static void AddRenderObjectFromProxy(MyRenderObject renderObject, bool rebalance = true)
        {
            AddRenderObject(renderObject, rebalance);
            m_renderObjects.Add(renderObject.ID, renderObject);
        }


        internal static void AddRenderObject(MyRenderObject renderObject, bool rebalance = true)
        {
            if (renderObject is MyManualCullableRenderObject)
            {
                var boundingBox = renderObject.WorldAABB;
                renderObject.ProxyData = m_manualCullingStructure.AddProxy(ref boundingBox, renderObject, 0, rebalance);

                MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                System.Diagnostics.Debug.Assert(cullableObject.GetQuery(MyOcclusionQueryID.MAIN_RENDER).OcclusionQuery == null);
                cullableObject.LoadContent();
                cullableObject.RenderCounter = m_renderObjectIncrementalCounter % OCCLUSION_INTERVAL;

                AddShadowRenderObject(renderObject, rebalance);
                return;
            }
         
            if (renderObject.NearFlag && !m_nearObjects.Contains(renderObject))
            {
                m_nearObjects.Add(renderObject);
            }
            else if (renderObject.ProxyData == MyElement.PROXY_UNASSIGNED)
            {
                var aabb = renderObject.WorldAABB;
                renderObject.SetDirty();

                if (renderObject is MyCullableRenderObject)
                {
                    MyCullableRenderObject cullableObject = renderObject as MyCullableRenderObject;

                    renderObject.ProxyData = m_cullingStructure.AddProxy(ref aabb, renderObject, 0);

                    //Move all existing included proxies to cull objects
                    m_prunningStructure.OverlapAllBoundingBox(ref aabb, m_renderObjectListForDraw);

                    foreach (MyRenderObject ro in m_renderObjectListForDraw)
                    {
                        System.Diagnostics.Debug.Assert(!(ro is MyCullableRenderObject));
                        Debug.Assert(!ro.NearFlag);

                        var roAABB = ro.WorldAABB;

                        if (ro.CullObject == null && aabb.Contains(roAABB) == VRageMath.ContainmentType.Contains)
                        {
                            RemoveRenderObject(ro, false);
                            ro.ProxyData = cullableObject.CulledObjects.AddProxy(ref roAABB, ro, 0);
                            cullableObject.EntitiesContained++;
                            ro.CullObject = cullableObject;
                        }
                    }

                    System.Diagnostics.Debug.Assert(cullableObject.GetQuery(MyOcclusionQueryID.MAIN_RENDER).OcclusionQuery == null);
                    cullableObject.LoadContent();
                    cullableObject.RenderCounter = m_renderObjectIncrementalCounter % OCCLUSION_INTERVAL;
                    m_renderObjectIncrementalCounter++;
                }
                else
                {
                    GetRenderProfiler().StartProfilingBlock("Overlap");
                    //find potential cull objects and move render object to it if it is fully included
                    m_cullingStructure.OverlapAllBoundingBox(ref aabb, m_cullObjectListForDraw);
                    GetRenderProfiler().EndProfilingBlock();
                    bool contained = false;
                    MyCullableRenderObject mostSuitableCO = null;
                    double minVolume = double.MaxValue;
                    foreach (MyCullableRenderObject co in m_cullObjectListForDraw)
                    {
                        if (co.WorldAABB.Contains(aabb) == VRageMath.ContainmentType.Contains)
                        {
                            var volume = co.WorldAABB.Volume;
                            if (volume < minVolume)
                            {
                                minVolume = volume;
                                mostSuitableCO = co;
                            }
                        }
                    }

                    {
                        if (mostSuitableCO != null)
                        {
                            GetRenderProfiler().StartProfilingBlock("AddProxy");
                            renderObject.ProxyData = mostSuitableCO.CulledObjects.AddProxy(ref aabb, renderObject, 0, rebalance);
                            GetRenderProfiler().EndProfilingBlock();
                            mostSuitableCO.EntitiesContained++;
                            renderObject.CullObject = mostSuitableCO;
                            contained = true;
                        }

                        if (!contained)
                        {
                            renderObject.ProxyData = m_prunningStructure.AddProxy(ref aabb, renderObject, 0, rebalance);
                            renderObject.CullObject = null;
                        }

                        if (renderObject.CastShadows)
                        {
                            AddShadowRenderObject(renderObject, rebalance);
                        }
                    }
                    if (renderObject is IMyBackgroundDrawableRenderObject)
                    {
                        (renderObject as IMyBackgroundDrawableRenderObject).BackgroundProxyData =  m_farObjectsPrunningStructure.AddProxy(ref aabb, renderObject, 0, rebalance);
                    }                 
                }
            }
        }

        internal static void AddShadowRenderObject(MyRenderObject renderObject, bool rebalance = true)
        {
            if (renderObject.ShadowProxyData != MyElement.PROXY_UNASSIGNED)
                RemoveShadowRenderObject(renderObject);

            if (renderObject.ShadowProxyData == MyElement.PROXY_UNASSIGNED && renderObject.CastShadows)
            {
                var aabb = renderObject.WorldAABB;
                renderObject.SetDirty();

                renderObject.ShadowProxyData = m_shadowPrunningStructure.AddProxy(ref aabb, renderObject, 0, rebalance);
            }
        }

        internal static void RemoveRenderObject(MyRenderObject renderObject, bool includeShadowObject = true)
        {        
            if (renderObject is MyManualCullableRenderObject)
            {
                m_manualCullingStructure.RemoveProxy(renderObject.ProxyData);
                renderObject.ProxyData = MyElement.PROXY_UNASSIGNED;

                MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                //return query to pool
                cullableObject.UnloadContent();

                if (includeShadowObject)
                    RemoveShadowRenderObject(renderObject);

                return;
            }

            if (renderObject.ParentCullObject != null)
            {
                renderObject.ParentCullObject.RemoveRenderObject(renderObject);
                return;
            }

            if (m_nearObjects.Contains(renderObject))
            {
                m_nearObjects.Remove(renderObject);
            }
            else if (renderObject.ProxyData != MyElement.PROXY_UNASSIGNED)
            {
                if (renderObject is MyCullableRenderObject)
                {
                    MyCullableRenderObject cullableObject = renderObject as MyCullableRenderObject;

                    //Move all existing included objects to render prunning structure
                    var aabb = BoundingBoxD.CreateInvalid();
                    cullableObject.CulledObjects.OverlapAllBoundingBox(ref aabb, m_renderObjectListForDraw);
                    foreach (MyRenderObject ro in m_renderObjectListForDraw)
                    {
                        Debug.Assert(!ro.NearFlag);
                        cullableObject.CulledObjects.RemoveProxy(ro.ProxyData);
                        var roAABB = ro.WorldAABB;
                        ro.ProxyData = m_prunningStructure.AddProxy(ref roAABB, ro, 0);
                        ro.CullObject = null;
                    }

                    //destroy cull object
                    m_cullingStructure.RemoveProxy(cullableObject.ProxyData);
                    cullableObject.ProxyData = MyElement.PROXY_UNASSIGNED;

                    //return query to pool
                    cullableObject.UnloadContent();
                }
                else
                {
                    if (renderObject.CullObject != null)
                    {
                        renderObject.CullObject.CulledObjects.RemoveProxy(renderObject.ProxyData);
                        renderObject.CullObject.EntitiesContained--;
                        renderObject.ProxyData = MyElement.PROXY_UNASSIGNED;

                        if (renderObject.CullObject.EntitiesContained == 0)
                        {
                            RemoveRenderObject(renderObject.CullObject, false);
                            m_renderObjects.Remove(renderObject.CullObject.ID);
                            renderObject.CullObject.UnloadContent();
                        }

                        renderObject.CullObject = null;
                    }
                    else
                    {
                        {
                            m_prunningStructure.RemoveProxy(renderObject.ProxyData);
                        }

                        if (renderObject is IMyBackgroundDrawableRenderObject)
                        {
                            m_farObjectsPrunningStructure.RemoveProxy((renderObject as IMyBackgroundDrawableRenderObject).BackgroundProxyData);
                            (renderObject as IMyBackgroundDrawableRenderObject).BackgroundProxyData = MyElement.PROXY_UNASSIGNED;  
                        }

                        renderObject.ProxyData = MyElement.PROXY_UNASSIGNED;                     
                    }                
                }               
            }

            if (includeShadowObject)
                RemoveShadowRenderObject(renderObject);
        }

        internal static void RemoveShadowRenderObject(MyRenderObject renderObject)
        {
            if (renderObject.ShadowProxyData != MyElement.PROXY_UNASSIGNED)
            {
                m_shadowPrunningStructure.RemoveProxy(renderObject.ShadowProxyData);
                renderObject.ShadowProxyData = MyElement.PROXY_UNASSIGNED;
            }
        }

        internal static void MoveRenderObject(MyRenderObject renderObject)
        {

            if (renderObject.ParentCullObject != null)
            {
                renderObject.ParentCullObject.MoveRenderObject(renderObject);
                return;
            }

            var aabb = renderObject.WorldAABB;

            if (renderObject is MyManualCullableRenderObject)
            {
                m_manualCullingStructure.MoveProxy(renderObject.ProxyData, ref aabb, Vector3D.Zero);
            }
            else
                if (renderObject is MyCullableRenderObject)
                {
                    m_cullingStructure.MoveProxy(renderObject.ProxyData, ref aabb, Vector3D.Zero);
                }
                else
                {

                    if (renderObject.CullObject != null)
                    {
                        //Cannot use move because cullobject aabb then does not fit
                        //renderObject.CullObject.CulledObjects.MoveProxy(renderObject.ProxyData, ref aabb, Vector3.Zero);
                        RemoveRenderObject(renderObject, false);

                        renderObject.SetDirty();
                        renderObject.ProxyData = m_prunningStructure.AddProxy(ref aabb, renderObject, 0, true);
                        renderObject.CullObject = null;
                    }
                    else
                    {
                        m_prunningStructure.MoveProxy(renderObject.ProxyData, ref aabb, Vector3D.Zero);
                    }

                    if (renderObject.ShadowProxyData != MyElement.PROXY_UNASSIGNED)
                    {
                        m_shadowPrunningStructure.MoveProxy(renderObject.ShadowProxyData, ref aabb, Vector3D.Zero);
                    }
                }

            if (renderObject is IMyBackgroundDrawableRenderObject)
            {
                m_farObjectsPrunningStructure.MoveProxy((renderObject as IMyBackgroundDrawableRenderObject).BackgroundProxyData, ref aabb, Vector3D.Zero);
            }
        }

        internal static MyRenderObject GetRenderObject(uint id)
        {
            MyRenderObject renderObject;
            m_renderObjects.TryGetValue(id, out renderObject);
            return renderObject;
        }

        internal static int RenderObjectUpdatesCounter = 0;

        internal static void UpdateRenderObject(MyRenderObject renderObject, bool sortIntoCullobjects = false)
        {
            GetRenderProfiler().StartProfilingBlock("UpdateRenderObject");
            if (renderObject.ProxyData != MyElement.PROXY_UNASSIGNED)
            {
                RenderObjectUpdatesCounter++;

                if (sortIntoCullobjects)
                {
                    GetRenderProfiler().StartProfilingBlock("RemoveRenderObject");
                    RemoveRenderObject(renderObject);
                    GetRenderProfiler().EndProfilingBlock();
                    GetRenderProfiler().StartProfilingBlock("AddRenderObject");
                    AddRenderObject(renderObject, false);
                    GetRenderProfiler().EndProfilingBlock();
                }
                else
                {
                    GetRenderProfiler().StartProfilingBlock("MoveRenderObject");
                    MoveRenderObject(renderObject);
                    GetRenderProfiler().EndProfilingBlock();
                }

                GetRenderProfiler().ProfileCustomValue("Updated objects count", RenderObjectUpdatesCounter);
            }

            GetRenderProfiler().EndProfilingBlock();
        }

        private static void DebugDrawPrunning()
        {

            var aabb = BoundingBoxD.CreateInvalid();
            List<MyElement> list = new List<MyElement>();
            List<MyElement> list2 = new List<MyElement>();

            m_manualCullingStructure.OverlapAllBoundingBox(ref aabb, list);

            foreach (MyElement element in list)
            {
                var elementAABB = element.WorldAABB;

                Vector4 color = Vector4.One;
                Vector4 green = new Vector4(0, 1, 0, 1);
                MyDebugDraw.DrawAABBLowRes(ref elementAABB, ref color, 1.0f);

                MyManualCullableRenderObject manualCull = element as MyManualCullableRenderObject;
                manualCull.CulledObjects.GetAll(list2, true);

                foreach (MyElement element2 in list2)
                {
                    MyRenderEntity renderEntity = element2 as MyRenderEntity;
                    var box = renderEntity.WorldAABB.Transform(renderEntity.WorldMatrix);
                    MyDebugDraw.DrawAABBLowRes(ref box, ref green, 1.0f);
                }
            }

            return;

            //List<MyElement> list = new List<MyElement>();
            //List<MyElement> list2 = new List<MyElement>();
            //BoundingBox aabb = new BoundingBox(new Vector3(float.MinValue), new Vector3(float.MaxValue));
            //m_prunningStructure.OverlapAllBoundingBox(ref aabb, list);

            //if (true)
            //{
            //    foreach (MyElement element in list)
            //    {
            //        if (Vector3.Distance(element.WorldAABB.Center, MyRenderCamera.Position) < 2000)
            //        {
            //            BoundingBox elementAABB;
            //            m_prunningStructure.GetFatAABB(element.ProxyData, out elementAABB);
            //            //BoundingBox elementAABB = element.GetWorldSpaceAABB();

            //            Vector4 color = Vector4.One;
            //            MyDebugDraw.DrawAABBLine(ref elementAABB, ref color, 1.0f, true);
            //        }
            //        //MyDebugDraw.DrawText(elementAABB.GetCenter(), new System.Text.StringBuilder(((MyRenderObject)element).Entity.DisplayName), Color.White, 0.7f);
            //    }
            //}   


            // aabb = new BoundingBox(new Vector3(float.MinValue), new Vector3(float.MaxValue));
            // list = new List<MyElement>();
            //list2 = new List<MyElement>();

            // m_cullingStructure.OverlapAllBoundingBox(ref aabb, list);

            // float i = 0;
            // foreach (MyElement element in list)
            // {
            //     BoundingBox elementAABB = element.WorldAABB;
            //     i++;
            //     //if (i % 16 != 0) continue;

            //     float r = (i * 0.6234890156176f) % 1.0f * 0.5f, g = (i * 0.7234890156176f) % 1.0f * 0.5f;
            //     Vector4 randColor = new Vector4(r, g, 0.5f - 0.5f * (r + g), 0.5f);
            //     Vector4 color = randColor * 2;

            //     if (Vector3.Distance(MyRenderCamera.Position, elementAABB.GetCenter()) < 30000)
            //     {
            //         //m_cullingStructure.GetFatAABB(element.ProxyData, out elementAABB);
            //         // MyDebugDraw.DrawAABBLine(ref elementAABB, ref color, 1.0f);
            //         //m_prunningStructure.GetFatAABB(element.ProxyData, out elementAABB);

            //         MyDebugDraw.DrawAABBLine(ref elementAABB, ref color, 1.0f, false);
            //         //MyDebugDraw.DrawText(elementAABB.GetCenter(), new System.Text.StringBuilder(((MyRenderObject)element).Entity.DisplayName), Color.White, 0.7f);

            //         MyCullableRenderObject cullObject = (MyCullableRenderObject)element;
            //         cullObject.CulledObjects.OverlapAllBoundingBox(ref aabb, list2);



            //         if (true)
            //         {
            //             foreach (MyElement element2 in list2)
            //             {
            //                 //elementAABB = element2.GetWorldSpaceAABB();


            //                 /*
            //       //if (Vector3.Distance(MyCamera.Position, elementAABB.GetCenter()) < 3000)
            //       if (((MyRenderObject)element2).Entity is MyVoxelMap)
            //       {
            //           MyDebugDraw.DrawAABBLine(ref elementAABB, ref color, 1.0f);
            //           MyDebugDraw.DrawText(elementAABB.GetCenter(), new System.Text.StringBuilder(list2.Count.ToString()), new Color(randColor * 2), 0.7f);

            //           cullObject.CulledObjects.GetFatAABB(element2.ProxyData, out elementAABB);
            //           if (!cullObject.GetQuery(MyOcclusionQueryID.MAIN_RENDER).OcclusionQueryVisible)
            //               color = Vector4.One;
            //           else
            //               color = randColor * 1.5f;
            //           MyDebugDraw.DrawAABBLine(ref elementAABB, ref color, 1.0f);
            //       }          */
            //             }
            //         }
            //     }
            // }

        }

        public static MyRenderObject GetAnyIntersectionWithLine(MyDynamicAABBTreeD tree, ref VRageMath.LineD line, MyRenderObject ignoreObject0, MyRenderObject ignoreObject, List<MyLineSegmentOverlapResult<MyElement>> elementList)
        {
            tree.OverlapAllLineSegment(ref line, elementList);

            foreach (MyLineSegmentOverlapResult<MyElement> element in elementList)
            {
                MyRenderObject renderObject = ((MyRenderObject)element.Element);

                Debug.Assert(!renderObject.NearFlag);
                //Debug.Assert(renderObject.Visible);

                //  Objects to ignore
                if ((renderObject == ignoreObject0) || (renderObject == ignoreObject))
                    continue;

                //Vector3? testResultEx;
                if (renderObject.GetIntersectionWithLine(ref line))
                {
                    return renderObject;
                }


                /*
 if (testResultEx != null)
 {
     Vector3 dir = line.Direction;

     if (Vector3.Dot((testResultEx.Value - line.From), dir) > 0)
     {
         if (ret == null)
         {
             ret = testResultEx;
             currentObject = renderObject;
         }

         if ((testResultEx.Value - line.From).Length() < (ret.Value - line.From).Length())
         {
             ret = testResultEx;
             currentObject = renderObject;
         }
     }
 }    */
            }

            return null;
        }


        internal static void GetEntitiesFromPrunningStructure(ref BoundingBoxD boundingBox, List<MyElement> list)
        {
            list.Clear();

            GetEntitiesFromPrunningStructure(m_prunningStructure, ref boundingBox, list);

            m_cullingStructure.OverlapAllBoundingBox(ref boundingBox, m_cullObjectListForDraw, 0, false);

            foreach (MyElement element in m_cullObjectListForDraw)
            {
                MyCullableRenderObject cullObject = (MyCullableRenderObject)element;

                GetEntitiesFromPrunningStructure(cullObject.CulledObjects, ref boundingBox, list);
            }
        }

        static void GetEntitiesFromPrunningStructure(MyDynamicAABBTreeD tree, ref BoundingBoxD boundingBox, List<MyElement> list)
        {
            tree.OverlapAllBoundingBox(ref boundingBox, list, 0, false);
        }

        static void AddCullingObjects(List<BoundingBoxD> bbs)
        {
            // create culling objects (bbox with smallest surface area first)
            foreach (var bb in bbs.OrderBy(a => a.SurfaceArea))
            {
                //Need to add to m_renderObjects because of LoadContent
                AddRenderObjectFromProxy(new MyCullableRenderObject(GlobalMessageCounter++, bb), false);
            }
        }

        internal static float CullingStructureWorstAllowedBalance = 0.05f;
        internal static float CullingStructureCutBadness = 20;  // Don't cut boxes.
        internal static float CullingStructureImbalanceBadness = 0.15f;  // Be close to the median.
        internal static float CullingStructureOffsetBadness = 0.1f;  // Be close to the geometric center (more so for initial splits).

        internal static void RebuildCullingStructure()
        {
            var list = new List<MyElement>();
            var everything = BoundingBoxD.CreateInvalid();
            var resultDivision = new List<BoundingBoxD>();

            // Clear old culling nodes
            m_cullingStructure.OverlapAllBoundingBox(ref everything, list);
            foreach (MyRenderObject ro in list)
            {
                RemoveRenderObject(ro);
            }

            // Split by type
            var roList = new List<MyRenderObject>();
            var prefabRoList = new List<MyRenderObject>();
            var voxelRoList = new List<MyRenderObject>();

            m_prunningStructure.OverlapAllBoundingBox(ref everything, list);
            foreach (MyRenderObject o in list)
            {
                System.Diagnostics.Debug.Assert(!(o is MyCullableRenderObject));
                System.Diagnostics.Debug.Assert(o.ParentCullObject == null);
                /*
          if (MyFakes.CULL_EVERY_RENDER_CELL)
          {
              if (o.Entity is AppCode.Game.Prefabs.MyPrefabBase)
              {
                  roList.Add(o);
                  prefabRoList.Add(o);
              }
              else if (o.Entity is MyVoxelMap)
              {
                  resultDivision.Add(o.GetWorldSpaceAABB());
              }
              else
                  roList.Add(o);
          }
          else    */
                {
                    if (o.CullingOptions == CullingOptions.Prefab)
                    {
                        roList.Add(o);
                        prefabRoList.Add(o);
                    }
                    else if (o.CullingOptions == CullingOptions.VoxelMap)
                    {
                        roList.Add(o);
                        voxelRoList.Add(o);
                    }
                    else
                        roList.Add(o);
                }
            }

            // Divide
            AddDivisionForCullingStructure(roList, Math.Max(MyRenderConstants.MIN_OBJECTS_IN_CULLING_STRUCTURE, (int)(roList.Count / MyRenderConstants.MAX_CULLING_OBJECTS * 1.5f)), resultDivision);

            AddDivisionForCullingStructure(
                prefabRoList,
                Math.Max(MyRenderConstants.MIN_PREFAB_OBJECTS_IN_CULLING_STRUCTURE, (int)(prefabRoList.Count / (MyRenderConstants.MAX_CULLING_PREFAB_OBJECTS * MyRenderConstants.m_maxCullingPrefabObjectMultiplier) * 1.5f)),
                resultDivision
            );

            AddDivisionForCullingStructure(
                voxelRoList,
                Math.Max(MyRenderConstants.MIN_VOXEL_RENDER_CELLS_IN_CULLING_STRUCTURE, (int)(voxelRoList.Count / (MyRenderConstants.MAX_CULLING_VOXEL_RENDER_CELLS) * 1.5f)),
                resultDivision
            );

            AddCullingObjects(resultDivision);
        }


        internal static void RebuildCullingStructureCullEveryPrefab()
        {
            var list = new List<MyElement>();
            var everything = BoundingBoxD.CreateInvalid();
            var resultDivision = new List<BoundingBoxD>();

            // Clear old culling nodes
            m_cullingStructure.OverlapAllBoundingBox(ref everything, list);
            foreach (MyRenderObject ro in list)
            {
                RemoveRenderObject(ro);
            }

            // Split by type
            var roList = new List<MyRenderObject>();

            m_prunningStructure.OverlapAllBoundingBox(ref everything, list);
            foreach (MyRenderObject o in list)
            {
                if (o is MyCullableRenderObject) continue;

                roList.Add(o);
                if (o.CullingOptions == CullingOptions.Prefab)  // every prefab will be culled on its own
                    resultDivision.Add(o.WorldAABB);
            }

            // Divide
            AddDivisionForCullingStructure(roList, Math.Max(MyRenderConstants.MIN_OBJECTS_IN_CULLING_STRUCTURE, (int)(roList.Count / MyRenderConstants.MAX_CULLING_OBJECTS * 1.5f)), resultDivision);

            AddCullingObjects(resultDivision);
        }


        // Compute a division and add it to resultDivision.
        internal static void AddDivisionForCullingStructure(List<MyRenderObject> roList, int objectCountLimit, List<BoundingBoxD> resultDivision)
        {
            List<List<MyRenderObject>> resultList = new List<List<MyRenderObject>>();

            // Have a stack of boxes to split; the initial box contains the whole sector
            Stack<List<MyRenderObject>> stackToDivide = new Stack<List<MyRenderObject>>();
            stackToDivide.Push(roList);
            int maxDivides = MyRenderConstants.MAX_CULLING_OBJECTS * 1000;  // sanity check

            while (stackToDivide.Count > 0 && maxDivides-- > 0)
            {
                // take the next box
                List<MyRenderObject> llist = stackToDivide.Pop();

                // if the object count is small, add it to the result list
                if (llist.Count <= objectCountLimit)
                {
                    resultList.Add(llist);
                    continue;
                }

                // get the tightest bounding box containing all objects
                BoundingBoxD caabb = BoundingBoxD.CreateInvalid();
                foreach (MyRenderObject lro in llist)
                {
                    caabb = lro.WorldAABB.Include(ref caabb);
                }

                // we'll optimize split badness
                double bestPlanePos = 0;
                int bestAxis = 0;
                double bestBadness = double.MaxValue;

                // find the longest axis
                // nice to have (not needed): forbid an axis if it didn't work in the last split
                double longestAxisSpan = double.MinValue;
                for (int axis = 0; axis <= 2; axis++)
                {
                    double axisSpan = caabb.Max.GetDim(axis) - caabb.Min.GetDim(axis);
                    if (axisSpan > longestAxisSpan)
                    {
                        longestAxisSpan = axisSpan;
                        bestAxis = axis;
                        bestPlanePos = 0.5f * (caabb.Max.GetDim(axis) + caabb.Min.GetDim(axis));  // sanity check: if nothing works, split in the middle
                    }
                }

                // find the best split perpendicular to the longest axis (nicest results)
                // nice to have (not needed): try all three axes
                for (int axis = bestAxis; axis <= bestAxis; axis++)
                {
                    double axisSpan = caabb.Max.GetDim(axis) - caabb.Min.GetDim(axis);
                    double axisCenter = 0.5f * (caabb.Max.GetDim(axis) + caabb.Min.GetDim(axis));

                    // lo = bounding box mins, hi = bounding box maxes; add a sentinel at the end
                    var lo = new List<double>(); lo.Add(double.MaxValue);
                    var hi = new List<double>(); hi.Add(double.MaxValue);
                    foreach (var ro in llist)
                    {
                        ro.WorldAABB.AssertIsValid();

                        lo.Add(ro.WorldAABB.Min.GetDim(axis));
                        hi.Add(ro.WorldAABB.Max.GetDim(axis));
                    }
                    lo.Sort();
                    hi.Sort();

                    // find the dividing plane that minimizes split badness
                    int leftCount = 0, cutCount = 0, rightCount = llist.Count;

                    for (int l = 0, h = 0; h < hi.Count - 1; )  // don't put everything on one side, that would be silly
                    {
                        // find split interval
                        double thisEventPos;
                        if (lo[l] < hi[h])
                        {
                            thisEventPos = lo[l];
                            rightCount--; cutCount++; l++;
                        }
                        else
                        {
                            thisEventPos = hi[h];
                            cutCount--; leftCount++; h++;
                        }


                        double nextEventPos = Math.Min(lo[l], hi[h]);  // nice to know

                        // if the split isn't too imbalanced
                        if (leftCount + cutCount >= CullingStructureWorstAllowedBalance * llist.Count &&
                            rightCount + cutCount >= CullingStructureWorstAllowedBalance * llist.Count)
                        {
                            // the split could be anywhere in (thisEventPos, nextEventPos); find the closest point in this interval to the geometric center
                            double closestSplitToCenter = axisCenter < thisEventPos ? thisEventPos : axisCenter > nextEventPos ? nextEventPos : axisCenter;

                            // compute badness
                            double badness =
                                cutCount * CullingStructureCutBadness  // Don't cut boxes.
                                + Math.Abs(leftCount - rightCount) * CullingStructureImbalanceBadness  // Be close to the median.
                                + Math.Abs(axisCenter - closestSplitToCenter) * CullingStructureOffsetBadness;  // Be close to the geometric center (more so for initial splits).

                            // found the best split?
                            if (badness < bestBadness)
                            {
                                bestBadness = badness;
                                bestAxis = axis;
                                bestPlanePos = 0.5f * (thisEventPos + nextEventPos);  // put the split plane between this and the next event
                            }
                        }
                    }
                }

                // split objects between left, right and cut
                var left = new List<MyRenderObject>();
                var right = new List<MyRenderObject>();
                var cut = new List<MyRenderObject>();

                foreach (MyRenderObject ro in llist)
                {
                    if (ro.WorldAABB.Max.GetDim(bestAxis) <= bestPlanePos)
                        left.Add(ro);
                    else if (ro.WorldAABB.Min.GetDim(bestAxis) >= bestPlanePos)
                        right.Add(ro);
                    else
                        cut.Add(ro);
                }

                // add cut boxes to the side with fewer boxes
                (left.Count < right.Count ? left : right).AddList(cut);

                if (left.Count == 0)
                {
                    resultList.Add(right);  // can't be cut better
                    continue;
                }
                else if (right.Count == 0)
                {
                    resultList.Add(left);  // can't be cut better
                    continue;
                }
                else
                {
                    stackToDivide.Push(left);
                    stackToDivide.Push(right);
                }
            }

            // add bounding boxes to the resulting division
            foreach (var xList in resultList)
            {
                if (xList.Count > 0)
                {
                    BoundingBoxD caabb = BoundingBoxD.CreateInvalid();
                    foreach (MyRenderObject ro in xList)
                        caabb = ro.WorldAABB.Include(ref caabb);

                    resultDivision.Add(caabb);
                }
            }
        }

        /*
    // Detects intersection for all entities
    internal static MyEntity GetClosestIntersectionWithLine(ref Line line, MyEntity ignorePhysObject0, MyEntity ignorePhysObject1, bool ignoreSelectable = false)
    {
        //  Get collision skins near the line's bounding box (use sweep-and-prune, so we iterate only close objects)
        BoundingBox boundingBox = BoundingBoxHelper.InitialBox;
        BoundingBoxHelper.AddLine(ref line, ref boundingBox);

        MyEntity retEntity = null;
        Vector3? ret = null;
        retEntity = GetEntityFromPrunningStructure(m_prunningStructure, ref line, ref boundingBox, retEntity, ref ret, ignorePhysObject0, ignorePhysObject1, ignoreSelectable, m_renderObjectListForIntersections);

        m_cullingStructure.OverlapAllBoundingBox(ref boundingBox, m_cullObjectListForIntersections);

        foreach (MyElement element in m_cullObjectListForIntersections)
        {
            MyCullableRenderObject cullObject = (MyCullableRenderObject)element;

            retEntity = GetEntityFromPrunningStructure(cullObject.CulledObjects, ref line, ref boundingBox, retEntity, ref ret, ignorePhysObject0, ignorePhysObject1, ignoreSelectable, m_renderObjectListForIntersections);
        }

        // retEntity = GetEntityFromPrunningStructure(m_cullingStructure, ref line, ref boundingBox, retEntity, ignorePhysObject0, ignorePhysObject1);

        return retEntity;
    }


    internal static MyIntersectionResultLineTriangleEx? GetAnyIntersectionWithLine(ref Line line, MyEntity ignorePhysObject0, MyEntity ignorePhysObject1, bool ignoreSelectable)
    {
        //  Get collision skins near the line's bounding box (use sweep-and-prune, so we iterate only close objects)
        BoundingBox boundingBox = BoundingBoxHelper.InitialBox;
        BoundingBoxHelper.AddLine(ref line, ref boundingBox);

        MyEntity retEntity = null;
        Vector3? ret = null;
        MyIntersectionResultLineTriangleEx? result = null;
        retEntity = GetEntityFromPrunningStructure(m_prunningStructure, ref line, ref boundingBox, retEntity, ref ret, ignorePhysObject0, ignorePhysObject1, ignoreSelectable, m_renderObjectListForIntersections);
        if (retEntity != null)
        {
            retEntity.GetIntersectionWithLine(ref line, out result);
            if (result.HasValue)
                return result;
        }

        m_cullingStructure.OverlapAllBoundingBox(ref boundingBox, m_cullObjectListForIntersections);

        foreach (MyElement element in m_cullObjectListForIntersections)
        {
            MyCullableRenderObject cullObject = (MyCullableRenderObject)element;

            retEntity = GetEntityFromPrunningStructure(cullObject.CulledObjects, ref line, ref boundingBox, retEntity, ref ret, ignorePhysObject0, ignorePhysObject1, ignoreSelectable, m_renderObjectListForIntersections);

            if (retEntity != null)
            {
                retEntity.GetIntersectionWithLine(ref line, out result);
                if (result.HasValue)
                    return result;
            }
        }

        return result;
    }
              
         */
        #endregion

        #region Occlusion queries

        static void IssueOcclusionQueries()
        {
            if (!m_currentSetup.EnableOcclusionQueries || !Settings.EnableHWOcclusionQueries)
                return;

            GetRenderProfiler().StartProfilingBlock("IssueOcclusionQueries");

            GetRenderProfiler().StartProfilingBlock("BlendState");

            bool showQueries = ShowHWOcclusionQueries;

            BlendState oldBlendState = BlendState.Current;

            //generate and draw bounding box of our renderCell in occlusion query 
            if (showQueries)
                BlendState.Opaque.Apply();
            else
            {
                MyStateObjects.DisabledColorChannels_BlendState.Apply();
                //BlendState.Opaque.Apply();
                //because of depth buffer what we are reading from shader
                MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary0), null);
                MyRender.Blit(MyRender.GetRenderTarget(MyRenderTargets.Depth), false);
            }

            Vector3D campos = MyRenderCamera.Position;

            GetRenderProfiler().EndProfilingBlock();

            RasterizerState oldRasterizeState = RasterizerState.Current;

            RasterizerState.CullNone.Apply();

            DepthStencilState.None.Apply();

            MyDebugDraw.PrepareFastOcclusionBoundingBoxDraw();

            MyPerformanceCounter.PerCameraDrawWrite.QueriesCount += m_renderOcclusionQueries.Count;

            BoundingBoxD aabbExtended;

            GetRenderProfiler().StartProfilingBlock("RunQueries");
            foreach (MyOcclusionQueryIssue queryIssue in m_renderOcclusionQueries)
            {
                //System.Diagnostics.Debug.Assert(!queryIssue.OcclusionQueryIssued);

                GetRenderProfiler().StartProfilingBlock("Tests");
                aabbExtended = queryIssue.CullObject.WorldAABB;
                aabbExtended.Inflate(2.0f);

                if (!Settings.EnableHWOcclusionQueries || aabbExtended.Contains(campos) == VRageMath.ContainmentType.Contains)
                {
                    queryIssue.OcclusionQueryIssued = false;
                    queryIssue.OcclusionQueryVisible = true;
                    GetRenderProfiler().EndProfilingBlock();
                    continue;
                }
                GetRenderProfiler().EndProfilingBlock();

                //GetRenderProfiler().StartProfilingBlock("OcclusionQuery.Begin");

                GetRenderProfiler().StartProfilingBlock("Tests2");

                queryIssue.OcclusionQueryIssued = !showQueries;
                //renderObject.OcclusionQueryVisible = true;
                if (queryIssue.OcclusionQuery == null)
                {
                    GetRenderProfiler().EndProfilingBlock();
                    GetRenderProfiler().EndProfilingBlock();
                    return;
                }
                GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("IsComplete");
                bool isCompleted = queryIssue.OcclusionQuery.IsComplete;
                GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("Tests3");
                BoundingBoxD aabbExtendedForOC = aabbExtended;
                aabbExtendedForOC.Inflate(-1);

                GetRenderProfiler().EndProfilingBlock();

                if (!showQueries)
                {
                    //    renderObject.OcclusionQuery = new OcclusionQuery(m_device);
                    GetRenderProfiler().StartProfilingBlock("Begin");
                    queryIssue.OcclusionQuery.Begin();
                    GetRenderProfiler().EndProfilingBlock();
                }
                //GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("DrawOcclusionBoundingBox");
                MyDebugDraw.FastOcclusionBoundingBoxDraw(aabbExtendedForOC, 1.0f);
                GetRenderProfiler().EndProfilingBlock();


                //GetRenderProfiler().StartProfilingBlock("OcclusionQuery.End");

                if (!showQueries)
                {
                    GetRenderProfiler().StartProfilingBlock("End");
                    queryIssue.OcclusionQuery.End();
                    GetRenderProfiler().EndProfilingBlock();
                }
                //GetRenderProfiler().EndProfilingBlock();
            }
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("oldBlendState");

            oldBlendState.Apply();

            foreach (var renderObject in m_renderObjectsToDraw)
            {
                renderObject.IssueOcclusionQueries();
            }

            if (EnableLights && Settings.EnableLightsRuntime && MyRender.CurrentRenderSetup.EnableLights.Value && !Settings.ShowBlendedScreens)
            {
                foreach (var renderObject in m_renderLightsForDraw)
                {
                    renderObject.IssueOcclusionQueries();
                }
            }


            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region Preload

        /*
        internal static void PreloadTexturesInRadius(float radius)
        {
            BoundingBox box = BoundingBox.CreateFromSphere(new BoundingSphere(MyCamera.Position, radius * 2));

            m_cullingStructure.OverlapAllBoundingBox(ref box, m_cullObjectListForDraw);
            m_prunningStructure.OverlapAllBoundingBox(ref box, m_renderObjectListForDraw);

            foreach (MyCullableRenderObject cullableObject in m_cullObjectListForDraw)
            {
                cullableObject.CulledObjects.OverlapAllBoundingBox(ref box, m_renderObjectListForDraw, 0, false);
            }

            foreach (MyRenderObject ro in m_renderObjectListForDraw)
            {
                ro.Entity.PreloadTextures();
            }

        }

        private static void PreloadEntityForDraw(MyEntity entity, Action BusyAction)
        {
            Stopwatch stopwatch = new Stopwatch();
            double msElapsed = 0;

            entity.PreloadForDraw();

            foreach (MyEntity child in entity.Children)
            {
                stopwatch.Start();

                PreloadEntityForDraw(child, BusyAction);


                stopwatch.Stop();

                msElapsed += stopwatch.Elapsed.TotalMilliseconds;

                stopwatch.Reset();

                if (msElapsed >= MyGuiConstants.LOADING_THREAD_DRAW_SLEEP_IN_MILISECONDS)
                {
                    msElapsed = 0;
                    BusyAction();
                }
            }
        }
        */
        /*
        internal static void PreloadEntitiesInRadius(float radius, Action BusyAction)
        {
            //Cannot use prunning structure because of deactivated entities which are not there
            foreach (MyEntity entity in MyEntities.GetEntities())
            {
                PreloadEntityForDraw(entity, BusyAction);
            }
        } */

        #endregion

    }
}