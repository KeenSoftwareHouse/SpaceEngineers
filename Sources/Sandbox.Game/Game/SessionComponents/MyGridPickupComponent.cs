using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyGridPickupComponent : MySessionComponentBase
    {
        public static MyGridPickupComponent Static;

        private Dictionary<MyDefinitionId, MyDefinitionId> m_blockVariationToBaseBlock;
        private Dictionary<MyDefinitionId, MyFixedPoint> m_blockMaxStackSizes;

        public override Type[] Dependencies
        {
            get
            {
                return base.Dependencies;
            }
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID;
            }
        }

        public MyGridPickupComponent()
        {
            Static = this;

            m_blockVariationToBaseBlock = new Dictionary<MyDefinitionId, MyDefinitionId>(MyDefinitionId.Comparer);
            m_blockMaxStackSizes = new Dictionary<MyDefinitionId, MyFixedPoint>(MyDefinitionId.Comparer);
        }

        public override void LoadData()
        {
            base.LoadData();

            m_blockVariationToBaseBlock = new Dictionary<MyDefinitionId, MyDefinitionId>(MyDefinitionId.Comparer);
            var cubeBlocks = MyDefinitionManager.Static.GetDefinitionsOfType<MyCubeBlockDefinition>();
            foreach (var block in cubeBlocks)
            {
                if (block.BlockStages == null)
                    continue;

                foreach (var variant in block.BlockStages)
                {
                    m_blockVariationToBaseBlock[variant] = block.Id;
                }
            }

            m_blockMaxStackSizes = new Dictionary<MyDefinitionId, MyFixedPoint>(MyDefinitionId.Comparer);
            var cubeBlockStackSizes = MyDefinitionManager.Static.GetDefinitions<MyCubeBlockStackSizeDefinition>();
            foreach (var definition in cubeBlockStackSizes)
            {
                if (definition.BlockMaxStackSizes != null)
                    foreach (var block in definition.BlockMaxStackSizes)
                        m_blockMaxStackSizes[block.Key] = block.Value;
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            m_blockVariationToBaseBlock = null;
            m_blockMaxStackSizes = null;
        }

        public MyDefinitionId GetBaseBlock(MyDefinitionId id)
        {
            MyDefinitionId definitionId;
            if (m_blockVariationToBaseBlock.TryGetValue(id, out definitionId))
                return definitionId;

            return id;
        }

        public MyFixedPoint GetMaxStackSize(MyDefinitionId id)
        {
            MyFixedPoint size;
            if (m_blockMaxStackSizes.TryGetValue(id, out size))
                return size;

            return 1;
        }
    }
}
