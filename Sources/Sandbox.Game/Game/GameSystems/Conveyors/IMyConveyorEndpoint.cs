using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRage.Game;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems.Conveyors
{
    // You can picture this class in a following way:
    // .-------.
    // | LGPos |
    // |   o-->| Direction
    // |       |
    // '-------'
    public struct ConveyorLinePosition : IEquatable<ConveyorLinePosition>
    {
        public Vector3I LocalGridPosition;

        /// <summary>
        /// Direction in local grid coordinates.
        /// </summary>
        public Base6Directions.Direction Direction;

        public Vector3I VectorDirection
        {
            get
            {
                return Base6Directions.GetIntVector(Direction);
            }
        }

        public Vector3I NeighbourGridPosition
        {
            get
            {
                return LocalGridPosition + Base6Directions.GetIntVector(Direction);
            }
        }

        public ConveyorLinePosition(Vector3I gridPosition, Base6Directions.Direction direction)
        {
            LocalGridPosition = gridPosition;
            Direction = direction;
        }

        // Returns a position that connects to this one in the direction Direction:
        // .-------.-------.
        // |       |       |
        // |   o-->|<--o   |
        // |       |       |
        // '-------*-------'
        public ConveyorLinePosition GetConnectingPosition()
        {
            return new ConveyorLinePosition(LocalGridPosition + VectorDirection, Base6Directions.GetFlippedDirection(Direction));
        }

        // Returns a position with the same position and with a flipped direction
        // .-------.    .-------.
        // |       |    |       |
        // |   o-->| ~> |<--o   |
        // |       |    |       |
        // '-------*    .-------.
        public ConveyorLinePosition GetFlippedPosition()
        {
            return new ConveyorLinePosition(LocalGridPosition, Base6Directions.GetFlippedDirection(Direction));
        }

        public bool Equals(ConveyorLinePosition other)
        {
            return LocalGridPosition == other.LocalGridPosition && Direction == other.Direction;
        }

        public override int GetHashCode()
        {
            return (((((((int)Direction * 397) ^ LocalGridPosition.X) * 397) ^ LocalGridPosition.Y) * 397) ^ LocalGridPosition.Z);
        }

        public override string ToString()
        {
            return LocalGridPosition.ToString() + " -> " + Direction.ToString();
        }
    }

    public struct ConveyorLineEnumerator : IEnumerator<MyConveyorLine>
    {
        int index;
        private IMyConveyorEndpoint m_enumerated;
        private MyConveyorLine m_line;

        public ConveyorLineEnumerator(IMyConveyorEndpoint enumerated)
        {
            index = -1;
            m_enumerated = enumerated;
            m_line = null;
        }

        public MyConveyorLine Current
        {
            get { return m_line; }
        }

        public void Dispose()
        {
            m_enumerated = null;
            m_line = null;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return m_line; }
        }

        public bool MoveNext()
        {
            while (MoveNextInternal());

            if (index >= m_enumerated.GetLineCount()) return false;

            return true;
        }

        private bool MoveNextInternal()
        {
            index++;
            if (index >= m_enumerated.GetLineCount()) return false;

            m_line = m_enumerated.GetConveyorLine(index);
            if (!m_line.IsWorking) return true;

            return false;
        }

        public void Reset()
        {
            index = 0;
        }
    }

    public interface IMyConveyorEndpoint : IMyPathVertex<IMyConveyorEndpoint>
    {
        /// <summary>
        /// Returns a connecting line for the given line position, or null, if no such line exists
        /// </summary>
        MyConveyorLine GetConveyorLine(ConveyorLinePosition position);
        MyConveyorLine GetConveyorLine(int index);
        ConveyorLinePosition GetPosition(int index);
        void DebugDraw();

        /// <summary>
        /// Changes a conveyor line of this block
        /// </summary>
        void SetConveyorLine(ConveyorLinePosition position, MyConveyorLine newLine);
        
        // SK: unused 
        // void SetConveyorLine(int index, MyConveyorLine line);

        int GetLineCount();

        MyCubeBlock CubeBlock { get; }
    }

    static class MyConveyorEndpointExtensions
    {
        private enum EndpointDebugShape
        {
            SHAPE_SPHERE = 0,
            SHAPE_CAPSULE = 1,
        }

        public static void DebugDraw(this IMyConveyorEndpoint endpoint)
        {
            if (!MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS) return;

            Vector3 centerPos = new Vector3();
            for (int i = 0; i < endpoint.GetLineCount(); ++i)
            {
                var position = endpoint.GetPosition(i);
                Vector3 pos = new Vector3(position.LocalGridPosition) + 0.5f * new Vector3(position.VectorDirection);
                centerPos += pos;
            }
            centerPos = centerPos * endpoint.CubeBlock.CubeGrid.GridSize / (float)endpoint.GetLineCount();
            centerPos = Vector3.Transform(centerPos, endpoint.CubeBlock.CubeGrid.WorldMatrix);

            for (int i = 0; i < endpoint.GetLineCount(); ++i)
            {
                var position = endpoint.GetPosition(i);
                MyConveyorLine line = endpoint.GetConveyorLine(i);
                Vector3 pos = (new Vector3(position.LocalGridPosition) + 0.5f * new Vector3(position.VectorDirection)) * endpoint.CubeBlock.CubeGrid.GridSize;
                Vector3 pos2 = (new Vector3(position.LocalGridPosition) + 0.4f * new Vector3(position.VectorDirection)) * endpoint.CubeBlock.CubeGrid.GridSize;
                pos = Vector3.Transform(pos, endpoint.CubeBlock.CubeGrid.WorldMatrix);
                pos2 = Vector3.Transform(pos2, endpoint.CubeBlock.CubeGrid.WorldMatrix);
                Vector3 dir = Vector3.TransformNormal(position.VectorDirection * endpoint.CubeBlock.CubeGrid.GridSize * 0.5f, endpoint.CubeBlock.CubeGrid.WorldMatrix);

                Color color = line.IsFunctional ? Color.Orange : Color.DarkRed;
                color = line.IsWorking ? Color.GreenYellow : color;

                EndpointDebugShape shape = EndpointDebugShape.SHAPE_SPHERE;
                float dirMultiplier = 1.0f;
                float radius = 0.05f;

                if (line.GetEndpoint(0) == null || line.GetEndpoint(1) == null)
                {
                    if (line.Type == MyObjectBuilder_ConveyorLine.LineType.SMALL_LINE)
                    {
                        dirMultiplier = 0.2f;
                        radius = 0.015f;
                        shape = EndpointDebugShape.SHAPE_SPHERE;
                    }
                    else
                    {
                        dirMultiplier = 0.1f;
                        radius = 0.015f;
                        shape = EndpointDebugShape.SHAPE_CAPSULE;
                    }
                }
                else
                {
                    if (line.Type == MyObjectBuilder_ConveyorLine.LineType.SMALL_LINE)
                    {
                        dirMultiplier = 1.0f;
                        radius = 0.05f;
                        shape = EndpointDebugShape.SHAPE_SPHERE;
                    }
                    else
                    {
                        dirMultiplier = 0.2f;
                        radius = 0.05f;
                        shape = EndpointDebugShape.SHAPE_CAPSULE;
                    }
                }

                MyRenderProxy.DebugDrawLine3D(pos, pos + dir * dirMultiplier, color, color, true);
                if (shape == EndpointDebugShape.SHAPE_SPHERE)
                {
                    MyRenderProxy.DebugDrawSphere(pos, radius * endpoint.CubeBlock.CubeGrid.GridSize, color.ToVector3(), 1.0f, false);
                }
                else if (shape == EndpointDebugShape.SHAPE_CAPSULE)
                {
                    MyRenderProxy.DebugDrawCapsule(pos - dir * dirMultiplier, pos + dir * dirMultiplier, radius * endpoint.CubeBlock.CubeGrid.GridSize, color, false);
                }

                if (MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS_LINE_IDS)
                    MyRenderProxy.DebugDrawText3D(pos2, line.GetHashCode().ToString(), color, 0.6f, false);

                MyRenderProxy.DebugDrawLine3D(pos, centerPos, color, color, false);
            }
        }
    }
}
