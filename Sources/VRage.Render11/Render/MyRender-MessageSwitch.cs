
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SharpDX.Direct3D11;
using VRage;
using VRage.OpenVRWrapper;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Import;
using VRageRender.Messages;
using VRageRender.Profiler;
using VRageRender.Vertex;
using VRage.Render11.GeometryStage2;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Instancing;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.LightingStage;

namespace VRageRender
{ 
    static class MathExt
    {
        internal static float Saturate(float v)
        {
            return (float) Math.Min(1, Math.Max(0, v));
        }

        internal static float Lerp(float a, float b, float x)
        {
            return a + (b - a) * x;
        }

        internal static float PointlightFalloffToRadius(float falloff, float range)
        {
            float r_ = 0.333933902326f * (float)Math.Sqrt(6.12430005749 + 5.9892092 * falloff) - 0.814156676978f;
            return r_ * range;
        }
    }

    static partial class MyRender11
    {
        internal static Vector3 ColorFromMask(Vector3 hsv)
        {
            return hsv;
            if (hsv != new Vector3(0, -1, 0) && hsv != new Vector3(0, 0, 0))
            {
                var rgb = new Vector3(hsv.X, MathExt.Saturate(1 + hsv.Y), MathExt.Saturate(1 + hsv.Z)).HSVtoColor().ToVector3();
                return rgb;
            }
            return Vector3.One;
        }

        private static void ProcessMessage(MyRenderMessageBase message)
        {
            if (MyCompilationSymbols.ProfileRenderMessages)
            {
                string msgName = VRage.Library.Utils.MyEnum<MyRenderMessageEnum>.GetName(message.MessageType);
                GetRenderProfiler().StartProfilingBlock(msgName);
            }

            ProcessMessageSafe(message);

            if (MyCompilationSymbols.ProfileRenderMessages)
                GetRenderProfiler().EndProfilingBlock();
        }

        private static void ProcessMessageSafe(MyRenderMessageBase message)
        {
#if !DEBUG
            ProcessMessageInternal(message);
#else
            try
            {
                ProcessMessageInternal(message);
            }
            catch
            {
                if (message != null)
                {
                    MyLog.Default.WriteLine("Error processing message: " + message.MessageType);
                }
                throw;
            }
#endif

        }

        private static void ProcessMessageInternal(MyRenderMessageBase message)
        {
            switch (message.MessageType)
            {
                case MyRenderMessageEnum.SetCameraViewMatrix:
                {
                    var rMessage = (MyRenderMessageSetCameraViewMatrix)message;

                    SetupCameraMatrices(rMessage);

                    break;
                }

                case MyRenderMessageEnum.DrawScene:
                {
                    var rMessage = (MyRenderMessageBase)message;

                    m_drawQueue.Enqueue(rMessage);

                    break;
                }

                case MyRenderMessageEnum.RebuildCullingStructure:
                {

                    break;
                }

                #region Profiler

                case MyRenderMessageEnum.RenderProfiler:
                {
                    var profMessage = (MyRenderMessageRenderProfiler)message;

                    MyRenderProfiler.HandleInput(profMessage.Command, profMessage.Index);

                    break;
                }

                #endregion

                #region Characters

                case MyRenderMessageEnum.CreateRenderCharacter:
                {
                    var rMessage = (MyRenderMessageCreateRenderCharacter)message;

                    var actor = MyActorFactory.CreateCharacter();
                    var renderable = actor.GetRenderable();
                    renderable.SetModel(MyMeshes.GetMeshId(MyStringId.GetOrCompute(rMessage.Model), 1.0f));
                    actor.SetMatrix(ref rMessage.WorldMatrix);

                    if (rMessage.ColorMaskHSV.HasValue)
                    {
                        var color = ColorFromMask(rMessage.ColorMaskHSV.Value);
                        renderable.SetKeyColor(new Vector4(color, 1));
                    }

                    actor.SetID(rMessage.ID);
					renderable.m_additionalFlags |= MyProxiesFactory.GetRenderableProxyFlags(rMessage.Flags);
                    renderable.m_drawFlags = MyDrawSubmesh.MySubmeshFlags.Gbuffer | MyDrawSubmesh.MySubmeshFlags.Depth;

                    break;
                }

                case MyRenderMessageEnum.SetCharacterSkeleton:
                {
                    var rMessage = (MyRenderMessageSetCharacterSkeleton)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.CharacterID);
                    if (actor == null)
                        MyRenderProxy.Fail(string.Format("Invalid character id '{0}'", rMessage.CharacterID));
                    else
                        actor.GetSkinning().SetSkeleton(rMessage.SkeletonBones, rMessage.SkeletonIndices);

                    //var entity = MyComponents.GetEntity(rMessage.CharacterID);
                    //MyComponents.SetSkeleton(entity, rMessage.SkeletonBones, rMessage.SkeletonIndices);

                    break;
                };

                case MyRenderMessageEnum.SetCharacterTransforms:
                {
                    var rMessage = (MyRenderMessageSetCharacterTransforms)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.CharacterID);
                    if (actor == null)
                        MyRenderProxy.Fail(string.Format("Invalid character id '{0}'", rMessage.CharacterID));
                    else
                        actor.GetSkinning().SetAnimationBones(rMessage.BoneAbsoluteTransforms, rMessage.BoneDecalUpdates);

                    //var entity = MyComponents.GetEntity(rMessage.CharacterID);
                    //MyComponents.SetAnimation(entity, rMessage.RelativeBoneTransforms);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderEntity:
                {
                    var rMessage = (MyRenderMessageUpdateRenderEntity)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(string.Format("Invalid actor id '{0}'", rMessage.ID));
                        break;
                    }

                    if (actor.GetRenderable() != null)
                    {
                        if (rMessage.ColorMaskHSV.HasValue)
                            actor.GetRenderable().SetKeyColor(new Vector4(ColorFromMask(rMessage.ColorMaskHSV.Value), 0));
                        actor.GetRenderable().SetDithering(rMessage.Dithering);
                        actor.GetRenderable().SetGlobalEmissivity(rMessage.Emissivity);
                    }
                    else if (actor.GetInstance() != null)
                    {
                        if (rMessage.ColorMaskHSV.HasValue)
                        {
                            Vector3 keyColor3 = ColorFromMask(rMessage.ColorMaskHSV.Value);
                            actor.GetInstance().KeyColor = new HalfVector3(keyColor3.X, keyColor3.Y, keyColor3.Z);
                        }
                        actor.GetInstance().SetDithered(rMessage.Dithering < 0.0f, Math.Abs(rMessage.Dithering));
                        actor.GetInstance().SetGlobalEmissivity(rMessage.Emissivity);
                    }
                    break;
                }

                case MyRenderMessageEnum.ChangeModel:
                {
                    var rMessage = (MyRenderMessageChangeModel)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(string.Format("Invalid actor id '{0}'", rMessage.ID));
                        break;
                    }

                    if (actor.GetRenderable() != null)
                    {
                        var r = actor.GetRenderable();
                        var modelId = MyMeshes.GetMeshId(X.TEXT_(rMessage.Model), rMessage.Scale);
                        if(r.GetModel() != modelId)
                        {
                            r.SetModel(modelId);
                        }
                    }

                    break;
                }

                case MyRenderMessageEnum.ChangeModelMaterial:
                {
                    var rMessage = (MyRenderMessageChangeModelMaterial)message;

                    MyRenderProxy.Error("MyRenderMessageChangeModelMaterial message is deprecated");

                    //var matId = MyMeshMaterialId.NULL;
                    //if (rMessage.Material.ToLower().Contains("debug"))
                    //{
                    //    matId = MyMeshMaterials1.DebugMaterialId;
                    //}
                    //else
                    //{
                    //    matId = MyMeshMaterials1.GetMaterialId(rMessage.Material);
                    //}

                    //MyAssetsLoader.GetModel(rMessage.Model).SetMaterial_SLOW(MyMeshMaterials1.GetProxyId(matId));

                    break;
                }
          
                #endregion

                #region Render objects

                case MyRenderMessageEnum.CreateRenderVoxelDebris:
                {
                    var rMessage = (MyRenderMessageCreateRenderVoxelDebris)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;

                    Matrix m = (Matrix)rMessage.WorldMatrix;

                    var actor = MyActorFactory.CreateSceneObject();
                    if (rMessage.Model != null)
                    {
                        actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT_(rMessage.Model), 1.0f));
                    }

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref rMessage.WorldMatrix);

                    MyRenderableComponent.DebrisEntityVoxelMaterial[rMessage.ID] = rMessage.VoxelMaterialIndex;

                    break;
                }

                case MyRenderMessageEnum.CreateScreenDecal:
                {
                    var rMessage = (MyRenderMessageCreateScreenDecal)message;

                    MyScreenDecals.AddDecal(rMessage.ID, rMessage.ParentID, ref rMessage.TopoData, rMessage.Flags, rMessage.SourceTarget, rMessage.Material, rMessage.MaterialIndex);

                    break;
                }

                case MyRenderMessageEnum.UpdateScreenDecal:
                {
                    var rMessage = (MyRenderMessageUpdateScreenDecal)message;

                    MyScreenDecals.UpdateDecals(rMessage.Decals);

                    break;
                }

				case MyRenderMessageEnum.CreateRenderEntity:
				{
					var rMessage = (MyRenderMessageCreateRenderEntity)message;

                    MyRenderProxy.Assert(!string.IsNullOrEmpty(rMessage.Model));

				    string mwmFilepath = rMessage.Model;
				    //string mwmFilepath = "";
                    //if (!string.IsNullOrEmpty(rMessage.Model))
				    //    mwmFilepath = rMessage.Model;
                    
                    // try to create model with the new pipeline
                    MyModels models = new MyModels();
                    if (mwmFilepath != null)
					{
                        var modelName = MyAssetsLoader.ModelRemap.Get(mwmFilepath, mwmFilepath);
					    if (MyDebugGeometryStage2.EnableNewGeometryPipeline)
					    {
                            MyManagers.ModelFactory.GetOrCreateModels(modelName, out models);
					    }

					    if (!MyDebugGeometryStage2.EnableNonstandardModels)
                            if (!MyManagers.ModelFactory.IsModelSuitable(modelName))
					            break;
					}

                    MyActor actor;
                    if (models.IsValid) // we were successful with the new pipeline!
                    {
                        // Use new pipeline
                        actor = MyActorFactory.CreateSceneObject2();
                        var instance = actor.GetInstance();
                        bool isVisible = (rMessage.Flags & RenderFlags.Visible) == RenderFlags.Visible;
                        MyVisibilityExtFlags visibleExt = MyVisibilityExtFlags.None;
                        if ((rMessage.Flags & RenderFlags.SkipInMainView) != RenderFlags.SkipInMainView)
                            visibleExt |= MyVisibilityExtFlags.Gbuffer;
                        if ((rMessage.Flags & RenderFlags.CastShadows) == RenderFlags.CastShadows)
                            visibleExt |= MyVisibilityExtFlags.Depth;

                        MyCompatibilityDataForTheOldPipeline compatibilityData = new MyCompatibilityDataForTheOldPipeline
                        {
                            Rescale = rMessage.Rescale,
                            DepthBias = rMessage.DepthBias,
                            MwmFilepath = rMessage.Model,
                            RenderFlags =  rMessage.Flags,
                        };
                        MyManagers.Instances.InitAndRegister(instance, models, isVisible, visibleExt, compatibilityData);
                    }
                    else
                    {
                        // Use old pipeline                    
                        MeshId mesh = MeshId.NULL;
                        var modelName = MyAssetsLoader.ModelRemap.Get(mwmFilepath, mwmFilepath);
                        mesh = MyMeshes.GetMeshId(X.TEXT_(modelName), rMessage.Rescale);
                        actor = MyActorFactory.CreateSceneObject();
                        var renderable = actor.GetRenderable();
                        renderable.m_additionalFlags |= MyProxiesFactory.GetRenderableProxyFlags(rMessage.Flags);
                        renderable.m_depthBias = rMessage.DepthBias;

                        if (mesh != MeshId.NULL)
                            renderable.SetModel(mesh);
                    }
                    

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref rMessage.WorldMatrix);

					break;
				}

				case MyRenderMessageEnum.CreateRenderEntityClouds:
				{
					var rMessage = (MyRenderMessageCreateRenderEntityClouds)message;

					if (rMessage.Technique == MyMeshDrawTechnique.CLOUD_LAYER)
					{
						MyCloudRenderer.CreateCloudLayer(
							rMessage.ID,
							rMessage.CenterPoint,
							rMessage.Altitude,
							rMessage.MinScaledAltitude,
							rMessage.ScalingEnabled,
							rMessage.FadeOutRelativeAltitudeStart,
							rMessage.FadeOutRelativeAltitudeEnd,
							rMessage.ApplyFogRelativeDistance,
							rMessage.MaxPlanetHillRadius,
							rMessage.Model,
                            rMessage.Textures,
							rMessage.RotationAxis,
							rMessage.AngularVelocity,
							rMessage.InitialRotation);
					}

					break;
				}

                case MyRenderMessageEnum.CreateRenderEntityAtmosphere:
                {
                    var rMessage = (MyRenderMessageCreateRenderEntityAtmosphere)message;

                    if (rMessage.Technique == MyMeshDrawTechnique.ATMOSPHERE) 
                    {
                        float earthPlanetRadius = 6360000f;
                        float earthAtmosphereRadius = 6420000f;

                        float earthAtmosphereToPlanetRatio = earthAtmosphereRadius / earthPlanetRadius;
                        float targetAtmosphereToPlanetRatio = rMessage.AtmosphereRadius / rMessage.PlanetRadius;
                        float targetToEarthRatio = (targetAtmosphereToPlanetRatio - 1) / (earthAtmosphereToPlanetRatio - 1);
                        earthAtmosphereRadius = earthPlanetRadius * targetAtmosphereToPlanetRatio;

                        float planetScaleFactor = (rMessage.PlanetRadius) / earthPlanetRadius;
                        float atmosphereScaleFactor = (rMessage.AtmosphereRadius - rMessage.PlanetRadius) / (rMessage.PlanetRadius * 0.5f);
                        
                        Vector3 rayleighScattering = new Vector3(5.8e-6f, 13.5e-6f, 33.1e-6f);
                        Vector3 mieScattering = new Vector3(2e-5f, 2e-5f, 2e-5f);
                        float rayleighHeightScale = 8000f;
                        float mieHeightScale = 1200f;

                        MyAtmosphereRenderer.CreateAtmosphere(rMessage.ID, rMessage.WorldMatrix, earthPlanetRadius, earthAtmosphereRadius, 
                            rayleighScattering, rayleighHeightScale, mieScattering, mieHeightScale,
                            planetScaleFactor, atmosphereScaleFactor);
                    }
                    break;
                }

                case MyRenderMessageEnum.RemoveDecal:
                {
                    var rMessage = (MyRenderMessageRemoveDecal)message;

                    MyScreenDecals.RemoveDecal(rMessage.ID);
                    break;
                }

                case MyRenderMessageEnum.SetDecalGlobals:
                {
                    var rMessage = (MyRenderMessageSetDecalGlobals)message;

                    MyScreenDecals.SetDecalGlobals(rMessage.Globals);

                    break;
                }

                case MyRenderMessageEnum.RegisterDecalsMaterials:
                {
                    var rMessage = (MyRenderMessageRegisterScreenDecalsMaterials)message;

                    MyScreenDecals.RegisterMaterials(rMessage.MaterialDescriptions);


                    break;
                }

                case MyRenderMessageEnum.ClearDecals:
                {
                    var rMessage = (MyRenderMessageClearScreenDecals)message;
                    MyScreenDecals.ClearDecals();
                    break;
                }

                case MyRenderMessageEnum.UpdateRenderObject:
                { 
                    var rMessage = (MyRenderMessageUpdateRenderObject)message;

                    MyRenderProxy.Assert(rMessage.ID != MyRenderProxy.RENDER_ID_UNASSIGNED);

                    MyRenderProxy.ObjectType objectType;
                    if (MyRenderProxy.ObjectTypes.TryGetValue(rMessage.ID, out objectType))
                    {
                        switch (objectType)
                        {
                            case MyRenderProxy.ObjectType.Entity:
                                var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                                if (actor != null)
                                {
                                    if (rMessage.LastMomentUpdateIndex != -1 && MyOpenVR.LmuDebugOnOff)
                                        MyOpenVR.LMUMatrixUpdate(ref rMessage.WorldMatrix, rMessage.LastMomentUpdateIndex);

                                    actor.SetMatrix(ref rMessage.WorldMatrix);
                                    if (rMessage.AABB.HasValue)
                                    {
                                        actor.SetAabb(rMessage.AABB.Value);
                                    }
                                }
                                break;
                            case MyRenderProxy.ObjectType.Clipmap:
                                if (MyClipmapFactory.ClipmapByID.ContainsKey(rMessage.ID))
                                {
                                    MyClipmapFactory.ClipmapByID[rMessage.ID].UpdateWorldMatrix(ref rMessage.WorldMatrix);
                                }
                                break;
                            default:
                                MyRenderProxy.Assert(false);
                                break;
                        }
                    }
                    else
                        MyRenderProxy.Fail(string.Format("Invalid render object id '{0}'", rMessage.ID));

                    break;
                }

                case MyRenderMessageEnum.RemoveRenderObject:
                {
                    var rMessage = (MyRenderMessageRemoveRenderObject)message;

                    MyRenderProxy.Assert(rMessage.ID != MyRenderProxy.RENDER_ID_UNASSIGNED);

                    MyHighlight.RemoveObjects(rMessage.ID, null);

                    MyRenderProxy.ObjectType objectType;
                    if (MyRenderProxy.ObjectTypes.TryGetValue(rMessage.ID, out objectType))
                    {
                        switch (objectType)
                        {
                            case MyRenderProxy.ObjectType.Entity:
                                var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                                if (actor != null)
                                {
                                    if (actor.GetRenderable() != null && actor.GetRenderable().GetModel().Info.Dynamic)
                                        MyMeshes.RemoveMesh(actor.GetRenderable().GetModel());

                                    actor.Destruct();
                                    MyScreenDecals.RemoveEntityDecals(rMessage.ID);
                                }
                                else if (MyDebugGeometryStage2.EnableNonstandardModels)
                                    MyRenderProxy.Error("Unresolved condition"); 
                                break;
                            case MyRenderProxy.ObjectType.InstanceBuffer:
                                MyInstancing.Remove(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Light:
                                MyLights.Remove(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Clipmap:
                                if (MyDebugGeometryStage2.EnableVoxels)
                                    MyClipmapFactory.Remove(rMessage.ID);
                                break;

                            case MyRenderProxy.ObjectType.GPUEmitter:
                                MyGPUEmitters.Remove(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Atmosphere:
                                MyAtmosphereRenderer.RemoveAtmosphere(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Cloud:
                                MyCloudRenderer.RemoveCloud(rMessage.ID);
                                break;

                            case MyRenderProxy.ObjectType.DebugDrawMesh:
                                MyPrimitivesRenderer.RemoveDebugMesh(rMessage.ID);
                                break;

                            case MyRenderProxy.ObjectType.Video:
                                MyVideoFactory.Remove(rMessage.ID);
                                break;

                            default:
                                MyRenderProxy.Assert(false);
                                break;
                        }
                        MyRenderProxy.RemoveMessageId(rMessage.ID, objectType);
                    }
                    else
                        MyRenderProxy.Fail(string.Format("Invalid render object id '{0}'", rMessage.ID));

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderObjectVisibility:
                {
                    var rMessage = (MyRenderMessageUpdateRenderObjectVisibility)message;

                    MyRenderProxy.Assert(rMessage.ID != MyRenderProxy.RENDER_ID_UNASSIGNED);

                    MyRenderProxy.ObjectType objectType;
                    if (MyRenderProxy.ObjectTypes.TryGetValue(rMessage.ID, out objectType))
                    {
                        switch (objectType)
                        {
                            case MyRenderProxy.ObjectType.Entity:
                                var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                                if (actor != null)
                                    actor.SetVisibility(rMessage.Visible);
                                break;
                        }
                    }
                    else
                        MyRenderProxy.Fail(string.Format("Invalid render object id '{0}'", rMessage.ID));
                    break;
                }


                case MyRenderMessageEnum.CreateRenderInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageCreateRenderInstanceBuffer)message;

                    //var instancing = MyComponentFactory<MyInstancingComponent>.Create();
                    //instancing.SetID(rMessage.ID);
                    //instancing.Init(rMessage.Type);
                    //instancing.SetDebugName(rMessage.DebugName);

                    MyInstancing.Create(rMessage.ID, rMessage.ParentID, rMessage.Type, rMessage.DebugName);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderInstanceBufferSettings:
                {
                    var rMessage = (MyRenderMessageUpdateRenderInstanceBufferSettings)message;

                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                        MyInstancing.UpdateGenericSettings(handle, rMessage.SetPerInstanceLod);
                    else
                        MyRenderProxy.Fail(String.Format("No instance buffer with ID '{0}'", rMessage.ID));

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderInstanceBufferRange:
                {
                    var rMessage = (MyRenderMessageUpdateRenderInstanceBufferRange)message;

                    // TODO: Turn this into partial update.
                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                    {
                        MyInstancing.UpdateGeneric(handle, rMessage.InstanceData, rMessage.InstanceData.Length);
                    }
                    else
                        MyRenderProxy.Fail(String.Format("No instance buffer with ID '{0}'", rMessage.ID));

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageUpdateRenderCubeInstanceBuffer)message;

                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                        MyInstancing.UpdateCube(MyInstancing.Get(rMessage.ID), rMessage.InstanceData, rMessage.DecalsData, rMessage.Capacity);
                    else
                        MyRenderProxy.Fail(String.Format("No instance buffer with ID '{0}'", rMessage.ID));
                    break;
                }

                case MyRenderMessageEnum.SetInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageSetInstanceBuffer)message;
                    
                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail("No actor with ID " + rMessage.ID);
                        break;
                    }
                    else
                    {
                        if (actor.GetRenderable() != null)
                        {
                            actor.GetRenderable().SetInstancing(MyInstancing.Get(rMessage.InstanceBufferId));
                            actor.SetLocalAabb(rMessage.LocalAabb);
                            actor.GetRenderable().SetInstancingCounters(rMessage.InstanceCount, rMessage.InstanceStart);
                        }
                        else if (actor.GetInstance() != null) // single instance will be converted to multi instance
                        {
                            if (rMessage.InstanceData != null) // if the message can processed by the new pipeline
                            { 
                                MyRenderProxy.Assert(rMessage.InstanceStart + rMessage.InstanceCount <= rMessage.InstanceData.Length);
                                if (rMessage.InstanceData != null)
                                { 
                                    actor.GetInstance().SetMultiInstancesTransformStrategy(rMessage.InstanceData, rMessage.InstanceStart, rMessage.InstanceCount);
                                    actor.SetLocalAabb(rMessage.LocalAabb);
                                }
                            }
                            else
                            {
                                // in this case, the message cannot be processed by the new pipeline, the model will be processed by the old pipeline
                                // this is temporary solution until the new pipeline will be ready for the new instancing

                                // the component will be created and filled with the data:
                                MyInstanceComponent oldComponent = actor.GetInstance();
                                MyRenderableComponent newComponent = MyComponentFactory<MyRenderableComponent>.Create();
                                string mwmFilepath = oldComponent.CompatibilityDataForTheOldPipeline.MwmFilepath;
                                float rescale = oldComponent.CompatibilityDataForTheOldPipeline.Rescale;
                                RenderFlags renderFlags = oldComponent.CompatibilityDataForTheOldPipeline.RenderFlags;
                                byte depthBias = oldComponent.CompatibilityDataForTheOldPipeline.DepthBias;

                                // the new component is ready, it can be switched with the old component and the old will be discarded:
                                actor.RemoveComponent<MyInstanceComponent>(oldComponent);
                                actor.AddComponent<MyRenderableComponent>(newComponent);

                                var modelName = MyAssetsLoader.ModelRemap.Get(mwmFilepath, mwmFilepath);
                                MeshId mesh = MyMeshes.GetMeshId(X.TEXT_(modelName), rescale);
                                if (mesh != MeshId.NULL)
                                    newComponent.SetModel(mesh);
                                actor.GetRenderable().m_additionalFlags |= MyProxiesFactory.GetRenderableProxyFlags(renderFlags);
                                actor.GetRenderable().m_depthBias = depthBias;
                                
                                // now, instancing is done on the new component
                                actor.GetRenderable().SetInstancing(MyInstancing.Get(rMessage.InstanceBufferId));
                                actor.SetLocalAabb(rMessage.LocalAabb);
                                actor.GetRenderable().SetInstancingCounters(rMessage.InstanceCount, rMessage.InstanceStart);
                            }
                        }
                        else
                            MyRenderProxy.Error("Unresolved condition");
                    }

                    break;
                }

                case MyRenderMessageEnum.CreateManualCullObject:
                {
                    var rMessage = (MyRenderMessageCreateManualCullObject)message;

                    var actor = MyActorFactory.CreateGroup();
                    actor.SetID(rMessage.ID);
                    Matrix m = (Matrix)rMessage.WorldMatrix;
                    actor.SetMatrix(ref rMessage.WorldMatrix);

                    break;
                }

                case MyRenderMessageEnum.SetParentCullObject:
                {
                    var rMessage = (MyRenderMessageSetParentCullObject)message;

                    var child = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    var parent = MyIDTracker<MyActor>.FindByID(rMessage.CullObjectID);

                    if (child != null && parent != null && parent.GetGroupRoot() != null && child.GetGroupLeaf() == null)
                    {
                        child.SetRelativeTransform(rMessage.ChildToParent);
                        parent.GetGroupRoot().Add(child);
                    }
                    else
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(string.Format("Invalid child '{0}' or parent '{1}' render object ids", rMessage.ID, rMessage.CullObjectID));
                    }

                    break;
                }

                case MyRenderMessageEnum.CreateLineBasedObject:
                {
                    var rMessage = (MyRenderMessageCreateLineBasedObject)message;

                    var actor = MyActorFactory.CreateSceneObject();
                    //actor.GetRenderable().SetModel(new MyDynamicMesh());

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref MatrixD.Identity);

                    MyMeshMaterials1.GetMaterialId("__ROPE_MATERIAL", null, rMessage.ColorMetalTexture, rMessage.NormalGlossTexture, rMessage.ExtensionTexture, MyMeshDrawTechnique.MESH);
                    actor.GetRenderable().SetModel(MyMeshes.CreateRuntimeMesh(X.TEXT_("LINE" + rMessage.ID), 1, true));

                    break;
                }

                case MyRenderMessageEnum.UpdateLineBasedObject:
                {
                    var rMessage = (MyRenderMessageUpdateLineBasedObject)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.ID));
                    }
                    else
                    {
                        //var mesh = actor.GetRenderable().GetMesh() as MyDynamicMesh;

                        MyVertexFormatPositionH4 [] stream0;
                        MyVertexFormatTexcoordNormalTangentTexindices[] stream1;

                        MyLineHelpers.GenerateVertexData(ref rMessage.WorldPointA, ref rMessage.WorldPointB, 
                            out stream0, out stream1);

                        var indices = MyLineHelpers.GenerateIndices(stream0.Length);
                        var sections = new MyRuntimeSectionInfo[] 
                        { 
                            new MyRuntimeSectionInfo { TriCount = indices.Length / 3, IndexStart = 0, MaterialName = "__ROPE_MATERIAL" } 
                        };

                        MyMeshes.UpdateRuntimeMesh(MyMeshes.GetMeshId(X.TEXT_("LINE" + rMessage.ID), 1.0f), 
                            indices, 
                            stream0, 
                            stream1, 
                            sections,
                            (BoundingBox)MyLineHelpers.GetBoundingBox(ref rMessage.WorldPointA, ref rMessage.WorldPointB));

                        //actor.SetAabb((BoundingBox)MyLineHelpers.GetBoundingBox(ref rMessage.WorldPointA, ref rMessage.WorldPointB));
                        actor.MarkRenderDirty();

                        var matrix = MatrixD.CreateTranslation((Vector3)(rMessage.WorldPointA + rMessage.WorldPointB) * 0.5f);
                        actor.SetMatrix(ref matrix);
                    }

                    break;
                }

                case MyRenderMessageEnum.SetRenderEntityData:
                {
                    var rMessage = (MyRenderMessageSetRenderEntityData)message;

                    MyRenderProxy.Error("MyRenderMessageSetRenderEntityData is deprecated!");

                    break;
                }

                case MyRenderMessageEnum.AddRuntimeModel:
                {
                    var rMessage = (MyRenderMessageAddRuntimeModel)message;

                    //MyDestructionMesh mesh = MyDestructionMesh.ModelsDictionary.Get(rMessage.Name);
                    //if (mesh == null)
                    //{
                        //mesh = new MyDestructionMesh(rMessage.Name);

                        //ProfilerShort.Begin("LoadBuffers");
                        //mesh.Fill(rMessage.ModelData.Indices, rMessage.ModelData.Positions, rMessage.ModelData.Normals, rMessage.ModelData.Tangents, rMessage.ModelData.TexCoords, rMessage.ModelData.Sections, rMessage.ModelData.AABB);
                        //ProfilerShort.End();

                    MyRenderProxy.Assert(!MyMeshes.Exists(rMessage.Name), "It is added already added mesh!");
                    MyRenderProxy.Assert(!MyRender11.Settings.UseGeometryArrayTextures, "Geometry array textures do not fully support runtimer models, please add support");
                    if(!MyMeshes.Exists(rMessage.Name))
                    {
                        {
                            ushort[] indices = new ushort[rMessage.ModelData.Indices.Count];
                            for (int i = 0; i < rMessage.ModelData.Indices.Count; i++)
                            {
                                indices[i] = (ushort)rMessage.ModelData.Indices[i];
                            }
                            var verticesNum = rMessage.ModelData.Positions.Count;
                            MyVertexFormatPositionH4[] stream0 = new MyVertexFormatPositionH4[verticesNum];
                            MyVertexFormatTexcoordNormalTangentTexindices[] stream1 = new MyVertexFormatTexcoordNormalTangentTexindices[verticesNum];

                            Vector4I[] arrayTexIndices = MyManagers.GeometryTextureSystem.CreateTextureIndices(rMessage.ModelData.Sections, rMessage.ModelData.Indices, rMessage.ModelData.Positions.Count);
                            for (int i = 0; i < verticesNum; i++)
                            {
                                stream0[i] = new MyVertexFormatPositionH4(rMessage.ModelData.Positions[i]);
                                stream1[i] = new MyVertexFormatTexcoordNormalTangentTexindices(
                                    rMessage.ModelData.TexCoords[i], rMessage.ModelData.Normals[i], rMessage.ModelData.Tangents[i], (Byte4) arrayTexIndices[i]);
                            }
                            var id = MyMeshes.CreateRuntimeMesh(X.TEXT_(rMessage.Name), rMessage.ModelData.Sections.Count, false);
                            MyMeshes.UpdateRuntimeMesh(id, indices, stream0, stream1, rMessage.ModelData.Sections.ToArray(), rMessage.ModelData.AABB);
                        }

                        if (rMessage.ReplacedModel != null)
                        {
                            //MyAssetsLoader.ModelRemap[rMessage.ReplacedModel] = rMessage.Name;
                            MyAssetsLoader.ModelRemap[rMessage.Name] = rMessage.ReplacedModel;
                        }

                        //if (MyAssetsLoader.LOG_MESH_STATISTICS)
                        //{
                        //    mesh.DebugWriteInfo();
                        //}
                    }
                    
                    break;
                }

                case MyRenderMessageEnum.UpdateModelProperties:
                {
                    var rMessage = (MyRenderMessageUpdateModelProperties)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.ID));
                        break;
                    }
                    else
                    {
                        string materialName = rMessage.MaterialName;

                        // careful, lod is ignored after all (properties apply to all lods)
                        var key = new MyEntityMaterialKey { LOD = rMessage.LOD, Material = X.TEXT_(materialName) };

                        if(rMessage.Enabled.HasValue)
                        {
                            if (!MyScene.EntityDisabledMaterials.ContainsKey(rMessage.ID))
                                MyScene.EntityDisabledMaterials.Add(rMessage.ID, new HashSet<MyEntityMaterialKey>());

                            if (!rMessage.Enabled.Value)
                                MyScene.EntityDisabledMaterials[rMessage.ID].Add(key);
                            else
                                MyScene.EntityDisabledMaterials[rMessage.ID].Remove(key);
                        }

                        if (actor.GetRenderable() != null)
                        {
                            MyRenderableComponent renderableComponent = actor.GetRenderable();
                            if ((rMessage.Emissivity.HasValue || rMessage.DiffuseColor.HasValue) && !renderableComponent.ModelProperties.ContainsKey(key))
                                renderableComponent.ModelProperties[key] = new MyModelProperties();

                            if(rMessage.Emissivity.HasValue)
                                renderableComponent.ModelProperties[key].Emissivity = rMessage.Emissivity.Value;

                            if(rMessage.DiffuseColor.HasValue)
                                renderableComponent.ModelProperties[key].ColorMul = rMessage.DiffuseColor.Value;
                        }
                        else if (actor.GetInstance() != null)
                        {
                            MyInstanceComponent instance = actor.GetInstance();

                            if (rMessage.Emissivity.HasValue)
                                instance.SetInstanceMaterialEmissivity(materialName, rMessage.Emissivity.Value);

                            if (rMessage.DiffuseColor.HasValue)
                                instance.SetInstanceMaterialColorMult(materialName, rMessage.DiffuseColor.Value);
                        }
                        else
                            MyRenderProxy.Error("Unresolved condition");

                        actor.MarkRenderDirty();

                        //MyHighlight.HandleHighlight(rMessage.ID, rMessage.MeshIndex, rMessage.OutlineColor, rMessage.OutlineThickness, rMessage.PulseTimeInFrames);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateModelHighlight:
                {
                    var rMessage = (MyRenderMessageUpdateModelHighlight)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.ID));
                        break;
                    }

                    if (rMessage.Thickness > 0) // the object will be added
                    { 
                        MyHighlight.AddObjects(rMessage.ID, rMessage.SectionNames, rMessage.OutlineColor, rMessage.Thickness, rMessage.PulseTimeInSeconds, rMessage.InstanceIndex);
                        if (rMessage.SubpartIndices != null)
                            foreach (uint index in rMessage.SubpartIndices)
                            { 
                                MyRenderProxy.Assert(index != -1, "The renderer received a UpdatemodelHighlight message with the invalid SubpartIndex");
                                if (index != -1)
                                    MyHighlight.AddObjects(index, null, rMessage.OutlineColor, rMessage.Thickness, rMessage.PulseTimeInSeconds, -1);
                            }
                    }
                    else // the object will be removed
                    {
                        MyHighlight.RemoveObjects(rMessage.ID, rMessage.SectionNames);
                        if (rMessage.SubpartIndices != null)
                            foreach (uint index in rMessage.SubpartIndices)
                                if (index != -1)
                                {
                                    MyRenderProxy.Assert(index != -1, "The renderer received a UpdatemodelHighlight message with the invalid SubpartIndex");
                                    MyHighlight.RemoveObjects(index, null);
                                }
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateColorEmissivity:
                {
                    var rMessage = (MyRenderMessageUpdateColorEmissivity)message;
                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.ID));
                    }
                    else
                    {
                        if (actor.GetRenderable() != null)
                            actor.GetRenderable().UpdateColorEmissivity(rMessage.LOD, rMessage.MaterialName, rMessage.DiffuseColor, rMessage.Emissivity);
                        else if (actor.GetInstance() != null)
                        {
                            MyInstanceMaterial instanceMaterial = new MyInstanceMaterial
                            {
                                ColorMult = rMessage.DiffuseColor,
                                Emissivity = rMessage.Emissivity,
                            };
                            actor.GetInstance().SetInstanceMaterial(rMessage.MaterialName, instanceMaterial);
                        }
                        else
                            MyRenderProxy.Error("Unresolved condition");
                    }

                    break;
                }

                case MyRenderMessageEnum.PreloadModel:
                {
                    var rMessage = (MyRenderMessagePreloadModel) message;

                    //MyAssetsLoader.GetModel(rMessage.Name);
                    MyMeshes.GetMeshId(X.TEXT_(rMessage.Name), rMessage.Rescale);

                    break;
                }

                case MyRenderMessageEnum.ChangeMaterialTexture:
                {
                    var rMessage = (MyRenderMessageChangeMaterialTexture)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.RenderObjectID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.RenderObjectID));
                    }
                    else
                    {
                        var r = actor.GetRenderable();
                        if (r == null)
                            break;

                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT_(rMessage.MaterialName) };

                        MyModelProperties properties;
                        if (!r.ModelProperties.TryGetValue(key, out properties))
                        {
                            properties = new MyModelProperties();
                            r.ModelProperties[key] = properties;
                        }

                        properties.AddTextureChanges(rMessage.Changes);

                        actor.MarkRenderDirty();
                    }

                    rMessage.Changes.Clear();
                   
                    break;
                }

                case MyRenderMessageEnum.RenderOffscreenTextureToMaterial:
                {
                    var rMessage = (MyRenderMessageRenderOffscreenTextureToMaterial)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.RenderObjectID);
                    if (actor == null)
                    {
                        if (MyDebugGeometryStage2.EnableNonstandardModels && MyDebugGeometryStage2.EnableVoxels)
                            MyRenderProxy.Fail(String.Format("Invalid actor id '{0}'", rMessage.RenderObjectID));
                    }
                    else
                    {
                        var manager = MyManagers.FileTextures;
                        IUserGeneratedTexture handle;
                        if (!manager.TryGetTexture(rMessage.OffscreenTexture, out handle))
                        {
                            var material = MyMeshMaterials1.GetMaterialId(rMessage.MaterialName).Info;

                            ITexture materialTexture;
                            switch (rMessage.TextureType)
                            {
                                case MyTextureType.ColorMetal:
                                    materialTexture = manager.GetTexture(material.ColorMetal_Texture, MyFileTextureEnum.COLOR_METAL, true);
                                    break;
                                case MyTextureType.NormalGloss:
                                    materialTexture = manager.GetTexture(material.NormalGloss_Texture, MyFileTextureEnum.NORMALMAP_GLOSS, true);
                                    break;
                                case MyTextureType.Extensions:
                                    materialTexture = manager.GetTexture(material.Extensions_Texture, MyFileTextureEnum.EXTENSIONS, true);
                                    break;
                                case MyTextureType.Alphamask:
                                    materialTexture = manager.GetTexture(material.Alphamask_Texture, MyFileTextureEnum.ALPHAMASK, true);
                                    break;
                                default:
                                    throw new Exception();
                            }

                            handle = manager.CreateGeneratedTexture(rMessage.OffscreenTexture, materialTexture.Size.X, materialTexture.Size.Y, rMessage.TextureType, 1);
                        }

                        handle.Reset();

                        SharpDX.Color? backgroundColor = null;
                        if (rMessage.BackgroundColor != null)
                            backgroundColor = new SharpDX.Color(rMessage.BackgroundColor.Value.PackedValue);

                        var texture = MyRender11.DrawSpritesOffscreen(rMessage.OffscreenTexture,
                            handle.Size.X, handle.Size.Y, handle.Format, backgroundColor);

                        var texture2 = MyManagers.RwTexturesPool.BorrowRtv("RenderOffscreenTextureBlend",
                            handle.Size.X, handle.Size.Y, handle.Format);

                        IBlendState blendState = rMessage.BlendAlphaChannel ? MyBlendStateManager.BlendAlphaPremult : MyBlendStateManager.BlendAlphaPremultNoAlphaChannel;

                        MyBlendTargets.RunWithStencil(texture2, texture, blendState);
                        texture.Release();
                        texture = texture2;

                        MyImmediateRC.RC.CopyResource(texture, handle);
                        texture.Release();

                        var renderableComponent = actor.GetRenderable();
                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT_(rMessage.MaterialName) };

                        MyModelProperties modelProperty;
                        if (!renderableComponent.ModelProperties.TryGetValue(key, out modelProperty))
                        {
                            modelProperty = new MyModelProperties();
                            renderableComponent.ModelProperties[key] = modelProperty;
                        }

                        modelProperty.AddTextureChange(new MyTextureChange() { TextureName = rMessage.OffscreenTexture, TextureType = rMessage.TextureType });

                        actor.MarkRenderDirty();
                    }
                    
                    break;
                }

                case MyRenderMessageEnum.PreloadMaterials:
                {
                    var rMessage = (MyRenderMessagePreloadMaterials)message;

                    //MyAssetsLoader.GetMaterials(rMessage.Name);
                    MyMeshes.GetMeshId(X.TEXT_(rMessage.Name), 1.0f);

                    break;
                }

                #endregion

                #region Voxels

                case MyRenderMessageEnum.CreateClipmap:
                {
                    var rMessage = (MyRenderMessageCreateClipmap)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;

                    var clipmap = new MyClipmapHandler(rMessage.ClipmapId, rMessage.ScaleGroup, rMessage.WorldMatrix, rMessage.SizeLod0, rMessage.Position, rMessage.PlanetRadius, rMessage.SpherizeWithDistance, rMessage.AdditionalRenderFlags, rMessage.PrunningFunc);
                    MyClipmapFactory.ClipmapByID[rMessage.ClipmapId] = clipmap;
                    clipmap.Base.LoadContent();

                    break;
                }

                case MyRenderMessageEnum.UpdateClipmapCell:
                {
                    var rMessage = (MyRenderMessageUpdateClipmapCell)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);

                    if (clipmap == null)
                        MyRenderProxy.Fail(String.Format("Invalid clipmap id '{0}'", rMessage.ClipmapId));
                    else
                        clipmap.Base.UpdateCell(rMessage);

                    break;
                }

                case MyRenderMessageEnum.InvalidateClipmapRange:
                {
                    var rMessage = (MyRenderMessageInvalidateClipmapRange)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);
                    if (clipmap == null)
                        MyRenderProxy.Fail(String.Format("Invalid clipmap id '{0}'", rMessage.ClipmapId));
                    else
                        clipmap.Base.InvalidateRange(rMessage.MinCellLod0, rMessage.MaxCellLod0);

                    break;
                }

                case MyRenderMessageEnum.CreateRenderVoxelMaterials:
                {
                    var rMessage = (MyRenderMessageCreateRenderVoxelMaterials)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;

                    MyRenderProxy.Assert(MyVoxelMaterials1.CheckIndices(rMessage.Materials));
                    MyVoxelMaterials1.Set(rMessage.Materials);

                    rMessage.Materials = null;

                    break;
                }


                case MyRenderMessageEnum.UpdateRenderVoxelMaterials:
                {
                    var rMessage = (MyRenderMessageUpdateRenderVoxelMaterials)message;
                    if (!MyDebugGeometryStage2.EnableVoxels)
                        break;
                    MyVoxelMaterials1.Set(rMessage.Materials, true);

                    rMessage.Materials = null;

                    break;
                }

                #endregion

                #region Lights

                case MyRenderMessageEnum.CreateRenderLight:
                {
                    var rMessage = (MyRenderMessageCreateRenderLight)message;

                    MyLights.Create(rMessage.ID);
                    break;
                }

                case MyRenderMessageEnum.UpdateRenderLight:
                {
                    var rMessage = (MyRenderMessageUpdateRenderLight)message;
                    MyRenderProxy.Assert(rMessage.Data.ID != MyRenderProxy.RENDER_ID_UNASSIGNED, "Light id is not assigned");

                    var light = MyLights.Get(rMessage.Data.ID);

                    if (light == LightId.NULL)
                    {
                        MyRenderProxy.Fail(String.Format("Non-existent light with id '{0}'", rMessage.Data.ID));
                    }
                    else
                    {
                        var lightInfo = new MyLightInfo
                        {
                            FlareId = FlareId.NULL,
                            SpotPosition = rMessage.Data.Position,
                            PointPosition = rMessage.Data.Position + rMessage.Data.PointPositionOffset * rMessage.Data.PointLight.Range * rMessage.Data.SpotLight.Direction,
                            Direction = rMessage.Data.SpotLight.Direction,
                            Up = rMessage.Data.SpotLight.Up,
                            CastsShadows = rMessage.Data.CastShadows,
                            ShadowsDistance = rMessage.Data.ShadowDistance,
                            ParentGID = rMessage.Data.ParentID,
                            UsedInForward = rMessage.Data.UseInForwardRender
                        };

                        if (rMessage.Data.Type.HasFlag(LightTypeEnum.PointLight))
                        {
                            MyLights.UpdatePointlight(light, rMessage.Data.PointLightOn,
                                rMessage.Data.PointLightIntensity, rMessage.Data.PointLight);
                        }

                        if (rMessage.Data.Type.HasFlag(LightTypeEnum.Spotlight))
                        {
                            MyLights.UpdateSpotlight(light, rMessage.Data.SpotLightOn, rMessage.Data.SpotLightIntensity, rMessage.Data.ReflectorConeMaxAngleCos,
                                rMessage.Data.SpotLight, MyManagers.FileTextures.GetTexture(rMessage.Data.ReflectorTexture, MyFileTextureEnum.CUSTOM));
                        }

                        MyLights.UpdateEntity(light, ref lightInfo);

                        MyLights.UpdateFlare(light, ref rMessage.Data.Glare);
                    }

                    break;
                }

                case MyRenderMessageEnum.SetLightShadowIgnore:
                {
                    var rMessage = (MyRenderMessageSetLightShadowIgnore)message;

                    MyLights.IgnoreShadowForEntity(rMessage.ID, rMessage.ID2);
                    break;
                }


                case MyRenderMessageEnum.ClearLightShadowIgnore:
                {
                    var rMessage = (MyRenderMessageClearLightShadowIgnore)message;

                    var light = MyLights.Get(rMessage.ID);
                    if(light != LightId.NULL)
                    {
                        MyLights.ClearIgnoredEntities(light);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateShadowSettings:
                {
                    var rMessage = (MyRenderMessageUpdateShadowSettings)message;
                    MyShadowCascades.Settings.CopyFrom(rMessage.Settings);
                    MyManagers.Shadow.SetSettings(rMessage.Settings);
                    break;
                }

                case MyRenderMessageEnum.UpdateNewLoddingSettings:
                {
                    var rMessage = (MyRenderMessageUpdateNewLoddingSettings)message;
                    var settings = rMessage.Settings;
                    MyManagers.GeometryRenderer.IsLodUpdateEnabled = settings.Global.IsUpdateEnabled;
                    MyManagers.Instances.SetLoddingSetting(settings.Global);
                    MyLodStrategy.SetSettings(settings.Global,
                        settings.GBuffer,
                        settings.CascadeDepths,
                        settings.SingleDepth);
                    break;
                }

                case MyRenderMessageEnum.UpdateNewPipelineSettings:
                {
                    var rMessage = (MyRenderMessageUpdateNewPipelineSettings) message;
                    var settings = rMessage.Settings;
                    MyManagers.ModelFactory.SetBlackListMaterialList(settings.BlackListMaterials);
                    MyMwmUtils.NoShadowCasterMaterials.Clear();
                    foreach(var material in settings.NoShadowCasterMaterials)
                        MyMwmUtils.NoShadowCasterMaterials.Add(material);
                    break;
                }

                case MyRenderMessageEnum.UpdateMaterialsSettings:
                {
                    var rMessage = (MyRenderMessageUpdateMaterialsSettings)message;
                    MyMaterialsSettings settings = rMessage.Settings;
                    MyManagers.GeometryTextureSystem.SetMaterialsSettings(settings);
                    break;
                }

                case MyRenderMessageEnum.UpdateFogSettings:
                {
                    var rMessage = (MyRenderMessageUpdateFogSettings)message;

                    if (m_debugOverrides.Fog)
                        MyRender11.Environment.Fog = rMessage.Settings;
                    else MyRender11.Environment.Fog.FogDensity = 0;

                    break;
                }


                case MyRenderMessageEnum.UpdateAtmosphereSettings:
                {
                    var rMessage = (MyRenderMessageUpdateAtmosphereSettings)message;

                    MyAtmosphereRenderer.UpdateSettings(rMessage.ID, rMessage.Settings);

                    break;
                }

                case MyRenderMessageEnum.EnableAtmosphere:
                {
                    var rMessage = (MyRenderMessageEnableAtmosphere)message;
                    MyAtmosphereRenderer.Enabled = rMessage.Enabled;
                    break;
                }

				case MyRenderMessageEnum.UpdateCloudLayerFogFlag:
				{
					var rMessage = (MyRenderMessageUpdateCloudLayerFogFlag)message;
					MyCloudRenderer.DrawFog = rMessage.ShouldDrawFog;
					break;
				}

                case MyRenderMessageEnum.UpdateRenderEnvironment:
                {
                    var rMessage = (MyRenderMessageUpdateRenderEnvironment)message;
                    MyRender11.Environment.Data = rMessage.Data;
                    m_resetEyeAdaptation |= rMessage.ResetEyeAdaptation;

                    /*MyRender11.Environment.DirectionalLightDir = VRageMath.Vector3.Normalize(rMessage.SunDirection);
                    if (rMessage.SunLightOn && m_debugOverrides.Sun)
                        MyRender11.Environment.DirectionalLightIntensity = rMessage.SunColor;
                    else MyRender11.Environment.DirectionalLightIntensity = new Vector3(0, 0, 0);

                    for (int lightIndex = 0; lightIndex < MyRender11.Environment.AdditionalSunIntensities.Length; ++lightIndex)
                    {
                        MyRender11.Environment.AdditionalSunIntensities[lightIndex] = rMessage.AdditionalSunIntensities[lightIndex];
                        MyRender11.Environment.AdditionalSunColors[lightIndex] = rMessage.AdditionalSunColors[lightIndex];
                        MyRender11.Environment.AdditionalSunDirections[lightIndex] = rMessage.AdditionalSunDirections[lightIndex];
                    }

                    MyRender11.Environment.DayTime = (float)(rMessage.DayTime - Math.Truncate(rMessage.DayTime));
                    MyRender11.Environment.SunDistance = rMessage.DistanceToSun;
                    MyRender11.Environment.SunColor = rMessage.SunColor;
                    MyRender11.Environment.SunMaterial = rMessage.SunMaterial;
                    MyRender11.Environment.SunSizeMultiplier = rMessage.SunSizeMultiplier;
                    MyRender11.Environment.SunBillboardEnabled = rMessage.SunBillboardEnabled;
                    MyRender11.Environment.PlanetFactor = rMessage.PlanetFactor;
                    MyRender11.Environment.Skybox = rMessage.DayBackgroundTexture;
                    MyRender11.Environment.NightSkybox = rMessage.NightBackgroundTexture;
                    MyRender11.Environment.NightSkyboxPrefiltered = rMessage.NightBackgroundPrefilteredTexture;
                    MyRender11.Environment.BackgroundOrientation = rMessage.BackgroundOrientation;
                    MyRender11.Environment.BackgroundColor = rMessage.BackgroundColor;

                    m_resetEyeAdaptation |= rMessage.ResetEyeAdaptation;*/

                    break;
                }

                case MyRenderMessageEnum.UpdateEnvironmentMap:
                {   
                    break;
                }

                case MyRenderMessageEnum.UpdateDebugOverrides:
                {
                    var rMessage = (MyRenderMessageUpdateDebugOverrides)message;

                    bool oldFXAA = FxaaEnabled;
                    m_debugOverrides = rMessage.Overrides;
                    bool newFXAA = FxaaEnabled;

                    if (oldFXAA != newFXAA)
                        UpdateAntialiasingMode(Settings.User.AntialiasingMode, Settings.User.AntialiasingMode);
                    break;
                }
                case MyRenderMessageEnum.UpdatePostprocessSettings:
                {
                    var rMessage = (MyRenderMessageUpdatePostprocessSettings)message;

                    Postprocess = rMessage.Settings;

                    if (Postprocess.EnableEyeAdaptation != rMessage.Settings.EnableEyeAdaptation)
                        m_resetEyeAdaptation = true;

                    break;
                }

                case MyRenderMessageEnum.UpdateSSAOSettings:
                {
                    var rMessage = (MyRenderMessageUpdateSSAOSettings)message;
                    MySSAO.Params = rMessage.Settings;
                    break;
                }

                case MyRenderMessageEnum.UpdateHBAO:
                {
                    var rMessage = (MyRenderMessageUpdateHBAO)message;
                    MyHBAO.Params = rMessage.Settings;
                    break;
                }

                #endregion

                #region Sprites

                case MyRenderMessageEnum.DrawSprite:
                case MyRenderMessageEnum.DrawSpriteNormalized:
                case MyRenderMessageEnum.DrawSpriteAtlas:
                case MyRenderMessageEnum.SpriteScissorPush:
                case MyRenderMessageEnum.SpriteScissorPop:
                case MyRenderMessageEnum.DrawString:
                {
                    m_drawQueue.Enqueue(message);
                    break;
                }

                #endregion

                #region Fonts and text

                case MyRenderMessageEnum.CreateFont:
                {
                    var createFontMessage = message as MyRenderMessageCreateFont;
                    MyRenderProxy.Assert(createFontMessage != null);

                    var renderFont = new MyRenderFont(createFontMessage.FontPath);
                    renderFont.LoadContent();
                    AddFont(createFontMessage.FontId, renderFont, createFontMessage.IsDebugFont);

                    break;
                }

                #endregion

                #region Textures
                // TODO: these messages need to be reviewed:
                case MyRenderMessageEnum.PreloadTextures:
                    {
                        var preloadMsg = message as MyRenderMessagePreloadTextures;

                        //MyFileTextureManager.Load(preloadMsg.Texture);
                        //MyTextureManager.PreloadTextures(preloadMsg.InDirectory, preloadMsg.Recursive);
                        //MyTextures.UnloadTexture(texMessage.Texture);

                        break;
                    }
                    
                case MyRenderMessageEnum.UnloadTexture:
                    {
                        var texMessage = (MyRenderMessageUnloadTexture)message;

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        texManager.DisposeTex(texMessage.Texture, true); // Ignore failures, the game can't know weather a texture is loaded.

                        break;
                    }

                case MyRenderMessageEnum.CreateGeneratedTexture:
                    {
                        var texMessage = (MyRenderMessageCreateGeneratedTexture)message;

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        texManager.CreateGeneratedTexture(texMessage.TextureName, texMessage.Width, texMessage.Height, texMessage.Type, texMessage.NumMipLevels);

                        break;
                    }

                case MyRenderMessageEnum.ResetGeneratedTexture:
                    {
                        var texMessage = (MyRenderMessageResetGeneratedTexture)message;

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        texManager.ResetGeneratedTexture(texMessage.TextureName, texMessage.Data);

                        break;
                    }

                case MyRenderMessageEnum.ReloadTextures:
                    {
                        var reloadMsg = (MyRenderMessageReloadTextures)message;

                        MyVoxelMaterials1.InvalidateMaterials();
                        MyMeshMaterials1.InvalidateMaterials();
                        MyManagers.FileTextures.DisposeTex(MyFileTextureManager.MyFileTextureHelper.IsAssetTextureFilter);
                        MyManagers.DynamicFileArrayTextures.ReloadAll();
                        MyGPUEmitters.ReloadTextures();
                        MyRender11.ReloadFonts();

                        break;
                    }

                case MyRenderMessageEnum.ReloadModels:
                    {
                        var reloadMsg = (MyRenderMessageReloadModels)message;

                        //MyMaterials.Clear();
                        MyAssetsLoader.ReloadMeshes();
                        MyRenderableComponent.MarkAllDirty();

                        break;
                    }
       
                #endregion

                case MyRenderMessageEnum.TakeScreenshot:
                {
                    var rMessage = (MyRenderMessageTakeScreenshot)message;

                    m_screenshot = new MyScreenshot(rMessage.PathToSave, rMessage.SizeMultiplier, rMessage.IgnoreSprites, rMessage.ShowNotification);

                    break;
                }

                case MyRenderMessageEnum.ReloadEffects:
                {
                    //MyShaderBundleFactory.ClearCache();
                    //MyShaderMaterial.ClearCache();
                    //MyShaderPass.ClearCache();

                    MyShaders.Recompile();
                    MyMaterialShaders.Recompile();

                    MyAtmosphereRenderer.RecomputeAtmospheres();

                    MyRenderableComponent.MarkAllDirty();

                    MyRender11.DisposeGrass();
                    break;
                }

                case MyRenderMessageEnum.PlayVideo:
                {
                    var rMessage = (MyRenderMessagePlayVideo)message;

                    MyVideoFactory.Create(rMessage.ID, rMessage.VideoFile);
                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if (video != null)
                    {
                        video.Volume = rMessage.Volume;
                    }

                    break;
                }

                case MyRenderMessageEnum.CloseVideo:
                {
                    var rMessage = (MyRenderMessageCloseVideo)message;
                    MyVideoFactory.Remove(rMessage.ID);
                    MyRenderProxy.RemoveMessageId(rMessage.ID, MyRenderProxy.ObjectType.Video);
                    break;
                }

                case MyRenderMessageEnum.UpdateGameplayFrame:
                {
                    var rMessage = (MyRenderMessageUpdateGameplayFrame)message;

                    GameplayFrameCounter = rMessage.GameplayFrame;

                    break;
                }

                case MyRenderMessageEnum.DrawVideo:
                {
                    var rMessage = (MyRenderMessageDrawVideo)message;

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if(video == null)
                        MyRenderProxy.Fail(String.Format("Invalid video id '{0}'", rMessage.ID));
                    else
                        video.Draw(rMessage.Rectangle, rMessage.Color, rMessage.FitMode);

                    break;
                }

                case MyRenderMessageEnum.UpdateVideo:
                {
                    var rMessage = (MyRenderMessageUpdateVideo)message;

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if(video == null)
                        MyRenderProxy.Fail(String.Format("Invalid video id '{0}'", rMessage.ID));
                    else
                        video.Update();

                    break;
                }

                case MyRenderMessageEnum.SetVideoVolume:
                {
                    var rMessage = (MyRenderMessageSetVideoVolume)message;

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if(video == null)
                        MyRenderProxy.Fail(String.Format("Invalid video id '{0}'", rMessage.ID));
                    else
                        video.Volume = rMessage.Volume;

                    break;
                }

                case MyRenderMessageEnum.VideoAdaptersRequest:
                {
                    MyRenderProxy.SendVideoAdapters(GetAdaptersList());
                    break;
                }

                case MyRenderMessageEnum.SwitchDeviceSettings:
                {
                    MyRenderProxy.RenderThread.SwitchSettings((message as MyRenderMessageSwitchDeviceSettings).Settings);
                    break;
                }

                case MyRenderMessageEnum.SwitchRenderSettings:
                {
                    var rMessage = (MyRenderMessageSwitchRenderSettings)message;
                    UpdateRenderSettings(rMessage.Settings);
                    break;
                }

                case MyRenderMessageEnum.SetMouseCapture:
                {
                    var umc = message as MyRenderMessageSetMouseCapture;

                    MyRenderProxy.RenderThread.SetMouseCapture(umc.Capture);
                    break;
                }

                case MyRenderMessageEnum.UnloadData:
                {
                    MyRender11.OnSessionEnd();
                    MyRender11.OnSessionStart();

                    break;
                }

                case MyRenderMessageEnum.CollectGarbage:
                {
                    GC.Collect();
                    break;
                }

                case MyRenderMessageEnum.SetFrameTimeStep:
                {
                    var rMessage = message as MyRenderMessageSetFrameTimeStep;
                    MyCommon.SetFrameTimeStep(rMessage.TimeStep);
                    break;
                }

                case MyRenderMessageEnum.ResetRandomness:
                {
                    var rMessage = message as MyRenderMessageResetRandomness;
                    MyCommon.SetRandomSeed(rMessage.Seed);
                    break;
                }

                case MyRenderMessageEnum.RenderColoredTexture:
                {
                    var rMessage = (MyRenderMessageRenderColoredTexture)message;
                    m_texturesToRender.AddRange(rMessage.texturesToRender);
                    break;
                }

                case MyRenderMessageEnum.CreateGPUEmitter:
                {
                    var rMessage = (MyRenderMessageCreateGPUEmitter)message;

                    //MyLight.Create(rMessage.ID);

                    MyGPUEmitters.Create(rMessage.ID);

                    break;
                }
                case MyRenderMessageEnum.UpdateGPUEmitters:
                {
                    var rMessage = (MyRenderMessageUpdateGPUEmitters)message;
                    MyGPUEmitters.UpdateData(rMessage.Emitters);
                    break;
                }
                case MyRenderMessageEnum.UpdateGPUEmittersTransform:
                {
                    var rMessage = (MyRenderMessageUpdateGPUEmittersTransform)message;
                    MyGPUEmitters.UpdateTransforms(rMessage.Emitters);
                    break;
                }
                case MyRenderMessageEnum.UpdateGPUEmittersLight:
                {
                    var rMessage = (MyRenderMessageUpdateGPUEmittersLight)message;
                    MyGPUEmitters.UpdateLight(rMessage.Emitters);
                    break;
                }
                case MyRenderMessageEnum.RemoveGPUEmitter:
                {
                    var rMessage = (MyRenderMessageRemoveGPUEmitter)message;
                    MyGPUEmitters.Remove(rMessage.GID, rMessage.Instant, false);
                    MyRenderProxy.RemoveMessageId(rMessage.GID, MyRenderProxy.ObjectType.GPUEmitter);
                    break;
                }

                #region Debug

                case MyRenderMessageEnum.DebugDrawPoint:
                case MyRenderMessageEnum.DebugDrawLine3D:
                case MyRenderMessageEnum.DebugDrawLine2D:
                case MyRenderMessageEnum.DebugDrawSphere:
                case MyRenderMessageEnum.DebugDrawAABB:
                case MyRenderMessageEnum.DebugDrawAxis:
                case MyRenderMessageEnum.DebugDrawOBB:
                case MyRenderMessageEnum.DebugDraw6FaceConvex:
                case MyRenderMessageEnum.DebugDrawCone:
                case MyRenderMessageEnum.DebugDrawTriangle:
                case MyRenderMessageEnum.DebugDrawCapsule:
                case MyRenderMessageEnum.DebugDrawText2D:
                case MyRenderMessageEnum.DebugDrawText3D:
                case MyRenderMessageEnum.DebugDrawModel:
                case MyRenderMessageEnum.DebugDrawTriangles:
                case MyRenderMessageEnum.DebugDrawPlane:
                case MyRenderMessageEnum.DebugDrawCylinder:
                case MyRenderMessageEnum.DebugDrawFrustrum:
                case MyRenderMessageEnum.DebugDrawMesh:
                case MyRenderMessageEnum.DebugWaitForPresent:
                case MyRenderMessageEnum.DebugClearPersistentMessages:
                {
                    m_debugDrawMessages.Enqueue(message);
                }
                break;

                case MyRenderMessageEnum.DebugCrashRenderThread:
                {
                    throw new InvalidOperationException("Forced exception");
                }

                case MyRenderMessageEnum.DebugPrintAllFileTexturesIntoLog:
                {
                    MyRender11.Log.WriteLine(MyManagers.FileTextures.GetFileTexturesDesc().ToString());;
                    MyRender11.Log.WriteLine(MyManagers.FileArrayTextures.GetFileTexturesDesc().ToString());
                    break;
                }
                #endregion
            }
        }
    }
}