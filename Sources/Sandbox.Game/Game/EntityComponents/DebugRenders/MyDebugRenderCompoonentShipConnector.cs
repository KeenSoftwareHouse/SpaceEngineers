using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Components
{
    class MyDebugRenderCompoonentShipConnector: MyDebugRenderComponent
    {
        MyShipConnector m_shipConnector = null;
        public MyDebugRenderCompoonentShipConnector(MyShipConnector shipConnector)
            : base(shipConnector)
        {
            m_shipConnector = shipConnector;
        }
        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_CONNECTORS_AND_MERGE_BLOCKS)
            {
                MyRenderProxy.DebugDrawSphere(m_shipConnector.ConstraintPositionWorld(), 0.05f, Color.Red, 1, false);

                //MyRenderProxy.DebugDrawText3D(this.WorldMatrix.Translation, m_connectionPosition.ToString(), Color.Red, 1.0f, false);
                //MyRenderProxy.DebugDrawText3D(this.WorldMatrix.Translation, m_connectorMode == Mode.Connector ? "Connector" : "Ejector", Color.Red, 1.0f, false);
                MyRenderProxy.DebugDrawText3D(m_shipConnector.PositionComp.WorldMatrix.Translation, m_shipConnector.DetectedGridCount.ToString(), Color.Red, 1.0f, false);
            }
        }
    }
}
