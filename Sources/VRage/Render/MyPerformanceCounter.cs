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
            public int RenderCellsInFrustum_LOD0;        //  Count of render cells visible and drawn in the frustum (LOD0)
            public int RenderCellsInFrustum_LOD1;        //  Count of render cells visible and drawn in the frustum (LOD1)
            public int VoxelTrianglesInFrustum_LOD0;          //  Count of really drawn voxel triangles visible and drawn in the frustum (if triangleVertexes is multi-textured, in this number is more times)
            public int VoxelTrianglesInFrustum_LOD1;          //  Count of really drawn voxel triangles visible and drawn in the frustum (if triangleVertexes is multi-textured, in this number is more times)
            public int EntitiesRendered;             //  Count of models visible and drawn in the frustum        
            public int ModelTrianglesInFrustum_LOD0;     //  Count of model triangles visible and drawn in the frustum LOD0
            public int ModelTrianglesInFrustum_LOD1;     //  Count of model triangles visible and drawn in the frustum LOD1   
            public int DecalsForVoxelsInFrustum;         //  Count of voxel decals visible and drawn in the frustum
            public int DecalsForEntitiesInFrustum;    //  Count of phys object decals visible and drawn in the frustum
            public int DecalsForCockipGlassInFrustum;    //  Count of glass cockpit decals visible and drawn in the frustum
            public int BillboardsInFrustum;              //  Count of billboards visible and drawn in the frustum
            public int BillboardsDrawCalls;              //  Count of billboard draw calls in the frustum (usually due to switching different particle textures or shaders)
            public int BillboardsSorted;              //  Count of sorted billboards
            public int OldParticlesInFrustum;               //  Count of particles visible and drawn in the frustum
            public int NewParticlesCount;               //  Count of new animated particles visible and drawn in the frustum
            public int ParticleEffectsTotal;            // Count of all living particle effects
            public int ParticleEffectsDrawn;            // Count of drawn particle effects
            public int EntitiesOccluded;                // Count of entities occluded by hw occ. queries
            public int QueriesCount;                    // Count of occlusion queries issued to GC
            public int LightsCount;                     //Count of lights rendered in frame
            public int RenderElementsInFrustum;         //Count of rendered elements in frustum
            public int RenderElementsIBChanges;         //Count of IB changes per frame
            public int RenderElementsInShadows;         //Count of rendered elements for shadows
            public int[] ShadowDrawCalls;
            public int TotalDrawCalls;

            // Per lod
            public int[] MaterialChanges;
            public int[] TechniqueChanges;
            public int[] VertexBufferChanges;
            public int[] EntityChanges;

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
                ShadowDrawCalls = new int[NoSplit + 1];

                int lodCount = MyUtils.GetMaxValueFromEnum<MyLodTypeEnum>() + 1;

                MaterialChanges = new int[lodCount];
                TechniqueChanges = new int[lodCount];
                VertexBufferChanges = new int[lodCount];
                EntityChanges = new int[lodCount];
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
                RenderCellsInFrustum_LOD1 = 0;
                RenderCellsInFrustum_LOD0 = 0;
                VoxelTrianglesInFrustum_LOD0 = 0;
                VoxelTrianglesInFrustum_LOD1 = 0;
                EntitiesRendered = 0;
                ModelTrianglesInFrustum_LOD0 = 0;
                ModelTrianglesInFrustum_LOD1 = 0;
                DecalsForVoxelsInFrustum = 0;
                DecalsForEntitiesInFrustum = 0;
                DecalsForCockipGlassInFrustum = 0;
                BillboardsInFrustum = 0;
                BillboardsDrawCalls = 0;
                BillboardsSorted = 0;
                OldParticlesInFrustum = 0;
                NewParticlesCount = 0;

                QueriesCount = 0;
                LightsCount = 0;
                RenderElementsInFrustum = 0;
                RenderElementsIBChanges = 0;
                RenderElementsInShadows = 0;

                //CustomCounters.Clear();
                ClearCustomCounters();

                GcMemory = GC.GetTotalMemory(false);

                for (int i = 0; i < ShadowDrawCalls.Length; i++)
                {
                    ShadowDrawCalls[i] = 0;
                }

                for (int i = 0; i < MaterialChanges.Length; i++)
                {
                    MaterialChanges[i] = 0;
                    TechniqueChanges[i] = 0;
                    VertexBufferChanges[i] = 0;
                    EntityChanges[i] = 0;
                }

                TotalDrawCalls = 0;
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

        public class MyPerCameraDraw11
        {
            //
            public int RenderableObjectsNum;
            public int ViewFrustumObjectsNum;
            public int[] ShadowCascadeObjectsNum;

            //
            public int MeshesDrawn;
            public int SubmeshesDrawn;
            public int ObjectConstantsChanges;
            public int MaterialConstantsChanges;
            public int TrianglesDrawn;
            public int InstancesDrawn;

            // api calls
            public int Draw;
            public int DrawInstanced;
            public int DrawIndexed;
            public int DrawIndexedInstanced;
            public int DrawAuto;
            public int SetVB;
            public int SetIB;
            public int SetIL;
            public int SetVS;
            public int SetPS;
            public int SetGS;
            public int SetCB;
            public int SetRasterizerState;
            public int SetBlendState;
            public int BindShaderResources;

            public void Reset()
            {
				CheckArrays();
                RenderableObjectsNum = 0;
                ViewFrustumObjectsNum = 0;
                

                MeshesDrawn = 0;
                SubmeshesDrawn = 0;
                ObjectConstantsChanges = 0;
                MaterialConstantsChanges = 0;
                TrianglesDrawn = 0;
                InstancesDrawn = 0;

                Draw = 0;
                DrawInstanced = 0;
                DrawIndexed = 0;
                DrawIndexedInstanced = 0;
                DrawAuto = 0;
                SetVB = 0;
                SetIB = 0;
                SetIL = 0;
                SetVS = 0;
                SetPS = 0;
                SetGS = 0;
                SetCB = 0;
                SetRasterizerState = 0;
                SetBlendState = 0;
                BindShaderResources = 0;
            }

			void CheckArrays()
			{
				if(ShadowCascadeObjectsNum == null || ShadowCascadeObjectsNum.Length != MyRenderProxy.Settings.ShadowCascadeCount)
					ShadowCascadeObjectsNum = new int[MyRenderProxy.Settings.ShadowCascadeCount];
			}
			public MyPerCameraDraw11()
			{
				CheckArrays();
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

        static MyPerCameraDraw11 PerCameraDraw11_0 = new MyPerCameraDraw11();
        static MyPerCameraDraw11 PerCameraDraw11_1 = new MyPerCameraDraw11();

        public static MyPerCameraDraw PerCameraDrawRead = PerCameraDraw0;
        public static MyPerCameraDraw PerCameraDrawWrite = PerCameraDraw0;

        public static MyPerCameraDraw11 PerCameraDraw11Read = PerCameraDraw11_0;
        public static MyPerCameraDraw11 PerCameraDraw11Write = PerCameraDraw11_0;

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

                PerCameraDraw11Read = PerCameraDraw11_1;
                PerCameraDraw11Write = PerCameraDraw11_0;
            }
            else
            {
                PerCameraDrawRead = PerCameraDraw0;
                PerCameraDrawWrite = PerCameraDraw1;

                PerCameraDraw11Read = PerCameraDraw11_0;
                PerCameraDraw11Write = PerCameraDraw11_1;
            }
        }
    }
}
