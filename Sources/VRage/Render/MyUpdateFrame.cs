using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Library.Utils;

namespace VRageRender
{
    /// <summary>
    /// Contains data produced by update frame, sent to render in thread-safe manner
    /// </summary>
    public class MyUpdateFrame
    {
        public bool Processed;
        public MyTimeSpan UpdateTimestamp;

        public readonly List<MyRenderMessageBase> RenderInput = new List<MyRenderMessageBase>(2048);

        public void Enqueue(MyRenderMessageBase message)
        {
            //RenderInput.Add(message);
            if (message.MessageType != MyRenderMessageEnum.DebugDrawAABB)
            {
                RenderInput.Add(message);
            }
            else
            {
                RenderInput.Insert(0, message);
            }
        }
    }
}
