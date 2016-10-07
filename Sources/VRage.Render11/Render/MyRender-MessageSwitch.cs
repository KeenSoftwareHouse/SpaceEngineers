
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
using VRageRender.Import;
using VRageRender.Messages;
using VRageRender.Profiler;
using VRageRender.Vertex;

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
                    if (actor != null)
                    {
                        actor.GetSkinning().SetSkeleton(rMessage.SkeletonBones, rMessage.SkeletonIndices);
                    }

                    //var entity = MyComponents.GetEntity(rMessage.CharacterID);
                    //MyComponents.SetSkeleton(entity, rMessage.SkeletonBones, rMessage.SkeletonIndices);

                    break;
                };

                case MyRenderMessageEnum.SetCharacterTransforms:
                {
                    var rMessage = (MyRenderMessageSetCharacterTransforms)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.CharacterID);
                    if (actor != null)
                    {
                        actor.GetSkinning().SetAnimationBones(rMessage.BoneAbsoluteTransforms, rMessage.BoneDecalUpdates);
                    }
                    //var entity = MyComponents.GetEntity(rMessage.CharacterID);
                    //MyComponents.SetAnimation(entity, rMessage.RelativeBoneTransforms);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderEntity:
                {
                    var rMessage = (MyRenderMessageUpdateRenderEntity)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor == null)
                        break;

                    var renderableComponent = actor.GetRenderable();
                    if (renderableComponent == null)
                        break;

                    if (rMessage.ColorMaskHSV.HasValue)
                    {
                        actor.GetRenderable().SetKeyColor(new Vector4(ColorFromMask(rMessage.ColorMaskHSV.Value), 0));
                    }
                    actor.GetRenderable().SetDithering(rMessage.Dithering);

                    break;
                }

                case MyRenderMessageEnum.ChangeModel:
                {
                    var rMessage = (MyRenderMessageChangeModel)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null && actor.GetRenderable() != null)
                    {
                        var r = actor.GetRenderable();
                        var modelId = MyMeshes.GetMeshId(X.TEXT_(rMessage.Model), rMessage.Rescale);
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

                case MyRenderMessageEnum.UpdateCockpitGlass:
                {
                    var rMessage = (MyRenderMessageUpdateCockpitGlass)message;

                    //if (MyRender11.Environment.CockpitGlass == null)
                    //{
                    //    MyRender11.Environment.CockpitGlass = MyActorFactory.CreateSceneObject();
                    //}

                    //MyRender11.Environment.CockpitGlass.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(rMessage.Model)));
                    //MyRender11.Environment.CockpitGlass.SetVisibility(rMessage.Visible);
                    //MyRender11.Environment.CockpitGlass.MarkRenderDirty();

                    //var matrix = (Matrix)rMessage.WorldMatrix;
                    //MyRender11.Environment.CockpitGlass.SetMatrix(ref matrix);


                    break;
                }

                case MyRenderMessageEnum.CreateRenderVoxelDebris:
                {
                    var rMessage = (MyRenderMessageCreateRenderVoxelDebris)message;

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

					Matrix m = (Matrix)rMessage.WorldMatrix;

					var actor = MyActorFactory.CreateSceneObject();
					if (rMessage.Model != null)
					{
						var model = MyAssetsLoader.ModelRemap.Get(rMessage.Model, rMessage.Model);

                        actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT_(model), rMessage.Rescale));
					}

					actor.SetID(rMessage.ID);
					actor.SetMatrix(ref rMessage.WorldMatrix);
                    var renderable = actor.GetRenderable();

					renderable.m_additionalFlags |= MyProxiesFactory.GetRenderableProxyFlags(rMessage.Flags);
                    renderable.m_depthBias = rMessage.DepthBias;

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
                    MyRenderProxy.RemoveMessageId(rMessage.ID, MyRenderProxy.ObjectType.ScreenDecal);
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
                    else MyRenderProxy.Assert(false);
                    break;
                }

                case MyRenderMessageEnum.RemoveRenderObject:
                {
                    var rMessage = (MyRenderMessageRemoveRenderObject)message;

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
                                    if (actor.GetRenderable() != null && actor.GetRenderable().GetModel().Info.Dynamic)
                                    {
                                        MyMeshes.RemoveMesh(actor.GetRenderable().GetModel());
                                    }

                                    actor.Destruct();
                                    MyScreenDecals.RemoveEntityDecals(rMessage.ID);
                                }
                                else MyRenderProxy.Assert(false);
                                break;
                            case MyRenderProxy.ObjectType.InstanceBuffer:
                                MyInstancing.Remove(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Light:
                                MyLights.Remove(rMessage.ID);
                                break;
                            case MyRenderProxy.ObjectType.Clipmap:
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
                    else MyRenderProxy.Assert(false);
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
                                {
                                    actor.SetVisibility(rMessage.Visible);
                                }
                                break;
                        }
                    }
                    else MyRenderProxy.Assert(false);
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

                    //var instancing = MyIDTracker<MyInstancingComponent>.FindByID(rMessage.ID);
                    //if(instancing != null)
                    //{
                    //    instancing.UpdateGeneric(rMessage.InstanceData, rMessage.Capacity);
                    //}

                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                    {
                        // TODO: Do something :P
                        MyInstancing.UpdateGenericSettings(handle, rMessage.SetPerInstanceLod);

                    }
                    else
                    {
                        // MyRenderProxy.Assert(handle != InstancingId.NULL, "No instance buffer with ID " + rMessage.ID);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderInstanceBufferRange:
                {
                    var rMessage = (MyRenderMessageUpdateRenderInstanceBufferRange)message;

                    //var instancing = MyIDTracker<MyInstancingComponent>.FindByID(rMessage.ID);
                    //if(instancing != null)
                    //{
                    //    instancing.UpdateGeneric(rMessage.InstanceData, rMessage.Capacity);
                    //}

                    // TODO: Turn this into partial update.
                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                    {
                        MyInstancing.UpdateGeneric(handle, rMessage.InstanceData, rMessage.InstanceData.Length);
                    }
                    else
                    {
                        // Debug.Assert(handle != InstancingId.NULL, "No instance buffer with ID " + rMessage.ID);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageUpdateRenderCubeInstanceBuffer)message;

                    //var instancing = MyIDTracker<MyInstancingComponent>.FindByID(rMessage.ID);
                    //if (instancing != null)
                    //{
                    //    instancing.UpdateCube(rMessage.InstanceData, rMessage.Capacity);
                    //}

                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                    {
                        MyInstancing.UpdateCube(MyInstancing.Get(rMessage.ID), rMessage.InstanceData, rMessage.DecalsData, rMessage.Capacity);
                    }
                    else
                        Debug.Fail("No instance buffer with ID " + rMessage.ID);

                    break;
                }

                case MyRenderMessageEnum.SetInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageSetInstanceBuffer)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    //var instancing = MyIDTracker<MyInstancingComponent>.FindByID(rMessage.InstanceBufferId);

                    if (actor != null)
                    {
                        //if (actor.GetComponent(MyActorComponentEnum.Instancing) != instancing)
                        //{
                        //    actor.AddComponent(instancing);
                        //}
                        //actor.SetLocalAabb(rMessage.LocalAabb);
                        //actor.GetRenderable().SetInstancingCounters(rMessage.InstanceCount, rMessage.InstanceStart);

                        actor.GetRenderable().SetInstancing(MyInstancing.Get(rMessage.InstanceBufferId));
                        actor.SetLocalAabb(rMessage.LocalAabb);
                        actor.GetRenderable().SetInstancingCounters(rMessage.InstanceCount, rMessage.InstanceStart);
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

                    break;
                }

                case MyRenderMessageEnum.CreateLineBasedObject:
                {
                    var rMessage = (MyRenderMessageCreateLineBasedObject)message;

                    var actor = MyActorFactory.CreateSceneObject();
                    //actor.GetRenderable().SetModel(new MyDynamicMesh());

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref MatrixD.Identity);

                    MyMeshMaterials1.GetMaterialId("__ROPE_MATERIAL", null, rMessage.ColorMetalTexture, rMessage.NormalGlossTexture, rMessage.ExtensionTexture, MyMesh.DEFAULT_MESH_TECHNIQUE);
                    actor.GetRenderable().SetModel(MyMeshes.CreateRuntimeMesh(X.TEXT_("LINE" + rMessage.ID), 1, true));

                    break;
                }

                case MyRenderMessageEnum.UpdateLineBasedObject:
                {
                    var rMessage = (MyRenderMessageUpdateLineBasedObject)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null)
                    {
                        //var mesh = actor.GetRenderable().GetMesh() as MyDynamicMesh;

                        MyVertexFormatPositionH4 [] stream0;
                        MyVertexFormatTexcoordNormalTangent [] stream1;

                        MyLineHelpers.GenerateVertexData(ref rMessage.WorldPointA, ref rMessage.WorldPointB, 
                            out stream0, out stream1);

                        var indices = MyLineHelpers.GenerateIndices(stream0.Length);
                        var sections = new MySectionInfo[] 
                        { 
                            new MySectionInfo { TriCount = indices.Length / 3, IndexStart = 0, MaterialName = "__ROPE_MATERIAL" } 
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

                    MyRenderProxy.Assert(false, "MyRenderMessageSetRenderEntityData is deprecated!");

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
                            MyVertexFormatTexcoordNormalTangent[] stream1 = new MyVertexFormatTexcoordNormalTangent[verticesNum];
                            for (int i = 0; i < verticesNum; i++)
                            {
                                stream0[i] = new MyVertexFormatPositionH4(rMessage.ModelData.Positions[i]);
                                stream1[i] = new MyVertexFormatTexcoordNormalTangent(
                                    rMessage.ModelData.TexCoords[i], rMessage.ModelData.Normals[i], rMessage.ModelData.Tangents[i]);
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
                    if (actor != null)
                    {
                        // careful, lod is ignored after all (properties apply to all lods)
                        var key = new MyEntityMaterialKey { LOD = rMessage.LOD, Material = X.TEXT_(rMessage.MaterialName) };

                        if(rMessage.Enabled.HasValue)
                        {
                            if (!MyScene.EntityDisabledMaterials.ContainsKey(rMessage.ID))
                            {
                                MyScene.EntityDisabledMaterials.Add(rMessage.ID, new HashSet<MyEntityMaterialKey>());
                            }

                            if (!rMessage.Enabled.Value)
                            {
                                MyScene.EntityDisabledMaterials[rMessage.ID].Add(key);
                            }
                            else
                            {
                                MyScene.EntityDisabledMaterials[rMessage.ID].Remove(key);
                            }
                        }

                        var renderableComponent = actor.GetRenderable();

                        if ((rMessage.Emissivity.HasValue || rMessage.DiffuseColor.HasValue) && !renderableComponent.ModelProperties.ContainsKey(key))
                        {
                            renderableComponent.ModelProperties[key] = new MyModelProperties();
                        }

                        if(rMessage.Emissivity.HasValue)
                        {
                            renderableComponent.ModelProperties[key].Emissivity = rMessage.Emissivity.Value;
                        }

                        if(rMessage.DiffuseColor.HasValue)
                        {
                            renderableComponent.ModelProperties[key].ColorMul = rMessage.DiffuseColor.Value;
                        }

                        actor.MarkRenderDirty();

                        MyOutline.HandleOutline(rMessage.ID, rMessage.MeshIndex, rMessage.OutlineColor, rMessage.OutlineThickness, rMessage.PulseTimeInFrames);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateModelHighlight:
                {
                    var rMessage = (MyRenderMessageUpdateModelHighlight)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null)
                    {
                        MyOutline.HandleOutline(rMessage.ID, rMessage.SectionIndices, rMessage.OutlineColor, rMessage.Thickness, rMessage.PulseTimeInFrames, rMessage.InstanceIndex);
                        if (rMessage.SubpartIndices != null)
                            foreach (uint index in rMessage.SubpartIndices)
                                MyOutline.HandleOutline(index, null, rMessage.OutlineColor, rMessage.Thickness, rMessage.PulseTimeInFrames, -1);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateColorEmissivity:
                {
                    var rMessage = (MyRenderMessageUpdateColorEmissivity)message;
                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null)
                    {
                        actor.GetRenderable().UpdateColorEmissivity(rMessage.LOD, rMessage.MaterialName, rMessage.DiffuseColor, rMessage.Emissivity);
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
                    if (actor != null)
                    {
                        var r = actor.GetRenderable();
                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT_(rMessage.MaterialName) };

                        if (!r.ModelProperties.ContainsKey(key))
                        {
                            r.ModelProperties[key] = new MyModelProperties();
                        }

                        if (r.ModelProperties[key].TextureSwaps == null)
                        {
                            r.ModelProperties[key].TextureSwaps = new List<MyMaterialTextureSwap>();

                            foreach(var s in rMessage.Changes)
                            {
                                r.ModelProperties[key].TextureSwaps.Add(new MyMaterialTextureSwap { 
                                    TextureName = X.TEXT_(s.TextureName), 
                                    MaterialSlot = s.MaterialSlot
                                });
                            }
                        }
                        else
                        {
                            foreach (var s in rMessage.Changes)
                            {
                                bool swapped = false;
                                for(int i=0; i<r.ModelProperties[key].TextureSwaps.Count; ++i)
                                {
                                    if(r.ModelProperties[key].TextureSwaps[i].MaterialSlot == s.MaterialSlot)
                                    {
                                        r.ModelProperties[key].TextureSwaps[i] = new MyMaterialTextureSwap
                                        {
                                            TextureName = X.TEXT_(s.TextureName),
                                            MaterialSlot = s.MaterialSlot
                                        };
                                        swapped = true;
                                        break;
                                    }
                                }

                                if(!swapped)
                                {
                                    r.ModelProperties[key].TextureSwaps.Add(new MyMaterialTextureSwap
                                        {
                                            TextureName = X.TEXT_(s.TextureName),
                                            MaterialSlot = s.MaterialSlot
                                        });
                                }
                            }
                        }

                        r.FreeCustomRenderTextures(key);

                        actor.MarkRenderDirty();
                    }

                    rMessage.Changes.Clear();
                   
                    break;
                }

                case MyRenderMessageEnum.DrawTextToMaterial:
                {
                    var rMessage = (MyRenderMessageDrawTextToMaterial)message;

                    //rMessage.EntityId
                    //rMessage.FontColor
                    //rMessage.MaterialName
                    //rMessage.Text;
                    //rMessage.TextScale;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.RenderObjectID);
                    if (actor != null)
                    {
                        var renderableComponent = actor.GetRenderable();
                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT_(rMessage.MaterialName) };

                        if (!renderableComponent.ModelProperties.ContainsKey(key))
                            renderableComponent.ModelProperties[key] = new MyModelProperties();
                        else
                            renderableComponent.ModelProperties[key].TextureSwaps = null;

                        IRtvTexture handle = renderableComponent.ModelProperties[key].CustomRenderedTexture;
                        if (handle == null && MyModelProperties.CustomTextures < MyModelProperties.MaxCustomTextures)
                        {
                            MyRwTextureManager texManager = MyManagers.RwTextures;
                            handle = texManager.CreateRtv("RenderableComponent.ModelProperties[key].CustomRenderedTexture", rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, 
                                optionFlags: ResourceOptionFlags.GenerateMipMaps,
                                mipmapLevels: 1);
                            renderableComponent.ModelProperties[key].CustomRenderedTexture = handle;
                            ++MyModelProperties.CustomTextures;
                        }

                        if (handle != null)
                        {
                            var clearColor = new SharpDX.Color4(rMessage.BackgroundColor.PackedValue);
                            clearColor.Alpha = 0;
                            MyRender11.RC.ClearRtv(handle, clearColor);

                            // my sprites renderer -> push state
                            MySpritesRenderer.PushState(new Vector2(rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution));


                            MySpritesRenderer.DrawText(Vector2.Zero, new StringBuilder(rMessage.Text), rMessage.FontColor, rMessage.TextScale);
                            // render text with fonts to rt
                            // update texture of proxy
                            MySpritesRenderer.Draw(handle, new MyViewport(rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution),
                                MyBlendStateManager.BlendAlphaPremultNoAlphaChannel);

                            // render to rt
                            // my sprites renderer -> pop state
                            MySpritesRenderer.PopState();


                            MyRender11.RC.GenerateMips(handle);

                            actor.MarkRenderDirty();
                        }
                        else
                        {
                            MyRenderProxy.TextNotDrawnToTexture(rMessage.EntityId);
                        }
                    }
                    else
                    {
                        MyRenderProxy.TextNotDrawnToTexture(rMessage.EntityId);
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

                    var clipmap = new MyClipmapHandler(rMessage.ClipmapId, rMessage.ScaleGroup, rMessage.WorldMatrix, rMessage.SizeLod0, rMessage.Position, rMessage.PlanetRadius, rMessage.SpherizeWithDistance, rMessage.AdditionalRenderFlags, rMessage.PrunningFunc);
                    MyClipmapFactory.ClipmapByID[rMessage.ClipmapId] = clipmap;
                    clipmap.Base.LoadContent();

                    break;
                }

                case MyRenderMessageEnum.UpdateClipmapCell:
                {
                    var rMessage = (MyRenderMessageUpdateClipmapCell)message;

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);

                    MyRenderProxy.Assert(clipmap != null);

                    if (clipmap != null)
                    {
                        clipmap.Base.UpdateCell(rMessage);
                    }
                    break;
                }

                case MyRenderMessageEnum.UpdateMergedVoxelMesh:
                {
                    var rMessage = (MyRenderMessageUpdateMergedVoxelMesh)message;

                    MyClipmapHandler clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);
                        if (clipmap != null)
                            clipmap.UpdateMergedMesh(rMessage);
                        break;
                    }

                case MyRenderMessageEnum.ResetMergedVoxels:
                    {
                        var rMessage = (MyRenderMessageResetMergedVoxels)message;

                        foreach(var clipmapHandler in MyClipmapFactory.ClipmapByID.Values)
                        {
                            if (clipmapHandler != null)
                                clipmapHandler.ResetMergedMeshes();
                        }
                        break;
                    }

                case MyRenderMessageEnum.InvalidateClipmapRange:
                {
                    var rMessage = (MyRenderMessageInvalidateClipmapRange)message;

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);
                    if (clipmap != null)
                    {
                        clipmap.Base.InvalidateRange(rMessage.MinCellLod0, rMessage.MaxCellLod0);
                    }

                    break;
                }

                case MyRenderMessageEnum.CreateRenderVoxelMaterials:
                {
                    var rMessage = (MyRenderMessageCreateRenderVoxelMaterials)message;

                    MyRenderProxy.Assert(MyVoxelMaterials1.CheckIndices(rMessage.Materials));
                    MyVoxelMaterials1.Set(rMessage.Materials);

                    rMessage.Materials = null;

                    break;
                }


                case MyRenderMessageEnum.UpdateRenderVoxelMaterials:
                {
                    var rMessage = (MyRenderMessageUpdateRenderVoxelMaterials)message;

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

                    var light = MyLights.Get(rMessage.Data.ID);

                    if(light != LightId.NULL)
                    {
                        var lightInfo = new MyLightInfo
                        {
                            FlareId = FlareId.NULL,
                            SpotPosition = rMessage.Data.Position,
                            PointPosition = rMessage.Data.Position + rMessage.Data.PointPositionOffset * rMessage.Data.PointLight.Range * rMessage.Data.SpotLight.Direction,
                            CastsShadows = rMessage.Data.CastShadows,
                            ShadowsDistance = rMessage.Data.ShadowDistance,
                            ParentGID = rMessage.Data.ParentID,
                            UsedInForward = rMessage.Data.UseInForwardRender
                        };

                        MyLights.UpdateEntity(light, ref lightInfo);

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

                        light.FlareId = MyFlareRenderer.Update(rMessage.Data.ParentID, light.FlareId, rMessage.Data.Glare);
                    }

                    break;
                }

                case MyRenderMessageEnum.SetLightShadowIgnore:
                {
                    var rMessage = (MyRenderMessageSetLightShadowIgnore)message;

                    var light = MyLights.Get(rMessage.ID);
                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID2);

                    if(light != LightId.NULL && actor != null)
                    {
                        if(!MyLights.IgnoredEntitites.ContainsKey(light))
                        {
                            MyLights.IgnoredEntitites[light] = new HashSet<uint>();
                        }
                        MyLights.IgnoredEntitites[light].Add(rMessage.ID2);
                    }

                    break;
                }


                case MyRenderMessageEnum.ClearLightShadowIgnore:
                {
                    var rMessage = (MyRenderMessageClearLightShadowIgnore)message;

                    var light = MyLights.Get(rMessage.ID);
                    if(light != LightId.NULL)
                    {
                        MyLights.IgnoredEntitites.Remove(light);
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
                        UpdateAntialiasingMode(m_renderSettings.AntialiasingMode, m_renderSettings.AntialiasingMode);
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
                    MyHBAO.Params = rMessage.Data;
                    break;
                }

                #endregion

                #region Sprites
                case MyRenderMessageEnum.DrawSprite:
                case MyRenderMessageEnum.DrawSpriteNormalized:
                case MyRenderMessageEnum.DrawSpriteAtlas:
                case MyRenderMessageEnum.SpriteScissorPush:
                case MyRenderMessageEnum.SpriteScissorPop:
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

                case MyRenderMessageEnum.DrawString:
                {
                    m_drawQueue.Enqueue(message);
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
                        texManager.DisposeTex(texMessage.Texture);
                        //MyTextures.UnloadTexture(texMessage.Texture);

                        break;
                    }

                case MyRenderMessageEnum.ReloadTextures:
                    {
                        var reloadMsg = (MyRenderMessageReloadTextures)message;

                        MyVoxelMaterials1.InvalidateMaterials();
                        MyMeshMaterials1.InvalidateMaterials();
                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        texManager.DisposeTex(MyFileTextureManager.MyFileTextureHelper.IsAssetTextureFilter);
                        MyGPUEmitters.ReloadTextures();
                        MyRender11.ReloadFonts();
                        //MyTextureManager.UnloadTextures();
                        //MyMaterialProxyFactory.ReloadTextures();

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

                    foreach (var f in MyComponentFactory<MyFoliageComponent>.GetAll())
                    {
                        f.Dispose();
                    }

                    break;
                }

                case MyRenderMessageEnum.ReloadGrass:
                {
                    MyRenderProxy.ReloadEffects();  // Need some delay
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
                    if (video != null)
                    {
                        video.Draw(rMessage.Rectangle, rMessage.Color, rMessage.FitMode);
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateVideo:
                {
                    var rMessage = (MyRenderMessageUpdateVideo)message;

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if(video != null)
                    {
                        video.Update();
                    }

                    break;
                }

                case MyRenderMessageEnum.SetVideoVolume:
                {
                    var rMessage = (MyRenderMessageSetVideoVolume)message;

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if (video != null)
                    {
                        video.Volume = rMessage.Volume;
                    }

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
                    if (rMessage.Settings.HasValue)
                        UpdateRenderSettings(rMessage.Settings.Value);

                    if (rMessage.SettingsOld.HasValue)
                        MyRender11.Settings = rMessage.SettingsOld.Value;

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
                    MyRender11.UnloadData();
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
                    MyGPUEmitters.UpdateTransforms(rMessage.GIDs, rMessage.Transforms);
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