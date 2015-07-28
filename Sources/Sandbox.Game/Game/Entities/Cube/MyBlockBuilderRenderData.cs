﻿#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Import;
using VRageMath;
using VRageRender;
using ModelId = System.Int32;


#endregion

namespace Sandbox.Game.Entities.Cube
{
    public class MyBlockBuilderRenderData
    {
        private class MyBuilderInstanceData
        {
            public ModelId Model;
            public MyInstanceInfo InstanceInfo = new MyInstanceInfo(MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.EnableColorMask, float.MaxValue);
            public List<MyCubeInstanceData> InstanceData = new List<MyCubeInstanceData>();
        }

        private Dictionary<ModelId, MyBuilderInstanceData> m_instanceParts = new Dictionary<ModelId, MyBuilderInstanceData>();
        private uint m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
        private List<MyCubeInstanceData> m_tmpInstanceData = new List<MyCubeInstanceData>(); // Merge instance data
        private Dictionary<ModelId, MyRenderInstanceInfo> m_instanceInfo = new Dictionary<ModelId, MyRenderInstanceInfo>();
        private Dictionary<ModelId, uint> m_instanceGroupRenderObjects = new Dictionary<ModelId, uint>();
        private BoundingBox m_cubeBuilderAABB;

        private float Transparency = (MyFakes.ENABLE_TRANSPARENT_CUBE_BUILDER) ? 0.25f : 0.0f;



        public void UnloadRenderObjects()
        {
            foreach (var renderObject in m_instanceGroupRenderObjects)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(renderObject.Value);
            }
            m_instanceGroupRenderObjects.Clear();

            if (m_instanceBufferId != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(m_instanceBufferId);
                m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        public void ClearInstanceData()
        {
            m_tmpInstanceData.Clear();
            m_cubeBuilderAABB = BoundingBox.CreateInvalid();

            foreach (var item in m_instanceParts)
            {
                item.Value.InstanceData.Clear();
            }
        }

        // TODO: this parameter won't be optional
        public void AddInstance(ModelId model, MatrixD matrix, ref MatrixD invGridWorldMatrix, Vector4 colorMaskHsv = default(Vector4), Vector3UByte[] bones = null, float gridSize = 1f)
        {
            Matrix localMatrix = (Matrix)(matrix * invGridWorldMatrix);

            MyBuilderInstanceData builderInstanceData;
            if (!m_instanceParts.TryGetValue(model, out builderInstanceData))
            {
                builderInstanceData = new MyBuilderInstanceData();
                builderInstanceData.Model = model;
                m_instanceParts.Add(model, builderInstanceData);
            }

            if (bones == null)
            {
                builderInstanceData.InstanceData.Add(new MyCubeInstanceData()
                {
                    ColorMaskHSV = new Vector4(MyPlayer.SelectedColor, 0),
                    EnableSkinning = false,
                    LocalMatrix = localMatrix
                });
            }
            else
            {
                var cubeInstance = new MyCubeInstanceData()
                {
					ColorMaskHSV = new Vector4(MyPlayer.SelectedColor, 0),
                    EnableSkinning = true,
                    LocalMatrix = localMatrix,
                };

                cubeInstance.BoneRange = gridSize;

                for (int i = 0; i < 9; i++)
                {
                    cubeInstance[i] = bones[i];
                }

                builderInstanceData.InstanceData.Add(cubeInstance);
            }

            m_cubeBuilderAABB = m_cubeBuilderAABB.Include(
                new BoundingBox(new Vector3(-MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large)),
                                new Vector3(MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large))).Transform(localMatrix));
        }

        public void UpdateRenderInstanceData()
        {
            if (m_instanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                m_instanceBufferId = MyRenderProxy.CreateRenderInstanceBuffer("Cube builder instance buffer", MyRenderInstanceBufferType.Cube);
            }

            // Merge data into one buffer
            Debug.Assert(m_tmpInstanceData.Count == 0, "Instance data is not cleared");
            m_instanceInfo.Clear();
            foreach (var part in m_instanceParts)
            {
                m_instanceInfo.Add(part.Key, new MyRenderInstanceInfo(m_instanceBufferId, m_tmpInstanceData.Count, part.Value.InstanceData.Count, part.Value.InstanceInfo.MaxViewDistance, part.Value.InstanceInfo.Flags));

                m_tmpInstanceData.AddList(part.Value.InstanceData);
            }

            if (m_tmpInstanceData.Count > 0)
            {
                MyRenderProxy.UpdateRenderCubeInstanceBuffer(m_instanceBufferId, m_tmpInstanceData, (int)(m_tmpInstanceData.Count * 1.2f));
            }
            m_tmpInstanceData.Clear();
        }

        public void UpdateRenderEntitiesData(MatrixD gridWorldMatrix, bool useTransparency)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceInfo)
            {
                uint renderObjectId;
                bool exists = m_instanceGroupRenderObjects.TryGetValue(item.Key, out renderObjectId);
                bool hasAnyInstances = item.Value.InstanceCount > 0;

                RenderFlags flags = item.Value.CastShadows ? RenderFlags.CastShadows : (RenderFlags)0;
                flags |= RenderFlags.Visible;

                if (!exists && hasAnyInstances)
                {
                    var model = MyModel.GetById(item.Key);
                    renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                        "Cube builder, part: " + item.Key,
                        model,
                        MatrixD.Identity,
                        MyMeshDrawTechnique.MESH,
                        flags,
                        CullingOptions.Default,
                        Vector3.One,
						MyPlayer.SelectedColor,
                        useTransparency ? Transparency : 0,
                        item.Value.MaxViewDistance
                    );

                    m_instanceGroupRenderObjects[item.Key] = renderObjectId;
                }
                else if (exists && !hasAnyInstances)
                {
                    uint objectId = m_instanceGroupRenderObjects[item.Key];
                    VRageRender.MyRenderProxy.RemoveRenderObject(objectId);
                    m_instanceGroupRenderObjects.Remove(item.Key);
                    renderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                    continue;
                }

                if (hasAnyInstances)
                {
					MyRenderProxy.UpdateRenderEntity(renderObjectId, Color.White, MyPlayer.SelectedColor, useTransparency ? Transparency : 0);
                    MyRenderProxy.UpdateRenderObject(renderObjectId, ref gridWorldMatrix, false);
                    MyRenderProxy.SetInstanceBuffer(renderObjectId, item.Value.InstanceBufferId, item.Value.InstanceStart, item.Value.InstanceCount, m_cubeBuilderAABB);
                }
            }
        }




    }
}
