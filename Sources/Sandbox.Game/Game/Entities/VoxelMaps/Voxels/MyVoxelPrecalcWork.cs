using ParallelTasks;

//  This thread is waiting until signaled, then processes all cells waiting in the queue, signals main thread that he is done and wait again for next signal

namespace Sandbox.Game.Voxels
{
    class MyVoxelPrecalcWork : IWork
    {
        IMyIsoMesher m_mesher;

        public MyVoxelPrecalcWork(IMyIsoMesher mesher)
        {
            m_mesher = mesher;
        }
        
        public void DoWork()
        {            
            while (true)
            {
                MyVoxelPrecalcTaskItem newTask;
                if (MyVoxelPrecalc.Tasks.TryDequeue(out newTask))
                {
                    //  If there is task, then calculate it
                    m_mesher.Precalc(newTask);
                }
                else
                {
                    break;
                }
            } 
        }

        public WorkOptions Options
        {
            get { return new WorkOptions() { MaximumThreads = 1 }; }
        }
    }
}
