using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_PistonTop))]
    public class MyPistonTop : MyCubeBlock, IMyConveyorEndpointBlock, IMyPistonTop
    {
        private MyPistonBase m_pistonBlock;

        internal void Attach(MyPistonBase pistonBase)
        {
            m_pistonBlock = pistonBase;
        }

        internal void Detach(bool isWelding)
        {
            if (isWelding == false)
            {
                m_pistonBlock = null;
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            if (m_pistonBlock != null)
            {
                var pistonBlock = m_pistonBlock;
                pistonBlock.Detach();
                pistonBlock.SyncDetach();
            }
            base.OnUnregisteredFromGridSystems();
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

        #region ModAPI Implementation
        bool ModAPI.Ingame.IMyPistonTop.IsAttached
        {
            get { return m_pistonBlock != null; } 
        }
        
        ModAPI.IMyPistonBase ModAPI.IMyPistonTop.Piston
        {
            get { return m_pistonBlock; }
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }
        #endregion
    }
}
