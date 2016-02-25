using System.Collections.Generic;
using VRage;

namespace VRageRender
{
    [PooledObject(poolPreallocationSize: 2)]
    class MyRenderingWorkRecordCommands : MyRenderingWork
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

        public override void DoWork()
        {
            ProfilerShort.Begin("MyRenderingWorkRecordCommands::DoWork");

            foreach (var subwork in m_subworks)
            {
                if ((subwork.Begin < subwork.End) || (subwork.List2 != null && subwork.List2.Length > 0))
                {
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
                }
            }

            if (m_isDeferred && m_subworks.Count > 0)
            {
                m_subworks[0].Pass.RC.Finish();
            }

            ProfilerShort.End();
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyRenderingWorkRecordCommands renderWork)
        {
            renderWork.Cleanup();
        }

        internal override void Cleanup()
        {
            base.Cleanup();

            m_subworks.Clear();
            m_isDeferred = false;
        }
    }
}
