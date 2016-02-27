using ParallelTasks;
using System.Collections.Generic;

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
        internal readonly List<MyRenderContext> Contexts = new List<MyRenderContext>();
        internal readonly List<MyRenderingPass> Passes = new List<MyRenderingPass>();

        public WorkPriority Priority { get { return WorkPriority.Normal; } }
        public WorkOptions Options { get { return Parallel.DefaultOptions; } }

        public abstract void DoWork();

        protected void AddDeferredContext(MyRenderContext rc)
        {
            Contexts.Add(rc);
        }

        protected void AddPass(MyRenderingPass pass)
        {
            Passes.Add(pass);
        }

        internal virtual void Cleanup()
        {
            Contexts.Clear();

            foreach (var pass in Passes)
                MyObjectPoolManager.Deallocate(pass);
            Passes.Clear();
        }
    }
}
