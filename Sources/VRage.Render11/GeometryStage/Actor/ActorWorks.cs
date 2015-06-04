using ParallelTasks;

namespace VRageRender
{
    class MyActorsUpdatingWork : IPrioritizedWork
    {
        MyRenderableComponent[] m_array;
        int m_start;
        int m_end;

        internal MyActorsUpdatingWork(MyRenderableComponent[] array, int start, int end)
        {
            m_array = array;
            m_start = start;
            m_end = end;
        }

        public WorkPriority Priority => WorkPriority.Normal;

        public void DoWork()
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("DoActorUpdateWork");

            for (int i = m_start; i < m_end; i++)
            {
                var renderable = m_array[i];
                if (renderable.IsVisible)
                {
                    renderable.OnFrameUpdate();
                }
            }

            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        public WorkOptions Options => Parallel.DefaultOptions;
    }
}
