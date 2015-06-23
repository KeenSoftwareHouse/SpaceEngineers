using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.Input;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems
{
    public class MyOxygenRoom
    {
        public int Index;

        public bool IsPressurized;
        public float EnvironmentOxygen;
        public double OxygenAmount;
        public int blockCount;
        public int DepressurizationTime;

        //NOTE(AF) for debugging only
        public Color Color;
        
        public MyOxygenRoom(int index)
        {
            IsPressurized = true;

            EnvironmentOxygen = 0f;
            Index = index;
            Color = new Color(MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat());
        }

        public float OxygenLevel(float gridSize)
        {
            return (float)(OxygenAmount / MaxOxygen(gridSize));
        }

        public double MissingOxygen(float gridSize)
        {
            return MaxOxygen(gridSize) - OxygenAmount;
        }

        public double MaxOxygen(float gridSize)
        {
            return blockCount * gridSize * gridSize * gridSize;
        }
    }

    public struct MyOxygenBlock
    {
        public MyOxygenRoom Room;
        public float PreviousOxygenAmount;
        public int OxygenChangeTime;
        

        public MyOxygenBlock(MyOxygenRoom room)
        {
            Room = room;
            PreviousOxygenAmount = 0;
            OxygenChangeTime = 0;
        }

        internal float OxygenAmount()
        {
            if (Room == null)
            {
                return 0f;
            }

            float targetOxygenAmount = (float)(Room.IsPressurized ? (Room.OxygenAmount / Room.blockCount) : Room.EnvironmentOxygen);

            float deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - OxygenChangeTime;
            
            float t = deltaTime / MyGridOxygenSystem.OXYGEN_UNIFORMIZATION_TIME_MS;
            if (t > 1f)
            {
                t = 1f;
            }
            return MathHelper.Lerp(PreviousOxygenAmount, targetOxygenAmount, t);
        }

        public float OxygenLevel(float gridSize)
        {
            return OxygenAmount() / (gridSize * gridSize * gridSize);
        }
    }

    public class MyGridOxygenSystem
    {
        private struct MyDepressurizationForceInfo
        {
            public Vector3 Direction;
            public float Strength;
            public int ForceCount;
        }

        private static bool DEBUG_MODE = false;
        public const float OXYGEN_UNIFORMIZATION_TIME_MS = 1500;

        private MyCubeGrid m_cubeGrid;

        private MyOxygenBlock[, ,] m_cubeRoom;
        private List<MyOxygenRoom> m_rooms;

        private List<RoomSquare> m_queue = new List<RoomSquare>();
        private int m_queueIndex = 0;
        private bool isPressurizing = false;

        private List<MyParticleEffect> m_depressurizationEffects = new List<MyParticleEffect>();

        //Intermediary storage. Needed because the pressurization process can be interrupted
        private MyOxygenBlock[, ,] m_tempPrevCubeRoom;
        private List<MyOxygenRoom> m_tempPrevRooms;
        
        private MyOxygenBlock[, ,] m_prevCubeRoom;
        private List<MyOxygenRoom> m_prevRooms;
        private Vector3I m_prevMin;

        private float[] m_savedRooms;

        private List<MyOxygenRoom> m_tempRooms;

        private List<IMyOxygenBlock> m_oxygenBlocks = new List<IMyOxygenBlock>();

        private int m_lastUpdateTime;

        private MySyncOxygenSystem SyncObject;

        private bool m_pressurizationPending = false;
        private List<MyEntity> m_entitiesInDepressurizationRange = new List<MyEntity>();
        private Dictionary<MyEntity, MyDepressurizationForceInfo> m_forcesToApply = new Dictionary<MyEntity, MyDepressurizationForceInfo>();

        //Cannot use Base6Direction because it's not optimal to process neighbours in that order
        private List<Vector3I> m_neighbours = new List<Vector3I>()
            {
                new Vector3I(1, 0, 0),
                new Vector3I(-1, 0, 0),
                new Vector3I(0, 1, 0),
                new Vector3I(0, -1, 0),
                new Vector3I(0, 0, 1),
                new Vector3I(0, 0, -1),
            };

        public MyGridOxygenSystem(MyCubeGrid cubeGrid)
        {
            this.m_cubeGrid = cubeGrid;

            cubeGrid.OnBlockAdded += cubeGrid_OnBlockAdded;
            cubeGrid.OnBlockRemoved += cubeGrid_OnBlockRemoved;

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            SyncObject = new MySyncOxygenSystem(m_cubeGrid);
        }

        public void RegisterOxygenBlock(IMyOxygenBlock block)
        {
            Debug.Assert(!m_oxygenBlocks.Contains(block));
            m_oxygenBlocks.Add(block);
        }
        
        public void UnregisterOxygenBlock(IMyOxygenBlock block)
        {
            Debug.Assert(m_oxygenBlocks.Contains(block));
            m_oxygenBlocks.Remove(block);
        }

        void cubeGrid_OnBlockAdded(MySlimBlock obj)
        {
            if (obj.FatBlock is IMyDoor)
            {
                ((IMyDoor)obj.FatBlock).DoorStateChanged += OnDoorStateChanged;
            }

            m_pressurizationPending = true;
        }

        void cubeGrid_OnBlockRemoved(MySlimBlock obj)
        {
            if (obj.FatBlock is IMyDoor)
            {
                ((IMyDoor)obj.FatBlock).DoorStateChanged -= OnDoorStateChanged;
            }

            m_pressurizationPending = true;
        }

        void OnDoorStateChanged(bool status)
        {
            m_pressurizationPending = true;
        }

        private struct OxygenProductionGroup
        {
            public SortedDictionary<int, List<IMyOxygenConsumer>> Consumers;
            public SortedDictionary<int, List<IMyOxygenProducer>> Producers;
            public List<MyOxygenTank> Tanks;
            public List<MyOxygenTank> NonStockpilingTanks;

            public IMyConveyorEndpoint FirstEndpoint;

            public OxygenProductionGroup(IMyOxygenBlock block)
            {
                var consumer = block as IMyOxygenConsumer;
                var producer = block as IMyOxygenProducer;
                var tank = block as MyOxygenTank;

                Producers = null;
                Consumers = null;
                FirstEndpoint = null;

                Consumers = new SortedDictionary<int, List<IMyOxygenConsumer>>();
                if (consumer != null)
                {
                    var newConsumerList = new List<IMyOxygenConsumer>();
                    newConsumerList.Add(consumer);
                    Consumers.Add(consumer.GetPriority(), newConsumerList);
                    FirstEndpoint = consumer.ConveyorEndpoint;
                }

                Producers = new SortedDictionary<int, List<IMyOxygenProducer>>();
                if (producer != null)
                {
                    var newProducerList = new List<IMyOxygenProducer>();
                    newProducerList.Add(producer);
                    Producers.Add(producer.GetPriority(), newProducerList);
                    FirstEndpoint = producer.ConveyorEndpoint;
                }

                Tanks = new List<MyOxygenTank>();
                NonStockpilingTanks = new List<MyOxygenTank>();
                if (tank != null)
                {
                    Tanks.Add(tank);
                    if (!tank.IsStockpiling)
                    {
                        NonStockpilingTanks.Add(tank);
                    }
                    FirstEndpoint = tank.ConveyorEndpoint;
                }
            }

            public void Add(IMyOxygenBlock block)
            {
                var consumer = block as IMyOxygenConsumer;

                if (consumer != null && consumer.ConsumptionNeed(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) > 0f)
                {
                    List<IMyOxygenConsumer> oxygenConsumerList;
                    int consumerPriority = consumer.GetPriority();
                    if (Consumers.TryGetValue(consumerPriority, out oxygenConsumerList))
                    {
                        oxygenConsumerList.Add(consumer);
                    }
                    else
                    {
                        var newConsumerList = new List<IMyOxygenConsumer>();
                        newConsumerList.Add(consumer);
                        Consumers.Add(consumer.GetPriority(), newConsumerList);
                    }
                }
                else
                {
                    var producer = block as IMyOxygenProducer;
                    if (producer != null && producer.ProductionCapacity(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) > 0f)
                    {
                        List<IMyOxygenProducer> oxygenProducerList;
                        int producerPriority = producer.GetPriority();
                        if (Producers.TryGetValue(producerPriority, out oxygenProducerList))
                        {
                            oxygenProducerList.Add(producer);
                        }
                        else
                        {
                            var newProducerList = new List<IMyOxygenProducer>();
                            newProducerList.Add(producer);
                            Producers.Add(producer.GetPriority(), newProducerList);
                        }
                    }
                    else
                    {
                        var tank = block as MyOxygenTank;
                        if (tank != null)
                        {
                            Tanks.Add(tank);
                            if (!tank.IsStockpiling)
                            {
                                NonStockpilingTanks.Add(tank);
                            }
                        }
                    }
                }
            }
        }

        private bool ShouldPressurize()
        {
            if (m_cubeGrid.Physics == null)
            {
                return false;
            }

            if (m_oxygenBlocks.Count > 0)
            {
                return true;
            }

            if (isPressurizing)
            {
                return true;
            }

            if (m_rooms != null)
            {
                foreach (var room in m_rooms)
                {
                    if (room.IsPressurized && room.OxygenAmount > 1f)
                    {
                        return true;
                    }

                    if (!room.IsPressurized)
                    {
                        float deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - room.DepressurizationTime;
                        if (deltaTime < OXYGEN_UNIFORMIZATION_TIME_MS)
                        {
                            return true;
                        }
                    }
                }

                m_rooms = null;
                m_cubeRoom = null;

                m_prevRooms = null;
                m_prevCubeRoom = null;

                m_tempPrevRooms = null;
                m_tempPrevCubeRoom = null;
            }

            return false;
        }

        public void UpdateBeforeSimulation()
        {
            if (m_pressurizationPending)
            {
                if (ShouldPressurize())
                {
                    PressurizeInternal();
                }
                m_pressurizationPending = false;
            }
            else if (isPressurizing)
            {
                ProfilerShort.Begin("Oxygen Pressurize");
                if (DEBUG_MODE)
                {
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.Shift))
                    {
                        if (PressurizeProcessQueue(0))
                        {
                            PressurizePostProcess();
                        }
                    }
                }
                else
                {
                    if (PressurizeProcessQueue(2500))
                    {
                        PressurizePostProcess();
                    }
                }
                ProfilerShort.End();
            }
        }

        #region Conveyor
        public void UpdateBeforeSimulation100()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            float deltaTime = (currentTime - m_lastUpdateTime) / 1000f;

            m_lastUpdateTime = currentTime;

            if (Sync.IsServer)
            {
                UpdateOxygenProduction(deltaTime);

                if (!isPressurizing && m_rooms != null)
                {
                    float[] oxygenAmount = new float[m_rooms.Count];
                    for (int i = 0; i < m_rooms.Count; i++)
                    {
                        oxygenAmount[i] = (float)m_rooms[i].OxygenAmount;
                    }
                    SyncObject.UpdateOxygenAmount(oxygenAmount);
                }
            }

            foreach (var effect in m_depressurizationEffects)
            {
                if (effect.GetElapsedTime() > 1f)
                {
                    effect.Stop();
                }
            }

            int index = 0;
            while (index < m_depressurizationEffects.Count)
            {
                if (m_depressurizationEffects[index].GetParticlesCount() == 0)
                {
                    m_depressurizationEffects[index].Close(true);
                    m_depressurizationEffects.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void UpdateOxygenProduction(float deltaTime)
        {
            List<OxygenProductionGroup> groups = new List<OxygenProductionGroup>();

            var logicalGroup = MyCubeGridGroups.Static.Logical.GetGroup(m_cubeGrid);

            if (m_cubeGrid.Physics == null)
            {
                return;
            }

            var mass = m_cubeGrid.Physics.Mass;

            var nodes = logicalGroup.Nodes;
            if (nodes.Count > 1)
            {
                foreach (var node in nodes)
                {
                    var children = node.Children;
                    if (node.NodeData != m_cubeGrid && node.NodeData.Physics != null && node.NodeData.Physics.Mass > mass)
                    {
                        //This is not the most massive grid in the system
                        return;
                    }
                }
            }

            foreach (var node in nodes)
            {
                if (node.NodeData.GridSystems.OxygenSystem == null)
                {
                    continue;
                }

                if (node.NodeData.Physics == null)
                {
                    continue;
                }

                var oxygenBlocks = node.NodeData.GridSystems.OxygenSystem.m_oxygenBlocks;

                for (int i = 0; i < oxygenBlocks.Count; i++)
                {
                    var block = oxygenBlocks[i];
                    if (!block.IsWorking())
                    {
                        continue;
                    }
                    bool found = false;

                    foreach (var group in groups)
                    {
                        if (MyGridConveyorSystem.Pathfinding.Reachable(group.FirstEndpoint, block.ConveyorEndpoint))
                        {
                            group.Add(block);
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        groups.Add(new OxygenProductionGroup(block));
                    }
                }
            }

            foreach (var group in groups)
            {
                if ((group.Consumers.Count == 0 || group.Producers.Count == 0) && group.Tanks.Count == 0)
                {
                    continue;
                }
                float consumption = 0f;
                float production = 0f;

                //TODO(AF) better way to get max priority?
                float[] consumptionPerPriorityLevels = group.Consumers.Count == 0 ? null : new float[group.Consumers.Last().Key + 1];

                foreach (var consumerList in group.Consumers.Values)
                {
                    foreach (var consumer in consumerList)
                    {
                        float c = consumer.ConsumptionNeed(deltaTime);
                        consumption += c;
                        consumptionPerPriorityLevels[consumer.GetPriority()] += c;
                    }
                }

                foreach (var producerList in group.Producers.Values)
                {
                    foreach (var producer in producerList)
                    {
                        production += producer.ProductionCapacity(deltaTime);
                    }
                }

                if (production > consumption)
                {
                    float productionLeft = production - consumption;

                    int remainingTanks = group.Tanks.Count;
                    foreach (var tank in group.Tanks)
                    {
                        float portion = productionLeft / remainingTanks;
                        float capacityLeft = tank.Capacity * (1f - tank.FilledRatio);

                        float portionForTank = Math.Min(portion, capacityLeft);

                        tank.Fill(portionForTank);

                        remainingTanks--;
                        productionLeft -= portionForTank;
                    }

                    foreach (var consumerList in group.Consumers)
                    {
                        foreach (var consumer in consumerList.Value)
                        {
                            consumer.Consume(consumer.ConsumptionNeed(deltaTime));
                        }
                    }

                    float toGenerate = production - productionLeft;
                    foreach (var producerList in group.Producers)
                    {
                        foreach (var producer in producerList.Value)
                        {
                            float maxProduction = producer.ProductionCapacity(deltaTime);
                            if (maxProduction > 0f)
                            {
                                if (toGenerate < maxProduction)
                                {
                                    producer.Produce(toGenerate);
                                    toGenerate = 0f;
                                    break;
                                }
                                else
                                {
                                    producer.Produce(maxProduction);
                                    toGenerate -= maxProduction;
                                }
                            }
                        }


                        if (toGenerate <= 0f)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    float originalConsumption = consumption;
                    
                    consumption -= production;
                    int remainingTanks = group.NonStockpilingTanks.Count;

                    foreach (var tank in group.NonStockpilingTanks)
                    {
                        float portion = consumption / remainingTanks;
                        float oxygenLeft = tank.Capacity * tank.FilledRatio;

                        float portionForTank = Math.Min(portion, oxygenLeft);
                        tank.Drain(portionForTank);
                        remainingTanks--;
                        production += portionForTank;
                    }

                    foreach (var producerList in group.Producers)
                    {
                        foreach (var producer in producerList.Value)
                        {
                            producer.Produce(producer.ProductionCapacity(deltaTime));
                        }
                    }

                    float originalProduction = production;

                    foreach (var consumerList in group.Consumers)
                    {
                        if (production <= 0f)
                        {
                            break;
                        }

                        float priorityConsumption = Math.Min(production, consumptionPerPriorityLevels[consumerList.Key]);
                        production -= priorityConsumption;
                        foreach (var consumer in consumerList.Value)
                        {
                            float c = priorityConsumption / consumerList.Value.Count;
                            consumer.Consume(c);
                        }
                    }
                }

                //Balance tanks
                float averageFill = 0f;
                foreach (var tank in group.Tanks)
                {
                    averageFill += tank.FilledRatio;
                }
                averageFill /= group.Tanks.Count;

                float tankProduction = 0f;
                float producingTanksTotalCapacity = 0f;

                float tankConsumption = 0f;
                float consumingTanksTotalCapacity = 0f;

                foreach (var tank in group.Tanks)
                {
                    if (averageFill > tank.FilledRatio)
                    {
                        float ratioDelta = Math.Min((averageFill - tank.FilledRatio), 0.05f);
                        tankConsumption += ratioDelta * tank.Capacity;
                        consumingTanksTotalCapacity += tank.Capacity;
                    }
                    else if(!tank.IsStockpiling)
                    {
                        float ratioDelta = Math.Min((tank.FilledRatio - averageFill), 0.05f);
                        tankProduction += ratioDelta * tank.Capacity;
                        producingTanksTotalCapacity += tank.Capacity;
                    }
                }

                float oxygenTransfer = Math.Min(tankConsumption, tankProduction);
                foreach (var tank in group.Tanks)
                {
                    if (averageFill > tank.FilledRatio)
                    {
                        tank.Fill(oxygenTransfer * (tank.Capacity / consumingTanksTotalCapacity));
                    }
                    else if (!tank.IsStockpiling)
                    {
                        tank.Drain(oxygenTransfer * (tank.Capacity / producingTanksTotalCapacity));
                    }
                }
            }
        }
        #endregion

        #region Pressurization
        private struct RoomSquare
        {
            public Vector3I Pos;
            public int Index;
            public bool WasWall;

            public RoomSquare(Vector3I pos, int index, bool wasWall = false)
            {
                Pos = pos;
                Index = index;
                WasWall = wasWall;
            }
        }

        public void Pressurize()
        {
            m_pressurizationPending = true;
        }

        private void PressurizeInternal()
        {
            PressurizeInitialize();

            ProfilerShort.Begin("Oxygen Pressurize");
            PressurizeProcessQueue(2500);
            ProfilerShort.End();
        }

        private int RoomIndex(int x, int y, int z)
        {
            return m_cubeRoom[x, y, z].Room != null ? m_cubeRoom[x, y, z].Room.Index : int.MaxValue;
        }

        private void PressurizeInitialize()
        {
            m_tempPrevCubeRoom = isPressurizing ? m_prevCubeRoom : m_cubeRoom;
            m_tempPrevRooms = isPressurizing? m_prevRooms : m_rooms;

            Vector3I size = GridMax() - GridMin();

            m_cubeRoom = new MyOxygenBlock[size.X + 1, size.Y + 1, size.Z + 1];
            m_queue.Clear();
            m_queue.Add(new RoomSquare(GridMin(), 0));
            m_tempRooms = new List<MyOxygenRoom>();
            m_cubeRoom[0, 0, 0] = new MyOxygenBlock(new MyOxygenRoom(0));
            m_tempRooms.Add(m_cubeRoom[0, 0, 0].Room);

            m_queueIndex = 0;
            isPressurizing = true;
        }

        private bool PressurizeProcessQueue(int count)
        {
            int index = 0;
            while (m_queueIndex < m_queue.Count)
            {
                for (int i = 0; i < 6; i++)
                {
                    var currentRoom = m_queue[m_queueIndex];

                    Vector3I current = currentRoom.Pos + m_neighbours[i];

                    if (!IsInBounds(current))
                    {
                        continue;
                    }

                    Vector3I posInGrid = current - GridMin();

                    int x = posInGrid.X;
                    int y = posInGrid.Y;
                    int z = posInGrid.Z;

                    int prevRoomIndex = currentRoom.Index;
                    int currRoomIndex = RoomIndex(x, y, z);

                    if (currRoomIndex <= prevRoomIndex)
                        continue;

                    bool isPressurized = IsPressurized(currentRoom.Pos, current);

                    if (isPressurized)
                    {
                        if (prevRoomIndex < currRoomIndex)
                        {
                            if (m_tempRooms.Count >= currRoomIndex)
                            {
                                continue;
                            }

                            prevRoomIndex = m_tempRooms.Count;
                            m_cubeRoom[x, y, z] = new MyOxygenBlock(new MyOxygenRoom(prevRoomIndex));
                            m_tempRooms.Add(m_cubeRoom[x, y, z].Room);
                            if (current == GridMin())
                            {

                            }
                            m_queue.Add(new RoomSquare(current, prevRoomIndex, !currentRoom.WasWall));
                            if (m_cubeRoom[x, y, z].Room == null)
                            {
                                m_cubeRoom[x, y, z].Room = new MyOxygenRoom(prevRoomIndex);
                            }
                            else
                            {
                                m_cubeRoom[x, y, z].Room = m_tempRooms[prevRoomIndex];
                            }
                        }
                    }
                    else
                    {
                        m_queue.Add(new RoomSquare(current, prevRoomIndex, currentRoom.WasWall));
                        if (m_cubeRoom[x, y, z].Room == null)
                        {
                            m_cubeRoom[x, y, z].Room = new MyOxygenRoom(prevRoomIndex);
                        }
                        else
                        {
                            if (prevRoomIndex < m_tempRooms.Count)
                            {
                                m_cubeRoom[x, y, z].Room = m_tempRooms[prevRoomIndex];
                            }
                        }

                        if (IsOnBounds(current))
                        {
                            m_cubeRoom[x, y, z].Room.IsPressurized = false;
                            m_cubeRoom[x, y, z].Room.EnvironmentOxygen = Math.Max(m_cubeRoom[x, y, z].Room.EnvironmentOxygen, MyOxygenProviderSystem.GetOxygenInPoint(m_cubeGrid.GridIntegerToWorld(posInGrid)));
                            m_cubeRoom[x, y, z].Room.DepressurizationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        }
                    }
                }
                m_queueIndex++;
                index++;

                if ((index > count))
                {
                    return false;
                }
            }
            m_queue.Clear();
            return true;
        }

        private void PressurizePostProcess()
        {
            m_prevCubeRoom = m_tempPrevCubeRoom;
            m_prevRooms = m_tempPrevRooms;

            for (int i = 0; i < m_cubeRoom.GetLength(0); i++)
                for (int j = 0; j < m_cubeRoom.GetLength(1); j++)
                    for (int k = 0; k < m_cubeRoom.GetLength(2); k++)
                    {
                        var oldRoom = m_cubeRoom[i, j, k];
                        var newRoom = m_tempRooms[m_cubeRoom[i, j, k].Room.Index];

                        m_cubeRoom[i, j, k].Room = newRoom;

                        if (!oldRoom.Room.IsPressurized)
                        {
                            newRoom.IsPressurized = false;
                            newRoom.EnvironmentOxygen = Math.Max(newRoom.EnvironmentOxygen, oldRoom.Room.EnvironmentOxygen);
                            newRoom.DepressurizationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        }

                        Vector3I current = new Vector3I(i, j, k) + GridMin();

                        newRoom.blockCount++;
                    }

            for (int i = 0; i < m_cubeRoom.GetLength(0); i++)
                for (int j = 0; j < m_cubeRoom.GetLength(1); j++)
                    for (int k = 0; k < m_cubeRoom.GetLength(2); k++)
                    {
                        var room = m_cubeRoom[i, j, k].Room;
                        if (room != null && room.blockCount < 2)
                        {
                            m_cubeRoom[i, j, k].Room = null;
                        }
                    }


            m_rooms = new List<MyOxygenRoom>();
            int index = 0;
            foreach (var room in m_tempRooms)
            {
                if (room.blockCount > 1)
                {
                    room.Index = index;

                    m_rooms.Add(room);
                    index++;
                }
            }

            if (m_savedRooms != null && m_savedRooms.Count() != m_rooms.Count)
            {
                m_savedRooms = null;
            }

            if (m_savedRooms != null)
            {
                for (int i = 0; i < m_rooms.Count; i++)
                {
                    m_rooms[i].OxygenAmount = m_savedRooms[i];
                }
            }
            else
            {
                if (m_prevCubeRoom != null)
                {
                    for (int i = 0; i < m_prevCubeRoom.GetLength(0); i++)
                        for (int j = 0; j < m_prevCubeRoom.GetLength(1); j++)
                            for (int k = 0; k < m_prevCubeRoom.GetLength(2); k++)
                            {
                                var prevRoom = m_prevCubeRoom[i, j, k];
                                if (prevRoom.Room == null || prevRoom.Room.blockCount < 2)
                                    continue;

                                double cubeOxygen = prevRoom.Room.IsPressurized ? prevRoom.OxygenAmount() : prevRoom.Room.EnvironmentOxygen * GridCubeVolume();// prevRoom.Room.OxygenAmount / prevRoom.Room.blockCount;

                                Vector3I pos = new Vector3I(i, j, k) + m_prevMin - GridMin();

                                if (pos.X < 0 || pos.X >= m_cubeRoom.GetLength(0))
                                    continue;
                                if (pos.Y < 0 || pos.Y >= m_cubeRoom.GetLength(1))
                                    continue;
                                if (pos.Z < 0 || pos.Z >= m_cubeRoom.GetLength(2))
                                    continue;
                                
                                var currentRoom = m_cubeRoom[pos.X, pos.Y, pos.Z].Room;
                                m_cubeRoom[pos.X, pos.Y, pos.Z].PreviousOxygenAmount = (float)cubeOxygen;
                                m_cubeRoom[pos.X, pos.Y, pos.Z].OxygenChangeTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                                if (currentRoom != null && currentRoom.blockCount > 1)
                                {
                                    currentRoom.OxygenAmount += cubeOxygen;
                                }

                                if (currentRoom != null && currentRoom.blockCount < 2)
                                {
                                    m_cubeRoom[pos.X, pos.Y, pos.Z].Room = null;
                                }
                            }

                    //Do breach detection in a separate pass to ensure that oxygen levels are correct
                    for (int i = 0; i < m_prevCubeRoom.GetLength(0); i++)
                        for (int j = 0; j < m_prevCubeRoom.GetLength(1); j++)
                            for (int k = 0; k < m_prevCubeRoom.GetLength(2); k++)
                            {
                                Vector3I pos = new Vector3I(i, j, k) + m_prevMin - GridMin();
                                if (!IsInGridBounds(pos, m_cubeRoom))
                                    continue;

                                var prevRoom = m_prevCubeRoom[i, j, k].Room;
                                var currentRoom = m_cubeRoom[pos.X, pos.Y, pos.Z].Room;
                                bool breachDetected = false;

                                //Do a preliminary scan to check if there is any new breach
                                for (int l = 0; l < 6; l++)
                                {
                                    Vector3I currNeighbourPos = pos + m_neighbours[l];
                                    if (!IsInGridBounds(currNeighbourPos, m_cubeRoom))
                                        continue;

                                    Vector3I prevNeighbourPos = new Vector3I(i, j, k) + m_neighbours[l];
                                    if (!IsInGridBounds(prevNeighbourPos, m_prevCubeRoom))
                                        continue;

                                    var currNeighbourRoom = m_cubeRoom[currNeighbourPos.X, currNeighbourPos.Y, currNeighbourPos.Z].Room;
                                    var prevNeighbourRoom = m_prevCubeRoom[prevNeighbourPos.X, prevNeighbourPos.Y, prevNeighbourPos.Z].Room;

                                    if (currNeighbourRoom == currentRoom && prevNeighbourRoom != prevRoom)
                                    {
                                        breachDetected = true;
                                    }
                                }

                                float minOxygenLevel = float.MaxValue;
                                float maxOxygenLevel = float.MinValue;
                                Vector3I minPos = Vector3I.Zero;
                                if (breachDetected)
                                {
                                    //Scan to determine if there is actually a difference in pressure levels
                                    for (int l = 0; l < 6; l++)
                                    {
                                        Vector3I currNeighbourPos = pos + m_neighbours[l];
                                        if (!IsInGridBounds(currNeighbourPos, m_cubeRoom))
                                            continue;

                                        Vector3I prevNeighbourPos = new Vector3I(i, j, k) + m_neighbours[l];
                                        if (!IsInGridBounds(prevNeighbourPos, m_prevCubeRoom))
                                            continue;

                                        var prevNeighbourRoom = m_prevCubeRoom[prevNeighbourPos.X, prevNeighbourPos.Y, prevNeighbourPos.Z].Room;
                                        if (prevNeighbourRoom == null)
                                            continue;

                                        if (IsPressurized(pos + GridMin(), currNeighbourPos + GridMin()))
                                            continue;

                                        float level = prevNeighbourRoom.IsPressurized ? prevNeighbourRoom.OxygenLevel(m_cubeGrid.GridSize) : prevNeighbourRoom.EnvironmentOxygen;

                                        if (level > maxOxygenLevel)
                                        {
                                            maxOxygenLevel = level;
                                        }
                                        if (level < minOxygenLevel)
                                        {
                                            minOxygenLevel = level;
                                            minPos = currNeighbourPos;
                                        }
                                    }
                                }

                                if (maxOxygenLevel - minOxygenLevel > 0.2f)
                                {
                                    Vector3D from = m_cubeGrid.GridIntegerToWorld(pos + GridMin());
                                    Vector3D to = m_cubeGrid.GridIntegerToWorld(minPos + GridMin());

                                    //Force
                                    float MAX_DISTANCE = 5f;

                                    var boundingSphere = new BoundingSphereD(to, MAX_DISTANCE);
                                    var decompressionDirection = Vector3D.Normalize(to - from);
                                    MyGamePruningStructure.GetAllEntitiesInSphere<MyEntity>(ref boundingSphere, m_entitiesInDepressurizationRange);

                                    foreach (var entity in m_entitiesInDepressurizationRange)
                                    {
                                        if (!(entity is MyCubeBlock) && !(entity is MyEntitySubpart) && entity.Physics != null)
                                        {
                                            var entityPos = entity.PositionComp.WorldMatrix.Translation;

                                            var forceDirection = (to - from) / 2f;
                                            var distance = (to - entityPos).Length();
                                            if (distance < MAX_DISTANCE)
                                            {
                                                forceDirection /= distance;

                                                if (Vector3D.Dot(decompressionDirection, forceDirection) < 0f)
                                                {
                                                    forceDirection = -forceDirection;
                                                }

                                                //float forceStrength = 500f * prevRoom.Room.OxygenLevel(m_cubeGrid.GridSize) * (1f - (float)distance / MAX_DISTANCE);
                                                float forceStrength = 500f * (1f - (float)distance / MAX_DISTANCE);

                                                MyDepressurizationForceInfo forceInfo;
                                                if (!m_forcesToApply.TryGetValue(entity, out forceInfo))
                                                {
                                                    forceInfo = new MyDepressurizationForceInfo();

                                                    forceInfo.Direction = forceDirection;
                                                    forceInfo.Strength = forceStrength;
                                                    forceInfo.ForceCount = 1;
                                                }
                                                else
                                                {
                                                    forceInfo.Direction = (forceInfo.Direction * forceInfo.ForceCount + forceDirection) / (forceInfo.ForceCount + 1);
                                                    forceInfo.Strength = (forceInfo.Strength * forceInfo.ForceCount + forceStrength) / (forceInfo.ForceCount + 1);
                                                    forceInfo.ForceCount++;
                                                }

                                                m_forcesToApply[entity] = forceInfo;
                                            }
                                        }
                                    }

                                    m_entitiesInDepressurizationRange.Clear();

                                    //Effect
                                    MyParticleEffect m_effect;
                                    if (MyParticlesManager.TryCreateParticleEffect(49, out m_effect))
                                    {
                                        var orientation = Matrix.CreateFromDir(to - from);
                                        orientation.Translation = from;
                                        m_effect.UserScale = 3f;

                                        m_effect.WorldMatrix = orientation;
                                        m_effect.AutoDelete = true;

                                        m_depressurizationEffects.Add(m_effect);
                                    }
                                }
                            }
                }

                foreach (var force in m_forcesToApply)
                {
                    var entity = force.Key;
                    var forceInfo = force.Value;

                    var character = entity as Sandbox.Game.Entities.Character.MyCharacter;
                    if (character != null)
                    {
                        if (character.Parent != null)
                        {
                            continue;
                        }
                        forceInfo.Strength *= 5f;
                    }

                    if (forceInfo.Strength > 1f)
                    {
                        if (character != null && character.IsDead == false)
                        {
                            character.EnableJetpack(true);
                        }

                        forceInfo.Direction.Normalize();
                        entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceInfo.Direction * forceInfo.Strength, entity.PositionComp.WorldMatrix.Translation, null);
                    }
                }

                m_forcesToApply.Clear();

                foreach (var room in m_rooms)
                {
                    if (room.OxygenLevel(m_cubeGrid.GridSize) > 1.0)
                    {
                        room.OxygenAmount = room.MaxOxygen(m_cubeGrid.GridSize);
                    }

                }
            }

            m_prevMin = GridMin();
            isPressurizing = false;
            m_queueIndex = 0;
        }

        private double GridCubeVolume()
        {
            double gridSize = m_cubeGrid.GridSize;
            return gridSize * gridSize * gridSize;
        }

        private bool IsInBounds(Vector3I pos)
        {
            if (GridMin() != Vector3I.Min(pos, GridMin()))
                return false;

            if (GridMax() != Vector3I.Max(pos, GridMax()))
                return false;

            return true;
        }

        private bool IsOnBounds(Vector3I pos)
        {
            if (ContainsZero((pos - GridMin())))
                return true;

            if (ContainsZero((GridMax() - pos)))
                return true;

            return false;
        }

        private bool IsInGridBounds(Vector3I pos, MyOxygenBlock[, ,] grid)
        {
            if (pos.X < 0 || pos.X >= grid.GetLength(0))
                return false;
            if (pos.Y < 0 || pos.Y >= grid.GetLength(1))
                return false;
            if (pos.Z < 0 || pos.Z >= grid.GetLength(2))
                return false;

            return true;
        }

        private Vector3I GridMin()
        {
            return m_cubeGrid.Min - Vector3I.One;
        }

        private Vector3I GridMax()
        {
            return m_cubeGrid.Max + Vector3I.One;
        }

        private bool ContainsZero(Vector3I v)
        {
            return v.X * v.Y * v.Z == 0;
        }

        private bool ApproxEqual(float x, float y, float ep)
        {
            return (Math.Abs(x - y) < ep);
        }

        private bool IsPressurized(Vector3I startPos, Vector3I endPos)
        {
            var startBlock = m_cubeGrid.GetCubeBlock(startPos);
            var endBlock = m_cubeGrid.GetCubeBlock(endPos);

            if (startBlock == endBlock)
            {
                if (startBlock != null)
                {
                    return startBlock.BlockDefinition.IsAirTight;
                }
                else
                {
                    return false;
                }
            }

            if (startBlock != null && (startBlock.BlockDefinition.IsAirTight || IsPressurized(startBlock, startPos, endPos - startPos)))
            {
                return true;
            }
            else
            {
                return endBlock != null && (endBlock.BlockDefinition.IsAirTight || IsPressurized(endBlock, endPos, startPos - endPos));
            }
        }

        private bool IsPressurized(MySlimBlock block, Vector3I pos, Vector3 normal)
        {
            if (block.BlockDefinition.BuildProgressModels.Count() > 0)
            {
                var lastModel = block.BlockDefinition.BuildProgressModels[block.BlockDefinition.BuildProgressModels.Length - 1];
                if (block.BuildLevelRatio < lastModel.BuildRatioUpperBound)
                {
                    return false;
                }
            }
            Matrix blockOrientation;
            
            block.Orientation.GetMatrix(out blockOrientation);

            blockOrientation.TransposeRotationInPlace();
            Vector3 transformedNormal = Vector3.Transform(normal, (blockOrientation));
            Vector3 offset = Vector3.Zero;
            if (block.FatBlock != null)
            {
                offset = pos - block.FatBlock.Position;
            }
            Vector3 transformedOffset = Vector3.Transform(offset, blockOrientation) + block.BlockDefinition.Center;

            bool isPressurized = block.BlockDefinition.IsCubePressurized[Vector3I.Round(transformedOffset)][Vector3I.Round(transformedNormal)];
            if (isPressurized)
            {
                return true;
            }

            if (block.FatBlock != null)
            {
                var doorBlock = block.FatBlock;

                if (doorBlock is MyDoor)
                {
                    var door = doorBlock as MyDoor;
                    if (!door.Open)
                    {
                        foreach (var mountPoint in block.BlockDefinition.MountPoints)
                        {
                            if (transformedNormal == mountPoint.Normal)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                else if (doorBlock is MyAdvancedDoor)
                {
                    var door = doorBlock as MyAdvancedDoor;
                    if (door.FullyClosed)
                    {
                        foreach (var mountPoint in block.BlockDefinition.MountPoints)
                        {
                            if (transformedNormal == mountPoint.Normal)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                else if (doorBlock is MyAirtightDoorGeneric)
                {
                    var hangarDoor = doorBlock as MyAirtightDoorGeneric;
                    if (hangarDoor.IsFullyClosed)
                    {
                        if (transformedNormal == Vector3.Forward || transformedNormal == Vector3.Backward)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        public MyOxygenBlock GetOxygenBlock(Vector3D worldPosition)
        {
            Vector3I blockPosition = m_cubeGrid.WorldToGridInteger(worldPosition) - GridMin();


            if (!isPressurizing)
            {
                if (m_cubeRoom != null && 
                    blockPosition.X >= 0 && blockPosition.X < m_cubeRoom.GetLength(0) &&
                    blockPosition.Y >= 0 && blockPosition.Y < m_cubeRoom.GetLength(1) &&
                    blockPosition.Z >= 0 && blockPosition.Z < m_cubeRoom.GetLength(2))
                {
                    return m_cubeRoom[blockPosition.X, blockPosition.Y, blockPosition.Z];
                }
            }
            else
            {
                if (m_prevCubeRoom != null && 
                    blockPosition.X >= 0 && blockPosition.X < m_prevCubeRoom.GetLength(0) &&
                    blockPosition.Y >= 0 && blockPosition.Y < m_prevCubeRoom.GetLength(1) &&
                    blockPosition.Z >= 0 && blockPosition.Z < m_prevCubeRoom.GetLength(2))
                {
                    return m_prevCubeRoom[blockPosition.X, blockPosition.Y, blockPosition.Z];
                }
            }
            return new MyOxygenBlock();
        }

        public MyOxygenBlock GetSafeOxygenBlock(Vector3 position)
        {
            var initial = GetOxygenBlock(position);
            if (initial.Room == null)
            {
                //Retry adjacent blocks
                Vector3D pos = Vector3D.Transform(position, m_cubeGrid.PositionComp.WorldMatrixNormalizedInv);
                pos /= m_cubeGrid.GridSize;

                List<Vector3D> neighbours = new List<Vector3D>(3);
                if (pos.X - Math.Floor(pos.X) > 0.5f)
                {
                    neighbours.Add(new Vector3D(-1.0, 0.0, 0.0));
                }
                else
                {
                    neighbours.Add(new Vector3D(1.0, 0.0, 0.0));
                }

                if (pos.Y - Math.Floor(pos.Y) > 0.5f)
                {
                    neighbours.Add(new Vector3D(0.0, -1.0, 0.0));
                }
                else
                {
                    neighbours.Add(new Vector3D(0.0, 1.0, 0.0));
                }

                if (pos.Z - Math.Floor(pos.Z) > 0.5f)
                {
                    neighbours.Add(new Vector3D(0.0, 0.0, -1.0));
                }
                else
                {
                    neighbours.Add(new Vector3D(0.0, 0.0, 1.0));
                }

                foreach (var offset in neighbours)
                {
                    Vector3D invPos = pos;
                    invPos += offset;
                    invPos *= m_cubeGrid.GridSize;
                    invPos = Vector3D.Transform(invPos, m_cubeGrid.PositionComp.WorldMatrix);

                    var second = GetOxygenBlock(invPos);
                    if (second.Room != null && second.Room.IsPressurized)
                    {
                        return second;
                    }
                }
            }
            return initial;
        }

        public void DebugDraw()
        {
            var cubeRoom = (!isPressurizing|| DEBUG_MODE) ? m_cubeRoom : m_prevCubeRoom;

            if (cubeRoom == null)
            {
                return;
            }

            ProfilerShort.Begin("Oxygen Debug Draw");
            for (int i = 0; i < cubeRoom.GetLength(0); i++)
                for (int j = 0; j < cubeRoom.GetLength(1); j++)
                    for (int k = 0; k < cubeRoom.GetLength(2); k++)
                    {
                        
                        Vector3I current = new Vector3I(i, j, k) + GridMin();

                        var currentBlock = m_cubeGrid.GetCubeBlock(current);
                        if (currentBlock != null && currentBlock.FatBlock == null && !DEBUG_MODE)
                            continue;

                        Vector3 worldPos = m_cubeGrid.GridIntegerToWorld(current);

                        if (cubeRoom[i, j, k].Room == null)
                        {
                            continue;
                        }

                        int roomIndex = cubeRoom[i, j, k].Room.Index;

                        if (roomIndex == int.MaxValue)
                            continue;

                        Color color = cubeRoom[i, j, k].Room.Color;

                        double oxygenLevel = cubeRoom[i, j, k].OxygenLevel(m_cubeGrid.GridSize);

                        if (oxygenLevel > 0.9999)
                        {
                            color = Color.Teal;
                        }
                        else
                        {
                            color = Color.Lerp(Color.Red, Color.Green, (float)oxygenLevel);
                        }

                        if (cubeRoom[i, j, k].Room.IsPressurized || DEBUG_MODE)
                        {
                            MyRenderProxy.DebugDrawSphere(worldPos, 0.15f, color.ToVector3(), 0.5f, false, true);
                        }
                        if (DEBUG_MODE)
                        {
                            MyRenderProxy.DebugDrawText3D(worldPos, roomIndex.ToString(), Color.White, 0.5f, false);
                        }
                    }

            ProfilerShort.End();
                    
        }

        internal float[] GetOxygenAmount()
        {
            var rooms = isPressurizing ? m_prevRooms : m_rooms;
            if (rooms != null)
            {
                float[] amount = new float[rooms.Count];
                for (int i = 0; i < rooms.Count; i++)
                {
                    amount[i] = (float)rooms[i].OxygenAmount;
                }
                return amount;
            }

            return null;
        }

        internal void Init(float[] oxygenAmount)
        {
            m_savedRooms = oxygenAmount;   
        }

        #region Sync

        internal void UpdateOxygenAmount(float[] oxygenAmount)
        {
            if (!isPressurizing && m_rooms != null && m_rooms.Count == oxygenAmount.Count())
            {
                for (int i = 0; i < m_rooms.Count; i++)
                {
                    m_rooms[i].OxygenAmount = oxygenAmount[i];
                }
            }
        }

        [PreloadRequired]
        internal class MySyncOxygenSystem
        {
            [ProtoContract]
            [MessageIdAttribute(7900, P2PMessageEnum.Unreliable)]
            protected struct UpdateOxygenAmountMsg
            {
                [ProtoMember]
                public long EntityId;
                [ProtoMember]
                public float[] OxygenAmount;
            }

            private MyCubeGrid m_cubeGrid;

            static MySyncOxygenSystem()
            {
                MySyncLayer.RegisterMessage<UpdateOxygenAmountMsg>(OnOxygenAmountUpdated, MyMessagePermissions.FromServer);
            }

            public MySyncOxygenSystem(MyCubeGrid cubeGrid)
            {
                m_cubeGrid = cubeGrid;
            }

            public void UpdateOxygenAmount(float[] oxygenAmount)
            {
                var msg = new UpdateOxygenAmountMsg();
                msg.EntityId = m_cubeGrid.EntityId;
                msg.OxygenAmount = oxygenAmount;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnOxygenAmountUpdated(ref UpdateOxygenAmountMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    if (MySession.Static.Settings.EnableOxygen)
                    {
                        cubeGrid.GridSystems.OxygenSystem.UpdateOxygenAmount(msg.OxygenAmount);
                    }
                }
            }
        }
        #endregion
    }
}
