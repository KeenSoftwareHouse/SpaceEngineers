using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("VRage", "Crash tests")]
    class MyGuiScreenDebugCrashTests : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugCrashTests()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCrashTests";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddCaption("Crash tests", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddButton(new StringBuilder("Exception in update thread."), onClick: UpdateThreadException);
            AddButton(new StringBuilder("Exception in render thread."), onClick: RenderThreadException);
            AddButton(new StringBuilder("Exception in worker thread."), onClick: WorkerThreadException);
            AddButton(new StringBuilder("Main thread invoked exception."), onClick: MainThreadInvokedException);
            AddButton(new StringBuilder("Update thread out of memory."), onClick: OutOfMemoryUpdateException);
            AddButton(new StringBuilder("Worker thread out of memory."), onClick: OutOfMemoryWorkerException);
            AddButton(new StringBuilder("Havok access violation."), onClick: HavokAccessViolationException);
            AddButton(new StringBuilder("Divide by zero."), onClick: DivideByZero);
            AddButton(new StringBuilder("Unsupported GPU."), onClick: UnsupportedGPU);

            m_currentPosition.Y += 0.01f;
        }

        void UnsupportedGPU(MyGuiControlButton sender)
        {
            // DXGI_ERROR_UNSUPPORTED
            SharpDX.Result res = new SharpDX.Result(0x887A0004);
            res.CheckError();
        }

        void UpdateThreadException(MyGuiControlButton sender)
        {
            throw new InvalidOperationException("Forced exception");
        }

        void RenderThreadException(MyGuiControlButton sender)
        {
            MyRenderProxy.DebugCrashRenderThread();
        }

        void WorkerThreadException(MyGuiControlButton sender)
        {
            ThreadPool.QueueUserWorkItem(WorkerThreadCrasher);
        }

        void MainThreadInvokedException(MyGuiControlButton sender)
        {
            MySandboxGame.Static.Invoke(MainThreadCrasher);
        }

        void OutOfMemoryUpdateException(MyGuiControlButton sender)
        {
            Allocate();
        }

        void OutOfMemoryWorkerException(MyGuiControlButton sender)
        {
            ThreadPool.QueueUserWorkItem(Allocate);
        }

        void HavokAccessViolationException(MyGuiControlButton sender)
        {
            ThreadPool.QueueUserWorkItem(HavokAccessViolation);
        }

        void HavokAccessViolation(object state = null)
        {
            var info = new Havok.HkRigidBodyCinfo();
            Console.WriteLine(info.LinearVelocity); // To prevent optimization
        }

        void Allocate(object state = null)
        {
            List<byte[]> chunks = new List<byte[]>();

            for (int j = 0; j < 10000000; j++)
            {
                byte[] chunk = new byte[1024000];
                for (int i = 0; i < chunk.Length; i++)
                {
                    chunk[i] = (byte)(i ^ chunks.Count);
                }
                chunks.Add(chunk);
            }
            Console.WriteLine(chunks.Count); // To prevent optimization
        }

        void MainThreadCrasher()
        {
            throw new InvalidOperationException("Forced exception");
        }

        void WorkerThreadCrasher(object state)
        {
            Thread.Sleep(2000);
            throw new InvalidOperationException("Forced exception");
        }

        void DivideByZero(MyGuiControlButton sender)
        {
            int x = 7;
            int y = 14;
            int z = y / (y - 2 * x);
            Console.WriteLine(z); // Make sure result is used
        }
    }
}
