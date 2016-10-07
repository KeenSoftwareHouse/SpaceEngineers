using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Utils;
using VRageRender.Messages;

namespace VRageRender
{
    /// <summary>
    /// Data shared between render and update
    /// </summary>
    public class MySharedData
    {
        // TODO: OP! Add message count, visible object count, billboard count, etc... logging (to make sure we clean everything up properly)

        SpinLockRef m_lock = new SpinLockRef();

        MySwapQueue<HashSet<uint>> m_outputVisibleObjects = MySwapQueue.Create<HashSet<uint>>();
        MyMessageQueue m_outputRenderMessages = new MyMessageQueue();

        MyUpdateData m_inputRenderMessages = new MyUpdateData();
        MySwapQueue<MyBillboardBatch<MyBillboard>> m_inputBillboards = MySwapQueue.Create<MyBillboardBatch<MyBillboard>>();
        MySwapQueue<MyBillboardBatch<MyTriangleBillboard>> m_inputTriangleBillboards = MySwapQueue.Create<MyBillboardBatch<MyTriangleBillboard>>();

        // TODO: OP! This shouldn't be here public, only methods (Enqueue should be sufficient)
        public MySwapQueue<MyBillboardBatch<MyBillboard>> Billboards { get { return m_inputBillboards; } }

        // TODO: OP! This shouldn't be here public, only methods (Enqueue should be sufficient)
        public MySwapQueue<MyBillboardBatch<MyTriangleBillboard>> TriangleBillboards { get { return m_inputTriangleBillboards; } }

        // TODO: OP! This shouldn't be here public, only methods (Enqueue should be sufficient)
        public MySwapQueue<HashSet<uint>> VisibleObjects { get { return m_outputVisibleObjects; } }

        // TODO: OP! This shouldn't be here public, only methods (Enqueue should be sufficient)
        public MyUpdateFrame CurrentUpdateFrame
        {
            get { return m_inputRenderMessages.CurrentUpdateFrame; }
        }

        public MyQueue<MyRenderMessageBase> MessagesForNextFrame = new MyQueue<MyRenderMessageBase>(128);

        // TODO: OP! This shouldn't be here, only methods (Enqueue should be sufficient)
        public MyMessageQueue RenderOutputMessageQueue
        {
            get { return m_outputRenderMessages; }
        }

        /// <summary>
        /// Refresh data from render (visible objects, render messages)
        /// </summary>
        public void BeforeUpdate()
        {
            using (m_lock.Acquire())
            {
                m_outputVisibleObjects.RefreshRead();

                // TODO: OP! This is wrong sync, but it's enough for existing output messages (we can get only half of these messages and second half next frame)
                m_outputRenderMessages.Commit();
            }
        }

        public void AfterUpdate(MyTimeSpan? updateTimestamp)
        {
            using (m_lock.Acquire())
            {
                if (updateTimestamp.HasValue)
                    m_inputRenderMessages.CurrentUpdateFrame.UpdateTimestamp = updateTimestamp.Value;

                m_inputRenderMessages.CommitUpdateFrame();
                m_inputBillboards.CommitWrite();
                m_inputBillboards.Write.Clear();
                m_inputTriangleBillboards.CommitWrite();
                m_inputTriangleBillboards.Write.Clear();
            }
        }

        public void BeforeRender(MyTimeSpan? currentDrawTime)
        {
            using (m_lock.Acquire())
            {
                if (currentDrawTime.HasValue)
                    MyRenderProxy.CurrentDrawTime = currentDrawTime.Value;
            }
        }

        public MyUpdateFrame GetRenderFrame(out bool isPreFrame)
        {
            using (m_lock.Acquire())
            {
                MyUpdateFrame result = m_inputRenderMessages.GetRenderFrame(out isPreFrame);
                if (!isPreFrame)
                {
                    m_inputBillboards.RefreshRead();
                    m_inputTriangleBillboards.RefreshRead();
                }
                return result;
            }
        }

        public void ReturnPreFrame(MyUpdateFrame frame)
        {
            m_inputRenderMessages.ReturnPreFrame(frame);
        }

        public void AfterRender()
        {
            using (m_lock.Acquire())
            {
                m_outputVisibleObjects.CommitWrite();
                m_outputVisibleObjects.Write.Clear();
            }
        }

        public void CommitBasicRenderMessages()
        {
            m_inputRenderMessages.CommitUpdateFrame();
            m_inputBillboards.CommitWrite();
            m_inputBillboards.Write.Clear();
            m_inputTriangleBillboards.CommitWrite();
            m_inputTriangleBillboards.Write.Clear();
        }
    }
}
