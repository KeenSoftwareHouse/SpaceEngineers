using ParallelTasks;
using System.Collections.Generic;
using VRage.Render11.RenderContext;

namespace VRageRender
{
    struct MyRenderingWorkItem
    {
        internal MyRenderingPass Pass;
        internal List<MyRenderCullResultFlat> Renderables;
        internal int Begin;
        internal int End;
        internal MyRenderableProxy_2[] List2;
    }    

    abstract class MyRenderingWork : IPrioritizedWork
    {
        internal readonly List<MyRenderContext> RCs = new List<MyRenderContext>();
        internal readonly List<MyRenderingPass> Passes = new List<MyRenderingPass>();

        public WorkPriority Priority { get { return WorkPriority.VeryHigh; } }
        public WorkOptions Options { get { return Parallel.DefaultOptions; } }

        public abstract void DoWork(WorkData workData = null);

        protected void AddDeferredContext(MyRenderContext rc)
        {
            RCs.Add(rc);
        }

        protected void AddPass(MyRenderingPass pass)
        {
            Passes.Add(pass);
        }

        internal virtual void Cleanup()
        {
            RCs.Clear();

            foreach (var pass in Passes)
                MyObjectPoolManager.Deallocate(pass);
            Passes.Clear();
        }
    }
}
