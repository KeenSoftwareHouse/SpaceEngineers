using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_PistonTop))]
    class MyPistonTop : MyCubeBlock, IMyConveyorEndpointBlock
    {
        private MyPistonBase m_pistonBlock;
        private long m_pistonBlockId;

        internal void Attach(MyPistonBase pistonBase)
        {
            m_pistonBlock = pistonBase;
            m_pistonBlockId = pistonBase.EntityId;
        }

        internal void Detach()
        {
            m_pistonBlock = null;
        }
        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);

            var ob = builder as MyObjectBuilder_PistonTop;
            m_pistonBlockId = ob.PistonBlockId;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_PistonTop;
            ob.PistonBlockId = m_pistonBlockId;
            return ob;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!Sync.IsServer)
                return;
            MyPistonBase piston;
            if (m_pistonBlockId != 0 && m_pistonBlock == null)
                if (MyEntities.TryGetEntityById<MyPistonBase>(m_pistonBlockId, out piston))
                    piston.Attach(this, true);
                else
                    m_pistonBlockId = 0;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            if (m_pistonBlock != null)
            {
                m_pistonBlock.Detach();
            }
            base.OnUnregisteredFromGridSystems();
        }

        public override void OnRemovedByCubeBuilder()
        {
            if (m_pistonBlock != null)
            {
                var tmpStatorBlock = m_pistonBlock;
                m_pistonBlock.Detach(); // This will call our detach and set m_pistonBlock to null
                if (Sync.IsServer)
                    tmpStatorBlock.CubeGrid.RemoveBlock(tmpStatorBlock.SlimBlock, updatePhysics: true);
            }
            base.OnRemovedByCubeBuilder();
        }

        public override void ContactPointCallback(ref MyGridContactInfo value)
        {
            base.ContactPointCallback(ref value);
            if (m_pistonBlock == null)
                return;
            //if (value.CollidingEntity == m_pistonBlock.CubeGrid || value.CollidingEntity == m_pistonBlock.Subpart3)
            if (value.CollidingEntity == m_pistonBlock.Subpart3)
            {
                value.EnableDeformation = false;
                value.EnableParticles = false;
            }
        }

        private MyAttachableConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }
    }
}
