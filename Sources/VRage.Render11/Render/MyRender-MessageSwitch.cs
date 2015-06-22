
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender.Profiler;
using VRageRender.Resources;
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

        private static void ProcessMessage(IMyRenderMessage message)
        {
#if DEBUG
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

        internal static StringBuilder m_messageTracker = new StringBuilder();

        private static void ProcessMessageInternal(IMyRenderMessage message)
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
                    var rMessage = (IMyRenderMessage)message;

                    m_drawQueue.Enqueue(rMessage);

                    m_messageTracker.Clear();

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
                    Matrix worldMatrixF = rMessage.WorldMatrix;
                    //actor.GetRenderable().SetModel(MyAssetsLoader.GetModel(rMessage.Model));
                    actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(rMessage.Model)));
                    actor.SetMatrix(ref worldMatrixF);

                    if (rMessage.ColorMaskHSV.HasValue)
                    {
                        var color = ColorFromMask(rMessage.ColorMaskHSV.Value);
                        actor.GetRenderable().SetKeyColor(new Vector4(color, 1));
                    }

                    actor.SetID(rMessage.ID);

                    //var entity = MyComponents.CreateEntity(rMessage.ID);
                    //MyComponents.CreateRenderable(
                    //    entity,
                    //    MyMeshes.GetMeshId(X.TEXT(rMessage.Model)),
                    //    rMessage.ColorMaskHSV.HasValue ? rMessage.ColorMaskHSV.Value : Vector3.One);
                    //MyComponents.SetMatrix(entity, ref rMessage.WorldMatrix);

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
                        actor.GetSkinning().SetAnimationBones(rMessage.RelativeBoneTransforms);
                    }
                    //var entity = MyComponents.GetEntity(rMessage.CharacterID);
                    //MyComponents.SetAnimation(entity, rMessage.RelativeBoneTransforms);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderEntity:
                {
                    var rMessage = (MyRenderMessageUpdateRenderEntity)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null && actor.GetRenderable() != null)
                    {
                        if(rMessage.ColorMaskHSV.HasValue)
                        {
                            actor.GetRenderable().SetKeyColor(new Vector4(ColorFromMask(rMessage.ColorMaskHSV.Value), 0));
                        }
                        actor.GetRenderable().SetDithering(rMessage.Dithering);
                    }

                    break;
                }

                case MyRenderMessageEnum.ChangeModel:
                {
                    var rMessage = (MyRenderMessageChangeModel)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null && actor.GetRenderable() != null)
                    {
                        var r = actor.GetRenderable();

                        var modelId = MyMeshes.GetMeshId(X.TEXT(rMessage.Model));
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

                case MyRenderMessageEnum.CreateRenderEntity:
                {
                    var rMessage = (MyRenderMessageCreateRenderEntity)message;

                    Matrix m = (Matrix)rMessage.WorldMatrix;

                    var actor = MyActorFactory.CreateSceneObject();
                    if (rMessage.Model != null) 
                    {
                        var model = MyAssetsLoader.ModelRemap.Get(rMessage.Model, rMessage.Model);

                        actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(model)));
                        //if (MyDestructionMesh.ModelsDictionary.ContainsKey(model))
                        //{
                        //    //actor.GetRenderable().SetModel(MyDestructionMesh.ModelsDictionary.Get(model));
                        //    actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(model)));
                        //}
                        //else
                        //{
                        //    //actor.GetRenderable().SetModel(MyAssetsLoader.GetModel(model));
                        //    actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(model)));
                        //}
                    }

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref m);

                    break;
                }

                case MyRenderMessageEnum.UpdateCockpitGlass:
                {
                    var rMessage = (MyRenderMessageUpdateCockpitGlass)message;

                    //if (MyEnvironment.CockpitGlass == null)
                    //{
                    //    MyEnvironment.CockpitGlass = MyActorFactory.CreateSceneObject();
                    //}

                    //MyEnvironment.CockpitGlass.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(rMessage.Model)));
                    //MyEnvironment.CockpitGlass.SetVisibility(rMessage.Visible);
                    //MyEnvironment.CockpitGlass.MarkRenderDirty();

                    //var matrix = (Matrix)rMessage.WorldMatrix;
                    //MyEnvironment.CockpitGlass.SetMatrix(ref matrix);


                    break;
                }

                case MyRenderMessageEnum.CreateRenderVoxelDebris:
                {
                    var rMessage = (MyRenderMessageCreateRenderVoxelDebris)message;

                    Matrix m = (Matrix)rMessage.WorldMatrix;

                    var actor = MyActorFactory.CreateSceneObject();
                    if (rMessage.Model != null)
                    {
                        actor.GetRenderable().SetModel(MyMeshes.GetMeshId(X.TEXT(rMessage.Model)));
                    }

                    actor.SetID(rMessage.ID);
                    actor.SetMatrix(ref m);

                    MyRenderableComponent.DebrisEntityVoxelMaterial[rMessage.ID] = rMessage.VoxelMaterialIndex;

                    break;
                }

                case MyRenderMessageEnum.CreateScreenDecal:
                {
                    var rMessage = (MyRenderMessageCreateScreenDecal)message;

                    MyScreenDecals.AddDecal(rMessage.ID, rMessage.ParentID, rMessage.LocalOBB, rMessage.DecalMaterial);

                    break;
                }

                case MyRenderMessageEnum.RemoveDecal:
                {
                    var rMessage = (MyRenderMessageRemoveDecal)message;

                    MyScreenDecals.RemoveDecal(rMessage.ID);

                    break;
                }

                case MyRenderMessageEnum.RegisterDecalsMaterials:
                {
                    var rMessage = (MyRenderMessageRegisterScreenDecalsMaterials)message;

                    MyScreenDecals.RegisterMaterials(rMessage.MaterialsNames, rMessage.MaterialsDescriptions);


                    break;
                }

                case MyRenderMessageEnum.UpdateRenderObject:
                { 
                    var rMessage = (MyRenderMessageUpdateRenderObject)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null)
                    {
                        Matrix m = (Matrix)rMessage.WorldMatrix;
                        actor.SetMatrix(ref m);
                        if(rMessage.AABB.HasValue)
                        { 
                            actor.SetAabb((BoundingBox)rMessage.AABB.Value);
                        }
                        
                    }
                    else
                    {
                        if (MyClipmapFactory.ClipmapByID.ContainsKey(rMessage.ID))
                        {
                            MyClipmapFactory.ClipmapByID[rMessage.ID].UpdateWorldMatrix(ref rMessage.WorldMatrix);
                        }
                    }

                    //var entity = MyComponents.GetEntity(rMessage.ID);
                    //if(entity != EntityId.NULL)
                    //{
                    //    MyComponents.SetMatrix(entity, ref rMessage.WorldMatrix);
                    //    if (rMessage.AABB.HasValue)
                    //    {
                    //        var aabb = rMessage.AABB.Value;
                    //        MyComponents.SetAabb(entity, ref aabb);
                    //    }
                    //}

                    break;
                }

                case MyRenderMessageEnum.RemoveRenderObject:
                {
                    var rMessage = (MyRenderMessageRemoveRenderObject)message;

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

                    var instancing = MyInstancing.Get(rMessage.ID);
                    if(instancing != InstancingId.NULL)
                    {
                        MyInstancing.Remove(rMessage.ID, instancing);
                    }

                    var light = MyLights.Get(rMessage.ID);
                    if(light != LightId.NULL)
                    {
                        MyLights.Remove(rMessage.ID, light);
                    }

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ID);
                    if(clipmap != null)
                    {
                        clipmap.RemoveFromUpdate();
                    }

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderObjectVisibility:
                {
                    var rMessage = (MyRenderMessageUpdateRenderObjectVisibility)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.ID);
                    if (actor != null)
                    {
                        actor.SetVisibility(rMessage.Visible);

                        //if(rMessage.NearFlag)
                        //{
                        //    actor.GetRenderable().m_additionalFlags = MyRenderableProxyFlags.InvertFaceCulling;
                        //    actor.MarkRenderDirty();
                        //}
                        //else
                        //{
                        //    actor.GetRenderable().m_additionalFlags = 0;
                        //    actor.MarkRenderDirty();
                        //}
                    }

                    break;
                }


                case MyRenderMessageEnum.CreateRenderInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageCreateRenderInstanceBuffer)message;

                    //var instancing = MyComponentFactory<MyInstancingComponent>.Create();
                    //instancing.SetID(rMessage.ID);
                    //instancing.Init(rMessage.Type);
                    //instancing.SetDebugName(rMessage.DebugName);

                    MyInstancing.Create(rMessage.ID, rMessage.Type, rMessage.DebugName);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderInstanceBuffer:
                {
                    var rMessage = (MyRenderMessageUpdateRenderInstanceBuffer)message;

                    //var instancing = MyIDTracker<MyInstancingComponent>.FindByID(rMessage.ID);
                    //if(instancing != null)
                    //{
                    //    instancing.UpdateGeneric(rMessage.InstanceData, rMessage.Capacity);
                    //}

                    var handle = MyInstancing.Get(rMessage.ID);

                    if (handle != InstancingId.NULL)
                    {
                        MyInstancing.UpdateGeneric(handle, rMessage.InstanceData, rMessage.Capacity);
                    }
                    else
                    {
                        Debug.Assert(handle != InstancingId.NULL, "No instance buffer with ID " + rMessage.ID);
                    }

                    rMessage.InstanceData.Clear();

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
                        MyInstancing.UpdateCube(MyInstancing.Get(rMessage.ID), rMessage.InstanceData, rMessage.Capacity);
                    }
                    else
                    {
                        Debug.Assert(handle != InstancingId.NULL, "No instance buffer with ID " + rMessage.ID);
                    }

                    rMessage.InstanceData.Clear();

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
                    actor.SetMatrix(ref m);

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
                    actor.SetMatrix(ref Matrix.Identity);

                    MyMeshMaterials1.GetMaterialId("__ROPE_MATERIAL", null, rMessage.ColorMetalTexture, rMessage.NormalGlossTexture, rMessage.ExtensionTexture, MyMesh.DEFAULT_MESH_TECHNIQUE);
                    actor.GetRenderable().SetModel(MyMeshes.CreateRuntimeMesh(X.TEXT("LINE" + rMessage.ID), 1, true));

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

                        MyMeshes.UpdateRuntimeMesh(MyMeshes.GetMeshId(X.TEXT("LINE" + rMessage.ID)), 
                            indices, 
                            stream0, 
                            stream1, 
                            sections,
                            (BoundingBox)MyLineHelpers.GetBoundingBox(ref rMessage.WorldPointA, ref rMessage.WorldPointB));

                        //actor.SetAabb((BoundingBox)MyLineHelpers.GetBoundingBox(ref rMessage.WorldPointA, ref rMessage.WorldPointB));
                        actor.MarkRenderDirty();

                        var matrix = Matrix.CreateTranslation((Vector3)(rMessage.WorldPointA + rMessage.WorldPointB) * 0.5f);
                        actor.SetMatrix(ref matrix);
                    }

                    break;
                }

                case MyRenderMessageEnum.SetRenderEntityData:
                {
                    var rMessage = (MyRenderMessageSetRenderEntityData)message;

                    Debug.Assert(false, "MyRenderMessageSetRenderEntityData is deprecated!");

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
                            var id = MyMeshes.CreateRuntimeMesh(X.TEXT(rMessage.Name), rMessage.ModelData.Sections.Count, false);
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
                        var key = new MyEntityMaterialKey { LOD = rMessage.LOD, Material = X.TEXT(rMessage.MaterialName) };

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

                        var r = actor.GetRenderable();

                        if ((rMessage.Emissivity.HasValue || rMessage.DiffuseColor.HasValue) && !r.ModelProperties.ContainsKey(key))
                        {
                            r.ModelProperties[key] = new MyModelProperties();
                        }

                        if(rMessage.Emissivity.HasValue)
                        {
                            r.ModelProperties[key].Emissivity = rMessage.Emissivity.Value;
                        }

                        if(rMessage.DiffuseColor.HasValue)
                        {
                            r.ModelProperties[key].ColorMul = rMessage.DiffuseColor.Value;
                        }

                        actor.MarkRenderDirty();
                    }

                    break;
                }

                case MyRenderMessageEnum.PreloadModel:
                {
                    var rMessage = (MyRenderMessagePreloadModel) message;

                    //MyAssetsLoader.GetModel(rMessage.Name);
                    MyMeshes.GetMeshId(X.TEXT(rMessage.Name));

                    break;
                }

                case MyRenderMessageEnum.ChangeMaterialTexture:
                {
                    var rMessage = (MyRenderMessageChangeMaterialTexture)message;

                    var actor = MyIDTracker<MyActor>.FindByID(rMessage.RenderObjectID);
                    if (actor != null)
                    {
                        var r = actor.GetRenderable();
                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT(rMessage.MaterialName) };

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
                                    TextureName = X.TEXT(s.TextureName), 
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
                                            TextureName = X.TEXT(s.TextureName),
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
                                            TextureName = X.TEXT(s.TextureName),
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
                        var r = actor.GetRenderable();
                        var key = new MyEntityMaterialKey { LOD = 0, Material = X.TEXT(rMessage.MaterialName) };

                        if (!r.ModelProperties.ContainsKey(key))
                        {
                            r.ModelProperties[key] = new MyModelProperties();
                        }
                        else
                        {
                            r.ModelProperties[key].TextureSwaps = null;
                        }

                        RwTexId handle = r.ModelProperties[key].CustomRenderedTexture;
                        if (handle == RwTexId.NULL && MyModelProperties.CustomTextures < MyModelProperties.MaxCustomTextures)
                        {
                           handle = MyRwTextures.CreateRenderTarget(rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, true);
                           r.ModelProperties[key].CustomRenderedTexture = handle;
                           ++MyModelProperties.CustomTextures;
                        }

                        if (handle != RwTexId.NULL)
                        {
                            var clearColor = new SharpDX.Color4(rMessage.BackgroundColor.PackedValue);
                            clearColor.Alpha = 0;
                            MyRender11.ImmediateContext.ClearRenderTargetView(handle.Rtv, clearColor);

                            // my sprites renderer -> push state
                            MySpritesRenderer.PushState(new Vector2(rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution));


                            MySpritesRenderer.DrawText(Vector2.Zero, new StringBuilder(rMessage.Text), rMessage.FontColor, rMessage.TextScale);
                            // render text with fonts to rt
                            // update texture of proxy
                            MySpritesRenderer.Draw(handle.Rtv, new MyViewport(rMessage.TextureResolution * rMessage.TextureAspectRatio, rMessage.TextureResolution));

                            // render to rt
                            // my sprites renderer -> pop state
                            MySpritesRenderer.PopState();
                            

                            MyRender11.ImmediateContext.GenerateMips(handle.ShaderView);

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
                    MyMeshes.GetMeshId(X.TEXT(rMessage.Name));

                    break;
                }

                #endregion

                #region Voxels

                case MyRenderMessageEnum.CreateClipmap:
                {
                    var rMessage = (MyRenderMessageCreateClipmap)message;

                    var clipmap = new MyClipmapHandler(rMessage.ClipmapId, rMessage.ScaleGroup, rMessage.WorldMatrix, rMessage.SizeLod0);
                    MyClipmapFactory.ClipmapByID[rMessage.ClipmapId] = clipmap;
                    clipmap.Base.LoadContent();
                    

                    break;
                }

                case MyRenderMessageEnum.UpdateClipmapCell:
                {
                    var rMessage = (MyRenderMessageUpdateClipmapCell)message;

                    var clipmap = MyClipmapFactory.ClipmapByID.Get(rMessage.ClipmapId);
                    if(clipmap != null)
                    {
                        clipmap.Base.UpdateCell(rMessage);
                    }

                    rMessage.Batches.Clear();
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

                    Debug.Assert(MyVoxelMaterials1.CheckIndices(rMessage.Materials));
                    MyVoxelMaterials1.Set(rMessage.Materials);

                    rMessage.Materials = null;

                    break;
                }


                #endregion

                #region Lights

                case MyRenderMessageEnum.CreateRenderLight:
                {
                    var rMessage = (MyRenderMessageCreateRenderLight)message;

                    //MyLight.Create(rMessage.ID);

                    MyLights.Create(rMessage.ID);

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderLight:
                {
                    var rMessage = (MyRenderMessageUpdateRenderLight)message;

                  
                    var light = MyLights.Get(rMessage.ID);


                    if(light != LightId.NULL)
                    {

                        var lightInfo = new MyLightInfo
                        {
                            Position = rMessage.Position,
                            PositionWithOffset = rMessage.Position + rMessage.Offset * rMessage.Range * rMessage.ReflectorDirection,
                            CastsShadows = rMessage.CastShadows,
                            ShadowsDistance = rMessage.ShadowDistance,
                            ParentGID = rMessage.ParentID,
                            UsedInForward = rMessage.UseInForwardRender
                        };

                        MyLights.UpdateEntity(light, ref lightInfo);

                        if ((rMessage.Type & LightTypeEnum.PointLight) > 0)
                        {
                            MyLights.UpdatePointlight(light, rMessage.LightOn, rMessage.Range, new Vector3(rMessage.Color.R, rMessage.Color.G, rMessage.Color.B) / 255.0f * rMessage.Intensity, rMessage.Falloff);
                        }
                        if ((rMessage.Type & LightTypeEnum.Hemisphere) > 0)
                        {
                            //rMessage.Color;
                            //rMessage.Falloff;
                            //rMessage.Intensity;
                            //rMessage.LightOn;
                            //rMessage.ReflectorDirection;
                            //rMessage.ReflectorUp;
                        }
                        if ((rMessage.Type & LightTypeEnum.Spotlight) > 0)
                        {
                            // because it's so in dx9...
                            float coneMaxAngleCos = 1 - rMessage.ReflectorConeMaxAngleCos;
                            coneMaxAngleCos = (float)Math.Min(Math.Max(coneMaxAngleCos, 0.01), 0.99f);
                            MyLights.UpdateSpotlight(light, rMessage.ReflectorOn,
                                rMessage.ReflectorDirection, rMessage.ReflectorRange, coneMaxAngleCos, rMessage.ReflectorUp,
                                new Vector3(rMessage.ReflectorColor.R, rMessage.ReflectorColor.G, rMessage.ReflectorColor.B) / 255.0f * rMessage.Intensity, rMessage.ReflectorFalloff,
                                MyTextures.GetTexture(rMessage.ReflectorTexture, MyTextureEnum.CUSTOM));
                        }

                        MyLights.UpdateGlare(light, new MyGlareDesc
                            {
                                Enabled = rMessage.GlareOn,
                                Material = X.TEXT(rMessage.GlareMaterial),
                                Intensity = rMessage.GlareIntensity,
                                QuerySize = rMessage.GlareQuerySize,
                                Type = rMessage.GlareType,
                                Size = rMessage.GlareSize,
                                MaxDistance = rMessage.GlareMaxDistance,
                                Color = rMessage.Color,
                                Direction = rMessage.ReflectorDirection,
                                Range = rMessage.Range
                            });
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

                case MyRenderMessageEnum.UpdateFogSettings:
                {
                    var rMessage = (MyRenderMessageUpdateFogSettings)message;

                    MyEnvironment.FogSettings = rMessage.Settings;

                    break;
                }

                case MyRenderMessageEnum.UpdateRenderEnvironment:
                {
                    var rMessage = (MyRenderMessageUpdateRenderEnvironment)message;

                    MyEnvironment.DirectionalLightDir = VRageMath.Vector3.Normalize(rMessage.SunDirection);
                    MyEnvironment.DirectionalLightIntensity = rMessage.SunIntensity * rMessage.SunColor.ToVector3();
                    MyEnvironment.DirectionalLightEnabled = rMessage.SunLightOn;
                    MyEnvironment.DayTime = (float)(rMessage.DayTime - Math.Truncate(rMessage.DayTime));
                    MyEnvironment.SunDistance = rMessage.DistanceToSun;
                    MyEnvironment.SunColor = rMessage.SunColor;
                    MyEnvironment.SunMaterial = rMessage.SunMaterial;
                    MyEnvironment.SunSizeMultiplier = rMessage.SunSizeMultiplier;
                    MyEnvironment.SunBillboardEnabled = rMessage.SunBillboardEnabled;

                    var skybox = rMessage.BackgroundTexture;

                    m_resetEyeAdaptation = m_resetEyeAdaptation || rMessage.ResetEyeAdaptation;

                    break;
                }

                case MyRenderMessageEnum.UpdateEnvironmentMap:
                {   
                    break;
                }

                case MyRenderMessageEnum.UpdatePostprocessSettings:
                {
                    var rMessage = (MyRenderMessageUpdatePostprocessSettings)message;

                    m_postprocessSettings = rMessage.Settings;

                    break;
                }

                case MyRenderMessageEnum.UpdateSSAOSettings:
                {
                    var rMessage = (MyRenderMessageUpdateSSAOSettings)message;


                    MySSAO.Params.MinRadius = rMessage.MinRadius;
                    MySSAO.Params.MaxRadius = rMessage.MaxRadius;
                    MySSAO.Params.RadiusGrow = rMessage.RadiusGrowZScale;

                    MySSAO.Params.RadiusBias = rMessage.Bias;
                    MySSAO.Params.Falloff = rMessage.Falloff;
                    MySSAO.Params.Normalization = rMessage.NormValue;
                    MySSAO.Params.Contrast = rMessage.Contrast;
                    
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
                    Debug.Assert(createFontMessage != null);

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

                case MyRenderMessageEnum.PreloadTextures:
                    {
                        var preloadMsg = message as MyRenderMessagePreloadTextures;

                        //MyTextureManager.PreloadTextures(preloadMsg.InDirectory, preloadMsg.Recursive);
                        //MyTextures.UnloadTexture(texMessage.Texture);

                        break;
                    }

                case MyRenderMessageEnum.UnloadTexture:
                    {
                        var texMessage = (MyRenderMessageUnloadTexture)message;

                        //MyTextureManager.UnloadTexture(texMessage.Texture);
                        MyTextures.UnloadTexture(texMessage.Texture);

                        break;
                    }

                case MyRenderMessageEnum.ReloadTextures:
                    {
                        var reloadMsg = (MyRenderMessageReloadTextures)message;

                        MyVoxelMaterials1.InvalidateMaterials();
                        MyMeshMaterials1.InvalidateMaterials();
                        MyTextures.ReloadAssetTextures();

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

                    m_screenshot = new MyScreenshot(rMessage.PathToSave, rMessage.SizeMultiplier, rMessage.IgnoreSprites);

                    break;
                }

                case MyRenderMessageEnum.ReloadEffects:
                {
                    m_reloadShaders = true;

                    //MyShaderBundleFactory.ClearCache();
                    //MyShaderMaterial.ClearCache();
                    //MyShaderPass.ClearCache();

                    MyShaders.Recompile();
                    MyMaterialShaders.Recompile();

                    MyRenderableComponent.MarkAllDirty();

                    foreach (var f in MyComponentFactory<MyFoliageComponent>.GetAll())
                    {
                        f.Dispose();
                    }

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

                    var video = MyVideoFactory.Videos.Get(rMessage.ID);
                    if (video != null)
                    {
                        video.Stop();
                        video.Dispose();
                        MyVideoFactory.Videos.Remove(rMessage.ID);
                    }

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
                        UpdateRenderSettings((message as MyRenderMessageSwitchRenderSettings).Settings);
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

                #region Debug draw

                case MyRenderMessageEnum.DebugDrawPoint:
                case MyRenderMessageEnum.DebugDrawLine3D:
                case MyRenderMessageEnum.DebugDrawLine2D:
                case MyRenderMessageEnum.DebugDrawSphere:
                case MyRenderMessageEnum.DebugDrawAABB:
                case MyRenderMessageEnum.DebugDrawAxis:
                case MyRenderMessageEnum.DebugDrawOBB:
                case MyRenderMessageEnum.DebugDrawCone:
                case MyRenderMessageEnum.DebugDrawTriangle:
                case MyRenderMessageEnum.DebugDrawCapsule:
                case MyRenderMessageEnum.DebugDrawText2D:
                case MyRenderMessageEnum.DebugDrawText3D:
                case MyRenderMessageEnum.DebugDrawModel:
                case MyRenderMessageEnum.DebugDrawTriangles:
                case MyRenderMessageEnum.DebugDrawPlane:
                case MyRenderMessageEnum.DebugDrawCylinder:
                {
                    m_debugDrawMessages.Enqueue(message);
                }
                break;

                case MyRenderMessageEnum.DebugCrashRenderThread:
                {
                    throw new InvalidOperationException("Forced exception");
                }
                #endregion
            }
        }
    }
}