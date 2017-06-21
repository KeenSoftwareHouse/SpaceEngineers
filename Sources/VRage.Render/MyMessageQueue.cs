using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRageRender.Messages;

namespace VRageRender
{
    public class MyMessageQueue : MyCommitQueue<MyRenderMessageBase>
    {
    }

    /// <summary>
    /// TODO: This should use some better sync, it could introduce delays with current state
    /// 1) Use spin lock
    /// 2) Lock only queue, not whole dictionary
    /// 3) Test count first and when it's insufficient, create new message, both should be safe to do out of any lock
    /// 4) Custom consumer/producer non-locking (except resize) queue could be better (maybe overkill)
    /// </summary>
    public class MyMessagePool : Dictionary<int, MyConcurrentQueue<MyRenderMessageBase>>
    {
        public MyMessagePool()
        {
            foreach (MyRenderMessageEnum renderMessageEnum in Enum.GetValues(typeof(MyRenderMessageEnum)))
            {
                base.Add((int)renderMessageEnum, new MyConcurrentQueue<MyRenderMessageBase>());
            }
        }

        public void Clear(MyRenderMessageEnum message)
        {
            base[(int)message].Clear();
        }

        public T Get<T>(MyRenderMessageEnum renderMessageEnum) where T : MyRenderMessageBase, new()
        {
            var queue = base[(int)renderMessageEnum];
            MyRenderMessageBase message;
            if (!queue.TryDequeue(out message))
            {
                message = new T();
                Debug.Assert(message.MessageType == renderMessageEnum, "Invalid message type, check arguments!");
            }
            message.Init();

            return (T)message;
        }

        public void Return(MyRenderMessageBase message)
        {
            if (message.IsPersistent)
                return;

            MyConcurrentQueue<MyRenderMessageBase> queue = base[(int)message.MessageType];

            message.Close();
            if (queue.Count < 2048)
            {
                queue.Enqueue(message);
            }
        }
    }
}
