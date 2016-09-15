using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Game;
using VRage.Profiler;
using VRageMath;
using VRageRender.Import;

namespace Sandbox.Game.GameSystems.Conveyors
{
    public class MyMultilineConveyorEndpoint : IMyConveyorEndpoint
    {
        protected MyConveyorLine[] m_conveyorLines;

        protected static Dictionary<MyDefinitionId, ConveyorLinePosition[]> m_linePositions = new Dictionary<MyDefinitionId, ConveyorLinePosition[]>();

        private MyCubeBlock m_block;
        public MyCubeBlock CubeBlock
        {
            get { return m_block; }
        }

        MyPathfindingData m_pathfindingData;
        MyPathfindingData IMyPathVertex<IMyConveyorEndpoint>.PathfindingData
        {
            get
            {
                return m_pathfindingData;
            }
        }

        public MyMultilineConveyorEndpoint(MyCubeBlock myBlock)
        {
            ProfilerShort.Begin("MyMultilineConveyorEndpoint(...)");
            m_block = myBlock;

            MyConveyorLine.BlockLinePositionInformation[] positionInfo = MyConveyorLine.GetBlockLinePositions(myBlock);
            m_conveyorLines = new MyConveyorLine[positionInfo.Length];

            MyGridConveyorSystem conveyorSystem = myBlock.CubeGrid.GridSystems.ConveyorSystem;

            int i = 0;
            foreach (var position in positionInfo)
            {
                var gridPosition = PositionToGridCoords(position.Position);

                MyConveyorLine line = conveyorSystem.GetDeserializingLine(gridPosition);
                if (line == null)
                {
                    line = new MyConveyorLine();
                    line.Init(gridPosition, gridPosition.GetConnectingPosition(), myBlock.CubeGrid, position.LineType, position.LineConductivity);
                    line.InitEndpoints(this, null);
                }
                else
                {
                    if (line.GetEndpointPosition(0).Equals(gridPosition))
                    {
                        line.SetEndpoint(0, this);
                    }
                    else if (line.GetEndpointPosition(1).Equals(gridPosition))
                    {
                        line.SetEndpoint(1, this);
                    }
                }
                m_conveyorLines[i] = line;
                i++;
            }

            myBlock.SlimBlock.ComponentStack.IsFunctionalChanged += UpdateLineFunctionality;
            myBlock.CubeGrid.GridSystems.ConveyorSystem.ResourceSink.IsPoweredChanged += UpdateLineFunctionality;

            m_pathfindingData = new MyPathfindingData(this);
            ProfilerShort.End();
        }

        public ConveyorLinePosition PositionToGridCoords(ConveyorLinePosition position)
        {
            return PositionToGridCoords(position, CubeBlock);
        }

        public static ConveyorLinePosition PositionToGridCoords(ConveyorLinePosition position, MyCubeBlock cubeBlock)
        {
            ConveyorLinePosition retval = new ConveyorLinePosition();

            Matrix matrix = new Matrix();
            cubeBlock.Orientation.GetMatrix(out matrix);
            Vector3 transformedPosition = Vector3.Transform(new Vector3(position.LocalGridPosition), matrix);

            retval.LocalGridPosition = Vector3I.Round(transformedPosition) + cubeBlock.Position;
            retval.Direction = cubeBlock.Orientation.TransformDirection(position.Direction);

            return retval;
        }

        public MyConveyorLine GetConveyorLine(ConveyorLinePosition position)
        {
            ConveyorLinePosition[] positions = GetLinePositions();
            for (int i = 0; i < positions.Length; ++i)
            {
                var gridPosition = PositionToGridCoords(positions[i]);
                if (gridPosition.Equals(position))
                {
                    return m_conveyorLines[i];
                }
            }

            return null;
        }

        public ConveyorLinePosition GetPosition(int index)
        {
            ConveyorLinePosition[] positions = GetLinePositions();
            var gridPosition = PositionToGridCoords(positions[index]);
            return gridPosition;
        }

        public MyConveyorLine GetConveyorLine(int index)
        {
            if (index >= m_conveyorLines.Length)
                throw new IndexOutOfRangeException();

            return m_conveyorLines[index];
        }

        public void SetConveyorLine(ConveyorLinePosition position, MyConveyorLine newLine)
        {
            ConveyorLinePosition[] positions = GetLinePositions();
            for (int i = 0; i < positions.Length; ++i)
            {
                var gridPosition = PositionToGridCoords(positions[i]);
                if (gridPosition.Equals(position))
                {
                    m_conveyorLines[i] = newLine;
                    return;
                }
            }

            return;
        }

        // SK: unused
        //public void SetConveyorLine(int index, MyConveyorLine line)
        //{
        //    if (index < m_conveyorLines.Count())
        //        throw new IndexOutOfRangeException();

        //    m_conveyorLines[index] = line;
        //}

        public int GetLineCount()
        {
            return m_conveyorLines.Length;
        }

        protected ConveyorLinePosition[] GetLinePositions()
        {
            ConveyorLinePosition[] retval = null;
            lock (m_linePositions)
            {
                if (!m_linePositions.TryGetValue(CubeBlock.BlockDefinition.Id, out retval))
                {
                    retval = GetLinePositions(CubeBlock, "detector_conveyor");
                    m_linePositions.Add(CubeBlock.BlockDefinition.Id, retval);
                }
            }
            return retval;
        }

        public static ConveyorLinePosition[] GetLinePositions(MyCubeBlock cubeBlock, string dummyName)
        {
            return GetLinePositions(cubeBlock, VRage.Game.Models.MyModels.GetModelOnlyDummies(cubeBlock.BlockDefinition.Model).Dummies, dummyName);
        }

        public static ConveyorLinePosition[] GetLinePositions(MyCubeBlock cubeBlock, IDictionary<string, MyModelDummy> dummies, string dummyName)
        {
            var definition = cubeBlock.BlockDefinition;
            float cubeSize = MyDefinitionManager.Static.GetCubeSize(definition.CubeSize);
            Vector3 blockCenter = new Vector3(definition.Size) * 0.5f * cubeSize;

            int count = 0;
            foreach (var dummy in dummies)
            {
                if (dummy.Key.ToLower().Contains(dummyName))
                {
                    count++;
                }
            }

            ConveyorLinePosition[] retval = new ConveyorLinePosition[count];
            int i = 0;
            foreach (var dummy in dummies)
            {
                if (dummy.Key.ToLower().Contains(dummyName))
                {
                    var matrix = dummy.Value.Matrix;
                    ConveyorLinePosition linePosition = new ConveyorLinePosition();

                    Vector3 doorPosition = matrix.Translation + definition.ModelOffset + blockCenter;

                    Vector3I doorPositionInt = Vector3I.Floor(doorPosition / cubeSize);
                    doorPositionInt = Vector3I.Max(Vector3I.Zero, doorPositionInt);
                    doorPositionInt = Vector3I.Min(definition.Size - Vector3I.One, doorPositionInt);

                    Vector3 cubeCenter = (new Vector3(doorPositionInt) + Vector3.Half) * cubeSize;

                    var direction = Vector3.Normalize(Vector3.DominantAxisProjection(doorPosition - cubeCenter));

                    linePosition.LocalGridPosition = doorPositionInt - definition.Center;
                    linePosition.Direction = Base6Directions.GetDirection(direction);
                    retval[i] = linePosition;
                    i++;
                }
            }
            return retval;
        }

        protected void UpdateLineFunctionality()
        {
            for (int i = 0; i < m_conveyorLines.Length; ++i)
            {
                m_conveyorLines[i].UpdateIsFunctional();
            }
        }

        public ConveyorLineEnumerator GetEnumeratorInternal()
        {
            return new ConveyorLineEnumerator(this);
        }

        IEnumerator<IMyPathEdge<IMyConveyorEndpoint>> IEnumerable<IMyPathEdge<IMyConveyorEndpoint>>.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        float IMyPathVertex<IMyConveyorEndpoint>.EstimateDistanceTo(IMyPathVertex<IMyConveyorEndpoint> other)
        {
            return Vector3.RectangularDistance((other as IMyConveyorEndpoint).CubeBlock.WorldMatrix.Translation, this.CubeBlock.WorldMatrix.Translation);
        }

        int IMyPathVertex<IMyConveyorEndpoint>.GetNeighborCount()
        {
            return GetNeighborCount();
        }

        protected virtual int GetNeighborCount()
        {
            return m_conveyorLines.Length;
        }

        IMyPathVertex<IMyConveyorEndpoint> IMyPathVertex<IMyConveyorEndpoint>.GetNeighbor(int index)
        {
            return GetNeighbor(index);
        }

        protected virtual IMyPathVertex<IMyConveyorEndpoint> GetNeighbor(int index)
        {
            return m_conveyorLines[index].GetOtherVertex(this);
        }

        IMyPathEdge<IMyConveyorEndpoint> IMyPathVertex<IMyConveyorEndpoint>.GetEdge(int index)
        {
            return GetEdge(index);
        }

        protected virtual IMyPathEdge<IMyConveyorEndpoint> GetEdge(int index)
        {
            return m_conveyorLines[index];
        }

        public void DebugDraw()
        {
           
        }
    }

}
