using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using VRage.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    public enum GridLinkTypeEnum
    {
        Logical = 0,
        Physical = 1,
        NoContactDamage = 2,
    }

    public partial class MyCubeGridGroups : IMySceneComponent
    {
        public static MyCubeGridGroups Static;

        MyGroupsBase<MyCubeGrid>[] m_groupsByType;

        public MyGroups<MyCubeGrid, MyGridLogicalGroupData> Logical = new MyGroups<MyCubeGrid, MyGridLogicalGroupData>(true);
        public MyGroups<MyCubeGrid, MyGridPhysicalGroupData> Physical = new MyGroups<MyCubeGrid, MyGridPhysicalGroupData>(true, MyGridPhysicalGroupData.IsMajorGroup);
        public MyGroups<MyCubeGrid, MyGridNoDamageGroupData> NoContactDamage = new MyGroups<MyCubeGrid, MyGridNoDamageGroupData>(true);
        // Groups for small block to large block connections.
        public MyGroups<MySlimBlock, MyBlockGroupData> SmallToLargeBlockConnections = new MyGroups<MySlimBlock, MyBlockGroupData>(false);

        // Groups of dynamic connected grids (similar to physical, usually more groups smaller groups, because static grids no longer connect dynamic grids)
        public MyGroups<MyCubeGrid, MyGridPhysicalDynamicGroupData> PhysicalDynamic = new MyGroups<MyCubeGrid, MyGridPhysicalDynamicGroupData>(false);

        private static readonly HashSet<object> m_tmpBlocksDebugHelper = new HashSet<object>();

        public MyCubeGridGroups()
        {
            m_groupsByType = new MyGroupsBase<MyCubeGrid>[3];
            m_groupsByType[(int)GridLinkTypeEnum.Logical] = Logical;
            m_groupsByType[(int)GridLinkTypeEnum.Physical] = Physical;
            m_groupsByType[(int)GridLinkTypeEnum.NoContactDamage] = NoContactDamage;
        }

        public void AddNode(GridLinkTypeEnum type, MyCubeGrid grid)
        {
            GetGroups(type).AddNode(grid);
        }

        public void RemoveNode(GridLinkTypeEnum type, MyCubeGrid grid)
        {
            GetGroups(type).RemoveNode(grid);
        }

        /// <summary>
        /// Creates link between parent and child.
        /// Parent is owner of constraint.
        /// LinkId must be unique only for parent, for grid it can be packed position of block which created constraint.
        /// </summary>
        public void CreateLink(GridLinkTypeEnum type, long linkId, MyCubeGrid parent, MyCubeGrid child)
        {
            GetGroups(type).CreateLink(linkId, parent, child);
            if (type == GridLinkTypeEnum.Physical && !parent.Physics.IsStatic && !child.Physics.IsStatic)
            {
                PhysicalDynamic.CreateLink(linkId, parent, child);
            }
        }

        /// <summary>
        /// Breaks link between parent and child, you can set child to null to find it by linkId.
        /// Returns true when link was removed, returns false when link was not found.
        /// </summary>
        public bool BreakLink(GridLinkTypeEnum type, long linkId, MyCubeGrid parent, MyCubeGrid child = null)
        {
            if (type == GridLinkTypeEnum.Physical)
            {
                PhysicalDynamic.BreakLink(linkId, parent, child);
            }
            return GetGroups(type).BreakLink(linkId, parent, child);
        }

        public void UpdateDynamicState(MyCubeGrid grid)
        {
            var group = PhysicalDynamic.GetGroup(grid);
            bool wasDynamic = group != null;
            bool isDynamic = !grid.IsStatic;
            if (wasDynamic && !isDynamic) // Became static, break all links
            {
                PhysicalDynamic.BreakAllLinks(grid);
            }
            else if (!wasDynamic && isDynamic) // Became dynamic, "copy" links from PhysicalGroup
            {
                var physNode = Physical.GetNode(grid);
                if (physNode != null)
                {
                    foreach (var child in physNode.ChildLinks)
                    {
                        if (!child.Value.NodeData.IsStatic)
                            PhysicalDynamic.CreateLink(child.Key, grid, child.Value.NodeData);
                    }
                    foreach (var parent in physNode.ParentLinks)
                    {
                        if (!parent.Value.NodeData.IsStatic)
                            PhysicalDynamic.CreateLink(parent.Key, parent.Value.NodeData, grid);
                    }
                }
            }
        }

        public MyGroupsBase<MyCubeGrid> GetGroups(GridLinkTypeEnum type)
        {
            return m_groupsByType[(int)type];
        }

        void IMySceneComponent.Load()
        {
            Static = new MyCubeGridGroups();
        }

        void IMySceneComponent.Unload()
        {
            Static = null;
        }

        internal static void DebugDrawBlockGroups<TNode, TGroupData>(MyGroups<TNode, TGroupData> groups)
            where TGroupData : IGroupData<TNode>, new()
            where TNode : MySlimBlock
        {
            int hue = 0;
            BoundingBoxD aabb1, aabb2;

            foreach (var g in groups.Groups)
            {
                Color color = new Vector3((hue++ % 15) / 15.0f, 1, 1).HSVtoColor();

                foreach (var m in g.Nodes)
                {
                    try
                    {
                        m.NodeData.GetWorldBoundingBox(out aabb1);

                        foreach (var child in m.Children)
                        {
                            m_tmpBlocksDebugHelper.Add(child);
                        }

                        // This is O(n^2), but it's only debug draw
                        foreach (var child in m_tmpBlocksDebugHelper)
                        {
                            MyGroups<TNode, TGroupData>.Node node = null;
                            int count = 0;
                            foreach (var c in m.Children)
                            {
                                if (child == c)
                                {
                                    node = c;
                                    count++;
                                }
                            }

                            node.NodeData.GetWorldBoundingBox(out aabb2);

                            MyRenderProxy.DebugDrawLine3D(aabb1.Center, aabb2.Center, color, color, false);
                            MyRenderProxy.DebugDrawText3D((aabb1.Center + aabb2.Center) * 0.5f, count.ToString(), color, 1.0f, false);
                        }

                        var lightColor = new Color(color.ToVector3() + 0.25f);
                        MyRenderProxy.DebugDrawSphere(aabb1.Center, 0.2f, lightColor.ToVector3(), 0.5f, false, true);

                        MyRenderProxy.DebugDrawText3D(aabb1.Center, m.LinkCount.ToString(), lightColor, 1.0f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    }
                    finally
                    {
                        m_tmpBlocksDebugHelper.Clear();
                    }
                }
            }
        }
    }
}
