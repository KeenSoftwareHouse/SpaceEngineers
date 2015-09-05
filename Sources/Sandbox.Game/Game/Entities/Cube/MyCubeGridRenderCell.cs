using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Utils;
using VRage.Import;
using VRageMath;
using VRageRender;
using Sandbox.Definitions;
using VRage.Utils;
using Sandbox.Engine.Models;
using ModelId = System.Int32;
using Sandbox.Graphics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using Sandbox.Common;
using Sandbox.Game.Components;
using VRage.Utils;
using VRage;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.Cube
{
    struct MyInstanceInfo
    {
        public MyInstanceFlagsEnum Flags;
        public float MaxViewDistance;

        public MyInstanceInfo(MyInstanceFlagsEnum flags, float maxViewDistance)
        {
            Flags = flags;
            MaxViewDistance = maxViewDistance;
        }
    }

    public class MyCubeGridRenderCell
    {
        struct EdgeInfoNormal
        {
            public Vector3 Normal;
            public Color Color;
            public MyStringHash EdgeModel;
        }

        public readonly MyRenderComponentCubeGrid m_gridRenderComponent;
        public readonly float EdgeViewDistance;
        public string DebugName;

        BoundingBox m_boundingBox = BoundingBox.CreateInvalid();
        BoundingBox m_tmpBoundingBox;

        //This convert array of model strings to same array of model IDs
        //static ModelId[][] m_cubeEdgeModelIds = MyCubeGridDefinitions.CubeEdgeModels.Select(s => s.Select(x => MyModel.GetId(x)).ToArray()).ToArray();
        //static ModelId[][] m_cubeHeavyEdgeModelIds = MyCubeGridDefinitions.CubeHeavyEdgeModels.Select(s => s.Select(x => MyModel.GetId(x)).ToArray()).ToArray();


        static List<MyCubeInstanceData> m_tmpInstanceData = new List<MyCubeInstanceData>(); // Merge instance data
        static Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> m_tmpInstanceParts = new Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>>();

        uint m_parentCullObject = MyRenderProxy.RENDER_ID_UNASSIGNED;
        uint m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
        Dictionary<ModelId, MyRenderInstanceInfo> m_instanceInfo = new Dictionary<ModelId, MyRenderInstanceInfo>();
        Dictionary<ModelId, uint> m_instanceGroupRenderObjects = new Dictionary<ModelId, uint>();

        HashSet<MyCubePart> m_cubeParts = new HashSet<MyCubePart>();
        Dictionary<long, MyFourEdgeInfo> m_edgeInfosNew = new Dictionary<long, MyFourEdgeInfo>();

        List<EdgeInfoNormal> m_edgesToCompare = new List<EdgeInfoNormal>();

        public HashSet<MyCubePart> CubeParts { get { return m_cubeParts; } }

        public MyCubeGridRenderCell(MyRenderComponentCubeGrid gridRender)
        {
            m_gridRenderComponent = gridRender;
            EdgeViewDistance = gridRender.GridSizeEnum == Common.ObjectBuilders.MyCubeSize.Large ? 130 : 35;
        }

        public bool AddCubePart(MyCubePart part)
        {
            return m_cubeParts.Add(part);
        }

        public bool RemoveCubePart(MyCubePart part)
        {
            return m_cubeParts.Remove(part);
        }

        public bool AddEdgeInfo(long hash, MyEdgeInfo info, MySlimBlock owner)
        {
            MyFourEdgeInfo fourInfo;
            if (!m_edgeInfosNew.TryGetValue(hash, out fourInfo))
            {
                fourInfo = new MyFourEdgeInfo(info.LocalOrthoMatrix, info.EdgeType);
                m_edgeInfosNew.Add(hash, fourInfo);
            }
            return fourInfo.AddInstance(owner.Position * owner.CubeGrid.GridSize, info.Color, info.EdgeModel, info.PackedNormal0, info.PackedNormal1);
        }

        public bool RemoveEdgeInfo(long hash, MySlimBlock owner)
        {
            MyFourEdgeInfo fourInfo;
            var result = m_edgeInfosNew.TryGetValue(hash, out fourInfo) && fourInfo.RemoveInstance(owner.Position * owner.CubeGrid.GridSize);
            if (result && fourInfo.Empty)
            {
                m_edgeInfosNew.Remove(hash);
            }
            return result;
        }

        bool InstanceDataCleared(Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> instanceParts)
        {
            foreach (var data in instanceParts)
            {
                if (data.Value.Item1.Count > 0)
                    return false;
            }
            return true;
        }

        public void RebuildInstanceParts(RenderFlags renderFlags)
        {
            var list = m_tmpInstanceParts;

            ProfilerShort.Begin("Assert data empty");
            Debug.Assert(InstanceDataCleared(list));
            ProfilerShort.End();

            ProfilerShort.Begin("Add cube parts");
            foreach (var part in m_cubeParts)
            {
                AddInstancePart(list, part.Model.UniqueId, ref part.InstanceData, MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.CastShadows | MyInstanceFlagsEnum.EnableColorMask);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Add edge parts");
            AddEdgeParts(list);
            ProfilerShort.End();

            UpdateRenderInstanceData(list, renderFlags);

            ProfilerShort.Begin("Clear parts");
            ClearInstanceParts(list);
            ProfilerShort.End();
        }

        private void AddEdgeParts(Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> instanceParts)
        {
            // This can be optimized in same way as cube parts are
            m_edgesToCompare.Clear();
            float reduceEpsilon = 0.1f;

            MyCubeInstanceData inst = new MyCubeInstanceData();
            inst.ResetBones();
            inst.SetTextureOffset(new Vector2(0, 0));

            foreach (var edgeInfoPair in m_edgeInfosNew)
            {
                if (edgeInfoPair.Value.Full/* || edgeInfoPair.Value.Empty*/)
                    continue;

                bool isVisible = false;
                m_edgesToCompare.Clear();

                //Find opposite normals and remove them
                Color color;
                MyStringHash edgeModel;
                Base27Directions.Direction normal0, normal1;
                for (int i = 0; i < MyFourEdgeInfo.MaxInfoCount; i++)
                {
                    if (edgeInfoPair.Value.GetNormalInfo(i, out color, out edgeModel, out normal0, out normal1))
                    {
                        m_edgesToCompare.Add(new EdgeInfoNormal() { Normal = Base27Directions.GetVector(normal0), Color = color, EdgeModel = edgeModel });
                        m_edgesToCompare.Add(new EdgeInfoNormal() { Normal = Base27Directions.GetVector(normal1), Color = color, EdgeModel = edgeModel });
                    }
                }

                int c = 0;
                bool wasFour = m_edgesToCompare.Count == 4;
                var baseEdgeModel = m_edgesToCompare[0].EdgeModel;

                while (c < m_edgesToCompare.Count)
                {
                    bool normalsRemoved = false;
                    for (int c2 = c + 1; c2 < m_edgesToCompare.Count; c2++)
                    {
                        //opposite normals?
                        if (MyUtils.IsZero(m_edgesToCompare[c].Normal + m_edgesToCompare[c2].Normal, reduceEpsilon))
                        {
                            if (c > c2)
                            {
                                m_edgesToCompare.RemoveAt(c);
                                m_edgesToCompare.RemoveAt(c2);
                            }
                            else
                            {
                                m_edgesToCompare.RemoveAt(c2);
                                m_edgesToCompare.RemoveAt(c);
                            }

                            normalsRemoved = true;
                            break;
                        }
                    }

                    if (normalsRemoved)
                        continue;

                    c++;
                }

                Debug.Assert(m_edgesToCompare.Count != 1, "Alone edge with one normal cannot exist");

                bool resultEdgesHaveDifferentColor = false;
                bool resultEdgesHaveDifferentArmorType = false;
                
                if (m_edgesToCompare.Count > 0)
                {
                    Color baseColor = m_edgesToCompare[0].Color;
                    foreach (var edge in m_edgesToCompare)
                    {
                        if (edge.Color != baseColor)
                        {
                            resultEdgesHaveDifferentColor = true;
                            break;
                        }
                    }


                    baseEdgeModel = m_edgesToCompare[0].EdgeModel;
                    foreach (var edge in m_edgesToCompare)
                    {
                        resultEdgesHaveDifferentArmorType |= baseEdgeModel != edge.EdgeModel;
                    }
                }

                if (m_edgesToCompare.Count == 1)
                    isVisible = false;
                else if (resultEdgesHaveDifferentColor || resultEdgesHaveDifferentArmorType)
                    isVisible = true;
                else
                    if (m_edgesToCompare.Count > 2)
                        isVisible = true;
                    else
                        if (m_edgesToCompare.Count == 0)
                            isVisible = wasFour;
                        else
                        {
                            Debug.Assert(m_edgesToCompare.Count == 2);

                            //Check normals angle to get visibility
                            float d = Vector3.Dot(m_edgesToCompare[0].Normal, m_edgesToCompare[1].Normal);

                            Debug.Assert(d != -1, "We already removed opposite normals");

                            if (Math.Abs(d) > 0.85f)
                            {   //consider this without outline
                                isVisible = false;
                            }
                            else
                                isVisible = true;
                        }

                if (isVisible)
                {
                    var definition = MyDefinitionManager.Static.GetEdgesDefinition(new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_EdgesDefinition)), baseEdgeModel));
                    var edgesSet = m_gridRenderComponent.GridSizeEnum == MyCubeSize.Large ? definition.Large : definition.Small;

                    int modelId = 0;
                    switch (edgeInfoPair.Value.EdgeType)
                    {
                        case MyCubeEdgeType.Horizontal:
                            modelId = MyModel.GetId(edgesSet.Horisontal);
                            break;
                        case MyCubeEdgeType.Horizontal_Diagonal:
                            modelId = MyModel.GetId(edgesSet.HorisontalDiagonal);
                            break;
                        case MyCubeEdgeType.Vertical:
                            modelId = MyModel.GetId(edgesSet.Vertical);
                            break;
                        case MyCubeEdgeType.Vertical_Diagonal:
                            modelId = MyModel.GetId(edgesSet.VerticalDiagonal);
                            break;
                    }

                    //var modelId = resultEdgesHaveDifferentArmorType || baseHeavy ? 
                      //  m_cubeHeavyEdgeModelIds[(int)Grid.GridSizeEnum][(int)edgeInfoPair.Value.EdgeType] :
                        //m_cubeEdgeModelIds[(int)Grid.GridSizeEnum][(int)edgeInfoPair.Value.EdgeType];

                    inst.PackedOrthoMatrix = edgeInfoPair.Value.LocalOrthoMatrix;
                    AddInstancePart(instanceParts, modelId, ref inst, 0, EdgeViewDistance);
                }
            }
        }

        void ClearInstanceParts(Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> instanceParts)
        {
            m_boundingBox = BoundingBox.CreateInvalid();

            foreach (var item in instanceParts)
            {
                item.Value.Item1.Clear();
            }
        }

        void AddInstancePart(Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> instanceParts, ModelId modelId, ref MyCubeInstanceData instance, MyInstanceFlagsEnum flags, float maxViewDistance = float.MaxValue)
        {
            Tuple<List<MyCubeInstanceData>, MyInstanceInfo> matrices;
            if (!instanceParts.TryGetValue(modelId, out matrices))
            {
                matrices = new Tuple<List<MyCubeInstanceData>, MyInstanceInfo>(new List<MyCubeInstanceData>(), new MyInstanceInfo(flags, maxViewDistance));
                instanceParts.Add(modelId, matrices);
            }

            m_tmpBoundingBox.Min = instance.LocalMatrix.Translation - new Vector3(m_gridRenderComponent.GridSize);
            m_tmpBoundingBox.Max = instance.LocalMatrix.Translation + new Vector3(m_gridRenderComponent.GridSize);
            m_boundingBox.Include(m_tmpBoundingBox);

            matrices.Item1.Add(instance);
        }

        void AddRenderObjectId(uint renderObjectId, bool forPositionUpdates)
        {
            var list = forPositionUpdates ? m_gridRenderComponent.RenderObjectIDs : m_gridRenderComponent.AdditionalRenderObjects;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    list[i] = renderObjectId;
                    MyEntities.AddRenderObjectToMap(renderObjectId, m_gridRenderComponent.Container.Entity);
                    return;
                }
            }

            // When list is full
            int pos = list.Length;
            if (forPositionUpdates)
            {
                m_gridRenderComponent.ResizeRenderObjectArray(list.Length + 3);
            }
            else
            {
                var oldSize = m_gridRenderComponent.AdditionalRenderObjects.Length;
                Array.Resize(ref  m_gridRenderComponent.AdditionalRenderObjects, list.Length + 3);
                for (int i = oldSize; i < list.Length + 3; i++)
                {
                     m_gridRenderComponent.AdditionalRenderObjects[i] = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
            }
            list = forPositionUpdates ? m_gridRenderComponent.RenderObjectIDs : m_gridRenderComponent.AdditionalRenderObjects;
            
            list[pos] = renderObjectId;
            MyEntities.AddRenderObjectToMap(renderObjectId, m_gridRenderComponent.Container.Entity);
        }

        void RemoveRenderObjectId(uint renderObjectId, bool forPositionUpdates)
        {
            var list = forPositionUpdates ? m_gridRenderComponent.RenderObjectIDs : m_gridRenderComponent.AdditionalRenderObjects;

            MyEntities.RemoveRenderObjectFromMap(renderObjectId);
            VRageRender.MyRenderProxy.RemoveRenderObject(renderObjectId);

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == renderObjectId)
                {
                    list[i] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
                    break;
                }
            }
        }

        private void UpdateRenderInstanceData(Dictionary<ModelId, Tuple<List<MyCubeInstanceData>, MyInstanceInfo>> instanceParts, RenderFlags renderFlags)
        {
            if (m_instanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                m_instanceBufferId = MyRenderProxy.CreateRenderInstanceBuffer(m_gridRenderComponent.Container.Entity.GetFriendlyName() + " " + m_gridRenderComponent.Container.Entity.EntityId.ToString() + ", instance buffer " + DebugName, MyRenderInstanceBufferType.Cube);
                AddRenderObjectId(m_instanceBufferId, false);
            }

            if (m_parentCullObject == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                m_parentCullObject = MyRenderProxy.CreateManualCullObject(m_gridRenderComponent.Container.Entity.GetFriendlyName() + " " + m_gridRenderComponent.Container.Entity.EntityId.ToString() + ", cull object", m_gridRenderComponent.Container.Entity.PositionComp.WorldMatrix);
                AddRenderObjectId(m_parentCullObject, true);
            }

            ProfilerShort.Begin("Merge render parts");

            // Merge data into one buffer
            Debug.Assert(m_tmpInstanceData.Count == 0, "Instance data is not cleared");
            m_instanceInfo.Clear();
            foreach (var part in instanceParts)
            {
                m_instanceInfo.Add(part.Key, new MyRenderInstanceInfo(m_instanceBufferId, m_tmpInstanceData.Count, part.Value.Item1.Count, part.Value.Item2.MaxViewDistance, part.Value.Item2.Flags));

                m_tmpInstanceData.AddList(part.Value.Item1);
            }
            ProfilerShort.End();

            if (m_tmpInstanceData.Count > 0)
            {
                ProfilerShort.Begin("Update instance buffer");
                MyRenderProxy.UpdateRenderCubeInstanceBuffer(m_instanceBufferId, m_tmpInstanceData, (int)(m_tmpInstanceData.Count * 1.2f));
                ProfilerShort.End();
            }
            m_tmpInstanceData.Clear();

            ProfilerShort.Begin("Update instance entitites");
            UpdateRenderEntitiesInstanceData(renderFlags, m_parentCullObject);
            ProfilerShort.End();
        }

        private void UpdateRenderEntitiesInstanceData(RenderFlags renderFlags, uint parentCullObject)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceInfo)
            {
                uint renderObjectId;
                bool exists = m_instanceGroupRenderObjects.TryGetValue(item.Key, out renderObjectId);
                bool hasAnyInstances = item.Value.InstanceCount > 0;

                var flags = item.Value.CastShadows ? renderFlags | RenderFlags.CastShadows : renderFlags & ~RenderFlags.CastShadows;

                if (!exists && hasAnyInstances)
                {
                    renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                        m_gridRenderComponent.Container.Entity.GetFriendlyName() + " " + m_gridRenderComponent.Container.Entity.EntityId.ToString() + ", part: " + item.Key,
                        MyModel.GetById(item.Key),
                        (MatrixD)m_gridRenderComponent.Container.Entity.PositionComp.WorldMatrix,
                        MyMeshDrawTechnique.MESH,
                        flags,
                        CullingOptions.Default,
                        m_gridRenderComponent.GetDiffuseColor(),
                        Vector3.Zero,
                        m_gridRenderComponent.Transparency,
                        item.Value.MaxViewDistance
                    );

                    m_instanceGroupRenderObjects[item.Key] = renderObjectId;

                    AddRenderObjectId(renderObjectId, !MyFakes.MANUAL_CULL_OBJECTS);
                    
                    if(MyFakes.MANUAL_CULL_OBJECTS)
                    {
                        Debug.Assert(parentCullObject != MyRenderProxy.RENDER_ID_UNASSIGNED, "Somethings wrong, parent cull object should have been created");
                        MyRenderProxy.SetParentCullObject(renderObjectId, parentCullObject, Matrix.Identity);
                    }
                }
                else if (exists && !hasAnyInstances)
                {
                    uint objectId = m_instanceGroupRenderObjects[item.Key];
                    RemoveRenderObjectId(objectId, !MyFakes.MANUAL_CULL_OBJECTS);
                    m_instanceGroupRenderObjects.Remove(item.Key);
                    renderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                    continue;
                }

                if (hasAnyInstances)
                {
                    MyRenderProxy.SetInstanceBuffer(renderObjectId, item.Value.InstanceBufferId, item.Value.InstanceStart, item.Value.InstanceCount, m_boundingBox);
                }
            }
        }

        internal void DebugDraw()
        {
            string text = String.Format("CubeParts:{0}, EdgeParts{1}", m_cubeParts.Count, m_edgeInfosNew.Count);

            MyRenderProxy.DebugDrawText3D(m_boundingBox.Center + m_gridRenderComponent.Container.Entity.PositionComp.WorldMatrix.Translation, text, Color.Red, 0.75f, false);

            var localMatrix = Matrix.CreateScale(m_boundingBox.Size) * Matrix.CreateTranslation(m_boundingBox.Center);
            var matrix = localMatrix * m_gridRenderComponent.Container.Entity.PositionComp.WorldMatrix;
            MyRenderProxy.DebugDrawOBB(matrix, Color.Red.ToVector3(), 0.25f, true, true);
        }

        internal uint ParentCullObject
        {
            get
            {
                return m_parentCullObject;
            }
        }

    }
}
