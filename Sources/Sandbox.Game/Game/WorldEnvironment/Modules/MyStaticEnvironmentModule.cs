using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.WorldEnvironment.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.WorldEnvironment.Modules
{
    public class MyStaticEnvironmentModule : MyEnvironmentModuleBase
    {
        private readonly HashSet<int> m_disabledItems = new HashSet<int>();

        private List<MyOrientedBoundingBoxD> m_boxes;

        private int m_minScannedLod = MyEnvironmentSectorConstants.MaximumLod;

        public override unsafe void Init(MyLogicalEnvironmentSectorBase sector, MyObjectBuilder_Base ob)
        {
            base.Init(sector, ob);

            var penv = (MyPlanetEnvironmentComponent)sector.Owner;

            if (penv.CollisionCheckEnabled)
            {
                m_boxes = penv.GetCollidedBoxes(sector.Id);
                if (m_boxes != null) m_boxes = new List<MyOrientedBoundingBoxD>(m_boxes); // duplicate the list so the debug draw works
            }

            var builder = (MyObjectBuilder_StaticEnvironmentModule)ob;

            if (builder != null)
            {
                var newDisabled = builder.DisabledItems;

                foreach (var item in newDisabled)
                {
                    if (!m_disabledItems.Contains(item))
                        OnItemEnable(item, false);
                }

                m_disabledItems.UnionWith(newDisabled);

                if (builder.Boxes != null && builder.MinScanned > 0)
                {
                    m_boxes = builder.Boxes;
                    m_minScannedLod = builder.MinScanned;
                }
            }

            // Postprocess positions so they are local and simplify tests
            if (m_boxes != null)
            {
                var sWorldPos = sector.WorldPos;

                int cnt = m_boxes.Count;
                fixed (MyOrientedBoundingBoxD* bb = m_boxes.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        bb[i].Center -= sWorldPos;
                    }
            }
        }

        public override unsafe void ProcessItems(Dictionary<short, MyLodEnvironmentItemSet> items, List<MySurfaceParams> surfaceParamsPerLod, int[] surfaceParamLodOffsets, int changedLodMin, int changedLodMax)
        {
            m_minScannedLod = changedLodMin;

            using (var batch = new MyEnvironmentModelUpdateBatch(Sector))
                foreach (var group in items)
                {
                    MyRuntimeEnvironmentItemInfo it;
                    Sector.GetItemDefinition((ushort)group.Key, out it);
                    MyDefinitionId modelCollection = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalModelCollectionDefinition), it.Subtype);

                    MyPhysicalModelCollectionDefinition modelCollectionDef = MyDefinitionManager.Static.GetDefinition<MyPhysicalModelCollectionDefinition>(modelCollection);
                    if (modelCollectionDef != null)
                    {
                        var info = group.Value;
                        int offt = info.LodOffsets[changedLodMin];

                        for (int i = offt; i < info.Items.Count; ++i)
                        {
                            var position = info.Items[i];

                            if (m_disabledItems.Contains(position) || IsObstructed(position)) continue;
                            var modelDef = modelCollectionDef.Items.Sample(MyHashRandomUtils.UniformFloatFromSeed(position));

                            batch.Add(modelDef, position);
                        }
                }
            }
        }

        private unsafe bool IsObstructed(int position)
        {
            if (m_boxes != null)
            {
                var pos = Sector.Items[position].Position;

                int cnt = m_boxes.Count;
                fixed (MyOrientedBoundingBoxD* obbs = m_boxes.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        if (obbs[i].Contains(ref pos)) return true;
                    }

            }
            return false;
        }

        public override void Close()
        {
        }

        public override MyObjectBuilder_EnvironmentModuleBase GetObjectBuilder()
        {
            if (m_disabledItems.Count > 0)
            {
                return new MyObjectBuilder_StaticEnvironmentModule
                {
                    DisabledItems = m_disabledItems,
                    Boxes = m_boxes,
                    MinScanned = m_minScannedLod
                };
            }
            return null;
        }

        public override unsafe void OnItemEnable(int itemId, bool enabled)
        {
            if (enabled)
                m_disabledItems.Remove(itemId);
            else
                m_disabledItems.Add(itemId);

            if (itemId >= Sector.Items.Count) return;

            fixed (ItemInfo* items = Sector.Items.GetInternalArray())
            {
                ItemInfo* item = items + itemId;
                if (item->ModelIndex >= 0 != enabled)
                {
                    var model = (short)~item->ModelIndex;
                    Sector.UpdateItemModel(itemId, model);
                }
            }
        }

        public override void HandleSyncEvent(int logicalItem, object data, bool fromClient)
        {
        }

        public override void DebugDraw()
        {
            if (m_boxes!= null)
            for (int i = 0; i < m_boxes.Count; i++)
            {
                var b = m_boxes[i];
                b.Center += Sector.WorldPos;

                MyRenderProxy.DebugDrawOBB(b, Color.Aquamarine, .3f, true, true);
            }
        }
    }
}
