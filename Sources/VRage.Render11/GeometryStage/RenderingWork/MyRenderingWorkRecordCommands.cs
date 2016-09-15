using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Profiler;
using VRage.Render11.RenderContext;

namespace VRageRender
{
    [PooledObject(poolPreallocationSize: 2)]
#if XB1
    class MyRenderingWorkRecordCommands : MyRenderingWork, IMyPooledObjectCleaner
#else // !XB1
    class MyRenderingWorkRecordCommands : MyRenderingWork
#endif // !XB1
    {
        internal readonly List<MyRenderingWorkItem> m_subworks = new List<MyRenderingWorkItem>();
        internal bool m_isDeferred = false;

        public MyRenderingWorkRecordCommands() { }

        internal void Init(List<MyRenderingWorkItem> subworks)
        {
            m_isDeferred = false;

            m_subworks.AddList(subworks);
            foreach (var subwork in subworks)
            {
                AddPass(subwork.Pass);
            }
        }

        internal void Init(MyRenderContext deferredRc, List<MyRenderingWorkItem> subworks)
        {
            m_isDeferred = true;

            m_subworks.AddList(subworks);

            AddDeferredContext(deferredRc);
            foreach (var subwork in subworks)
            {
                AddPass(subwork.Pass);
                subwork.Pass.SetContext(deferredRc);
            }
        }

        public override void DoWork(ParallelTasks.WorkData workData = null)
        {
            ProfilerShort.Begin("MyRenderingWorkRecordCommands::DoWork");

            foreach (var subwork in m_subworks)
            {
                subwork.Pass.Elapsed = 0;

                if ((subwork.Begin < subwork.End) || (subwork.List2 != null && subwork.List2.Length > 0))
                {
                    long Started = Stopwatch.GetTimestamp();
                    subwork.Pass.Begin();

                    for (int subworkIndex = subwork.Begin; subworkIndex < subwork.End; subworkIndex++)
                    {
                        subwork.Pass.FeedProfiler(subwork.Renderables[subworkIndex].SortKey);
                        subwork.Pass.RecordCommands(subwork.Renderables[subworkIndex].RenderProxy);
                    }

                    if (subwork.List2 != null)
                    {
                        for (int i = 0; i < subwork.List2.Length; i++)
                        {
                            subwork.Pass.RecordCommands(ref subwork.List2[i]);
                        }
                    }

                    subwork.Pass.End();
                    subwork.Pass.Elapsed = Stopwatch.GetTimestamp() - Started;
                }
            }

            //if (m_isDeferred && m_subworks.Count > 0)
            //{
            //    m_subworks[0].Pass.RC.Finish();
            //}

            ProfilerShort.End();
        }

#if XB1
        public void ObjectCleaner()
        {
            Cleanup();
        }
#else // !XB1
        [PooledObjectCleaner]
        public static void Cleanup(MyRenderingWorkRecordCommands renderWork)
        {
            renderWork.Cleanup();
        }
#endif // !XB1

        internal override void Cleanup()
        {
            base.Cleanup();

            m_subworks.Clear();
            m_isDeferred = false;
        }
    }
}
