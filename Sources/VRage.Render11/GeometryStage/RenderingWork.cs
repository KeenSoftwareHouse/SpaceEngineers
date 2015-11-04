using ParallelTasks;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRageRender
{
    class MyRenderingWork : IPrioritizedWork
    {
        internal List<MyRenderContext> Contexts = new List<MyRenderContext>();
        internal List<MyRenderingPass> Passes = new List<MyRenderingPass>();

        // override interface "virtual" methods, since they are not inherited properly by default 
        public virtual void DoWork()
        {
            throw new NotImplementedException();
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.Normal; }
        }

        public WorkOptions Options
        {
            get
            {
                return Parallel.DefaultOptions;
            }
        }

        internal void AddDeferredContext(MyRenderContext rc)
        {
            Contexts.Add(rc);
        }

        internal void AddPass(MyRenderingPass pass)
        {
            Passes.Add(pass);
        }
    }

    class MyRenderingWork_LoopObjectThenPass : MyRenderingWork
    {
        MyRenderCullResult[] m_renderables;
        int m_from;
        int m_to;

        internal MyRenderingWork_LoopObjectThenPass(MyRenderCullResult[] list, int from, int to, List<MyRenderingPass> passes)
        {
            m_renderables = list;
            m_from = from;
            m_to = to;

            Passes = passes;
            foreach(var pass in Passes)
            {
                AddDeferredContext(pass.m_RC);
            }
        }

        public void Run()
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("DoRenderWork");

            for (int i = 0; i < Passes.Count; i++)
            {
                Passes[i].Begin();
            }

            int from = m_from;
            int to = m_to;
            for (int i = from; i < to; i++)
            {
                var proxy = m_renderables[i].RenderProxy;

                for (int p = 0; p < Passes.Count; p++)
                {
                    if ((Passes[p].ProcessingMask & m_renderables[i].ProcessingMask.Data) > 0)
                    {
                        Passes[p].FeedProfiler(m_renderables[i].SortKey);
                        Passes[p].RecordCommands(proxy);
                    }
                }
            }

            for (int i = 0; i < Passes.Count; i++)
            {
                Passes[i].End();
            }

            for (int p = 0; p < Passes.Count; p++)
            {
                Passes[p].RC.Finish();
            }

            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        public override void DoWork()
        {
            Run();
        }
    }

    struct MyRenderingWorkItem
    {
        internal MyRenderingPass Pass;
        internal List<MyRenderCullResultFlat> Renderables;
        internal int Begin;
        internal int End;
        internal MyRenderableProxy_2 [] List2;
    }

    class MyRenderingWork_LoopPassThenObject : MyRenderingWork
    {
        internal List<MyRenderingWorkItem> m_subworks = new List<MyRenderingWorkItem>();
        internal bool m_isDeferred = false;

        internal MyRenderingWork_LoopPassThenObject(List<MyRenderingWorkItem> subworks)
        {
            m_isDeferred = false;

            m_subworks = subworks;

            foreach (var subwork in subworks)
            {
                AddPass(subwork.Pass);
            }
        }

        internal MyRenderingWork_LoopPassThenObject(MyRenderContext deferredRc, List<MyRenderingWorkItem> subworks)
        {
            m_isDeferred = true;

            m_subworks = subworks;

            AddDeferredContext(deferredRc);
            foreach(var subwork in subworks)
            {
                AddPass(subwork.Pass);
                subwork.Pass.SetContext(deferredRc);
            }
        }

        public void Run()
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("DoRenderWork");

            foreach(var subwork in m_subworks)
            {
                subwork.Pass.Begin();

                for(int i=subwork.Begin; i<subwork.End; i++)
                {
                    subwork.Pass.FeedProfiler(subwork.Renderables[i].SortKey);
                    subwork.Pass.RecordCommands(subwork.Renderables[i].RenderProxy);
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

            if(m_isDeferred && m_subworks.Count > 0)
            {
                m_subworks[0].Pass.RC.Finish();
            }
            
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        public override void DoWork()
        {
            Run();
        }
    }
}
