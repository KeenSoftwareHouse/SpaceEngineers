using System;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.EntityComponents.DebugRenders
{
    public class MyDebugRenderComponentShipMergeBlock: MyDebugRenderComponent
    {
        MyShipMergeBlock m_shipMergeBlock = null;
        public MyDebugRenderComponentShipMergeBlock(MyShipMergeBlock shipConnector)
            : base(shipConnector)
        {
            m_shipMergeBlock = shipConnector;
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_CONNECTORS_AND_MERGE_BLOCKS)
            {
                Matrix WorldMatrix = m_shipMergeBlock.PositionComp.WorldMatrix;
                MyRenderProxy.DebugDrawLine3D(m_shipMergeBlock.Physics.RigidBody.Position, m_shipMergeBlock.Physics.RigidBody.Position + m_shipMergeBlock.WorldMatrix.Right, Color.Green, Color.Green, false);
                MyRenderProxy.DebugDrawSphere(Vector3.Transform(m_shipMergeBlock.Position * m_shipMergeBlock.CubeGrid.GridSize, Matrix.Invert(m_shipMergeBlock.WorldMatrix)), 1, Color.Green, 1, false);

                MyRenderProxy.DebugDrawSphere(m_shipMergeBlock.WorldMatrix.Translation, 0.2f, m_shipMergeBlock.InConstraint ? Color.Yellow : Color.Orange, 1, false);
                if (m_shipMergeBlock.InConstraint)
                {
                    MyRenderProxy.DebugDrawSphere(m_shipMergeBlock.Other.WorldMatrix.Translation, 0.2f, Color.Yellow, 1, false);
                    MyRenderProxy.DebugDrawLine3D(m_shipMergeBlock.WorldMatrix.Translation, m_shipMergeBlock.Other.WorldMatrix.Translation, Color.Yellow, Color.Yellow, false);
                }

                MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + m_shipMergeBlock.CubeGrid.WorldMatrix.GetDirectionVector(Base6Directions.GetDirection(m_shipMergeBlock.PositionComp.LocalMatrix.Right)), Color.Red, Color.Red, false);
                MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + m_shipMergeBlock.CubeGrid.WorldMatrix.GetDirectionVector(Base6Directions.GetDirection(m_shipMergeBlock.PositionComp.LocalMatrix.Up)), Color.Green, Color.Green, false);
                MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + m_shipMergeBlock.CubeGrid.WorldMatrix.GetDirectionVector(Base6Directions.GetDirection(m_shipMergeBlock.PositionComp.LocalMatrix.Backward)), Color.Blue, Color.Blue, false);
                MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + m_shipMergeBlock.CubeGrid.WorldMatrix.GetDirectionVector(m_shipMergeBlock.OtherRight), Color.Violet, Color.Violet, false);

                MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation, "Bodies: " + m_shipMergeBlock.GridCount, Color.White, 1.0f, false);

                if (m_shipMergeBlock.Other != null)
                {
                    float x = (float)Math.Exp(-((WorldMatrix.Translation - m_shipMergeBlock.Other.WorldMatrix.Translation).Length() - m_shipMergeBlock.CubeGrid.GridSize) * 6.0f);
                    MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation + m_shipMergeBlock.CubeGrid.WorldMatrix.GetDirectionVector(Base6Directions.GetDirection(m_shipMergeBlock.PositionComp.LocalMatrix.Up)) * 0.5f, x.ToString("0.00"), Color.Red, 1.0f, false);
                }
            }
        }
    }
}
