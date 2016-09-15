using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

using VRageMath;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingFrustum = VRageMath.BoundingFrustum;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using VRage.Generics;

namespace VRageRender
{
//    // per level lru cache
//    class MyFoliageCacheLevel
//    {
//        enum MyCacheStateEnum
//        {
//            Hot,
//            Warm,
//            Cold
//        }

//        internal static readonly int LEVELS = 3;
//        static readonly int[] LEVEL_CACHE_TRIMMING = new[] { 4, 8, 32 };
//        static readonly int[] GRASS_STREAM_LIMIT = new[] { 400000, 100000, 25000 };
//        static readonly int[] BILLBOARDS_STREAM_LIMIT = new[] { 1, 1, 1 };

//        #region LevelData

//        LinkedListNode<MyFoliageCacheLevel> m_node;
//        MyCacheStateEnum m_state;
//        internal bool IsRemoved { get { return m_node == null; } }

//        internal bool Dirty;
//        int m_level;
//        internal MyVertexBuffer m_grassSO;
//        internal MyVertexBuffer m_billboardsSO;

//        #endregion

//        private unsafe MyFoliageCacheLevel(int level)
//        {
//            m_level = level;

//            var grassStride = sizeof(Half4) + sizeof(uint);
//            m_grassSO = new MyVertexBuffer(grassStride * GRASS_STREAM_LIMIT[level], ResourceUsage.Default, null, BindFlags.VertexBuffer | BindFlags.StreamOutput);
//            m_grassSO.Stride = grassStride;
//            m_grassSO.SetDebugName("grass stream output buffer");

//            Dirty = true;

//            var billboardStride = 1;
//        }

//        internal void Dispose()
//        {
//            if(m_grassSO != null)
//            {
//                m_grassSO.Dispose();
//                m_grassSO = null;
//            }
//            if (m_billboardsSO != null)
//            {
//                m_billboardsSO.Dispose();
//                m_billboardsSO = null;
//            }
//        }

//        static MyFoliageCacheLevel()
//        {
//            m_cache = new LinkedList<MyFoliageCacheLevel>[LEVELS];

//            for(int i=0; i<LEVELS; i++)
//            {
//                m_cache[i] = new LinkedList<MyFoliageCacheLevel>();
//            }
//        }

//        internal void Bump()
//        {
//            m_state = MyCacheStateEnum.Hot;

//            if(m_node != null)
//            { 
//                m_cache[m_level].Remove(m_node);
//            }
//            m_node = m_cache[m_level].AddLast(this);
//        }

//        internal unsafe static MyFoliageCacheLevel Allocate(int level)
//        {
//            var levelCache = m_cache[level];
//            MyFoliageCacheLevel value = null;
//            if (levelCache.Count > 0 && levelCache.First.Value.IsRemoved)
//            {
//                value = levelCache.First.Value;
//            }
//            else
//            {
//                value = new MyFoliageCacheLevel(level);
//            }
//            value.Bump();
//            value.Dirty = true;

//            return value;
//        }
        
//        internal static void Sweep()
//        {
//            for(int l=0; l<LEVELS; l++)
//            {
//                int levelCacheSizeLimit = LEVEL_CACHE_TRIMMING[l];

//                for(var it = m_cache[l].First; it != null; it = it.Next)
//                {
//                    // make warm cold
//                    if(it.Value.m_state == MyCacheStateEnum.Warm)
//                    {
//                        it.Value.m_state = MyCacheStateEnum.Cold;
//                    }
//                    // make hot warm
//                    if (it.Value.m_state == MyCacheStateEnum.Hot)
//                    {
//                        it.Value.m_state = MyCacheStateEnum.Warm;
//                    }
//                }

//                // remove LRU colds to trim size
//                while(m_cache[l].Count > levelCacheSizeLimit && m_cache[l].First.Value.m_state == MyCacheStateEnum.Cold)
//                {
//                    m_cache[l].First.Value.Remove();
//                    m_cache[l].RemoveFirst();
//                }
//            }
//        }

//        void Remove()
//        {
//            Dirty = true;
//            m_node = null;
//            Dispose();
//        }

//        internal static void ClearAllLevels()
//        {
//            for(int l=0; l<LEVELS; l++)
//            {
//                foreach(var v in m_cache[l])
//                {
//                    v.Remove();
//                }
//                m_cache[l].Clear();
//            }
//        }

//        static LinkedList<MyFoliageCacheLevel> [] m_cache;
//    }

//    class MyFoliageComponent : MySceneObjectComponent
//    {
//        internal int m_proxyID;
//        MyFoliageCacheLevel [] m_levels;

//        internal MyVertexBuffer GetGrassSO(int l)
//        {
//            return m_levels[l].m_grassSO;
//        }

//        internal bool GetLevelStatusAndPrepare(int l)
//        {
//            bool streamingNeeded = false;

//            if(m_levels[l] == null || m_levels[l].IsRemoved)
//            {
//                m_levels[l] = MyFoliageCacheLevel.Allocate(l);
//                streamingNeeded = true;
//            }
//            else
//            {
//                streamingNeeded = m_levels[l].Dirty;
//                m_levels[l].Bump();
//            }

//            return streamingNeeded;
//        }

//        internal void SetValid(int l)
//        {
//            m_levels[l].Dirty = false;
//        }

//        internal sealed override void Attach(MySceneObject obj)
//        {
//            base.Attach(obj);
            
//            for(int i=0; i<m_levels.Length; i++)
//                if(m_levels[i] != null)
//                    m_levels[i].Dirty = true;
//        }

//        internal sealed override void SetAabb(ref VRageMath.BoundingBox bb)
//        {
//            if (m_proxyID == -1)
//                m_proxyID = MyFoliageRenderer.m_foliageTree.AddProxy(ref bb, this, 0);
//            else
//                MyFoliageRenderer.m_foliageTree.MoveProxy(m_proxyID, ref bb, Vector3.Zero);
//        }

//        static MyObjectFactory<MyFoliageComponent> m_factory = new MyObjectFactory<MyFoliageComponent>(50);

//        internal static MyFoliageComponent Create()
//        {
//            var item = m_factory.GetOrCreate();
//            item.m_proxyID = -1;
//            item.m_levels = new MyFoliageCacheLevel[MyFoliageCacheLevel.LEVELS];
//            return item;
//        }

//        internal static void Remove(MyFoliageComponent component)
//        {
//            m_factory.Remove(component);
//        }

//        internal static void RemoveAll()
//        {
//            m_factory.Clear();
//        }
//    }

//    class MyObjectFactory<T> where T : class, new()
//    {
//        MyObjectsPool<T> m_pool;

//        internal MyObjectFactory(int size)
//        {
//            m_pool = new MyObjectsPool<T>(size);
//        }

//        internal T GetOrCreate()
//        {
//            T item;
//            m_pool.AllocateOrCreate(out item);
//            return item;
//        }

//        internal void Remove(T item)
//        {
//            m_pool.Deallocate(item);
//        }

//        internal void Clear()
//        {
//            m_pool.DeallocateAll();
//        }
//    }

//    class MyFoliageRenderer
//    {
//        static MyFoliageStreamingPass [] m_foliageStreaming;
//        static MyFoliagePass m_foliagePass;

//        internal static void GetLodDistances(int l, out float near, out float far)
//        {
//            float[] list = new[] { MyRender.Settings.FoliageLod0Distance, MyRender.Settings.FoliageLod1Distance, MyRender.Settings.FoliageLod2Distance, MyRender.Settings.FoliageLod3Distance };

//            near = 1f;
//            far = list[l];

//            if (list[l] < 1f)
//                return;

//            if(l>0)
//            { 
//                for (int i = l-1; l >= 0; l--)
//                {
//                    if (list[i] > 1f)
//                    {
//                        near = list[i];
//                        break;
//                    }
//                }
//            }
//        }

//        internal static MyDynamicAABBTree m_foliageTree = new MyDynamicAABBTree(Vector3.Zero);
//        // each frame uses old one - swapping reduces need to copy data
//        static Dictionary<MyFoliageComponent, int> m_marked0 = new Dictionary<MyFoliageComponent, int>();
//        static Dictionary<MyFoliageComponent, int> m_marked1 = new Dictionary<MyFoliageComponent, int>();
//        static int m_markedCurrentIndex = 0;
//        static Dictionary<int, List<MyFoliageComponent>> m_reverseDict = new Dictionary<int, List<MyFoliageComponent>>();
//        static List<MyFoliageComponent> m_list = new List<MyFoliageComponent>();
//        static List<BoundingFrustum> m_frustums = new List<BoundingFrustum>();

//        internal static void RemoveAll()
//        {
//            m_foliageTree.Clear();
//            MyFoliageCacheLevel.ClearAllLevels();
//            MyFoliageComponent.RemoveAll();
//        }

//        internal static void Rebuild()
//        {
//            MyFoliageCacheLevel.ClearAllLevels();
//        }

//        static MyFoliageRenderer()
//        {
//            for(int l=0; l< MyFoliageCacheLevel.LEVELS; l++)
//            {
//                m_reverseDict[l] = new List<MyFoliageComponent>();
//            }
//            m_foliageStreaming = new MyFoliageStreamingPass[MyFoliageCacheLevel.LEVELS];
//        }

//        internal static void Init()
//        {
//            for (int l = 0; l < MyFoliageCacheLevel.LEVELS; l++)
//            {
//                m_foliageStreaming[l] = new MyFoliageStreamingPass(l);
//            }
            
//            m_foliagePass = new MyFoliagePass();
//        }

//        static Matrix m_frozenView;

//        static Color GetLevelDebugColor(int l)
//        {
//            switch (l)
//            {
//                case 0:
//                    return Color.Red;
//                case 1:
//                    return Color.Green;
//                case 2:
//                    return Color.Blue;
//                default:
//                    return Color.Purple;
//            }
//        }

//        internal static void RecreateFrustums()
//        {
//            if (!MyRender.Settings.FreezeFoliageViewer)
//            {
//                m_frozenView = MyRender11.Environment.View;
//            }

//            m_frustums.Clear();
//            int L = MyFoliageCacheLevel.LEVELS;

//            for (int l = 0; l < L; l++)
//            {
//                float near;
//                float far;
//                GetLodDistances(l, out near, out far);

//                if (near > far)
//                    m_frustums.Add(null);
//                else
//                    m_frustums.Add(new BoundingFrustum((MyRender.Settings.FreezeFoliageViewer ? m_frozenView : MyEnvironment.View) *
//                        Matrix.CreateFromPerspectiveFieldOfView(ref MyRender11.Environment.Projection, near, far)));
//            }
//        }

//        internal static void Update()
//        {
//            int L = MyFoliageCacheLevel.LEVELS;

//            MyFoliageCacheLevel.Sweep();

//            var markedCurrent = m_markedCurrentIndex == 0 ? m_marked0 : m_marked1;
//            var markedPrev = m_markedCurrentIndex == 0 ? m_marked1 : m_marked0;
//            markedCurrent.Clear();

//            RecreateFrustums();

//            for(int l=0; l<L; l++)
//            {
//                if (m_frustums[l] == null) 
//                    continue;

//                m_foliageStreaming[l].SetupPipeline();
//                var frustum = m_frustums[l];
//                m_foliageTree.OverlapAllFrustumConservative(ref frustum, m_list, 0);

//                for(int i=0; i<m_list.Count; i++)
//                {
//                    var e = m_list[i];

//                    int current;
//                    int prev;
//                    if (!markedCurrent.TryGetValue(e, out current))
//                    {
//                        markedCurrent[e] = l;

//                        bool streamingNeeded = e.GetLevelStatusAndPrepare(l);
//                        if (streamingNeeded)
//                            m_foliageStreaming[l].RecordCommands(e.Parent);
//                        e.SetValid(l);
//                    }
//                    else if (markedPrev.ContainsKey(e) && markedPrev[e] == l)
//                    {
//                        markedCurrent[e] = l;

//                        // well we could skip caching lod for transition but it is simpler this way 
//                        bool streamingNeeded = e.GetLevelStatusAndPrepare(l);
//                        if (streamingNeeded)
//                            m_foliageStreaming[l].RecordCommands(e.Parent);
//                        e.SetValid(l);
//                    }
//                }

//                m_foliageStreaming[l].CleanupPipeline();
//                var cl = m_foliageStreaming[l].End();
//                MyRender.Context.ExecuteCommandList(cl, false);
//                cl.Dispose();
//            }

//            if(MyRender.Settings.EnableFoliageDebug && MyRender.Settings.FreezeFoliageViewer)
//            {
//                var batch = MyLinesRenderer.CreateBatch();
//                for (int l = 0; l < L; l++)
//                {
//                    if(m_frustums[l] != null)
//                    {
//                        batch.AddFrustum(m_frustums[l], Color.Yellow);
//                        MyPrimitivesRenderer.DrawFrustum(m_frustums[l], GetLevelDebugColor(l), 0.5f);
//                    }
//                }
//                batch.Commit();
//            }
//        }

//        internal static void Render()
//        {
//            var markedCurrent = m_markedCurrentIndex == 0 ? m_marked0 : m_marked1;
//            var markedPrev = m_markedCurrentIndex == 0 ? m_marked1 : m_marked0;
//            m_markedCurrentIndex = (m_markedCurrentIndex + 1) % 2;

//            int L = MyFoliageCacheLevel.LEVELS;
//            for (int l = 0; l < L; l++)
//            {
//                m_reverseDict[l].Clear();
//            }

//            foreach (var kv in markedCurrent)
//            {
//                m_reverseDict[kv.Value].Add(kv.Key);
//            }

//            foreach(var kv in m_reverseDict)
//            {
//                m_foliagePass.Level = kv.Key;
//                m_foliagePass.Shape = MyFloraShapeEnum.Billboard_X;

//                m_foliagePass.ViewProjection = MyRender11.Environment.View * MyRender11.Environment.Projection;
//                m_foliagePass.Viewport = new MyViewport(MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);
//                //m_foliagePass.DepthBuffer = MyRender.MainGbuffer.DepthBuffer.DepthStencil;
//                //m_foliagePass.RTs = MyRender.MainGbuffer.GbufferTargets;

//                m_foliagePass.TransferData();
//                m_foliagePass.SetupPipeline();

//                var list = kv.Value;
//                for (int i = 0; i < list.Count; i++ )
//                {
//                    m_foliagePass.RecordCommands(list[i].Parent);
//                }

//                //m_foliagePass.CleanupPipeline();

//                var cl = m_foliagePass.End();
//                MyRender.Context.ExecuteCommandList(cl, false);
//                cl.Dispose();
//            }

//            if (MyRender.Settings.EnableFoliageDebug)
//            {
//                var batch = MyLinesRenderer.CreateBatch();
//                var keys = new int[] { 3,2,1,0 };

//                foreach (var key in keys)
//                {
//                    if (!m_reverseDict.ContainsKey(key)) continue;
//                    Color color = GetLevelDebugColor(key);
                    
//                    var list = m_reverseDict[key];
//                    for (int i = 0; i < list.Count; i++)
//                    {
//                        batch.AddBoundingBox(m_foliageTree.GetAabb(list[i].m_proxyID), color);
//                    }
//                }
//                batch.Commit();
//            }
//        }
//    }

//    class MySceneObjectComponent
//    {
//        protected MySceneObject m_sceneObject;

//        internal MySceneObject Parent { get { return m_sceneObject; } }

//        internal virtual void SetAabb(ref BoundingBox bb) { }

//        internal virtual void Attach(MySceneObject obj)
//        {
//            m_sceneObject = obj;
//        }

//        protected MySceneObjectComponent()
//        {
//        }
//    }

//    enum MySceneComponentEnum
//    {
//        Foliage
//    }

//    partial class MySceneObject
//    {
//        internal MyFoliageComponent m_foliage;

//        List<MySceneObjectComponent> m_components = new List<MySceneObjectComponent>();

//        internal void AttachComponent(MySceneComponentEnum type)
//        {
//            MySceneObjectComponent component = null;

//            switch(type)
//            {
//                case MySceneComponentEnum.Foliage:
//                    if(m_foliage == null)
//                    { 
//                        m_foliage = MyFoliageComponent.Create();
//                        m_components.Add(m_foliage);
//                    }
//                    m_foliage.SetAabb(ref m_spatial.m_aabb);
//                    component = m_foliage;
//                    break;
//            }

//            component.Attach(this);
//        }

//        // kind of callback
//        internal void OnAabbUpdate(ref BoundingBox bb)
//        {
//            foreach(var c in m_components)
//            {
//                c.SetAabb(ref bb);
//            }
//        }
//    }
}
