using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage;
using VRage.Import;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender.Profiler;
using VRageRender.RenderObjects;
using VRageRender.Textures;

namespace VRageRender
{
    internal static partial class MyRender
    {
        private static void ProcessMessage(IMyRenderMessage message)
        {
            try
            {
                MyRender.GetRenderProfiler().StartProfilingBlock(MyEnum<MyRenderMessageType>.GetName(message.MessageClass));
                MyRender.GetRenderProfiler().StartProfilingBlock(Partition.Select(MyEnum<MyRenderMessageEnum>.GetName(message.MessageType).GetHashCode(), "A", "B", "C", "D", "E", "F", "G", "H", "I"));
                MyRender.GetRenderProfiler().StartProfilingBlock(MyEnum<MyRenderMessageEnum>.GetName(message.MessageType));
                ProcessMessageInternal(message);
                MyRender.GetRenderProfiler().EndProfilingBlock();
                MyRender.GetRenderProfiler().EndProfilingBlock();
                MyRender.GetRenderProfiler().EndProfilingBlock();
            }
            catch
            {
                if (message != null)
                {
                    MyLog.Default.WriteLine("Error processing message: " + message.MessageType);
                    MyLog.Default.WriteLine("Details: " + message.ToString());
                }
                throw;
            }
        }

        private static void ProcessMessageInternal(IMyRenderMessage message)
        {
            switch (message.MessageType)
            {
                #region Sprites
                case MyRenderMessageEnum.DrawSprite:
                case MyRenderMessageEnum.DrawSpriteNormalized:
                case MyRenderMessageEnum.DrawSpriteAtlas:
                case MyRenderMessageEnum.SpriteScissorPush:
                case MyRenderMessageEnum.SpriteScissorPop:
                    {
                        EnqueueDrawMessage(message);
                        break;
                    }

                #endregion

                #region Textures

                case MyRenderMessageEnum.PreloadTextures:
                    {
                        var preloadMsg = message as MyRenderMessagePreloadTextures;

                        MyTextureManager.PreloadTextures(preloadMsg.InDirectory, preloadMsg.Recursive);

                        break;
                    }

                case MyRenderMessageEnum.UnloadTexture:
                    {
                        var texMessage = (MyRenderMessageUnloadTexture)message;

                        MyTextureManager.UnloadTexture(texMessage.Texture);



                        break;
                    }

                #endregion

                #region Profiler

                case MyRenderMessageEnum.RenderProfiler:
                    {
                        var profMessage = (MyRenderMessageRenderProfiler)message;

                        MyRenderProfiler.HandleInput(profMessage.Command, profMessage.Index);

                        break;
                    }

                #endregion

                #region Render objects

                case MyRenderMessageEnum.CreateRenderEntity:
                    {
                        var rMessage = (MyRenderMessageCreateRenderEntity)message;

                        MyRenderEntity renderEntity;

                        // AlesR : refactor
                        if (string.IsNullOrEmpty(rMessage.Model))
                        {
                            ProfilerShort.Begin("CreateRenderEntity-NoModel");
                            renderEntity = new MyRenderEntity(
                                rMessage.ID,
                                rMessage.DebugName,
                                rMessage.WorldMatrix,
                                rMessage.Technique,
                                rMessage.Flags
                            );
                            ProfilerShort.BeginNextBlock("SetMaxDist");
                            renderEntity.MaxViewDistance = rMessage.MaxViewDistance;
                            ProfilerShort.End();
                        }
                        else
                        {
                            ProfilerShort.Begin("CreateRenderEntity-Model");
                            renderEntity = new MyRenderEntity(
                                rMessage.ID,
                                rMessage.DebugName,
                                rMessage.Model,
                                rMessage.WorldMatrix,
                                rMessage.Technique,
                                rMessage.Flags
                            );
                            ProfilerShort.End();

                            if (renderEntity.Lods.Count == 0)
                                return;

                            ProfilerShort.Begin("SetMaxDist");
                            renderEntity.MaxViewDistance = rMessage.MaxViewDistance;
                            ProfilerShort.End();
                            ProfilerShort.Begin("renderEntity.LoadContent");
                            renderEntity.LoadContent();
                            ProfilerShort.End();
                        }

                        ProfilerShort.Begin("AddRenderObjectFromProxy");
                        AddRenderObjectFromProxy(renderEntity);
                        ProfilerShort.End();

                        break;
                    }

                case MyRenderMessageEnum.CreateRenderEntityAtmosphere:
                    {
                        var rMessage = (MyRenderMessageCreateRenderEntityAtmosphere)message;

                        MyRenderEntity renderEntity;


                        ProfilerShort.Begin("CreateRenderEntity-Atmosphere");
                        renderEntity = new MyRenderAtmosphere(
                            rMessage.ID,
                            rMessage.DebugName,
                            rMessage.Model,
                            rMessage.WorldMatrix,
                            rMessage.Technique,
                            rMessage.Flags,
                            rMessage.AtmosphereRadius,
                            rMessage.PlanetRadius,
                            rMessage.AtmosphereWavelengths
                        );
                        ProfilerShort.End();

                        if (renderEntity.Lods.Count == 0)
                            return;

                        ProfilerShort.Begin("SetMaxDist");
                        renderEntity.MaxViewDistance = rMessage.MaxViewDistance;
                        ProfilerShort.End();
                        ProfilerShort.Begin("renderEntity.LoadContent");
                        renderEntity.LoadContent();
                        ProfilerShort.End();

                        ProfilerShort.Begin("AddRenderObjectFromProxy");
                        AddRenderObjectFromProxy(renderEntity);
                        ProfilerShort.End();

                        break;
                    }

                case MyRenderMessageEnum.AddRuntimeModel:
                    {
                        var rMessage = (MyRenderMessageAddRuntimeModel)message;
                        var model = new MyRenderModel(MyMeshDrawTechnique.MESH);
                        ProfilerShort.Begin("LoadBuffers");
                        model.LoadBuffers(rMessage.ModelData);
                        ProfilerShort.End();
                        MyRenderModels.AddRuntimeModel(rMessage.Name, model);
                        break;
                    }

                case MyRenderMessageEnum.PreloadModel:
                    {
                        var rMessage = (MyRenderMessagePreloadModel)message;
                        MyRenderModels.GetModel(rMessage.Name);
                        break;
                    }


                case MyRenderMessageEnum.UnloadModel:
                    {
                        var rMessage = (MyRenderMessageUnloadModel)message;

                        MyRenderModels.UnloadModel(rMessage.Name);

                        break;
                    }
                case MyRenderMessageEnum.PreloadMaterials:
                    {
                        var rMessage = (MyRenderMessagePreloadMaterials)message;
                        MyRenderModels.GetMaterials(rMessage.Name);
                        break;
                    }

                case MyRenderMessageEnum.SetRenderEntityData:
                    {
                        var rMessage = (MyRenderMessageSetRenderEntityData)message;
                        var entity = (MyRenderEntity)GetRenderObject(rMessage.ID);

                        if (entity != null)
                            entity.AddData(rMessage);

                        break;
                    }


                case MyRenderMessageEnum.SetRenderEntityLOD:
                    {
                        var rMessage = (MyRenderMessageSetRenderEntityLOD)message;

                        var entity = (MyRenderEntity)GetRenderObject(rMessage.ID);
                        if (entity != null)
                        {
                            entity.AddLOD(rMessage.Distance, rMessage.Model);
                        }

                        break;
                    }

                case MyRenderMessageEnum.CreateRenderBatch:
                    {
                        var rMessage = (MyRenderMessageCreateRenderBatch)message;

                        MyRenderBatch renderBatch = new MyRenderBatch(
                            rMessage.ID,
                            rMessage.DebugName,
                            (MatrixD)rMessage.WorldMatrix,
                            rMessage.Flags,
                            rMessage.RenderBatchParts
                            );

                        renderBatch.LoadContent();
                        AddRenderObjectFromProxy(renderBatch);



                        break;
                    }

                case MyRenderMessageEnum.CreateRenderInstanceBuffer:
                    {
                        var rMessage = (MyRenderMessageCreateRenderInstanceBuffer)message;

                        MyRenderInstanceBuffer renderBatch = new MyRenderInstanceBuffer(rMessage.ID, rMessage.DebugName, rMessage.Type);

                        renderBatch.LoadContent();
                        AddRenderObjectFromProxy(renderBatch);



                        break;
                    }

                case MyRenderMessageEnum.CreateLineBasedObject:
                    {
                        var rMessage = (MyRenderMessageCreateLineBasedObject)message;

                        var lineBasedObj = new MyRenderLineBasedObject(rMessage.ID, rMessage.DebugName);
                        lineBasedObj.LoadContent();
                        AddRenderObjectFromProxy(lineBasedObj);

                        break;
                    }

                case MyRenderMessageEnum.UpdateLineBasedObject:
                    {
                        var rMessage = (MyRenderMessageUpdateLineBasedObject)message;

                        var obj = (MyRenderLineBasedObject)GetRenderObject(rMessage.ID);
                        if (obj != null)
                        {
                            obj.SetWorldPoints(ref rMessage.WorldPointA, ref rMessage.WorldPointB);
                            UpdateRenderObject(obj);
                        }

                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderCubeInstanceBuffer)message;

                        var obj = (MyRenderInstanceBuffer)GetRenderObject(rMessage.ID);
                        obj.UpdateCube(rMessage.InstanceData, rMessage.Capacity);

                        rMessage.InstanceData.Clear();


                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderInstanceBuffer:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderInstanceBuffer)message;

                        var obj = (MyRenderInstanceBuffer)GetRenderObject(rMessage.ID);
                        obj.Update(rMessage.InstanceData, rMessage.Capacity);

                        rMessage.InstanceData.Clear();


                        break;
                    }


                case MyRenderMessageEnum.SetInstanceBuffer:
                    {
                        var rMessage = (MyRenderMessageSetInstanceBuffer)message;

                        var entity = (MyRenderEntity)GetRenderObject(rMessage.ID);
                        if (entity != null)
                        {
                            //RemoveRenderObject(entity);
                            var buffer = rMessage.InstanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED ? null : (MyRenderInstanceBuffer)GetRenderObject(rMessage.InstanceBufferId);
                            entity.SetInstanceData(buffer, rMessage.InstanceStart, rMessage.InstanceCount, (BoundingBoxD)rMessage.LocalAabb);
                            MoveRenderObject(entity);
                        }


                        break;
                    }

                case MyRenderMessageEnum.CreateManualCullObject:
                    {
                        var rMessage = (MyRenderMessageCreateManualCullObject)message;

                        MyManualCullableRenderObject manualCullObject = new MyManualCullableRenderObject(rMessage.ID, (MatrixD)rMessage.WorldMatrix);
                        manualCullObject.DebugName = rMessage.DebugName;
                        manualCullObject.WorldMatrix = (MatrixD)rMessage.WorldMatrix;

                        AddRenderObjectFromProxy(manualCullObject, false);



                        break;
                    }

                case MyRenderMessageEnum.SetParentCullObject:
                    {
                        var rMessage = (MyRenderMessageSetParentCullObject)message;

                        MyRenderObject renderObject = GetRenderObject(rMessage.ID);
                        RemoveRenderObject(renderObject);
                        //m_renderObjects.Remove(rMessage.ID);

                        MyManualCullableRenderObject manualCullObject = GetRenderObject(rMessage.CullObjectID) as MyManualCullableRenderObject;
                        if (manualCullObject != null)
                        {
                            RemoveRenderObject(manualCullObject);

                            manualCullObject.AddRenderObject(renderObject, (MatrixD?)rMessage.ChildToParent);

                            AddRenderObject(manualCullObject);
                        }
                        else
                        { 
                        }
                        break;
                    }

                case MyRenderMessageEnum.SetCameraViewMatrix:
                    {
                        var rMessage = (MyRenderMessageSetCameraViewMatrix)message;
                        rMessage.UpdateTime = MyRender.CurrentUpdateTime;

                        // EnqueueDrawMessage(rMessage);

                        // var rMessage = (MyRenderMessageSetCameraViewMatrix)drawMessage;

                        MyRenderCamera.ProjectionMatrix = rMessage.ProjectionMatrix;
                        MyRenderCamera.ProjectionMatrixForNearObjects = rMessage.NearProjectionMatrix;
                        MyRenderCamera.SetViewMatrix(rMessage.ViewMatrix, rMessage.UpdateTime);

                        MyRenderCamera.SafeNearForForward = rMessage.SafeNear;
                        MyRenderCamera.FieldOfView = rMessage.FOV;
                        MyRenderCamera.FieldOfViewForNearObjects = rMessage.NearFOV;

                        if ((MyRenderCamera.NEAR_PLANE_DISTANCE != rMessage.NearPlane) ||
                            (MyRenderCamera.FAR_PLANE_DISTANCE != rMessage.FarPlane) ||
                            (MyRenderCamera.NEAR_PLANE_FOR_NEAR_OBJECTS != rMessage.NearObjectsNearPlane) ||
                            (MyRenderCamera.FAR_PLANE_FOR_NEAR_OBJECTS != rMessage.NearObjectsFarPlane))
                        {
                            MyRenderCamera.NEAR_PLANE_DISTANCE = rMessage.NearPlane;
                            MyRenderCamera.FAR_PLANE_DISTANCE = rMessage.FarPlane;
                            MyRenderCamera.NEAR_PLANE_FOR_NEAR_OBJECTS = rMessage.NearObjectsNearPlane;
                            MyRenderCamera.FAR_PLANE_FOR_NEAR_OBJECTS = rMessage.NearObjectsFarPlane;

                            foreach (var effect in m_effects)
                            {
                                effect.SetNearPlane(MyRenderCamera.NEAR_PLANE_DISTANCE);
                                effect.SetFarPlane(MyRenderCamera.FAR_PLANE_DISTANCE);
                            }
                        }

                        MyRenderCamera.UpdateCamera();

                        break;
                    }

                case MyRenderMessageEnum.DrawScene:
                    {
                        var rMessage = (IMyRenderMessage)message;
                        EnqueueDrawMessage(rMessage);

                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderObject:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderObject)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            //System.Diagnostics.Debug.Assert(renderObject.ParentCullObject == null);
                            if (renderObject.ParentCullObject != null)
                            {
                                MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                                if (transformObject != null)
                                {
                                    //System.Diagnostics.Debug.Assert(Vector3D.IsZero(transformObject.WorldMatrix.Translation - rMessage.WorldMatrix.Translation, 0.01f));
                                }
                            }

                            {
                                MyRenderCharacter characterObject = renderObject as MyRenderCharacter;
                                if (characterObject != null)
                                {
                                    if (rMessage.AABB.HasValue)
                                        characterObject.ActualWorldAABB = rMessage.AABB.Value;
                                }
                                MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                                if (transformObject != null)
                                {
                                    transformObject.WorldMatrix = rMessage.WorldMatrix;
                                    UpdateRenderObject(transformObject, rMessage.SortIntoCulling);
                                }
                                MyRenderClipmap clipmap = renderObject as MyRenderClipmap;
                                if (clipmap != null)
                                {
                                    clipmap.UpdateWorldMatrix(ref rMessage.WorldMatrix, rMessage.SortIntoCulling);
                                    UpdateRenderObject(clipmap, rMessage.SortIntoCulling);
                                }
                                MyManualCullableRenderObject manualCullableRenderObject = renderObject as MyManualCullableRenderObject;
                                if (manualCullableRenderObject != null)
                                {
                                    manualCullableRenderObject.WorldMatrix = rMessage.WorldMatrix;
                                    UpdateRenderObject(manualCullableRenderObject, rMessage.SortIntoCulling);
                                }
                            }
                        }



                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderObjectVisibility:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderObjectVisibility)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                            if (transformObject != null)
                            {
                                transformObject.ClearInterpolator();
                            }
                            MyManualCullableRenderObject manualCullableRenderObject = renderObject as MyManualCullableRenderObject;
                            if (manualCullableRenderObject != null)
                            {
                                manualCullableRenderObject.ClearInterpolator();
                            }

                            if (renderObject.NearFlag != rMessage.NearFlag)
                            {
                                var parentCullObject = renderObject.ParentCullObject;
                                if (parentCullObject != null)
                                {
                                    parentCullObject.RemoveRenderObject(renderObject);
                                }

                                RemoveRenderObject(renderObject, true);

                                renderObject.NearFlag = rMessage.NearFlag;

                                if (parentCullObject != null)
                                {
                                    RemoveRenderObject(parentCullObject);
                                    parentCullObject.AddRenderObject(renderObject);
                                    AddRenderObject(parentCullObject);

                                    if (renderObject.NearFlag && !m_nearObjects.Contains(renderObject))
                                    {
                                        m_nearObjects.Add(renderObject);
                                    }
                                }
                                else
                                {
                                    AddRenderObject(renderObject);
                                }
                            }
                            else
                            {
                                renderObject.Visible = rMessage.Visible;
                            }
                        }



                        break;
                    }

                case MyRenderMessageEnum.RemoveRenderObject:
                    {
                        var rMessage = (MyRenderMessageRemoveRenderObject)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            RemoveRenderObject(renderObject);
                            m_renderObjects.Remove(rMessage.ID);
                            renderObject.UnloadContent();
                        }
                        else
                        {
                        } // Put breakpoint here



                        break;
                    }


                case MyRenderMessageEnum.UpdateRenderEntity:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderEntity)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderEntity renderEntity = (MyRenderEntity)renderObject;
                            if (rMessage.DiffuseColor.HasValue)
                            {
                                renderEntity.EntityColor = rMessage.DiffuseColor.Value;
                            }
                            if (rMessage.ColorMaskHSV.HasValue)
                            {
                                renderEntity.EntityColorMaskHSV = rMessage.ColorMaskHSV.Value;
                            }
                            renderEntity.EntityDithering = rMessage.Dithering;
                        }



                        break;
                    }

                case MyRenderMessageEnum.EnableRenderModule:
                    {
                        var rMessage = (MyRenderMessageEnableRenderModule)message;

                        EnableRenderModule((MyRenderModuleEnum)rMessage.ID, rMessage.Enable);



                        break;
                    }

                case MyRenderMessageEnum.UseCustomDrawMatrix:
                    {
                        var rMessage = (MyRenderMessageUseCustomDrawMatrix)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderEntity renderEntity = (MyRenderEntity)renderObject;

                            renderEntity.UseCustomDrawMatrix = rMessage.Enable;
                            renderEntity.DrawMatrix = (MatrixD)rMessage.DrawMatrix;
                        }

                        break;
                    }

                case MyRenderMessageEnum.CreateClipmap:
                    {
                        var rMessage = (MyRenderMessageCreateClipmap)message;

                        var clipmap = new MyRenderClipmap(rMessage);

                        AddRenderObjectFromProxy(clipmap);
                        clipmap.LoadContent();

                        break;
                    }

                case MyRenderMessageEnum.UpdateClipmapCell:
                    {
                        var rMessage = (MyRenderMessageUpdateClipmapCell)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ClipmapId, out renderObject))
                        {
                            var clipmap = (MyRenderClipmap)renderObject;
                            clipmap.UpdateCell(rMessage);
                        }
                        rMessage.Batches.Clear();

                        break;
                    }

                case MyRenderMessageEnum.InvalidateClipmapRange:
                    {
                        var rMessage = (MyRenderMessageInvalidateClipmapRange)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ClipmapId, out renderObject))
                        {
                            var clipmap = (MyRenderClipmap)renderObject;
                            clipmap.InvalidateRange(rMessage.MinCellLod0, rMessage.MaxCellLod0);
                        }
                        break;
                    }


                case MyRenderMessageEnum.RebuildCullingStructure:
                    {
                        MyRender.RebuildCullingStructure();



                        break;
                    }

                case MyRenderMessageEnum.ReloadEffects:
                    {
                        MyRender.RootDirectoryEffects = MyRender.RootDirectoryDebug;
                        //MyRender.RootDirectoryEffects = MyRender.RootDirectory;
                        MyRender.LoadEffects();



                        break;
                    }

                case MyRenderMessageEnum.ReloadModels:
                    {
                        MyRenderModels.ReloadModels();



                        break;
                    }

                case MyRenderMessageEnum.ReloadTextures:
                    {
                        MyTextureManager.ReloadTextures(false);



                        break;
                    }

                case MyRenderMessageEnum.CreateRenderVoxelMaterials:
                    {
                        MyRenderVoxelMaterials.Clear();

                        var rMessage = (MyRenderMessageCreateRenderVoxelMaterials)message;

                        for (int i = 0; i < rMessage.Materials.Length; ++i)
                            MyRenderVoxelMaterials.Add(ref rMessage.Materials[i]);

                        rMessage.Materials = null;


                        break;
                    }

                case MyRenderMessageEnum.CreateRenderVoxelDebris:
                    {
                        var rMessage = (MyRenderMessageCreateRenderVoxelDebris)message;

                        MyRenderVoxelDebris renderVoxelDebris = new MyRenderVoxelDebris(
                            rMessage.ID,
                            rMessage.DebugName,
                            rMessage.Model,
                            (MatrixD)rMessage.WorldMatrix,
                            rMessage.TextureCoordOffset,
                            rMessage.TextureCoordScale,
                            rMessage.TextureColorMultiplier,
                            rMessage.VoxelMaterialIndex
                            );

                        AddRenderObjectFromProxy(renderVoxelDebris);



                        break;
                    }


                case MyRenderMessageEnum.CreateRenderCharacter:
                    {
                        var rMessage = (MyRenderMessageCreateRenderCharacter)message;

                        MyRenderCharacter renderCharacter = new MyRenderCharacter(
                            rMessage.ID,
                            rMessage.DebugName,
                            rMessage.Model,
                            (MatrixD)rMessage.WorldMatrix,
                            rMessage.Flags
                            );

                        AddRenderObjectFromProxy(renderCharacter);



                        break;
                    }

                case MyRenderMessageEnum.UpdateModelProperties:
                    {
                        var rMessage = (MyRenderMessageUpdateModelProperties)message;

                        if (rMessage.ID == MyRenderProxy.RENDER_ID_UNASSIGNED)
                        {
                            MyRenderModel model = MyRenderModels.GetModel(rMessage.Model);
                            MyRenderMesh mesh = null;

                            if (rMessage.MaterialName != null)
                            {
                                foreach (var rMesh in model.GetMeshList())
                                {
                                    if (rMesh.Material.MaterialName == rMessage.MaterialName)
                                    {
                                        mesh = rMesh;
                                        break;
                                    }
                                }
                            }
                            else
                                mesh = model.GetMeshList()[rMessage.MeshIndex];

                            if (mesh != null)
                            {
                                MyRenderMeshMaterial material = mesh.Material;

                                if (rMessage.Enabled.HasValue)
                                    material.Enabled = rMessage.Enabled.Value;

                                if (rMessage.DiffuseColor.HasValue)
                                    material.DiffuseColor = rMessage.DiffuseColor.Value.ToVector3();

                                if (rMessage.SpecularIntensity.HasValue)
                                    material.SpecularIntensity = rMessage.SpecularIntensity.Value;

                                if (rMessage.SpecularPower.HasValue)
                                    material.SpecularPower = rMessage.SpecularPower.Value;

                                if (rMessage.Emissivity.HasValue)
                                    material.Emissivity = rMessage.Emissivity.Value;
                            }

                            model.HasSharedMaterials = true;
                        }
                        else
                        {
                            MyRenderObject renderObject;
                            if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                            {
                                MyRenderEntity renderEntity = renderObject as MyRenderEntity;
                                List<MyRenderMeshMaterial> materials = renderEntity.Lods[rMessage.LOD].MeshMaterials;
                                MyRenderModel model = renderEntity.Lods[rMessage.LOD].Model;
                                MyRenderMeshMaterial material = null;

                                model.HasSharedMaterials = false;

                                if (rMessage.MaterialName != null)
                                {
                                    foreach (var rMaterial in materials)
                                    {
                                        if (rMaterial.MaterialName == rMessage.MaterialName)
                                        {
                                            material = rMaterial;
                                            break;
                                        }
                                    }
                                }
                                else
                                    material = materials[rMessage.MeshIndex];

                                if (material != null)
                                {
                                    if (rMessage.Enabled.HasValue)
                                        material.Enabled = rMessage.Enabled.Value;

                                    if (rMessage.DiffuseColor.HasValue)
                                        material.DiffuseColor = rMessage.DiffuseColor.Value.ToVector3();

                                    if (rMessage.SpecularIntensity.HasValue)
                                        material.SpecularIntensity = rMessage.SpecularIntensity.Value;

                                    if (rMessage.SpecularPower.HasValue)
                                        material.SpecularPower = rMessage.SpecularPower.Value;

                                    if (rMessage.Emissivity.HasValue)
                                        material.Emissivity = rMessage.Emissivity.Value;
                                }
                            }
                        }



                        break;
                    }

                case MyRenderMessageEnum.ChangeModel:
                    {
                        var rMessage = (MyRenderMessageChangeModel)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderEntity entity = renderObject as MyRenderEntity;
                            if (rMessage.UseForShadow)
                            {
                                entity.ChangeShadowModels(rMessage.LOD, rMessage.Model);
                                entity.ChangeModels(rMessage.LOD, rMessage.Model);
                            }
                            else
                            {
                                entity.ChangeShadowModels(rMessage.LOD, entity.Lods[rMessage.LOD].Model.AssetName);
                                entity.ChangeModels(rMessage.LOD, rMessage.Model);
                            }
                        }




                        break;
                    }

                case MyRenderMessageEnum.UpdateVoxelMaterialsProperties:
                    {
                        var rMessage = (MyRenderMessageUpdateVoxelMaterialsProperties)message;

                        var material = MyRenderVoxelMaterials.Get(rMessage.MaterialIndex);

                        material.SpecularIntensity = rMessage.SpecularIntensity;
                        material.SpecularPower = rMessage.SpecularPower;



                        break;
                    }

                case MyRenderMessageEnum.ChangeMaterialTexture:
                    {
                        var rMessage = (MyRenderMessageChangeMaterialTexture)message;
                        MyRenderMeshMaterial material = GetMeshMaterial(rMessage.RenderObjectID, rMessage.MaterialName);
                        if (material != null)
                        {
                            material.DiffuseTexture = MyTextureManager.GetTexture<MyTexture2D>(rMessage.Changes[0].TextureName, "", null, LoadingMode.Immediate);
                        }
                        rMessage.Changes.Clear();

                        break;
                    }

                case MyRenderMessageEnum.DrawTextToMaterial:
                    {
                        var rMessage = (MyRenderMessageDrawTextToMaterial)message;
                        MyRenderMeshMaterial material = GetMeshMaterial(rMessage.RenderObjectID, rMessage.MaterialName);
                        if (material != null)
                        {
                            var id = new MyRenderTextureId();
                            id.EntityId = rMessage.EntityId;
                            id.RenderObjectId = rMessage.RenderObjectID;

                            material.DiffuseTexture = MyRender.RenderTextToTexture(id, rMessage.Text, rMessage.TextScale , rMessage.FontColor, rMessage.BackgroundColor, rMessage.TextureResolution, rMessage.TextureAspectRatio);
                            if (material.DiffuseTexture == null)
                            {
                                MyRenderProxy.TextNotDrawnToTexture(rMessage.EntityId);
                            }
                        }
                        break;
                    }
                case MyRenderMessageEnum.ReleaseRenderTexture:
                    {
                        var rMessage = (MyRenderMessageReleaseRenderTexture)message;
                        var id = new MyRenderTextureId();
                        id.EntityId = rMessage.EntityId;
                        id.RenderObjectId = rMessage.RenderObjectID;
                        if (MyRenderTexturePool.ReleaseRenderTexture(id))
                        {
                            MyRenderProxy.RenderTextureFreed(MyRenderTexturePool.FreeResourcesCount());
                        }
                        break;
                    }
                #endregion

                #region Lights

                case MyRenderMessageEnum.CreateRenderLight:
                    {
                        var rMessage = (MyRenderMessageCreateRenderLight)message;

                        MyRenderLight renderLight = new MyRenderLight(
                            rMessage.ID
                            );

                        AddRenderObjectFromProxy(renderLight);



                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderLight:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderLight)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderLight renderLight = renderObject as MyRenderLight;
                            if (renderLight != null)
                            {
                                bool dirtyAABB = false;
                                if (renderLight.m_parentID != rMessage.ParentID)
                                    dirtyAABB = true;

                                if (renderLight.m_position != rMessage.Position)
                                    dirtyAABB = true;

                                renderLight.UpdateParameters(
                                    rMessage.Type,
                                    rMessage.Position,
                                    rMessage.ParentID,
                                    rMessage.Offset,
                                    rMessage.Color,
                                    rMessage.SpecularColor,
                                    rMessage.Falloff,
                                    rMessage.Range,
                                    rMessage.Intensity,
                                    rMessage.LightOn,
                                    rMessage.UseInForwardRender,
                                    rMessage.ReflectorIntensity,
                                    rMessage.ReflectorOn,
                                    rMessage.ReflectorDirection,
                                    rMessage.ReflectorUp,
                                    rMessage.ReflectorConeMaxAngleCos,
                                    rMessage.ReflectorColor,
                                    rMessage.ReflectorRange,
                                    rMessage.ReflectorFalloff,
                                    rMessage.ReflectorTexture,
                                    rMessage.ShadowDistance,
                                    rMessage.CastShadows,
                                    rMessage.GlareOn,
                                    rMessage.GlareType,
                                    rMessage.GlareSize,
                                    rMessage.GlareQuerySize,
                                    rMessage.GlareIntensity,
                                    rMessage.GlareMaterial,
                                    rMessage.GlareMaxDistance
                                    );

                                if (dirtyAABB)
                                    UpdateRenderObject(renderLight, false);
                            }
                        }



                        break;
                    }


                case MyRenderMessageEnum.SetLightShadowIgnore:
                    {
                        var rMessage = (MyRenderMessageSetLightShadowIgnore)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderLight renderLight = (MyRenderLight)renderObject;
                            renderLight.ShadowIgnoreObjects.Add(rMessage.ID2);
                        }



                        break;
                    }


                case MyRenderMessageEnum.ClearLightShadowIgnore:
                    {
                        var rMessage = (MyRenderMessageClearLightShadowIgnore)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyRenderLight renderLight = (MyRenderLight)renderObject;
                            renderLight.ShadowIgnoreObjects.Clear();
                        }



                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderEnvironment:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderEnvironment)message;

                        Sun.Direction = rMessage.SunDirection;
                        Sun.Color = rMessage.SunColor;
                        Sun.BackColor = rMessage.SunBackColor;
                        Sun.BackIntensity = rMessage.SunBackIntensity;
                        Sun.Intensity = rMessage.SunIntensity;
                        Sun.LightOn = rMessage.SunLightOn;
                        Sun.SpecularColor = rMessage.SunSpecularColor;
                        Sun.SunSizeMultiplier = rMessage.SunSizeMultiplier;
                        Sun.DistanceToSun = rMessage.DistanceToSun;

                        MyRender.AmbientColor = rMessage.AmbientColor;
                        MyRender.AmbientMultiplier = rMessage.AmbientMultiplier;
                        MyRender.EnvAmbientIntensity = rMessage.EnvAmbientIntensity;

                        MyBackgroundCube.Filename = rMessage.BackgroundTexture;
                        MyBackgroundCube.BackgroundColor = rMessage.BackgroundColor;
                        MyBackgroundCube.BackgroundOrientation = rMessage.BackgroundOrientation;
                        Sun.SunMaterial = rMessage.SunMaterial;

                        MyBackgroundCube.Static.ReloadContent();

                        break;
                    }
                #endregion

                #region Post processes

                case MyRenderMessageEnum.UpdateHDRSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateHDRSettings)message;

                        MyPostProcessHDR postProcessHDR = MyRender.GetPostProcess(MyPostProcessEnum.HDR) as MyPostProcessHDR;

                        postProcessHDR.Enabled = rMessage.Enabled;
                        postProcessHDR.Exposure = rMessage.Exposure;
                        postProcessHDR.Threshold = rMessage.Threshold;
                        postProcessHDR.BloomIntensity = rMessage.BloomIntensity;
                        postProcessHDR.BloomIntensityBackground = rMessage.BloomIntensityBackground;
                        postProcessHDR.VerticalBlurAmount = rMessage.VerticalBlurAmount;
                        postProcessHDR.HorizontalBlurAmount = rMessage.HorizontalBlurAmount;
                        postProcessHDR.NumberOfBlurPasses = rMessage.NumberOfBlurPasses;



                        break;
                    }

                case MyRenderMessageEnum.UpdateAntiAliasSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateAntiAliasSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.FXAA) as MyPostProcessAntiAlias;

                        postProcess.Enabled = rMessage.Enabled;



                        break;
                    }

                case MyRenderMessageEnum.UpdateVignettingSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateVignettingSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.Vignetting) as MyPostProcessVignetting;

                        postProcess.Enabled = rMessage.Enabled;
                        postProcess.VignettingPower = rMessage.VignettingPower;



                        break;
                    }

                case MyRenderMessageEnum.UpdateColorMappingSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateColorMappingSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.ColorMapping) as MyPostProcessColorMapping;

                        postProcess.Enabled = rMessage.Enabled;



                        break;
                    }

                case MyRenderMessageEnum.UpdateContrastSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateContrastSettings)message;
                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.Contrast) as MyPostProcessContrast;

                        postProcess.Enabled = rMessage.Enabled;
                        postProcess.Contrast = rMessage.Contrast;
                        postProcess.Hue = rMessage.Hue;
                        postProcess.Saturation = rMessage.Saturation;



                        break;
                    }

                case MyRenderMessageEnum.UpdateChromaticAberrationSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateChromaticAberrationSettings)message;
                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.ChromaticAberration) as MyPostProcessChromaticAberration;

                        postProcess.Enabled = rMessage.Enabled;
                        postProcess.DistortionLens = rMessage.DistortionLens;
                        postProcess.DistortionCubic = rMessage.DistortionCubic;
                        postProcess.DistortionWeights = rMessage.DistortionWeights;



                        break;
                    }

                case MyRenderMessageEnum.UpdateSSAOSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateSSAOSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.VolumetricSSAO2) as MyPostProcessVolumetricSSAO2;

                        postProcess.Enabled = rMessage.Enabled;

                        postProcess.ShowOnlySSAO = rMessage.ShowOnlySSAO;
                        postProcess.UseBlur = rMessage.UseBlur;

                        postProcess.MinRadius = rMessage.MinRadius;
                        postProcess.MaxRadius = rMessage.MaxRadius;
                        postProcess.RadiusGrowZScale = rMessage.RadiusGrowZScale;
                        postProcess.CameraZFar = rMessage.CameraZFar;

                        postProcess.Bias = rMessage.Bias;
                        postProcess.Falloff = rMessage.Falloff;
                        postProcess.NormValue = rMessage.NormValue;
                        postProcess.Contrast = rMessage.Contrast;



                        break;
                    }

                case MyRenderMessageEnum.UpdateFogSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateFogSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.VolumetricFog) as MyPostProcessVolumetricFog;

                        postProcess.Enabled = rMessage.Settings.Enabled;
                        FogProperties.FogNear = rMessage.Settings.FogNear;
                        FogProperties.FogFar = rMessage.Settings.FogFar;
                        FogProperties.FogMultiplier = rMessage.Settings.FogMultiplier;
                        FogProperties.FogBacklightMultiplier = rMessage.Settings.FogBacklightMultiplier;
                        FogProperties.FogColor = rMessage.Settings.FogColor;



                        break;
                    }

                case MyRenderMessageEnum.UpdateGodRaysSettings:
                    {
                        var rMessage = (MyRenderMessageUpdateGodRaysSettings)message;

                        var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.GodRays) as MyPostProcessGodRays;

                        postProcess.Enabled = rMessage.Enabled;
                        postProcess.Density = rMessage.Density;
                        postProcess.Weight = rMessage.Weight;
                        postProcess.Decay = rMessage.Decay;
                        postProcess.Exposition = rMessage.Exposition;
                        postProcess.ApplyBlur = rMessage.ApplyBlur;



                        break;
                    }

                #endregion

                #region Environment

                case MyRenderMessageEnum.UpdateEnvironmentMap:
                    {
                        EnqueueDrawMessage(message);
                        break;
                    }

                #endregion

                #region Video

                case MyRenderMessageEnum.PlayVideo:
                    {
                        var rMessage = (MyRenderMessagePlayVideo)message;

                        MyRender.PlayVideo(rMessage.ID, rMessage.VideoFile, rMessage.Volume);



                        break;
                    }

                case MyRenderMessageEnum.UpdateVideo:
                    {
                        var rMessage = (MyRenderMessageUpdateVideo)message;

                        MyRender.UpdateVideo(rMessage.ID);



                        break;
                    }

                case MyRenderMessageEnum.DrawVideo:
                    {
                        var rMessage = (MyRenderMessageDrawVideo)message;

                        EnqueueDrawMessage(rMessage);

                        break;
                    }

                case MyRenderMessageEnum.CloseVideo:
                    {
                        var rMessage = (MyRenderMessageCloseVideo)message;

                        MyRender.CloseVideo(rMessage.ID);



                        break;
                    }

                case MyRenderMessageEnum.SetVideoVolume:
                    {
                        var rMessage = (MyRenderMessageSetVideoVolume)message;

                        MyRender.SetVideoVolume(rMessage.ID, rMessage.Volume);



                        break;
                    }


                #endregion

                #region Secondary camera

                case MyRenderMessageEnum.DrawSecondaryCamera:
                    {
                        var rMessage = (MyRenderMessageDrawSecondaryCamera)message;

                        EnqueueDrawMessage(rMessage);

                        break;
                    }

                case MyRenderMessageEnum.DrawSecondaryCameraSprite:
                    {
                        var rMessage = (MyRenderMessageDrawSecondaryCameraSprite)message;
                        EnqueueDrawMessage(rMessage);
                        break;
                    }

                #endregion

                #region Decals

                case MyRenderMessageEnum.CreateDecal:
                    {
                        var rMessage = (MyRenderMessageCreateDecal)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            MyDecals.AddDecal(
                                renderObject,
                                ref rMessage.Triangle,
                                rMessage.TrianglesToAdd,
                                rMessage.Texture,
                                (Vector3D)rMessage.Position,
                                rMessage.LightSize,
                                rMessage.Emissivity
                               );
                        }



                        break;
                    }


                case MyRenderMessageEnum.HideDecals:
                    {
                        var rMessage = (MyRenderMessageHideDecals)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.ID, out renderObject))
                        {
                            if (rMessage.Radius == 0 && renderObject is MyRenderTransformObject)
                            {
                                MyDecals.RemoveModelDecals(renderObject as MyRenderTransformObject);
                            }
                            else
                            {
                                if (renderObject is MyRenderVoxelCell)
                                {
                                    VRageMath.BoundingSphere bs = new VRageMath.BoundingSphere(rMessage.Center, rMessage.Radius);
                                    MyDecals.HideTrianglesAfterExplosion(renderObject as MyRenderVoxelCell, ref bs);
                                }
                            }
                        }



                        break;
                    }



                #endregion

                #region Cockpit

                case MyRenderMessageEnum.UpdateCockpitGlass:
                    {
                        var rMessage = (MyRenderMessageUpdateCockpitGlass)message;

                        MyCockpitGlass.Visible = rMessage.Visible;
                        MyCockpitGlass.PlayerHeadForCockpitInteriorWorldMatrix = (MatrixD)rMessage.WorldMatrix;
                        MyCockpitGlass.GlassDirtAlpha = rMessage.DirtAlpha;
                        MyCockpitGlass.Model = rMessage.Model;



                        break;
                    }

                #endregion

                #region Billboards and quality

                case MyRenderMessageEnum.UpdateBillboardsColorize:
                    {
                        var rMessage = (MyRenderMessageUpdateBillboardsColorize)message;

                        EnqueueDrawMessage(rMessage);

                        break;
                    }

                case MyRenderMessageEnum.AddLineBillboardLocal:
                    {
                        var rMessage = (MyRenderMessageAddLineBillboardLocal)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.RenderObjectID, out renderObject))
                        {
                            renderObject.Billboards.Add(rMessage);
                        }

                        break;
                    }

                case MyRenderMessageEnum.AddPointBillboardLocal:
                    {
                        var rMessage = (MyRenderMessageAddPointBillboardLocal)message;

                        MyRenderObject renderObject;
                        if (m_renderObjects.TryGetValue(rMessage.RenderObjectID, out renderObject))
                        {
                            renderObject.Billboards.Add(rMessage);
                        }

                        break;
                    }

                case MyRenderMessageEnum.UpdateDistantImpostors:
                    {
                        var rMessage = (MyRenderMessageUpdateDistantImpostors)message;

                        MyDistantImpostors.ImpostorProperties = rMessage.ImpostorProperties;
                        MyDistantImpostors.Static.ReloadContent();



                        break;
                    }

                case MyRenderMessageEnum.SetTextureIgnoreQuality:
                    {
                        var rMessage = (MyRenderMessageSetTextureIgnoreQuality)message;

                        MyTextureManager.TexturesWithIgnoredQuality.Add(rMessage.Path);
                        MyTextureManager.UnloadTexture(rMessage.Path);



                        break;
                    }

                case MyRenderMessageEnum.UpdateRenderQuality:
                    {
                        var rMessage = (MyRenderMessageUpdateRenderQuality)message;

                        MyRenderQualityProfile profile = MyRenderConstants.m_renderQualityProfiles[(int)rMessage.RenderQuality];

                        profile.LodTransitionDistanceNear = rMessage.LodTransitionDistanceNear;
                        profile.LodTransitionDistanceFar = rMessage.LodTransitionDistanceFar;
                        profile.LodTransitionDistanceBackgroundStart = rMessage.LodTransitionDistanceBackgroundStart;
                        profile.LodTransitionDistanceBackgroundEnd = rMessage.LodTransitionDistanceBackgroundEnd;
                        profile.EnvironmentLodTransitionDistance = rMessage.EnvironmentLodTransitionDistance;
                        profile.EnvironmentLodTransitionDistanceBackground = rMessage.EnvironmentLodTransitionDistanceBackground;
                        profile.EnableCascadeBlending = rMessage.EnableCascadeBlending;

                        MyRenderTexturePool.RenderQualityChanged(rMessage.RenderQuality);

                        break;
                    }

                case MyRenderMessageEnum.TakeScreenshot:
                    {
                        var rMessage = (MyRenderMessageTakeScreenshot)message;

                        m_screenshot = new MyScreenshot(rMessage.SizeMultiplier, rMessage.PathToSave, rMessage.IgnoreSprites, rMessage.ShowNotification);
                        ScreenshotOnlyFinal = !rMessage.Debug;

                        //Will do before draw
                        //UpdateScreenSize();
                        //MyEnvironmentMap.Reset();



                        break;
                    }
                case MyRenderMessageEnum.RenderColoredTexture:
                    {
                        var rMessage = (MyRenderMessageRenderColoredTexture)message;
                        m_texturesToRender.AddRange(rMessage.texturesToRender);
                        break;
                    }

                #endregion

                #region Characters

                case MyRenderMessageEnum.SetCharacterSkeleton:
                    {
                        var rMessage = (MyRenderMessageSetCharacterSkeleton)message;

                        MyRenderObject renderCharacterObject;
                        if (m_renderObjects.TryGetValue(rMessage.CharacterID, out renderCharacterObject))
                        {
                            MyRenderCharacter renderCharacter = (MyRenderCharacter)renderCharacterObject;
                            renderCharacter.SetSkeleton(rMessage.SkeletonBones, rMessage.SkeletonIndices);
                        }

                        break;
                    }

                case MyRenderMessageEnum.SetCharacterTransforms:
                    {
                        var rMessage = (MyRenderMessageSetCharacterTransforms)message;

                        MyRenderObject renderCharacterObject;
                        if (m_renderObjects.TryGetValue(rMessage.CharacterID, out renderCharacterObject))
                        {
                            MyRenderCharacter renderCharacter = (MyRenderCharacter)renderCharacterObject;
                            renderCharacter.SetAnimationBones(rMessage.RelativeBoneTransforms);
                        }



                        break;
                    }


                #endregion

                #region Debug draw

                case MyRenderMessageEnum.DebugDrawLine3D:
                case MyRenderMessageEnum.DebugDrawLine2D:
                case MyRenderMessageEnum.DebugDrawSphere:
                case MyRenderMessageEnum.DebugDrawAABB:
                case MyRenderMessageEnum.DebugDrawAxis:
                case MyRenderMessageEnum.DebugDrawOBB:
                case MyRenderMessageEnum.DebugDrawTriangle:
                case MyRenderMessageEnum.DebugDrawCapsule:
                case MyRenderMessageEnum.DebugDrawText2D:
                case MyRenderMessageEnum.DebugDrawText3D:
                case MyRenderMessageEnum.DebugDrawModel:
                case MyRenderMessageEnum.DebugDrawTriangles:
                case MyRenderMessageEnum.DebugDrawPlane:
                case MyRenderMessageEnum.DebugDrawCylinder:
                    {
                        EnqueueDebugDrawMessage(message);
                    }
                    break;

                case MyRenderMessageEnum.DebugCrashRenderThread:
                    {
                        throw new InvalidOperationException("Forced exception");
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
                        EnqueueDrawMessage(message);
                        break;
                    }

                #endregion

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
                        // Dx9 Only understands interpolation and render quality.
                        var rMessage = (MyRenderMessageSwitchRenderSettings)message;
                        MyRenderProxy.Settings.EnableObjectInterpolation = rMessage.Settings.InterpolationEnabled;
                        MyRenderProxy.Settings.EnableCameraInterpolation = rMessage.Settings.InterpolationEnabled;
                        MyRenderProxy.RenderThread.SwitchQuality(rMessage.Settings.Dx9Quality);
                        break;
                    }

                case MyRenderMessageEnum.UnloadData:
                    {
                        MyRender.UnloadData();



                        break;
                    }

                case MyRenderMessageEnum.CollectGarbage:
                    {
                        GC.Collect();
                        break;
                    }

                case MyRenderMessageEnum.UpdatePostprocessSettings:
                    {
                        break;
                    }

                default:
                  //  System.Diagnostics.Debug.Assert(false, "Unknown message");
                    break;
            }
        }
        private static MyRenderMeshMaterial GetMeshMaterial(uint renderObjectID, string materialName)
        {
            MyRenderObject renderObject;
            if (m_renderObjects.TryGetValue(renderObjectID, out renderObject))
            {
                MyRenderEntity renderEntity = renderObject as MyRenderEntity;
                List<MyRenderMeshMaterial> materials = renderEntity.Lods[0].MeshMaterials;
                MyRenderModel model = renderEntity.Lods[0].Model;

                model.HasSharedMaterials = false;


                foreach (var rMaterial in materials)
                {
                    if (rMaterial.MaterialName == materialName)
                    {
                        return rMaterial;
                    }
                }
            }
            return null;
        }
    }
}
