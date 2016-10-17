using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Algorithms;
using VRage.Collections;
using VRage.Game;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems.Conveyors
{
    // You can picture this class in a following way (example):
    //
    // Line section 0 (length = 3, direction = ->)
    // .-------.-------.-------.-------.
    // |  EP1  |       |       |     o-+-----> Packet queue #2 from EP2 to EP1
    // |   o-->|-------+-------+---O   |
    // |       |       |       | o-+---+-----> Packet queue #1 from EP1 to EP2
    // '-------'-------'-------+---+---|
    //                         |   |   |
    //                         |   |   |
    //                         |   |   |  Line section 1 (length = 3, direction = |)
    //                         |---+---| -.                                       v
    //    Length = 6 blocks    |   |   |   \
    //   (i.e. distance that   |   |   |    }- This is one line segment
    //    one would travel     |   |   |   /
    //    when going from EP1  |-------| -'
    //    to EP2)              |   ^   |
    //                         |   o EP2
    //                         |       |
    //                         '-------'
    public class MyConveyorLine : IEnumerable<Vector3I>, IMyPathEdge<IMyConveyorEndpoint>
    {
        /// <summary>
        /// Enumerates inner line positions (i.e. not endpoint positions)
        /// </summary>
        public struct LinePositionEnumerator : IEnumerator<Vector3I>
        {
            private MyConveyorLine m_line;

            Vector3I m_currentPosition;
            Vector3I m_direction;
            private int m_index;
            private int m_sectionIndex;
            private int m_sectionLength;

            public LinePositionEnumerator(MyConveyorLine line)
            {
                m_line = line;

                m_currentPosition = line.m_endpointPosition1.LocalGridPosition;
                m_direction = line.m_endpointPosition1.VectorDirection;
                m_index = 0;
                m_sectionIndex = 0;
                m_sectionLength = m_line.m_length;

                UpdateSectionLength();
            }

            public Vector3I Current
            {
                get
                {
                    return m_currentPosition;
                }
            }

            public void Dispose() {}
            
            public bool MoveNext()
            {
                m_index++;
                m_currentPosition += m_direction;

                if (m_index >= m_sectionLength)
                {
                    m_index = 0;
                    m_sectionIndex++;
                    if (m_line.m_sections == null || m_sectionIndex >= m_line.m_sections.Count) return false;

                    m_direction = Base6Directions.GetIntVector(m_line.m_sections[m_sectionIndex].Direction);
                    UpdateSectionLength();
                }

                return true;
            }

            public void Reset()
            {
                m_currentPosition = m_line.m_endpointPosition1.LocalGridPosition;
                m_direction = m_line.m_endpointPosition1.VectorDirection;
                m_index = 0;
                m_sectionIndex = 0;
                m_sectionLength = m_line.m_length;

                UpdateSectionLength();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            private void UpdateSectionLength()
            {
                if (m_line.m_sections == null || m_line.m_sections.Count == 0) return;
                m_sectionLength = m_line.m_sections[m_sectionIndex].Length;
            }
        }

        private struct SectionInformation
        {
            public Base6Directions.Direction Direction;
            public int Length;

            public void Reverse()
            {
                Direction = Base6Directions.GetFlippedDirection(Direction);
            }

            public override string ToString()
            {
                return Length.ToString() + " -> " + Direction.ToString();
            }
        }

        public struct BlockLinePositionInformation
        {
            public ConveyorLinePosition Position;
            public MyObjectBuilder_ConveyorLine.LineType LineType;
            public MyObjectBuilder_ConveyorLine.LineConductivity LineConductivity;
        }

        private static ConcurrentDictionary<MyDefinitionId, BlockLinePositionInformation[]> m_blockLinePositions = new ConcurrentDictionary<MyDefinitionId, BlockLinePositionInformation[]>();

        private static readonly float CONVEYOR_PER_LINE_PENALTY = 1.0f;

        private const int FRAMES_PER_BIG_UPDATE = 64;

        // Conveyor packet positions are calculated from endpoint position 1 to endpoint position 2
        private ConveyorLinePosition m_endpointPosition1;
        private ConveyorLinePosition m_endpointPosition2;
        private IMyConveyorEndpoint m_endpoint1;
        private IMyConveyorEndpoint m_endpoint2;

        private MyObjectBuilder_ConveyorLine.LineType m_type;
        private MyObjectBuilder_ConveyorLine.LineConductivity m_conductivity;

        private int m_length;
        private MyCubeGrid m_cubeGrid;

        [ThreadStatic]
        private static bool m_invertedConductivity = false;

        public class InvertedConductivity : IDisposable
        {
            public InvertedConductivity()
            {
                m_invertedConductivity = !m_invertedConductivity;
            }

            public void Dispose()
            {
                m_invertedConductivity = !m_invertedConductivity;
            }
        }

        // A conveyor packet queue from endpoint 1 to endpoint 2
        private MySinglyLinkedList<MyConveyorPacket> m_queue1;
        // A conveyor packet queue from endpoint 2 to endpoint 1
        private MySinglyLinkedList<MyConveyorPacket> m_queue2;

        private List<SectionInformation> m_sections;

        private static List<SectionInformation> m_tmpSections1 = new List<SectionInformation>();
        private static List<SectionInformation> m_tmpSections2 = new List<SectionInformation>();

        private bool m_stopped1;
        private bool m_stopped2;

        private float m_queuePosition;

        private bool m_isFunctional;
        private bool m_isWorking;

        public bool IsFunctional
        {
            get
            {
                return m_isFunctional;
            }
        }

        public bool IsWorking
        {
            get
            {
                return m_isWorking;
            }
        }

        private LinePositionEnumerator m_enumerator = new LinePositionEnumerator();

        public int Length
        {
            get
            {
                return m_length;
            }
        }

        public bool IsDegenerate
        {
            get
            {
                Debug.Assert(Length != 0);
                return Length == 1 && HasNullEndpoints;
            }
        }

        public bool IsCircular
        {
            get
            {
                return Length != 1 && m_endpointPosition1.GetConnectingPosition().Equals(m_endpointPosition2);
            }
        }

        public bool HasNullEndpoints
        {
            get
            {
                return m_endpoint1 == null && m_endpoint2 == null;
            }
        }

        public bool IsDisconnected
        {
            get
            {
                return m_endpoint1 == null || m_endpoint2 == null;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_queue1.Count == 0 && m_queue2.Count == 0;
            }
        }

        public MyObjectBuilder_ConveyorLine.LineType Type
        {
            get
            {
                return m_type;
            }
        }

        public MyObjectBuilder_ConveyorLine.LineConductivity Conductivity
        {
            get
            {
                return m_conductivity;
            }
        }

        public MyConveyorLine()
        {
            // CH:TODO: Allocate packet queues on first demand
            m_queue1 = new MySinglyLinkedList<MyConveyorPacket>();
            m_queue2 = new MySinglyLinkedList<MyConveyorPacket>();

            m_length = 0;

            m_queuePosition = 0.0f;
            m_stopped1 = false;
            m_stopped2 = false;
            m_sections = null;

            m_isFunctional = false;
            m_isWorking = false;
        }

        public MyObjectBuilder_ConveyorLine GetObjectBuilder()
        {
            MyObjectBuilder_ConveyorLine ob = new MyObjectBuilder_ConveyorLine();
            foreach (var packet in m_queue1)
            {
                MyObjectBuilder_ConveyorPacket packetOb = new MyObjectBuilder_ConveyorPacket();
                packetOb.Item = packet.Item.GetObjectBuilder();
                packetOb.LinePosition = packet.LinePosition;

                ob.PacketsForward.Add(packetOb);
            }

            foreach (var packet in m_queue2)
            {
                MyObjectBuilder_ConveyorPacket packetOb = new MyObjectBuilder_ConveyorPacket();
                packetOb.Item = packet.Item.GetObjectBuilder();
                packetOb.LinePosition = packet.LinePosition;

                ob.PacketsBackward.Add(packetOb);
            }

            ob.StartPosition = m_endpointPosition1.LocalGridPosition;
            ob.StartDirection = m_endpointPosition1.Direction;

            ob.EndPosition = m_endpointPosition2.LocalGridPosition;
            ob.EndDirection = m_endpointPosition2.Direction;

            if (m_sections != null)
            {
                ob.Sections = new List<SerializableLineSectionInformation>(m_sections.Count);
                foreach (var section in m_sections)
                {
                    SerializableLineSectionInformation sInfo = new SerializableLineSectionInformation();
                    sInfo.Direction = section.Direction;
                    sInfo.Length = section.Length;

                    ob.Sections.Add(sInfo);
                }
            }

            ob.ConveyorLineType = m_type;
            ob.ConveyorLineConductivity = m_conductivity;

            return ob;
        }

        /// <summary>
        /// (Re)initializes the section list and ensures there will be enough space in it
        /// </summary>
        /// <param name="size">Required capacity of the section list. -1 means no resizing or initial capacity</param>
        private void InitializeSectionList(int size = -1)
        {
            if (m_sections != null)
            {
                m_sections.Clear();
                if (size != -1)
                    m_sections.Capacity = size;
            }
            else
            {
                if (size != -1)
                    m_sections = new List<SectionInformation>(size);
                else
                    m_sections = new List<SectionInformation>();
            }
        }

        public void Init(MyObjectBuilder_ConveyorLine objectBuilder, MyCubeGrid cubeGrid)
        {
            m_cubeGrid = cubeGrid;

            foreach (var packetOb in objectBuilder.PacketsForward)
            {
                MyConveyorPacket packet = new MyConveyorPacket();
                packet.Init(packetOb, m_cubeGrid);
                Debug.Assert(m_queue1.Count == 0 || packet.LinePosition < m_queue1.Last().LinePosition, "Conveyor packet line positions must be in decreasing order in the object builder");
                m_queue1.Append(packet);
            }

            foreach (var packetOb in objectBuilder.PacketsBackward)
            {
                MyConveyorPacket packet = new MyConveyorPacket();
                packet.Init(packetOb, m_cubeGrid);
                Debug.Assert(m_queue2.Count == 0 || packet.LinePosition < m_queue2.Last().LinePosition, "Conveyor packet line positions must be in decreasing order in the object builder");
                m_queue2.Append(packet);
            }

            m_endpointPosition1 = new ConveyorLinePosition(objectBuilder.StartPosition, objectBuilder.StartDirection);
            m_endpointPosition2 = new ConveyorLinePosition(objectBuilder.EndPosition, objectBuilder.EndDirection);

            m_length = 0;            
            if (objectBuilder.Sections != null && objectBuilder.Sections.Count != 0)
            {
                Debug.Assert(objectBuilder.Sections.Count >= 2, "If conveyor line sections are not null, there must be at least two of them");

                InitializeSectionList(objectBuilder.Sections.Count);
                foreach (var section in objectBuilder.Sections)
                {
                    SectionInformation sInfo = new SectionInformation();
                    sInfo.Direction = section.Direction;
                    sInfo.Length = section.Length;

                    m_sections.Add(sInfo);
                    m_length += sInfo.Length;
                }
            }

            if (m_length == 0)
                m_length = m_endpointPosition2.LocalGridPosition.RectangularDistance(m_endpointPosition1.LocalGridPosition);

            Debug.Assert(m_length > 0, "Degenerate line loaded from object builder");

            m_type = objectBuilder.ConveyorLineType;
            // Backwards compatibility
            if (m_type == MyObjectBuilder_ConveyorLine.LineType.DEFAULT_LINE)
            {
                if (cubeGrid.GridSizeEnum == MyCubeSize.Small)
                {
                    m_type = MyObjectBuilder_ConveyorLine.LineType.SMALL_LINE;
                }
                else if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    m_type = MyObjectBuilder_ConveyorLine.LineType.LARGE_LINE;
                }
            }

            m_conductivity = objectBuilder.ConveyorLineConductivity;

            StopQueuesIfNeeded();
            RecalculatePacketPositions();
        }

        public void Init(ConveyorLinePosition endpoint1, ConveyorLinePosition endpoint2, MyCubeGrid cubeGrid, MyObjectBuilder_ConveyorLine.LineType type, MyObjectBuilder_ConveyorLine.LineConductivity conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FULL, Vector3I? corner = null)
        {
            m_cubeGrid = cubeGrid;
            m_type = type;
            m_conductivity = conductivity;

            m_endpointPosition1 = endpoint1;
            m_endpointPosition2 = endpoint2;
            m_isFunctional = false;

            if (corner.HasValue)
            {
                InitializeSectionList(2);

                Vector3I intDir1 = corner.Value - endpoint1.LocalGridPosition;
                int length1 = intDir1.RectangularLength();
                intDir1 /= length1;
                Vector3I intDir2 = endpoint2.LocalGridPosition - corner.Value;
                int length2 = intDir2.RectangularLength();
                intDir2 /= length2;

                Base6Directions.Direction dir1 = Base6Directions.GetDirection(intDir1);
                Base6Directions.Direction dir2 = Base6Directions.GetDirection(intDir2);

                Debug.Assert(dir1 == m_endpointPosition1.Direction);
                Debug.Assert(Base6Directions.GetFlippedDirection(dir2) == m_endpointPosition2.Direction);

                SectionInformation sInfo = new SectionInformation() { Direction = dir1, Length = length1 };
                SectionInformation sInfo2 = new SectionInformation() { Direction = dir2, Length = length2 };

                m_sections.Add(sInfo);
                m_sections.Add(sInfo2);
            }

            m_length = endpoint1.LocalGridPosition.RectangularDistance(endpoint2.LocalGridPosition);
        }

        private void InitAfterSplit(ConveyorLinePosition endpoint1, ConveyorLinePosition endpoint2, List<SectionInformation> sections, int newLength, MyCubeGrid cubeGrid, MyObjectBuilder_ConveyorLine.LineType lineType)
        {
            m_endpointPosition1 = endpoint1;
            m_endpointPosition2 = endpoint2;
            m_sections = sections;
            m_length = newLength;
            m_cubeGrid = cubeGrid;
            m_type = lineType;
        }

        public void InitEndpoints(IMyConveyorEndpoint endpoint1, IMyConveyorEndpoint endpoint2)
        {
            Debug.Assert(endpoint1 == null || endpoint2 == null);

            m_endpoint1 = endpoint1;
            m_endpoint2 = endpoint2;
            UpdateIsFunctional();
        }

        private void RecalculatePacketPositions()
        {
            Debug.Assert(m_queue1 != null && m_queue2 != null, "One of the packet queues was null");

            int sectionStartPosition = 0;
            Vector3I sectionStart = m_endpointPosition1.LocalGridPosition;
            Base6Directions.Direction startDirection = m_endpointPosition1.Direction;

            int sectionIndex = 0;
            int sectionLength = Length;

            if (m_sections != null)
            {
                sectionIndex = m_sections.Count - 1;
                sectionStartPosition = Length - m_sections[sectionIndex].Length;
                sectionStart = m_endpointPosition2.LocalGridPosition - Base6Directions.GetIntVector(startDirection) * m_sections[sectionIndex].Length;
                startDirection = m_sections[sectionIndex].Direction;
                sectionLength = m_sections[sectionIndex].Length;
            }

            Base6Directions.Direction startOffset = Base6Directions.GetPerpendicular(startDirection);

            MySinglyLinkedList<MyConveyorPacket>.Enumerator e1 = m_queue1.GetEnumerator();
            bool e1Valid = e1.MoveNext();
            while (sectionStartPosition >= 0)
            {
                while (e1Valid && e1.Current.LinePosition >= sectionStartPosition)
                {
                    e1.Current.SetLocalPosition(sectionStart, sectionStartPosition, m_cubeGrid.GridSize, startDirection, startOffset);
                    e1.Current.SetSegmentLength(m_cubeGrid.GridSize);
                    e1Valid = e1.MoveNext();
                }

                if (m_sections == null || !e1Valid) break;

                sectionIndex--;
                if (sectionIndex < 0) break;

                startDirection = m_sections[sectionIndex].Direction;
                sectionLength = m_sections[sectionIndex].Length;
                sectionStartPosition -= sectionLength;
                sectionStart -= Base6Directions.GetIntVector(startDirection) * sectionLength;
            }

            sectionStartPosition = 0;
            sectionStart = m_endpointPosition2.LocalGridPosition;
            startDirection = m_endpointPosition2.Direction;
            startOffset = Base6Directions.GetFlippedDirection(startOffset);

            sectionIndex = 0;
            sectionLength = Length;

            if (m_sections != null)
            {
                sectionLength = m_sections[sectionIndex].Length;
                sectionStartPosition = Length - sectionLength;
                startDirection = Base6Directions.GetFlippedDirection(m_sections[sectionIndex].Direction);
                sectionStart = m_endpointPosition1.LocalGridPosition - Base6Directions.GetIntVector(startDirection) * sectionLength;
            }

            MySinglyLinkedList<MyConveyorPacket>.Enumerator e2 = m_queue2.GetEnumerator();
            bool e2Valid = e2.MoveNext();
            while (sectionStartPosition >= 0)
            {
                while (e2Valid && e2.Current.LinePosition >= sectionStartPosition)
                {
                    e2.Current.SetLocalPosition(sectionStart, sectionStartPosition, m_cubeGrid.GridSize, startDirection, startOffset);
                    e2.Current.SetSegmentLength(m_cubeGrid.GridSize);
                    e2Valid = e2.MoveNext();
                }

                if (m_sections == null || !e2Valid) break;

                sectionIndex++;
                if (sectionIndex >= m_sections.Count) break;

                sectionLength = m_sections[sectionIndex].Length;
                sectionStartPosition -= sectionLength;
                startDirection = Base6Directions.GetFlippedDirection(m_sections[sectionIndex].Direction);
                sectionStart -= Base6Directions.GetIntVector(startDirection) * sectionLength;
            }
        }

        public IMyConveyorEndpoint GetEndpoint(int index)
        {
            if (index == 0)
                return m_endpoint1;
            if (index == 1)
                return m_endpoint2;

            throw new IndexOutOfRangeException();
        }

        public void SetEndpoint(int index, IMyConveyorEndpoint endpoint)
        {
            if (index == 0)
            {
                m_endpoint1 = endpoint;
                return;
            }
            if (index == 1)
            {
                m_endpoint2 = endpoint;
                return;
            }

            throw new IndexOutOfRangeException();
        }

        public ConveyorLinePosition GetEndpointPosition(int index)
        {
            if (index == 0)
                return m_endpointPosition1;
            if (index == 1)
                return m_endpointPosition2;

            throw new IndexOutOfRangeException();
        }

        public static BlockLinePositionInformation[] GetBlockLinePositions(MyCubeBlock block)
        {
            BlockLinePositionInformation[] retval;

            if (m_blockLinePositions.TryGetValue(block.BlockDefinition.Id, out retval))
                return retval;

            var definition = block.BlockDefinition;
            float cubeSize = MyDefinitionManager.Static.GetCubeSize(definition.CubeSize);
            Vector3 blockCenter = new Vector3(definition.Size) * 0.5f * cubeSize;

            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(block.BlockDefinition.Model);

            int count = 0;
            foreach (var dummy in finalModel.Dummies)
            {
                String[] parts = dummy.Key.ToLower().Split('_');
                if (parts.Length < 2) continue;
                if (parts[0] == "detector" && parts[1].StartsWith("conveyor"))
                {
                    count++;
                }
            }

            retval = new BlockLinePositionInformation[count];
            int i = 0;
            foreach (var dummy in finalModel.Dummies)
            {
                String[] parts = dummy.Key.ToLower().Split('_');
                if (parts.Length < 2 || parts[0] != "detector" || !parts[1].StartsWith("conveyor")) continue;

                bool smallLine = parts.Length > 2 && parts[2] == "small";
                if (smallLine)
                {
                    retval[i].LineType = MyObjectBuilder_ConveyorLine.LineType.SMALL_LINE;
                }
                else
                {
                    retval[i].LineType = MyObjectBuilder_ConveyorLine.LineType.LARGE_LINE;
                }

                retval[i].LineConductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FULL;

                bool input = (parts.Length > 2 && parts[2] == "in") || (parts.Length > 3 && parts[3] == "in");
                if (input)
                {
                    retval[i].LineConductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD;
                }

                bool output = (parts.Length > 2 && parts[2] == "out") || (parts.Length > 3 && parts[3] == "out");
                if (output)
                {
                    retval[i].LineConductivity = MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD;
                }

                var matrix = dummy.Value.Matrix;
                ConveyorLinePosition linePosition = new ConveyorLinePosition();

                Vector3 doorPosition = matrix.Translation + definition.ModelOffset + blockCenter;

                Vector3I doorPositionInt = Vector3I.Floor(doorPosition / cubeSize);
                doorPositionInt = Vector3I.Max(Vector3I.Zero, doorPositionInt);
                doorPositionInt = Vector3I.Min(definition.Size - Vector3I.One, doorPositionInt);

                Vector3 cubeCenter = (new Vector3(doorPositionInt) + Vector3.Half) * cubeSize;

                var direction = Vector3.Normalize(Vector3.DominantAxisProjection(Vector3.Divide(doorPosition - cubeCenter, cubeSize)));

                linePosition.LocalGridPosition = doorPositionInt - definition.Center;
                linePosition.Direction = Base6Directions.GetDirection(direction);
                retval[i].Position = linePosition;
                i++;
            }

            m_blockLinePositions.TryAdd(definition.Id, retval);
            return retval;
        }

        public void RecalculateConductivity()
        {
            m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FULL;

            MyObjectBuilder_ConveyorLine.LineConductivity conductivity1 = MyObjectBuilder_ConveyorLine.LineConductivity.FULL;
            MyObjectBuilder_ConveyorLine.LineConductivity conductivity2 = MyObjectBuilder_ConveyorLine.LineConductivity.FULL;

            if (m_endpoint1 != null && m_endpoint1 is MyMultilineConveyorEndpoint)
            {
                var multilineEndpoint1 = m_endpoint1 as MyMultilineConveyorEndpoint;
                foreach (var position in MyConveyorLine.GetBlockLinePositions(multilineEndpoint1.CubeBlock))
                {
                    if (multilineEndpoint1.PositionToGridCoords(position.Position).Equals(m_endpointPosition1))
                    {
                        conductivity1 = position.LineConductivity;
                        break;
                    }
                }
            }

            if (m_endpoint2 != null && m_endpoint2 is MyMultilineConveyorEndpoint)
            {
                var multilineEndpoint = m_endpoint2 as MyMultilineConveyorEndpoint;
                foreach (var position in MyConveyorLine.GetBlockLinePositions(multilineEndpoint.CubeBlock))
                {
                    if (multilineEndpoint.PositionToGridCoords(position.Position).Equals(m_endpointPosition2))
                    {
                        conductivity2 = position.LineConductivity;

                        if (conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD)
                        {
                            conductivity2 = MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD;
                        }
                        else if (conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD)
                        {
                            conductivity2 = MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD;
                        }
                        break;
                    }
                }
            }

            if (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.NONE || conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.NONE
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD)
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD))
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.NONE;
            }
            else if ((conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.FULL && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD)
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FULL)
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD))
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD;
            }
            else if (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.FULL && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.FULL)
                || (conductivity1 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD && conductivity2 == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD))
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD;
            }
            else
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FULL;
            }
        }

        /// <summary>
        /// Merges the other line into this line
        /// </summary>
        public void Merge(MyConveyorLine mergingLine, IMyConveyorSegmentBlock newlyAddedBlock = null)
        {
            ConveyorLinePosition thisConnectingPosition = m_endpointPosition2.GetConnectingPosition();
            if (mergingLine.m_endpointPosition1.Equals(thisConnectingPosition))
            {
                MergeInternal(mergingLine, newlyAddedBlock);
            }
            else if (mergingLine.m_endpointPosition2.Equals(thisConnectingPosition))
            {
                mergingLine.Reverse();
                MergeInternal(mergingLine, newlyAddedBlock);
            }
            else
            {
                this.Reverse();
                thisConnectingPosition = m_endpointPosition2.GetConnectingPosition();
                if (mergingLine.m_endpointPosition1.Equals(thisConnectingPosition))
                {
                    MergeInternal(mergingLine, newlyAddedBlock);
                }
                else if (mergingLine.m_endpointPosition2.Equals(thisConnectingPosition))
                {
                    mergingLine.Reverse();
                    MergeInternal(mergingLine, newlyAddedBlock);
                }
                else
                {
                    Debug.Fail("Should not get here");
                }
            }
            mergingLine.RecalculateConductivity();
        }

        public void MergeInternal(MyConveyorLine mergingLine, IMyConveyorSegmentBlock newlyAddedBlock = null)
        {
            Debug.Assert(m_sections == null || m_sections.Count > 1, "Invalid sections");
            Debug.Assert(mergingLine.m_sections == null || mergingLine.m_sections.Count > 1, "Invalid sections");

            this.m_endpointPosition2 = mergingLine.m_endpointPosition2;
            this.m_endpoint2 = mergingLine.m_endpoint2;

            if (mergingLine.m_sections != null)
            {
                if (m_sections == null)
                {
                    Debug.Assert(mergingLine.m_sections[0].Direction == this.m_endpointPosition1.Direction);

                    InitializeSectionList(mergingLine.m_sections.Count);
                    m_sections.AddList(mergingLine.m_sections);

                    SectionInformation sInfo = m_sections[0];
                    sInfo.Length += m_length - 1;
                    m_sections[0] = sInfo;
                }
                else
                {
                    m_sections.Capacity = m_sections.Count + mergingLine.m_sections.Count - 1;
                    Debug.Assert(m_sections[m_sections.Count - 1].Direction == mergingLine.m_sections[0].Direction);

                    SectionInformation sInfo = m_sections[m_sections.Count - 1];
                    sInfo.Length += mergingLine.m_sections[0].Length - 1;
                    m_sections[m_sections.Count - 1] = sInfo;

                    for (int i = 1; i < mergingLine.m_sections.Count; ++i)
                        m_sections.Add(mergingLine.m_sections[i]);
                }
            }
            else
            {
                if (m_sections != null)
                {
                    SectionInformation sInfo = m_sections[m_sections.Count - 1];
                    sInfo.Length += mergingLine.m_length - 1;
                    m_sections[m_sections.Count - 1] = sInfo;
                }
            }

            Debug.Assert(CheckSectionConsistency(), "Conveyor line sections are inconsistent!");

            m_length = m_length + mergingLine.m_length - 1;

            // TODO: Merge Conveyor packet queues

            UpdateIsFunctional();
            
            // Newly added blocks cannot be enumerated by the line, so they have to be checked separately
            if (newlyAddedBlock != null)
            {
                m_isFunctional = m_isFunctional & newlyAddedBlock.ConveyorSegment.CubeBlock.IsFunctional;
                m_isWorking = m_isWorking & m_isFunctional;
            }
        }

        public bool CheckSectionConsistency()
        {
            if (m_sections == null) return true;

            Base6Directions.Direction? prevDirection = null;

            foreach (var section in m_sections)
            {
                if (prevDirection != null && prevDirection.Value == section.Direction)
                    return false;

                prevDirection = section.Direction;
            }

            return true;
        }

        /// <summary>
        /// Helper method that reverses the direction of the line.
        /// This method serves as a helper for the merging and splitting methods and should be relatively quick,
        /// because each added or removed conveyor block will trigger line merging or splitting.
        /// Once this won't be the case, consider refactoring the places from where this method is called.
        /// </summary>
        public void Reverse()
        {
            ConveyorLinePosition tmp = m_endpointPosition1;
            m_endpointPosition1 = m_endpointPosition2;
            m_endpointPosition2 = tmp;

            IMyConveyorEndpoint tmpEndpoint = m_endpoint1;
            m_endpoint1 = m_endpoint2;
            m_endpoint2 = tmpEndpoint;

            if (m_conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD)
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD;
            }
            else if (m_conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD)
            {
                m_conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD;
            }

            if (m_sections != null)
            {
                for (int i = 0; i < (m_sections.Count + 1) / 2; ++i)
                {
                    int i2 = m_sections.Count - i - 1;

                    SectionInformation tmpSection = m_sections[i];
                    tmpSection.Direction = Base6Directions.GetFlippedDirection(tmpSection.Direction);

                    SectionInformation tmpSection2 = m_sections[i2];
                    tmpSection2.Direction = Base6Directions.GetFlippedDirection(tmpSection2.Direction);

                    m_sections[i] = tmpSection2;
                    m_sections[i2] = tmpSection;
                }
            }

            // TODO: swap packet queues
        }

        public void DisconnectEndpoint(IMyConveyorEndpoint endpoint)
        {
            if (endpoint == m_endpoint1)
                m_endpoint1 = null;
            if (endpoint == m_endpoint2)
                m_endpoint2 = null;
            UpdateIsFunctional();
            return;
        }

        public IEnumerator<Vector3I> GetEnumerator()
        {
            return new LinePositionEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new LinePositionEnumerator(this);
        }

        public float GetWeight()
        {
            return Length + CONVEYOR_PER_LINE_PENALTY;
        }

        public IMyConveyorEndpoint GetOtherVertex(IMyConveyorEndpoint endpoint)
        {
            if (!m_isWorking)
                return null;

            var conductivity = m_conductivity;
            if (m_invertedConductivity)
            {
                if (m_conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD)
                {
                    conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD;
                }
                else if (m_conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD)
                {
                    conductivity = MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD;
                }
            }

            if (endpoint == m_endpoint1)
            {
                if (conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.FULL || conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.BACKWARD)
                    return m_endpoint2;
                else
                    return null;
            }
            if (endpoint == m_endpoint2)
            {
                if (conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.FULL || conductivity == MyObjectBuilder_ConveyorLine.LineConductivity.FORWARD)
                    return m_endpoint1;
                else
                    return null;
            }

            Debug.Fail("Conveyor line does not contain the given endpoint");
            return null;
        }

        public override string ToString()
        {
            return m_endpointPosition1.LocalGridPosition.ToString() + " <-> " + m_endpointPosition2.LocalGridPosition.ToString();
        }

        /// <summary>
        /// Returns a conveyor line that is newly created by removing a segment in position "position"
        /// </summary>
        public MyConveyorLine RemovePortion(Vector3I startPosition, Vector3I endPosition)
        {
            if (IsCircular)
                RotateCircularLine(startPosition);

            // Find out the correct order of splitting positions
            if (startPosition != endPosition)
            {
                bool swapPositions = false;

                if (m_sections != null)
                {
                    Vector3I sectionStart = m_endpointPosition1.LocalGridPosition;
                    int l1, l2;

                    foreach (var section in m_sections)
                    {
                        bool startInSection = PositionIsInSection(startPosition, sectionStart, section, out l1);
                        bool endInSection = PositionIsInSection(endPosition, sectionStart, section, out l2);
                        if (startInSection && endInSection)
                        {
                            if (l2 < l1)
                            {
                                swapPositions = true;
                            }
                            break;
                        }
                        else if (endInSection)
                        {
                            swapPositions = true;
                            break;
                        }
                        else if (startInSection)
                        {
                            break;
                        }

                        sectionStart += Base6Directions.GetIntVector(section.Direction) * section.Length;
                    }
                }
                else
                {
                    if (Vector3I.DistanceManhattan(m_endpointPosition1.LocalGridPosition, endPosition) <
                        Vector3I.DistanceManhattan(m_endpointPosition1.LocalGridPosition, startPosition))
                    {
                        swapPositions = true;
                    }
                }

                if (swapPositions)
                {
                    Vector3I swap = startPosition;
                    startPosition = endPosition;
                    endPosition = swap;
                }
            }

            // Calculate the section information for both lines after split
            List<SectionInformation> sections1 = null;
            List<SectionInformation> sections2 = null;
            
            ConveyorLinePosition newPosition1 = new ConveyorLinePosition(startPosition, m_endpointPosition2.Direction);
            ConveyorLinePosition newPosition2 = new ConveyorLinePosition(endPosition, m_endpointPosition1.Direction);
            ConveyorLinePosition dummyPosition = new ConveyorLinePosition();

            int line1Length = 0;
            int line2Length = 0;
            if (m_sections != null)
            {
                m_tmpSections1.Clear();
                m_tmpSections2.Clear();

                SplitSections(m_sections, Length, m_endpointPosition1.LocalGridPosition, startPosition, m_tmpSections1, m_tmpSections2, out newPosition1, out newPosition2, out line1Length);
                line2Length = Length - line1Length;

                if (m_tmpSections1.Count > 1)
                {
                    sections1 = new List<SectionInformation>();
                    sections1.AddList(m_tmpSections1);
                }

                if (startPosition != endPosition)
                {
                    m_tmpSections1.Clear();
                    int removedLineLength;
                    SplitSections(m_tmpSections2, line2Length, newPosition2.LocalGridPosition, endPosition, null, m_tmpSections1, out dummyPosition, out newPosition2, out removedLineLength);
                    line2Length -= removedLineLength;

                    if (m_tmpSections1.Count > 1)
                    {
                        sections2 = new List<SectionInformation>();
                        sections2.AddList(m_tmpSections1);
                    }
                }
                else if (m_tmpSections2.Count > 1)
                {
                    sections2 = new List<SectionInformation>();
                    sections2.AddList(m_tmpSections2);
                }

                m_tmpSections1.Clear();
                m_tmpSections2.Clear();
            }
            else
            {
                line1Length = startPosition.RectangularDistance(m_endpointPosition1.LocalGridPosition);
                line2Length = endPosition.RectangularDistance(m_endpointPosition2.LocalGridPosition);
            }

            MyConveyorLine newLine = null;
            if (line1Length <= 1 || line1Length < line2Length) // Line 1 will be new line
            {
                if (line1Length > 1 || (line1Length > 0 && m_endpoint1 != null)) // Line 1 will be != null
                {
                    newLine = new MyConveyorLine();
                    newLine.InitAfterSplit(m_endpointPosition1, newPosition1, sections1, line1Length, m_cubeGrid, m_type);
                    newLine.InitEndpoints(m_endpoint1, null);
                }

                InitAfterSplit(newPosition2, m_endpointPosition2, sections2, line2Length, m_cubeGrid, m_type);
                InitEndpoints(null, m_endpoint2);
            }
            else
            {
                if (line2Length > 1 || (line2Length > 0 && m_endpoint2 != null))
                {
                    newLine = new MyConveyorLine();
                    newLine.InitAfterSplit(newPosition2, m_endpointPosition2, sections2, line2Length, m_cubeGrid, m_type);
                    newLine.InitEndpoints(null, m_endpoint2);
                }

                InitAfterSplit(m_endpointPosition1, newPosition1, sections1, line1Length, m_cubeGrid, m_type);
                InitEndpoints(m_endpoint1, null);
            }

            RecalculateConductivity();
            if(newLine != null)
                newLine.RecalculateConductivity();
            return newLine;
        }

        private static void SplitSections(
            List<SectionInformation> sections,
            int lengthLimit,
            Vector3I startPosition,
            Vector3I splittingPosition,
            List<SectionInformation> sections1,
            List<SectionInformation> sections2,
            out ConveyorLinePosition newPosition1,
            out ConveyorLinePosition newPosition2,
            out int line1Length)
        {
            bool splittingAtCorner = false;
            int splittingSectionIndex = 0;
            line1Length = 0;
            Vector3I sectionStart = startPosition;
            SectionInformation splittingSection = new SectionInformation();

            int splittingLength = 0;
            for (splittingSectionIndex = 0; splittingSectionIndex < sections.Count; ++splittingSectionIndex)
            {
                splittingSection = sections[splittingSectionIndex];
                if (PositionIsInSection(splittingPosition, sectionStart, splittingSection, out splittingLength))
                {
                    line1Length += splittingLength;

                    if (splittingLength == 0)
                        splittingAtCorner = true;

                    break;
                }

                line1Length += splittingSection.Length;
                sectionStart += Base6Directions.GetIntVector(splittingSection.Direction) * splittingSection.Length;
            }

            Debug.Assert(line1Length < lengthLimit, "Did not find splitting conveyor line section!");

            newPosition2 = new ConveyorLinePosition(splittingPosition, splittingSection.Direction);
            if (splittingAtCorner)
                newPosition1 = new ConveyorLinePosition(splittingPosition, Base6Directions.GetFlippedDirection(sections[splittingSectionIndex - 1].Direction));
            else
                newPosition1 = new ConveyorLinePosition(splittingPosition, Base6Directions.GetFlippedDirection(splittingSection.Direction));

            int newSectionCount1 = splittingAtCorner ? splittingSectionIndex : splittingSectionIndex + 1;
            int newSectionCount2 = sections.Count - splittingSectionIndex;

            SectionInformation newSection = new SectionInformation();

            // Append sections to first list
            if (sections1 != null)
            {
                for (int i = 0; i < newSectionCount1 - 1; ++i)
                    sections1.Add(sections[i]);

                if (splittingAtCorner)
                {
                    sections1.Add(sections[newSectionCount1 - 1]);
                }
                else
                {
                    newSection.Direction = sections[newSectionCount1 - 1].Direction;
                    newSection.Length = splittingLength;
                    sections1.Add(newSection);
                }
            }

            // Append sections to second list
            newSection.Direction = sections[splittingSectionIndex].Direction;
            newSection.Length = sections[splittingSectionIndex].Length - splittingLength;
            sections2.Add(newSection);
            for (int i = 1; i < newSectionCount2; ++i)
            {
                sections2.Add(sections[splittingSectionIndex + i]);
            }
        }

        /// <summary>
        /// Tells, whether the given position lies within the given section (start point included and end point excluded).
        /// If it does, the sectionLength variable contains the distance from sectionStart to position.
        /// </summary>
        private static bool PositionIsInSection(Vector3I position, Vector3I sectionStart, SectionInformation section, out int sectionLength)
        {
            sectionLength = 0;

            Vector3I dirVector = Base6Directions.GetIntVector(section.Direction);
            Vector3I startToPosition = position - sectionStart;
            switch (Base6Directions.GetAxis(section.Direction))
            {
                case Base6Directions.Axis.ForwardBackward:
                    sectionLength = dirVector.Z * startToPosition.Z;
                    break;
                case Base6Directions.Axis.LeftRight:
                    sectionLength = dirVector.X * startToPosition.X;
                    break;
                case Base6Directions.Axis.UpDown:
                    sectionLength = dirVector.Y * startToPosition.Y;
                    break;
                default:
                    Debug.Fail("Invalid value in axis enum");
                    break;
            }

            if (sectionLength >= 0 && sectionLength < section.Length && startToPosition.RectangularLength() == sectionLength)
                return true;

            return false;
        }

        /// <summary>
        /// Rotates the circular line so that the block to be removed is the first block in the line
        /// </summary>
        private void RotateCircularLine(Vector3I position)
        {
            Debug.Assert(m_sections[0].Direction == m_sections[m_sections.Count - 1].Direction);

            List<SectionInformation> newSections = new List<SectionInformation>(m_sections.Count+1);

            Vector3I sectionStart = m_endpointPosition1.LocalGridPosition;
            for (int i = 0; i < m_sections.Count; ++i)
            {
                SectionInformation section = m_sections[i];

                int splittingPosition = 0;
                Vector3I dirVector = Base6Directions.GetIntVector(section.Direction);
                Vector3I startToPosition = position - sectionStart;
                switch (Base6Directions.GetAxis(section.Direction))
                {
                    case Base6Directions.Axis.ForwardBackward:
                        splittingPosition = dirVector.Z * startToPosition.Z;
                        break;
                    case Base6Directions.Axis.LeftRight:
                        splittingPosition = dirVector.X * startToPosition.X;
                        break;
                    case Base6Directions.Axis.UpDown:
                        splittingPosition = dirVector.Y * startToPosition.Y;
                        break;
                    default:
                        Debug.Fail("Invalid value in axis enum");
                        break;
                }

                // We found the section that will be split - it's the one that has this position as final corner point or intermediate point
                if (splittingPosition > 0 && splittingPosition <= section.Length && startToPosition.RectangularLength() == splittingPosition)
                {
                    Debug.Assert(i < m_sections.Count - 1 || splittingPosition < section.Length);

                    // Add second part of the split section
                    SectionInformation firstSection = new SectionInformation();
                    firstSection.Direction = m_sections[i].Direction;
                    firstSection.Length = m_sections[i].Length - splittingPosition + 1;
                    newSections.Add(firstSection);

                    // Add sections between the split section and the last section
                    for (int j = i + 1; j < m_sections.Count - 1; ++j)
                        newSections.Add(m_sections[j]);

                    // Add the final section merged with the first section
                    SectionInformation mergedSection = new SectionInformation();
                    mergedSection.Direction = m_sections[0].Direction;
                    mergedSection.Length = m_sections[0].Length + m_sections[m_sections.Count - 1].Length - 1;
                    newSections.Add(mergedSection);

                    // Add sections between the first section and the split section
                    for (int j = 1; j < i; ++j)
                    {
                        newSections.Add(m_sections[j]);
                    }

                    // Add first part of the split section
                    SectionInformation lastSection = new SectionInformation();
                    lastSection.Direction = m_sections[i].Direction;
                    lastSection.Length = splittingPosition;
                    newSections.Add(lastSection);

                    break;
                }

                sectionStart += Base6Directions.GetIntVector(section.Direction) * section.Length;
            }

            m_sections = newSections;
            Debug.Assert(m_sections[0].Direction == m_sections[m_sections.Count - 1].Direction);

            m_endpointPosition2 = new ConveyorLinePosition(position, Base6Directions.GetFlippedDirection(m_sections[0].Direction));
            m_endpointPosition1 = m_endpointPosition2.GetConnectingPosition();
        }

        private MyCubeGrid GetGrid()
        {
            if (m_endpoint1 == null || m_endpoint2 == null)
                return null;

            var grid = m_endpoint1.CubeBlock.CubeGrid;
            Debug.Assert(m_endpoint2.CubeBlock.CubeGrid == grid, "Grids were different on the two ends of a conveyor line");

            return grid;
        }

        public void StopQueuesIfNeeded()
        {
            if (m_queuePosition != 0.0f) return;

            if (!m_stopped1 && m_queue1.Count != 0)
            {
                MyConveyorPacket firstPacket = m_queue1.First();
                if (firstPacket.LinePosition >= Length - 1)
                    m_stopped1 = true;
            }
            if (!m_stopped2 && m_queue2.Count != 0)
            {
                MyConveyorPacket firstPacket = m_queue2.First();
                if (firstPacket.LinePosition >= Length - 1)
                    m_stopped2 = true;
            }
        }

        public void Update()
        {
            // Do a big update every time the queue position gets over 1.0
            m_queuePosition += 1 / (float)FRAMES_PER_BIG_UPDATE;
            if (m_queuePosition >= 1.0f)
            {
                BigUpdate();
            }
        }

        public void BigUpdate()
        {
            StopQueuesIfNeeded();

            if (!m_stopped1)
            {
                foreach (var packet in m_queue1)
                {
                    packet.LinePosition++;
                }
            }
            if (!m_stopped2)
            {
                foreach (var packet in m_queue2)
                {
                    packet.LinePosition++;
                }
            }

            if (!m_isWorking)
            {
                m_stopped1 = true;
                m_stopped2 = true;
            }

            m_queuePosition = 0.0f;

            if (!m_stopped1 || !m_stopped2)
                RecalculatePacketPositions();
        }

        public void UpdateIsFunctional()
        {
            m_isFunctional = UpdateIsFunctionalInternal();
            UpdateIsWorking();
        }

        public void UpdateIsWorking()
        {
            if (!m_isFunctional)
            {
                m_isWorking = false;
                return;
            }

            if (IsDisconnected)
            {
                m_isWorking = false;
                return;
            }

            var grid = GetGrid();
            //grid.GridSystems.ConveyorSystem.ResourceSink.Update();
            m_isWorking = grid.GridSystems.ConveyorSystem.ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        private bool UpdateIsFunctionalInternal()
        {
            if (m_endpoint1 == null || m_endpoint2 == null || !m_endpoint1.CubeBlock.IsFunctional || !m_endpoint2.CubeBlock.IsFunctional)
            {
                return false;
            }

            var grid = m_endpoint1.CubeBlock.CubeGrid;
            Debug.Assert(m_endpoint2.CubeBlock.CubeGrid == grid, "Grids were different on the two ends of a conveyor line");

            foreach (var position in this)
            {
                var block = grid.GetCubeBlock(position);
                if (block == null || block.FatBlock == null)
                    continue;

                if (!block.FatBlock.IsFunctional)
                {
                    return false;
                }
            }

            return true;
        }

        public void PrepareForDraw(MyCubeGrid grid)
        {
            if (m_queue1.Count == 0 && m_queue2.Count == 0) return;

            if (!m_stopped1)
                foreach (var packet in m_queue1)
                    packet.MoveRelative(1.0f / (float)FRAMES_PER_BIG_UPDATE);
            if (!m_stopped2)
                foreach (var packet in m_queue2)
                    packet.MoveRelative(1.0f / (float)FRAMES_PER_BIG_UPDATE);
            
            return;

            Matrix mat = grid.WorldMatrix;
            Vector3 position = Vector3.Transform(m_endpointPosition1.LocalGridPosition * grid.GridSize, grid.WorldMatrix);
            Vector3 direction = Vector3.TransformNormal(m_endpointPosition1.VectorDirection * grid.GridSize, grid.WorldMatrix);
            foreach (var packet in m_queue1)
            {
                mat.Translation = position + direction * ((float)packet.LinePosition + m_queuePosition);
                packet.PositionComp.SetWorldMatrix(mat);
            }

            position = Vector3.Transform(m_endpointPosition2.LocalGridPosition * grid.GridSize, grid.WorldMatrix);
            direction = Vector3.TransformNormal(m_endpointPosition2.VectorDirection * grid.GridSize, grid.WorldMatrix);
            foreach (var packet in m_queue2)
            {
                mat.Translation = position + direction * ((float)packet.LinePosition + m_queuePosition);
                packet.PositionComp.SetWorldMatrix(mat);
            }
        }

        public void DebugDraw(MyCubeGrid grid)
        {
            //if (!MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS_LINE_CAPSULES) return;

            Vector3 pos = new Vector3(m_endpointPosition1.LocalGridPosition) * grid.GridSize;
            Vector3 pos2 = new Vector3(m_endpointPosition2.LocalGridPosition) * grid.GridSize;
            pos = Vector3.Transform(pos, grid.WorldMatrix);
            pos2 = Vector3.Transform(pos2, grid.WorldMatrix);

            String text = m_endpoint1 == null ? "- " : "# ";
            text += m_length.ToString();
            text += " ";
            text += m_type.ToString();
            text += m_endpoint2 == null ? " -" : " #";
            text += " ";
            text += m_conductivity.ToString();

            MyRenderProxy.DebugDrawText3D((pos + pos2) * 0.5f, text, Color.Blue, 1.0f, false);
            var col = IsFunctional? Color.Green : Color.Red;
            MyRenderProxy.DebugDrawLine3D(pos, pos2, col, col, false);

//            MyRenderProxy.DebugDrawCapsule(pos, pos2, 0.1f, Color.DarkOliveGreen, false);
        }

        public void DebugDrawPackets()
        {
            foreach (var packet in m_queue1)
            {
                MyRenderProxy.DebugDrawSphere(packet.WorldMatrix.Translation, 0.2f, Color.Red.ToVector3(), 1.0f, false);
                MyRenderProxy.DebugDrawText3D(packet.WorldMatrix.Translation, packet.LinePosition.ToString(), Color.White, 1.0f, false);
            }

            foreach (var packet in m_queue2)
            {
                MyRenderProxy.DebugDrawSphere(packet.WorldMatrix.Translation, 0.2f, Color.Red.ToVector3(), 1.0f, false);
                MyRenderProxy.DebugDrawText3D(packet.WorldMatrix.Translation, packet.LinePosition.ToString(), Color.White, 1.0f, false);
            }
        }
    }
}
