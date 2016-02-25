using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems.Conveyors
{
    public interface IMyConveyorSegmentBlock
    {
        MyConveyorSegment ConveyorSegment { get; }
        void InitializeConveyorSegment();
    }

    public class MyConveyorSegment
    {
        public MyConveyorLine ConveyorLine { get; private set; }
        public ConveyorLinePosition ConnectingPosition1 { get; private set; }
        public ConveyorLinePosition ConnectingPosition2 { get; private set; }
        public MyCubeBlock CubeBlock { get; private set; }

        public bool IsCorner
        {
            get
            {
                Vector3I dir1 = ConnectingPosition1.VectorDirection;
                Vector3I dir2 = ConnectingPosition2.VectorDirection;
                return Vector3I.Dot(ref dir1, ref dir2) != -1;
            }
        }

        public void Init(MyCubeBlock myBlock, ConveyorLinePosition a, ConveyorLinePosition b, MyObjectBuilder_ConveyorLine.LineType type, MyObjectBuilder_ConveyorLine.LineConductivity conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FULL)
        {
            CubeBlock = myBlock;
            ConnectingPosition1 = a;
            ConnectingPosition2 = b;

            // Neighbour grid position of one of the connecting positions is inside this block
            var linePosition = (myBlock as IMyConveyorSegmentBlock).ConveyorSegment.ConnectingPosition1.NeighbourGridPosition;

            ConveyorLine = myBlock.CubeGrid.GridSystems.ConveyorSystem.GetDeserializingLine(linePosition);
            if (ConveyorLine == null)
            {
                ConveyorLine = new MyConveyorLine();
                if (IsCorner)
                    ConveyorLine.Init(a, b, myBlock.CubeGrid, type, conductivity, CalculateCornerPosition());
                else
                    ConveyorLine.Init(a, b, myBlock.CubeGrid, type, conductivity, (Vector3I?)null);
            }
            else
            {
                Debug.Assert(ConveyorLine.Type == type, "Conveyor line type mismatch on segment deserialization");
            }

            myBlock.SlimBlock.ComponentStack.IsFunctionalChanged += CubeBlock_IsFunctionalChanged;
        }

        public void SetConveyorLine(MyConveyorLine newLine)
        {
            ConveyorLine = newLine;
        }

        public bool CanConnectTo(ConveyorLinePosition connectingPosition, MyObjectBuilder_ConveyorLine.LineType type)
        {
            if (type == ConveyorLine.Type &&
                   (connectingPosition.Equals(ConnectingPosition1.GetConnectingPosition()) ||
                    connectingPosition.Equals(ConnectingPosition2.GetConnectingPosition()))
               ) return true;
            return false;
        }

        private void CubeBlock_IsFunctionalChanged()
        {
            ConveyorLine.UpdateIsFunctional();
        }

        public Base6Directions.Direction CalculateConnectingDirection(Vector3I connectingPosition)
        {
            Vector3 halfExtents = new Vector3(CubeBlock.Max - CubeBlock.Min + Vector3I.One) * 0.5f;
            Vector3 dirVec = new Vector3(CubeBlock.Max + CubeBlock.Min) * 0.5f;
            dirVec = dirVec - connectingPosition;
            dirVec = Vector3.Multiply(dirVec, halfExtents);
            dirVec = Vector3.DominantAxisProjection(dirVec);
            dirVec.Normalize();
            return Base6Directions.GetDirection(dirVec);
        }

        private Vector3I CalculateCornerPosition()
        {
            Vector3I posDist = ConnectingPosition2.LocalGridPosition - ConnectingPosition1.LocalGridPosition;

            Debug.Assert(posDist.AbsMin() == 0);

            switch(Base6Directions.GetAxis(ConnectingPosition1.Direction))
            {
                case Base6Directions.Axis.ForwardBackward:
                    return ConnectingPosition1.LocalGridPosition + new Vector3I(0, 0, posDist.Z);
                case Base6Directions.Axis.LeftRight:
                    return ConnectingPosition1.LocalGridPosition + new Vector3I(posDist.X, 0, 0);
                case Base6Directions.Axis.UpDown:
                    return ConnectingPosition1.LocalGridPosition + new Vector3I(0, posDist.Y, 0);
            }

            Debug.Fail("Should not get here");
            return Vector3I.Zero;
        }

        public void DebugDraw()
        {
            if (!MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS) return;

            Vector3 pos = (ConnectingPosition1.LocalGridPosition + ConnectingPosition1.VectorDirection * 0.5f) * CubeBlock.CubeGrid.GridSize;
            Vector3 pos2 = (ConnectingPosition2.LocalGridPosition + ConnectingPosition2.VectorDirection * 0.5f) * CubeBlock.CubeGrid.GridSize;
            pos = Vector3.Transform(pos, CubeBlock.CubeGrid.WorldMatrix);
            pos2 = Vector3.Transform(pos2, CubeBlock.CubeGrid.WorldMatrix);

            Color color = ConveyorLine.IsFunctional ? Color.Orange : Color.DarkRed;
            color = ConveyorLine.IsWorking ? Color.GreenYellow : color;

            MyRenderProxy.DebugDrawLine3D(pos, pos2, color, color, false);

            if (ConveyorLine == null)
                return;

            if (MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS_LINE_IDS)
                MyRenderProxy.DebugDrawText3D((pos + pos2) * 0.5f, ConveyorLine.GetHashCode().ToString(), color, 0.5f, false);
        }
    }
}
