using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;

namespace VRageRender
{
    public class MyMessageQueue : MyCommitQueue<IMyRenderMessage>
    {
    }

    /// <summary>
    /// TODO: This should use some better sync, it could introduce delays with current state
    /// 1) Use spin lock
    /// 2) Lock only queue, not whole dictionary
    /// 3) Test count first and when it's insufficient, create new message, both should be safe to do out of any lock
    /// 4) Custom consumer/producer non-locking (except resize) queue could be better (maybe overkill)
    /// </summary>
    public class MyMessagePool : Dictionary<int, MyConcurrentQueue<IMyRenderMessage>>
    {
        FastResourceLock m_lock = new FastResourceLock();

        public MyMessagePool()
        {
            foreach (MyRenderMessageEnum renderMessageEnum in Enum.GetValues(typeof(MyRenderMessageEnum)))
            {
                base.Add((int)renderMessageEnum, new MyConcurrentQueue<IMyRenderMessage>());
            }
        }

        public void Clear(MyRenderMessageEnum message)
        {
            base[(int)message].Clear();
        }

        public T Get<T>(MyRenderMessageEnum renderMessageEnum) where T : class, IMyRenderMessage, new()
        {
            var queue = base[(int)renderMessageEnum];
            IMyRenderMessage message;
            if(!queue.TryDequeue(out message))
            {
                var result = new T();
                Debug.Assert(result.MessageType == renderMessageEnum, "Invalid message type, check arguments!");
                return result;
            }
            return (T)message;
        }

        public void Return(IMyRenderMessage message)
        {
            MyConcurrentQueue<IMyRenderMessage> queue = base[(int)message.MessageType];
            if (queue.Count < 2048)
            {
                queue.Enqueue(message);
            }
        }
    }
}
