using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Utils;

//  This class is used for measurements like drawn triangles, number of textures loaded, etc.
//  IMPORTANT: Use this class only for profiling / debuging. Don't use it for real game code.

namespace VRageRender
{
    public static class MyPerformanceCounter
    {
        public struct Timer
        {
            static Stopwatch m_timer;
            public static readonly Timer Empty = new Timer() { Runtime = 0, StartTime = long.MaxValue };

            static Timer()
            {
                m_timer = new Stopwatch();
                m_timer.Start();
            }

            public long StartTime;
            public long Runtime;

            public float RuntimeMs
            {
                get
                {
                    return (float)(Runtime / (double)Stopwatch.Frequency * 1000.0);
                }
            }

            public void Start()
            {
                //Debug.Assert(!IsRunning, "Timer is already running, timers are not reentrant");
                StartTime = m_timer.ElapsedTicks;
            }

            public void Stop()
            {
                Runtime += m_timer.ElapsedTicks - StartTime;
                StartTime = long.MaxValue;
            }

            private bool IsRunning { get { return StartTime != long.MaxValue; } }
        }

        public const int NoSplit = MyShadowConstants.NumSplits;

        //  These counters are "reseted" before every camera draw
        public class MyPerCameraDraw
        {
            // Custom timers which can be added at runtime, useful when profiling performance and using Edit&Continue
            public readonly Dictionary<string, Timer> CustomTimers = new Dictionary<string, Timer>(5);
            public readonly Dictionary<string, float> CustomCounters = new Dictionary<string, float>(5);

            private long m_gcMemory;

            public long GcMemory
            {
                get { return Interlocked.Read(ref m_gcMemory); }
                set { Interlocked.Exchange(ref m_gcMemory, value); }
            }

            readonly List<string> m_tmpKeys = new List<string>();

            public MyPerCameraDraw()
            {
                int lodCount = MyUtils.GetMaxValueFromEnum<MyLodTypeEnum>() + 1;
            }

            public float this[string name]
            {
                get
                {
                    float result;
                    if (!CustomCounters.TryGetValue(name, out result))
                        result = 0;
                    return result;
                }
                set
                {
                    CustomCounters[name] = value;
                }
            }

            public void SetCounter(string name, float count)
            {
                CustomCounters[name] = count;
            }

            public void StartTimer(string name)
            {
                //if (!AppCode.App.MySandboxGame.IsMainThread())
                //    return;

                Timer t;
                bool exists = CustomTimers.TryGetValue(name, out t);
                t.Start();
                CustomTimers[name] = t;
            }

            public void StopTimer(string name)
            {
                //if (!AppCode.App.MySandboxGame.IsMainThread())
                //    return;

                Timer t;
                if (CustomTimers.TryGetValue(name, out t))
                {
                    t.Stop();
                    CustomTimers[name] = t;
                }
            }

            public void Reset()
            {
                //CustomCounters.Clear();
                ClearCustomCounters();

                GcMemory = GC.GetTotalMemory(false);
            }

            public void ClearCustomCounters()
            {
                m_tmpKeys.Clear();
                //foreach (var counter in CustomCounters)
                //{
                //    m_tmpKeys.Add(counter.Key);
                //}
                //foreach (var key in m_tmpKeys)
                //{
                //    CustomCounters[key] = -1;
                //}
            }

            public List<string> SortedCounterKeys
            {
                get
                {
                    m_tmpKeys.Clear();
                    //foreach (var counter in CustomCounters)
                    //{
                    //    m_tmpKeys.Add(counter.Key);
                    //}
                    //m_tmpKeys.Sort();
                    return m_tmpKeys;
                }
            }
        }

        //  These counters are never "reseted", they keep increasing during the whole app lifetime
        public class MyPerAppLifetime
        {
            //  Texture2D loading statistics
            public int Textures2DCount;            //  Total number of all loaded textures since application start (it will increase after game-screen load)
            public int Textures2DSizeInPixels;     //  Total number of pixels in all loaded textures (it will increase after game-screen load)
            public double Textures2DSizeInMb;         //  Total size in Mb of all loaded textures (it will increase after game-screen load)
            public int NonMipMappedTexturesCount;
            public int NonDxtCompressedTexturesCount;
            public int DxtCompressedTexturesCount;

            //  TextureCube loading statistics
            public int TextureCubesCount;            //  Total number of all loaded textures since application start (it will increase after game-screen load)
            public int TextureCubesSizeInPixels;     //  Total number of pixels in all loaded textures (it will increase after game-screen load)
            public double TextureCubesSizeInMb;         //  Total size in Mb of all loaded textures (it will increase after game-screen load)

            //  Model loading statistics (this is XNA's generic model - not the one we use)
            public int ModelsCount;

            //  MyModel loading statistics (this is our custom model class)
            public int MyModelsCount;
            public int MyModelsMeshesCount;
            public int MyModelsVertexesCount;
            public int MyModelsTrianglesCount;

            // Sizes of model and voxel buffers
            public int ModelVertexBuffersSize;          // Size of model vertex buffers in bytes
            public int ModelIndexBuffersSize;           // Size of model index buffers in bytes
            public int VoxelVertexBuffersSize;          // Size of voxel vertex buffers in bytes
            public int VoxelIndexBuffersSize;           // Size of voxel index buffers in bytes

            public int MyModelsFilesSize; // Size of loaded model files in bytes

            public List<string> LoadedTextureFiles = new List<string>();
            public List<string> LoadedModelFiles = new List<string>();
        }


        static MyPerCameraDraw PerCameraDraw0 = new MyPerCameraDraw();
        static MyPerCameraDraw PerCameraDraw1 = new MyPerCameraDraw();

        public static MyPerCameraDraw PerCameraDrawRead = PerCameraDraw0;
        public static MyPerCameraDraw PerCameraDrawWrite = PerCameraDraw0;

        public static MyPerAppLifetime PerAppLifetime = new MyPerAppLifetime();
        public static bool LogFiles = false;

        public static void Restart(string name)
        {
            MyPerformanceCounter.PerCameraDrawRead.CustomTimers.Remove(name);
            MyPerformanceCounter.PerCameraDrawWrite.CustomTimers.Remove(name);
            MyPerformanceCounter.PerCameraDrawRead.StartTimer(name);
            MyPerformanceCounter.PerCameraDrawWrite.StartTimer(name);
        }

        public static void Stop(string name)
        {
            MyPerformanceCounter.PerCameraDrawRead.StopTimer(name);
            MyPerformanceCounter.PerCameraDrawWrite.StopTimer(name);
        }
        

        internal static void SwitchCounters()
        {
            if (PerCameraDrawRead == PerCameraDraw0)
            {
                PerCameraDrawRead = PerCameraDraw1;
                PerCameraDrawWrite = PerCameraDraw0;
            }
            else
            {
                PerCameraDrawRead = PerCameraDraw0;
                PerCameraDrawWrite = PerCameraDraw1;
            }
        }
    }
}
