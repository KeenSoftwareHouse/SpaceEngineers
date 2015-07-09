using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Common.Components;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentDrawPowerReciever : MyDebugRenderComponent
    {
        private MyPowerReceiver m_powerReciever = null;
        private IMyEntity m_entity =null;

        public MyDebugRenderComponentDrawPowerReciever(MyPowerReceiver powerReciever, IMyEntity entity)
            : base(null)
        {
            m_powerReciever = powerReciever;
            m_entity = entity;
        }

        public override bool DebugDraw()
        {
            m_powerReciever.DebugDraw(m_entity.PositionComp.WorldMatrix);
            return true;
        }
    }
}
