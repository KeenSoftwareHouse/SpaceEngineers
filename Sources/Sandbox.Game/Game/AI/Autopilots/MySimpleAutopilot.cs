using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI
{
    [MyAutopilotType(typeof(MyObjectBuilder_SimpleAutopilot))]
    class MySimpleAutopilot : MyAutopilotBase
    {
        private Vector3D m_destination;
        private Vector3 m_direction;

        public MySimpleAutopilot() : this(Vector3.Zero, Vector3.One) { }

        public MySimpleAutopilot(Vector3D destination, Vector3 direction)
        {
            m_destination = destination;
            m_direction = direction;
        }

        public override MyObjectBuilder_AutopilotBase GetObjectBuilder()
        {
            MyObjectBuilder_SimpleAutopilot ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SimpleAutopilot>();

            ob.Destination = m_destination;
            ob.Direction = m_direction;

            return ob;
        }

        public override void Init(MyObjectBuilder_AutopilotBase objectBuilder)
        {
            MyObjectBuilder_SimpleAutopilot ob = (MyObjectBuilder_SimpleAutopilot)objectBuilder;

            m_destination = ob.Destination;
            m_direction = ob.Direction;
        }

        protected override void OnShipControllerChanged()
        { }

        public override void Update()
        {
            if (ShipController == null) return;

            if ((ShipController.PositionComp.GetPosition() - m_destination).Dot(m_direction) > 0.0f)
            {
                BoundingBox playerBox = new BoundingBox(Vector3.MaxValue, Vector3.MinValue);
                MyEntities.GetInflatedPlayerBoundingBox(ref playerBox, MyNeutralShipSpawner.NEUTRAL_SHIP_FORBIDDEN_RADIUS);

                if (playerBox.Contains(ShipController.PositionComp.GetPosition()) != ContainmentType.Contains)
                {
                    var shipGroup = MyCubeGridGroups.Static.Logical.GetGroup(ShipController.CubeGrid);
                    foreach (var node in shipGroup.Nodes)
                    {
                        node.NodeData.SyncObject.SendCloseRequest();
                    }
                }
            }
        }

        public override void DebugDraw()
        {
            if (!MyDebugDrawSettings.DEBUG_DRAW_NEUTRAL_SHIPS || ShipController == null) return;

            Vector3D cameraPos = MySector.MainCamera.Position;

            Vector3D origin = Vector3D.Normalize(ShipController.PositionComp.GetPosition() - cameraPos);
            Vector3D destination = Vector3D.Normalize(m_destination - cameraPos);
            Vector3D halfPoint = Vector3D.Normalize((origin + destination) * 0.5f) + cameraPos; // Prevent going through the camera
            origin += cameraPos;
            destination += cameraPos;
            Vector3D currentPoint = Vector3D.Normalize(ShipController.WorldMatrix.Translation - cameraPos) + cameraPos;

            MyRenderProxy.DebugDrawLine3D(origin, halfPoint, Color.Red, Color.Red, false);
            MyRenderProxy.DebugDrawLine3D(halfPoint, destination, Color.Red, Color.Red, false);
            MyRenderProxy.DebugDrawSphere(currentPoint, 0.01f, Color.Orange.ToVector3(), 1.0f, false);
            MyRenderProxy.DebugDrawSphere(currentPoint + m_direction * 0.015f, 0.005f, Color.Yellow.ToVector3(), 1.0f, false);
        }
    }
}
