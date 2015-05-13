using System.Collections.Generic;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using SysUtils.Utils;
using VRage.Common.Utils;
using System.Collections.Concurrent;
using ParallelTasks;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Common;
using VRageRender;
using Sandbox.Game.Entities.VoxelMaps;
using System.Diagnostics;
using System.Threading;
using System;
using VRage.Common.Plugins;

//  This class server for precalculating voxels to triangles. Primary voxels from data cells, but also is used for converting whole data cells when LOD is calculated.
//  This class is static and uses all available cores to work in parallel, thus if user has quad core, work is calculated in four parallel threads.
//  This class works in single-core mode (if machine has only 1 core) or in multi-core mode (using worked threads)
//  In multi-core mode this class creates and starts parallel threads (e.g. four), sets them into waiting. Then when work is needed to do, queue Tasks is filled
//  with task and we send signal to all threads so they wake up, do they work (go over queue, do each job). When they are done, every thread signal he is done and goes to waiting again.
//  Method PrecalcQueue() finishes only after all threads are finished and queue is completely done (empty).
//
//  IMPORTANT: This class assumess all other classes are used in read-only mode (e.g. traversing voxels).
//  
//  Thread - represents classical C# thread
//  Tasks - piece of work to do, e.g. calculate one data cell


namespace Sandbox.Game.Voxels
{
    public struct MyVoxelPrecalcTaskItem
    {
        public MyLodTypeEnum Type;
        public MyVoxelMap VoxelMap;
        public MyVoxelGeometry.CellData Cache;
        public Vector3I VoxelStart;

        public MyVoxelPrecalcTaskItem(MyLodTypeEnum type, MyVoxelMap voxelMap, MyVoxelGeometry.CellData cache, Vector3I voxelStart)
        {
            Type = type;
            VoxelMap = voxelMap;
            Cache = cache;
            VoxelStart = voxelStart;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class MyVoxelPrecalc : MySessionComponentBase
    {
        //  Don't use threads if no more than one core is available, instead use this one task. Or if you don't need to precalculate in parallel.
        static IMyIsoMesher m_singleCoreTask;

        //  Use threads if more than one core is available
        //  Good tutorials on thread synchronization events: 
        //      http://www.codeproject.com/KB/threads/AutoManualResetEvents.aspx
        //      http://www.albahari.com/threading/part2.aspx#_ProducerConsumerQWaitHandle

        static Task[] m_tasks;
        static MyVoxelPrecalcWork[] m_precalWorks;

        //  This is needed in single-core mode and in multi-core too
        public static ConcurrentQueue<MyVoxelPrecalcTaskItem> Tasks;

        public static int AffectedRangeOffset
        {
            get { return m_singleCoreTask.AffectedRangeOffset; }
        }

        public static int AffectedRangeSizeChange
        {
            get { return m_singleCoreTask.AffectedRangeSizeChange; }
        }

        public static int InvalidatedRangeInflate
        {
            get { return m_singleCoreTask.InvalidatedRangeInflate; }
        }

        public static int VertexPositionRangeSizeChange
        {
            get { return m_singleCoreTask.VertexPositionRangeSizeChange; }
        }

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelPrecalc.LoadData");

            MySandboxGame.Log.WriteLine("MyVoxelPrecalc.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            Type mesherType = typeof(MyMarchingCubesMesher);
            Debug.Assert(MyPlugins.Loaded);
            if (MyFakes.ENABLE_ISO_MESHER_FROM_PLUGIN && MyPlugins.PluginAssembly != null)
            {
                var interfaceType = typeof(IMyIsoMesher);
                foreach (var type in MyPlugins.PluginAssembly.GetTypes())
                {
                    if (interfaceType.IsAssignableFrom(type))
                    {
                        mesherType = type;
                        break;
                    }
                }
            }

            //  For calculating on main thread
            m_singleCoreTask = Activator.CreateInstance(mesherType) as IMyIsoMesher;

            Tasks = new ConcurrentQueue<MyVoxelPrecalcTaskItem>();
            if (MySandboxGame.NumberOfCores > 1)
            {
                m_tasks = new Task[MySandboxGame.NumberOfCores];
                m_precalWorks = new MyVoxelPrecalcWork[MySandboxGame.NumberOfCores];

                for (int i = 0; i < MySandboxGame.NumberOfCores; i++)
                    m_precalWorks[i] = new MyVoxelPrecalcWork(Activator.CreateInstance(mesherType) as IMyIsoMesher);
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelPrecalc.LoadData() - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected override void UnloadData()
        {
            m_precalWorks = null;
            m_singleCoreTask = null;
        }

        public static void AddToQueue(
            MyLodTypeEnum type,
            MyVoxelMap voxelMap,
            MyVoxelGeometry.CellData cache,
            int voxelStartX, int voxelStartY, int voxelStartZ)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread, "Only update thread should queue voxel precalc.");
            MyVoxelPrecalcTaskItem a = new MyVoxelPrecalcTaskItem(type, voxelMap, cache, new Vector3I(voxelStartX, voxelStartY, voxelStartZ));
            Tasks.Enqueue(a);
        }

        //  Precalculate voxel cell into cache (makes triangles and vertex buffer from voxels)
        //  Doesn't use threads, just main thread. Use when you don't want to precalculate many cells in parallel.
        public static void PrecalcImmediatelly(MyVoxelPrecalcTaskItem task)
        {
            m_singleCoreTask.Precalc(task);
        }

        //  Precalculate voxel cell into cache (makes triangles and vertex buffer from voxels)
        //  Uses threads (if more cores), calculates all cells in the queue.
        public static void PrecalcQueue()
        {
            //  Don't bother with this if queque isn't empty. 
            //  This is especially important in multi-core mode, because we don't want to uselessly signal worker threads!
            //  IMPORTANT: Don't need to lock Tasks, because at this point no other thread should access it.
            if (MyVoxelPrecalc.Tasks.Count <= 0) return;

            if (MySandboxGame.NumberOfCores == 1 || MyFakes.ENABLE_FORCED_SINGLE_CORE_PRECALC)
            {
                //  Precalculate all cells in the queue (do it in main thread)
                while (MyVoxelPrecalc.Tasks.Count > 0)
                {
                    MyVoxelPrecalcTaskItem newTask;
                    MyVoxelPrecalc.Tasks.TryDequeue(out newTask);
                    m_singleCoreTask.Precalc(newTask);
                }
            }
            else
            {
                for (int i = 0; i < MySandboxGame.NumberOfCores; i++)
                {
                    m_tasks[i] = Parallel.Start(m_precalWorks[i]);
                }

                for (int i = 0; i < MySandboxGame.NumberOfCores; i++)
                {
                    m_tasks[i].Wait();
                }
            }
        }
    }
}
