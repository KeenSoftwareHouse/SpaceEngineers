using ParallelTasks;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Generics;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
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
        public MyOxygenRoomLink Link;
        //NOTE(AF) for debugging only
        public Color Color;
        
        public MyOxygenRoom(int index)
        {
            IsPressurized = true;

            EnvironmentOxygen = 0f;
            Index = index;
            Color = new Color(MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat());
        }
        /*public MyOxygenRoom():this(-1)//for object pool
        {
        }*/

        public float OxygenLevel(float gridSize)
        {
            return (float)(OxygenAmount / MaxOxygen(gridSize));
        }

        public double MissingOxygen(float gridSize)
        {
            return Math.Max(MaxOxygen(gridSize) - OxygenAmount, 0.0);
        }

        public double MaxOxygen(float gridSize)
        {
            return blockCount * gridSize * gridSize * gridSize;
        }
    }

    /// <summary>
    /// Used as a pointer so that we can change rooms fast without iterating through all of the blocks
    /// </summary>
    public class MyOxygenRoomLink
    {
        public MyOxygenRoom Room;
        public MyOxygenRoomLink(MyOxygenRoom room)
        {
            SetRoom(room);
        }
        public void SetRoom(MyOxygenRoom room)
        {
            Room = room;
            Room.Link = this;
        }
        public MyOxygenRoomLink()//for object pool
        {
        }
    }

    class MyOxygenRoomLinkPool
    {
        public MyObjectsPool<MyOxygenRoomLink> pool;
        public MyOxygenRoomLinkPool(int capacity)
        {
            pool=new MyObjectsPool<MyOxygenRoomLink>(capacity);
        }
        public MyOxygenRoomLink get()
        {
            MyOxygenRoomLink ret;
            pool.AllocateOrCreate(out ret);
            return ret;
        }
        public void give(MyOxygenRoomLink item)
        {
            pool.Deallocate(item);
        }
    }
    /*class MyOxygenRoomPool
    {
        MyObjectsPool<MyOxygenRoom> pool;
        public MyOxygenRoomPool(int capacity)
        {
            pool = new MyObjectsPool<MyOxygenRoom>(capacity);
        }
        MyOxygenRoom get()
        {
            MyOxygenRoom ret;
            pool.AllocateOrCreate(out ret);
            ret.Index = -1;
            ret.IsPressurized = true;
            ret.EnvironmentOxygen = 0f;//todo set defaults function
            return ret;
        }
        void give(MyOxygenRoom item)
        {
            pool.Deallocate(item);
        }
    }*/

    public struct MyOxygenBlock
    {
        public MyOxygenRoomLink RoomLink;
        public MyOxygenRoom Room
        {
            get
            {
                if (RoomLink == null) return null;
                return RoomLink.Room;
            }
        }
        public float PreviousOxygenAmount;
        public int OxygenChangeTime;
        

        public MyOxygenBlock(MyOxygenRoomLink roomPointer)
        {
            RoomLink = roomPointer;
            PreviousOxygenAmount = 0;
            OxygenChangeTime = 0;
        }
        public void SetDefaults(MyOxygenRoomLink roomPointer)
        {
            RoomLink = roomPointer;
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
            
            float t = deltaTime / MyGridGasSystem.OXYGEN_UNIFORMIZATION_TIME_MS;
            if (t > 1f)
            {
                t = 1f;
            }
            return MathHelper.Lerp(PreviousOxygenAmount, targetOxygenAmount, t);
        }

        public void Reset()
        {
            if (RoomLink == null) return;
            //RoomLink.Room = null;
            RoomLink = null;
            PreviousOxygenAmount = 0;
            OxygenChangeTime = 0;
        }

        public float OxygenLevel(float gridSize)
        {
            return OxygenAmount() / (gridSize * gridSize * gridSize);
        }

        public override string ToString()
        {
            return "MyOxygenBlock - Oxygen: " + OxygenAmount() + "/" + PreviousOxygenAmount;
        }
    }

    public class MyGridGasSystem
    {
        private struct MyDepressurizationForceInfo
        {
            public Vector3 Direction;
            public float Strength;
            public int ForceCount;
        }

        private static bool DEBUG_MODE = false;

        private readonly MyCubeGrid m_cubeGrid;
        private readonly MySoundPair m_airleakSound = new MySoundPair("EventAirVent");

		#region Oxygen
		public const float OXYGEN_UNIFORMIZATION_TIME_MS = 1500;

        private bool m_isPressurizing = false;
        //Intermediary storage. Needed because the pressurization process can be interrupted
        private MyOxygenBlock[, ,] m_tempPrevCubeRoom;
        private List<MyOxygenRoom> m_tempPrevRooms;

        private MyOxygenBlock[, ,] m_cubeRoom;
        private List<MyOxygenRoom> m_rooms;

        private Queue<RoomSquare> m_queue = new Queue<RoomSquare>();
        private int m_qMaxSize;//debug only
        private int m_roomCnt;//debug only

        private MyOxygenBlock[, ,] m_prevCubeRoom;
        private List<MyOxygenRoom> m_prevRooms;
        private Vector3I m_prevMin;
        private Vector3I m_storedGridMin;
        private Vector3I m_storedGridMax;

        // Room dimensions
        private Vector3I m_cubeRoomDimensions = Vector3I.Zero;
        private Vector3I m_prevCubeRoomDimensions = Vector3I.Zero;
        private Vector3I m_tempPrevCubeRoomDimensions = Vector3I.Zero;

        private float[] m_savedRooms;

        private List<MyOxygenRoom> m_tempRooms;

        private int m_gasBlockCounter = 0;

        private bool m_pressurizationPending = false;
        private readonly List<MyEntity> m_entitiesInDepressurizationRange = new List<MyEntity>();
        private readonly Dictionary<MyEntity, MyDepressurizationForceInfo> m_forcesToApply = new Dictionary<MyEntity, MyDepressurizationForceInfo>();
        private readonly List<Vector3I> m_deletedBlocks = new List<Vector3I>();

        MyOxygenRoomLinkPool m_OxygenRoomLinkPool;
        
        Task m_backgroundTask;
        bool m_bgTaskRunning = false;
        //bool m_doPostProcess = false; //GK: PostProccess creates lags. Add to background
		#endregion

        private int m_lastUpdateTime;
        private bool isClosing = false;

        //Cannot use Base6Direction because it's not optimal to process neighbours in that order
        private readonly List<Vector3I> m_neighbours = new List<Vector3I>()
            {
                new Vector3I(1, 0, 0),
                new Vector3I(-1, 0, 0),
                new Vector3I(0, 1, 0),
                new Vector3I(0, -1, 0),
                new Vector3I(0, 0, 1),
                new Vector3I(0, 0, -1),
            };

        public MyGridGasSystem(MyCubeGrid cubeGrid)
        {
            m_cubeGrid = cubeGrid;

            cubeGrid.OnBlockAdded += cubeGrid_OnBlockAdded;
            cubeGrid.OnBlockRemoved += cubeGrid_OnBlockRemoved;

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void OnGridClosing()
        {
            // This will invalidate any running thread
            isClosing = true;

            // Wait for the task to finish
            if (m_bgTaskRunning)
            {
                // Wait can throw an exception; but since the thread will most likely be blocking at this point anyway it is okay to try-catch it, it shouldn't affect performance
                try
                {
                    m_backgroundTask.Wait();
                }
                catch (Exception ex)
                {
                    MySandboxGame.Log.WriteLineAndConsole("MyGridGasSystem.OnGridClosing: " + ex.Message + ", " + ex.StackTrace);
                }
            }

            m_cubeGrid.OnBlockAdded -= cubeGrid_OnBlockAdded;
            m_cubeGrid.OnBlockRemoved -= cubeGrid_OnBlockRemoved;

            foreach (var block in m_cubeGrid.GetFatBlocks())
            {
                IMyDoor door = block as IMyDoor;
                if(door != null)
                    door.DoorStateChanged -= OnDoorStateChanged;
            }

            // Clear out pool to release memory
            if (m_OxygenRoomLinkPool != null && m_OxygenRoomLinkPool.pool != null)
                m_OxygenRoomLinkPool.pool.DeallocateAll();
        }

        void cubeGrid_OnBlockAdded(MySlimBlock addedBlock)
        {
            if (addedBlock.FatBlock is IMyDoor)
                ((IMyDoor)addedBlock.FatBlock).DoorStateChanged += OnDoorStateChanged;

            m_gasBlockCounter += addedBlock.FatBlock is IMyGasBlock ? 1 : 0;

            m_pressurizationPending = true;
        }

        void cubeGrid_OnBlockRemoved(MySlimBlock deletedBlock)
        {
	        var door = deletedBlock.FatBlock as IMyDoor;
	        if (door != null)
                door.DoorStateChanged -= OnDoorStateChanged;

            m_gasBlockCounter -= deletedBlock.FatBlock is IMyGasBlock ? 1 : 0;

            m_deletedBlocks.Add(deletedBlock.Position);
        }

        void OnDoorStateChanged(bool status)
        {
            m_pressurizationPending = true;
        }

        public void OnCubeGridShrinked()
        {
            m_pressurizationPending = true;
        }

        private bool ShouldPressurize()
        {
            if (m_cubeGrid.Physics == null)
                return false;

            if (m_gasBlockCounter > 0 || m_isPressurizing)
                return true;

	        if (m_rooms == null)
		        return false;

            for (int roomIndex = 0; roomIndex < m_rooms.Count; roomIndex++)
            {
                MyOxygenRoom room = m_rooms[roomIndex];

                if (room.IsPressurized && room.OxygenAmount > 1f)
			        return true;

                // If it is not pressurized, check if enough time elapsed since it was changed to prevent too-early depressurization
		        if (!room.IsPressurized)
		        {
			        float deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - room.DepressurizationTime;
			        if (deltaTime < OXYGEN_UNIFORMIZATION_TIME_MS)
				        return true;
		        }
	        }

            // Grid is no longer pressurized, clear out data
            // TODO: Performance optimization! This is probably a bad idea!
	        m_rooms = null;
	        m_cubeRoom = null;
            m_cubeRoomDimensions = Vector3I.Zero;

	        m_prevRooms = null;
	        m_prevCubeRoom = null;
            m_prevCubeRoomDimensions = Vector3I.Zero;

	        m_tempPrevRooms = null;
	        m_tempPrevCubeRoom = null;
            m_tempPrevCubeRoomDimensions = Vector3I.Zero;

	        return false;
        }

        public void UpdateBeforeSimulation()
        {
            if (MyFakes.BACKGROUND_OXYGEN && m_bgTaskRunning)
                return;//wait

            MySimpleProfiler.Begin("Oxygen");
            if (m_pressurizationPending)
            {
                ProfilerShort.Begin("Oxygen Initialize");
                if (ShouldPressurize())
                {
                    if (MyFakes.BACKGROUND_OXYGEN)
                    {
                        //m_doPostProcess = false;
                        PressurizeInitialize();
                    }
                    else
                        PressurizeInternal();
                }
                m_pressurizationPending = false;
                ProfilerShort.End();
            }
            if (m_isPressurizing)
            {
                ProfilerShort.Begin("Oxygen Pressurize");
                /*
                if (m_doPostProcess)
                {
                    PressurizePostProcess();
                    m_doPostProcess = false;
                }
                else
                */
                if (!m_bgTaskRunning)
                {
                    m_backgroundTask = Parallel.Start(BackgroundPressurizeStart, BackgroundPressurizeFinished);
                    m_bgTaskRunning = true;
                }
                ProfilerShort.End();
            }
            if (!m_isPressurizing && m_deletedBlocks.Count > 0)
            {
                ProfilerShort.Begin("Removing blocks");
                foreach (var deletedBlock in m_deletedBlocks)
                {
                    RemoveBlock(deletedBlock);
                }
                ApplyDepressurizationForces();
                m_deletedBlocks.Clear();
                ProfilerShort.End();
            }
            MySimpleProfiler.End("Oxygen");
        }
        protected void BackgroundPressurizeStart()
        {   //reading grid in realtime can clash with main thread, but process will be restarted anyway after grid change
            //do not write anything into main thread data!
            PressurizeProcessQueue(10000);
        }

        protected void BackgroundPressurizeFinished()
        {
            m_bgTaskRunning = false;
        }

        public void UpdateBeforeSimulation100()
        {
            if (m_bgTaskRunning)
                return;

            MySimpleProfiler.Begin("Oxygen");
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            float deltaTime = (currentTime - m_lastUpdateTime) / 1000f;

            m_lastUpdateTime = currentTime;

            if (Sync.IsServer)
            {
                if (!m_isPressurizing && m_rooms != null)
                {
                    float[] oxygenAmount = new float[m_rooms.Count];
                    for (int i = 0; i < m_rooms.Count; i++)
                    {
                        oxygenAmount[i] = (float)m_rooms[i].OxygenAmount;
                    }
                    m_cubeGrid.UpdateOxygenAmount(oxygenAmount);
                }
            }
            MySimpleProfiler.End("Oxygen");
        }

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
        }

        private int RoomIndex(int x, int y, int z)
        {
            return m_cubeRoom[x, y, z].Room != null ? m_cubeRoom[x, y, z].Room.Index : int.MaxValue;
        }

        private void PressurizeInitialize()
        {
            if (!m_isPressurizing)
            {
                m_tempPrevCubeRoom = m_cubeRoom;
                m_tempPrevCubeRoomDimensions = m_cubeRoomDimensions;
                m_tempPrevRooms = m_rooms;
            }

            Vector3I size = GridMax() - GridMin();

            // Only allocate new m_cubeRoom if the grid size changes
            Vector3I newRoomDimensions = size + Vector3I.One;
            //if (m_cubeRoom == null || newRoomDimensions.X != m_cubeRoomDimensions.X || newRoomDimensions.Y != m_cubeRoomDimensions.Y || newRoomDimensions.Z != m_cubeRoomDimensions.Z)
            {
                m_cubeRoom = new MyOxygenBlock[newRoomDimensions.X, newRoomDimensions.Y, newRoomDimensions.Z];
                m_cubeRoomDimensions = newRoomDimensions;
            }
            /*else
            {
                // Reset the grid instead
                for (int i = 0; i < m_cubeRoomDimensions.X; i++)
                {
                    for (int j = 0; j < m_cubeRoomDimensions.Y; j++)
                    {
                        for (int k = 0; k < m_cubeRoomDimensions.Z; k++)
                        {
                            m_cubeRoom[i, j, k].Reset();
                        }
                    }
                }
            }*/

            if (m_OxygenRoomLinkPool == null)
                m_OxygenRoomLinkPool = new MyOxygenRoomLinkPool(2 * (size.X + 1) * (size.Y + 1) * (size.Z + 1) + 10);
            else
            {
                ProfilerShort.Begin("MarkAllActiveForDeallocate");
                m_OxygenRoomLinkPool.pool.MarkAllActiveForDeallocate();
                ProfilerShort.End();
            }
            m_queue.Clear();
            m_qMaxSize = 0;
            m_roomCnt = 0;
            m_storedGridMin = GridMin();
            m_storedGridMax = GridMax();
            m_queue.Enqueue(new RoomSquare(GridMin(), 0));
            m_tempRooms = new List<MyOxygenRoom>();
            MyOxygenRoomLink link = m_OxygenRoomLinkPool.get();
            link.SetRoom(new MyOxygenRoom(0));
            m_cubeRoom[0, 0, 0].SetDefaults(link);
            //m_cubeRoom[0, 0, 0] = new MyOxygenBlock(new MyOxygenRoomLink(new MyOxygenRoom(0)));
            m_tempRooms.Add(m_cubeRoom[0, 0, 0].Room);

            m_deletedBlocks.Clear();
            
            m_isPressurizing = true;
        }
        
        private bool PressurizeProcessQueue(int count, bool useCount = false)
        {
            if (DEBUG_MODE)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Shift))
                    count = 0;
                else
                    return false;
            }
            ProfilerShort.Begin("Oxygen PressurizeProcessQueue");
            int index = 0;
            while (m_queue.Count > 0 && !isClosing)
            {
                if (m_qMaxSize < m_queue.Count)
                    m_qMaxSize = m_queue.Count;
                var currentRoom = m_queue.Dequeue();//m_queue[m_queueIndex];
                for (int i = 0; i < 6; i++)
                {
                    Vector3I current = currentRoom.Pos + m_neighbours[i];

                    if (m_storedGridMin != Vector3I.Min(current, m_storedGridMin) 
                        || m_storedGridMax != Vector3I.Max(current, m_storedGridMax))
                        continue;

                    Vector3I posInGrid = current - m_storedGridMin;

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
                        ProfilerShort.Begin("Oxygen PressurizeProcessQueue: Presurized");
                        if (prevRoomIndex < currRoomIndex)
                        {
                            if (m_tempRooms.Count >= currRoomIndex)
                            {
                                ProfilerShort.End();
                                continue;
                            }

                            prevRoomIndex = m_tempRooms.Count;
                            //m_cubeRoom[x, y, z] = new MyOxygenBlock(new MyOxygenRoomLink(new MyOxygenRoom(prevRoomIndex)));
                            var link=m_OxygenRoomLinkPool.get();
                            link.SetRoom(new MyOxygenRoom(prevRoomIndex));
                            m_cubeRoom[x, y, z].SetDefaults(link);
                            m_roomCnt++;
                            m_tempRooms.Add(m_cubeRoom[x, y, z].Room);

                            m_queue.Enqueue(new RoomSquare(current, prevRoomIndex, !currentRoom.WasWall));
                            if (m_cubeRoom[x, y, z].Room == null)
                            {
                                //m_cubeRoom[x, y, z].RoomLink = new MyOxygenRoomLink(new MyOxygenRoom(prevRoomIndex));
                                link=m_OxygenRoomLinkPool.get();
                                link.SetRoom(new MyOxygenRoom(prevRoomIndex));
                                m_cubeRoom[x, y, z].RoomLink=link;
                                m_roomCnt++;
                            }
                            else
                            {
                                m_cubeRoom[x, y, z].RoomLink = m_tempRooms[prevRoomIndex].Link;
                            }
                        }
                        ProfilerShort.End();
                    }
                    else
                    {
                        ProfilerShort.Begin("Oxygen PressurizeProcessQueue: not Presurized");
                        ProfilerShort.Begin("Queue add");
                        m_queue.Enqueue(new RoomSquare(current, prevRoomIndex, currentRoom.WasWall));
                        ProfilerShort.End();
                        ProfilerShort.Begin("if section");
                        if (m_cubeRoom[x, y, z].Room == null)
                        {
                            ProfilerShort.Begin("new roomLink");
                            //m_cubeRoom[x, y, z].RoomLink = new MyOxygenRoomLink(new MyOxygenRoom(prevRoomIndex));
                            var link = m_OxygenRoomLinkPool.get();
                            link.SetRoom(new MyOxygenRoom(prevRoomIndex));
                            m_cubeRoom[x, y, z].RoomLink = link;
                            m_roomCnt++;
                            ProfilerShort.End();
                        }
                        else
                        {
                            if (prevRoomIndex < m_tempRooms.Count)
                            {
                                m_cubeRoom[x, y, z].RoomLink = m_tempRooms[prevRoomIndex].Link;
                            }
                        }
                        ProfilerShort.End();
                        ProfilerShort.Begin("OnBounds");
                        if (IsOnBounds(current))
                        {
                            m_cubeRoom[x, y, z].Room.IsPressurized = false;
                            m_cubeRoom[x, y, z].Room.EnvironmentOxygen = Math.Max(m_cubeRoom[x, y, z].Room.EnvironmentOxygen, MyOxygenProviderSystem.GetOxygenInPoint(m_cubeGrid.GridIntegerToWorld(posInGrid)));
                            m_cubeRoom[x, y, z].Room.DepressurizationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        }
                        ProfilerShort.End();
                        ProfilerShort.End();
                    }
                }
                index++;

                if (useCount && index > count)
                {
                    ProfilerShort.End();
                    return false;
                }
            }
            if (DEBUG_MODE)
            {
                MyLog.Default.WriteLine("Q max size: " + m_qMaxSize);
                MyLog.Default.WriteLine("pool Active: " + m_OxygenRoomLinkPool.pool.ActiveCount);
            }
            ProfilerShort.End();
            //m_doPostProcess = true;
            PressurizePostProcess();

            return true;
        }

        private void PressurizePostProcess()
        {
            // No need to post-process when closing
            if (isClosing) return;

            ProfilerShort.Begin("Oxygen PressurizePostProcess");
            m_prevCubeRoom = m_tempPrevCubeRoom;
            m_prevCubeRoomDimensions = m_tempPrevCubeRoomDimensions;
            m_prevRooms = m_tempPrevRooms;

            ProfilerShort.Begin("DeallocateAllMarked");
            m_OxygenRoomLinkPool.pool.DeallocateAllMarked();
            ProfilerShort.End();

            ProfilerShort.Begin("Block 1");
            for (int i = 0; i < m_cubeRoomDimensions.X; i++)
                for (int j = 0; j < m_cubeRoomDimensions.Y; j++)
                    for (int k = 0; k < m_cubeRoomDimensions.Z; k++)
                    {
                        MyOxygenBlock cubeRoom = m_cubeRoom[i, j, k];
                        var newRoom = m_tempRooms[cubeRoom.Room.Index];

                        m_cubeRoom[i, j, k].RoomLink = newRoom.Link;

                        if (!cubeRoom.Room.IsPressurized)
                        {
                            newRoom.IsPressurized = false;
                            newRoom.EnvironmentOxygen = Math.Max(newRoom.EnvironmentOxygen, cubeRoom.Room.EnvironmentOxygen);
                            newRoom.DepressurizationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        }

                        newRoom.blockCount++;
                    }
            ProfilerShort.End();
            ProfilerShort.Begin("Block 2");
            for (int i = 0; i < m_cubeRoomDimensions.X; i++)
                for (int j = 0; j < m_cubeRoomDimensions.Y; j++)
                    for (int k = 0; k < m_cubeRoomDimensions.Z; k++)
                    {
                        var room = m_cubeRoom[i, j, k].Room;
                        if (room != null && room.blockCount < 2)
                        {
                            m_cubeRoom[i, j, k].RoomLink = null;
                        }
                    }
            ProfilerShort.End();

            ProfilerShort.Begin("Block 3");
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

            if (m_savedRooms != null && m_savedRooms.Length != m_rooms.Count)
            {
                m_savedRooms = null;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Block 4");
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
                    ProfilerShort.Begin("Block 4 A");
                    for (int i = 0; i < m_prevCubeRoomDimensions.X; i++)
                        for (int j = 0; j < m_prevCubeRoomDimensions.Y; j++)
                            for (int k = 0; k < m_prevCubeRoomDimensions.Z; k++)
                            {
                                var prevRoom = m_prevCubeRoom[i, j, k];
                                if (prevRoom.Room == null || prevRoom.Room.blockCount < 2)
                                    continue;

                                double cubeOxygen = prevRoom.Room.IsPressurized ? prevRoom.OxygenAmount() : prevRoom.Room.EnvironmentOxygen * GridCubeVolume();// prevRoom.Room.OxygenAmount / prevRoom.Room.blockCount;

                                Vector3I pos = new Vector3I(i, j, k) + m_prevMin - GridMin();

                                if (!IsInGridBounds(ref pos, ref m_cubeRoomDimensions))
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
                                    m_cubeRoom[pos.X, pos.Y, pos.Z].RoomLink = null;
                                }
                            }
                    ProfilerShort.End();

                    ProfilerShort.Begin("Block 4 B");
                    //Do breach detection in a separate pass to ensure that oxygen levels are correct
                    for (int i = 0; i < m_prevCubeRoomDimensions.X; i++)
                        for (int j = 0; j < m_prevCubeRoomDimensions.Y; j++)
                            for (int k = 0; k < m_prevCubeRoomDimensions.Z; k++)
                            {
                                Vector3I pos = new Vector3I(i, j, k) + m_prevMin - GridMin();
                                if (!IsInGridBounds(ref pos, ref m_cubeRoomDimensions))
                                    continue;

                                var prevRoom = m_prevCubeRoom[i, j, k].Room;
                                var currentRoom = m_cubeRoom[pos.X, pos.Y, pos.Z].Room;
                                bool breachDetected = false;

                                //Do a preliminary scan to check if there is any new breach
                                for (int l = 0; l < 6; l++)
                                {
                                    Vector3I currNeighbourPos = pos + m_neighbours[l];
                                    if (!IsInGridBounds(ref currNeighbourPos, ref m_cubeRoomDimensions))
                                        continue;

                                    Vector3I prevNeighbourPos = new Vector3I(i, j, k) + m_neighbours[l];
                                    if (!IsInGridBounds(ref prevNeighbourPos, ref m_prevCubeRoomDimensions))
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
                                        if (!IsInGridBounds(ref currNeighbourPos, ref m_cubeRoomDimensions))
                                            continue;

                                        Vector3I prevNeighbourPos = new Vector3I(i, j, k) + m_neighbours[l];
                                        if (!IsInGridBounds(ref prevNeighbourPos, ref m_prevCubeRoomDimensions))
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

                                    AddDepressurizationEffects(from, to);
                                }
                            }
                    ProfilerShort.End();
                }

                ProfilerShort.Begin("Block 4 C");
                ApplyDepressurizationForces();
                ProfilerShort.End();

                ProfilerShort.Begin("Block 4 D");
                foreach (var room in m_rooms)
                {
                    if (room.OxygenLevel(m_cubeGrid.GridSize) > 1.0)
                    {
                        room.OxygenAmount = room.MaxOxygen(m_cubeGrid.GridSize);
                    }

                }
                ProfilerShort.End();
            }
            ProfilerShort.End();

            m_prevMin = GridMin();
            m_isPressurizing = false;
            ProfilerShort.End();
        }

        private void AddDepressurizationEffects(Vector3D from, Vector3D to)
        {
            //Force
            float MAX_DISTANCE = 5f;

            var boundingSphere = new BoundingSphereD(to, MAX_DISTANCE);
            var decompressionDirection = Vector3D.Normalize(to - from);
            MyGamePruningStructure.GetAllEntitiesInSphere(ref boundingSphere, m_entitiesInDepressurizationRange);

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
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(49, out effect))
            {
                var orientation = Matrix.CreateFromDir(to - from);
                orientation.Translation = from;

                effect.WorldMatrix = orientation;

                MyEntity3DSoundEmitter airLeakSound = MyAudioComponent.TryGetSoundEmitter();
                if (airLeakSound != null)
                {
                    airLeakSound.SetPosition(from);
                    airLeakSound.PlaySound(m_airleakSound);
                }
            }
        }

        private void ApplyDepressurizationForces()
        {
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
	                    var jetpack = character.JetpackComp;
						if(jetpack != null)
							jetpack.TurnOnJetpack(true);
                    }


                    if (Vector3.GetNormalized(ref forceInfo.Direction))
                    {
                        entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceInfo.Direction * forceInfo.Strength, entity.PositionComp.WorldMatrix.Translation, null);
                    }
                }
            }

            m_forcesToApply.Clear();
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

        private bool IsInGridBounds(ref Vector3I pos, ref Vector3I dimensions)
        {
            if (pos.X < 0 || pos.X >= dimensions.X)
                return false;
            if (pos.Y < 0 || pos.Y >= dimensions.Y)
                return false;
            if (pos.Z < 0 || pos.Z >= dimensions.Z)
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
            ProfilerShort.Begin("IsPressurized");
            if (block.BlockDefinition.BuildProgressModels.Length > 0)
            {
                var lastModel = block.BlockDefinition.BuildProgressModels[block.BlockDefinition.BuildProgressModels.Length - 1];
                if (block.BuildLevelRatio < lastModel.BuildRatioUpperBound)
                {
                    ProfilerShort.End();
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
                ProfilerShort.End();
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
                                ProfilerShort.End();
                                return false;
                            }
                        }
                        ProfilerShort.End();
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
                                ProfilerShort.End();
                                return false;
                            }
                        }
                        ProfilerShort.End();
                        return true;
                    }
                }
                else if (doorBlock is MyAirtightSlideDoor)
                {
                    var hangarDoor = doorBlock as MyAirtightDoorGeneric;
                    if (hangarDoor.IsFullyClosed)
                    {
                        //check only forward for slide door from backgward it should be not accessible (closed door)
                        if (transformedNormal == Vector3.Forward)
                        {
                            ProfilerShort.End();
                            return true;
                        }
                    }
                }
                else if (doorBlock is MyAirtightDoorGeneric)
                {
                    var hangarDoor = doorBlock as MyAirtightDoorGeneric;
                    if (hangarDoor.IsFullyClosed)
                    {
                        if (transformedNormal == Vector3.Forward || transformedNormal == Vector3.Backward)
                        {
                            ProfilerShort.End();
                            return true;
                        }
                    }
                }
            }
            ProfilerShort.End();
            return false;
        }

        private void RemoveBlock(Vector3I deletedBlockPosition)
        {
            Vector3I current = deletedBlockPosition;
            MyOxygenRoom maxRoom = GetOxygenRoomForCubeGridPosition(current);
            for (int i = 0; i < 6; i++)
            {
                Vector3I neighbour = current + m_neighbours[i];

                if (!IsInBounds(current) || !IsInBounds(neighbour))
                {
                    continue;
                }

                if (IsPressurized(current, neighbour))
                {
                    continue;
                }

                var neighbourRoom = GetOxygenRoomForCubeGridPosition(neighbour);
                if (neighbourRoom != null)
                {
                    if (maxRoom == null)
                    {
                        maxRoom = neighbourRoom;
                    }
                    else if (maxRoom.blockCount < neighbourRoom.blockCount)
                    {
                        maxRoom = neighbourRoom;
                    }
                }
            }

            if (maxRoom == null)
            {
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3I neighbour = current + m_neighbours[i];

                if (!IsInBounds(current) || !IsInBounds(neighbour))
                {
                    continue;
                }

                var neighbourRoom = GetOxygenRoomForCubeGridPosition(neighbour);
                if (neighbourRoom != null && neighbourRoom != maxRoom)
                {
                    maxRoom.blockCount += neighbourRoom.blockCount;
                    maxRoom.OxygenAmount += neighbourRoom.OxygenAmount;


                    if (maxRoom.IsPressurized && !neighbourRoom.IsPressurized)
                    {
                        if (maxRoom.OxygenLevel(m_cubeGrid.GridSize) - neighbourRoom.EnvironmentOxygen > 0.2f)
                        {
                            Vector3D from = m_cubeGrid.GridIntegerToWorld(current);
                            Vector3D to = m_cubeGrid.GridIntegerToWorld(neighbour);

                            AddDepressurizationEffects(from, to);
                        }

                        maxRoom.IsPressurized = false;
                        maxRoom.OxygenAmount = 0f;
                        maxRoom.EnvironmentOxygen = Math.Max(maxRoom.EnvironmentOxygen, neighbourRoom.EnvironmentOxygen);
                        maxRoom.DepressurizationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    }
                    else if (!maxRoom.IsPressurized && neighbourRoom.IsPressurized)
                    {
                        maxRoom.EnvironmentOxygen = Math.Max(maxRoom.EnvironmentOxygen, neighbourRoom.EnvironmentOxygen);
                        if (neighbourRoom.OxygenLevel(m_cubeGrid.GridSize) - maxRoom.EnvironmentOxygen > 0.2f)
                        {
                            Vector3D from = m_cubeGrid.GridIntegerToWorld(neighbour);
                            Vector3D to = m_cubeGrid.GridIntegerToWorld(current);

                            AddDepressurizationEffects(from, to);
                        }
                    }

                    neighbourRoom.Link.Room = maxRoom;
                }
            }

            Vector3I blockPosition = current - GridMin();
            m_cubeRoom[blockPosition.X, blockPosition.Y, blockPosition.Z].RoomLink = maxRoom.Link;
            maxRoom.blockCount++;
        }

        private MyOxygenRoom GetOxygenRoomForCubeGridPosition(Vector3I gridPosition)
        {
            var grid = m_cubeRoom ?? m_prevCubeRoom;
            if (grid == null)
            {
                return null;
            }

            Vector3I blockPosition = gridPosition - GridMin();
            if (m_cubeRoom == null)
            {
                return null;
            }

            // Perform dimension check, room computation may not be finished yet for the older grid
            Vector3I dimensions = (grid == m_cubeRoom) ? m_cubeRoomDimensions : m_prevCubeRoomDimensions;
            if (blockPosition.X < 0 || blockPosition.X >= dimensions.X) return null;
            if (blockPosition.Y < 0 || blockPosition.Y >= dimensions.Y) return null;
            if (blockPosition.Z < 0 || blockPosition.Z >= dimensions.Z) return null;

            var oxygenBlock = grid[blockPosition.X, blockPosition.Y, blockPosition.Z];
            return oxygenBlock.Room;
        }
        #endregion

        public MyOxygenBlock GetOxygenBlock(Vector3D worldPosition)
        {
            Vector3I blockPosition = m_cubeGrid.WorldToGridInteger(worldPosition) - GridMin();

            if (!m_isPressurizing)
            {
                if (m_cubeRoom != null && IsInGridBounds(ref blockPosition, ref m_cubeRoomDimensions))
                {
                    return m_cubeRoom[blockPosition.X, blockPosition.Y, blockPosition.Z];
                }
            }
            else
            {
                if (m_tempPrevCubeRoom != null && IsInGridBounds(ref blockPosition, ref m_tempPrevCubeRoomDimensions))
                {
                    return m_tempPrevCubeRoom[blockPosition.X, blockPosition.Y, blockPosition.Z];
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
            var cubeRoom = (!m_isPressurizing|| DEBUG_MODE) ? m_cubeRoom : m_prevCubeRoom;
            var cubeRoomDimensions = (!m_isPressurizing || DEBUG_MODE) ? m_cubeRoomDimensions : m_prevCubeRoomDimensions;

            if (cubeRoom == null)
            {
                return;
            }

            ProfilerShort.Begin("Oxygen Debug Draw");
            for (int i = 0; i < cubeRoomDimensions.X; i++)
                for (int j = 0; j < cubeRoomDimensions.Y; j++)
                    for (int k = 0; k < cubeRoomDimensions.Z; k++)
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
                            MyRenderProxy.DebugDrawText3D(worldPos, roomIndex.ToString()/* + " " + (i + GridMin().X) + " " + (j + GridMin().Y) + " " + (k + GridMin().Z)*/, Color.White, 0.5f, false);
                        }
                    }

            ProfilerShort.End();
                    
        }

        internal float[] GetOxygenAmount()
        {
            var rooms = m_isPressurizing ? m_prevRooms : m_rooms;
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
            if (!m_isPressurizing && m_rooms != null && m_rooms.Count == oxygenAmount.Length)
            {
                for (int i = 0; i < m_rooms.Count; i++)
                {
                    m_rooms[i].OxygenAmount = oxygenAmount[i];
                }
            }
        }
        #endregion
    }
}
