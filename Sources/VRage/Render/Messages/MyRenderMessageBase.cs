
using System.Diagnostics;

namespace VRageRender
{
    public abstract class MyRenderMessageBase
    {
        /// <summary>
        /// Get message class
        /// </summary>
        public abstract MyRenderMessageType MessageClass { get; }

        /// <summary>
        /// Gets message type
        /// </summary>
        public abstract MyRenderMessageEnum MessageType { get; }

        //private bool m_open = false;

        //private StackTrace m_lastInit, m_lastClose;

        public virtual void Close()
        {
            /*Debug.Assert(m_open);
            m_open = false;*/
        }

        public virtual void Init()
        {
            /*Debug.Assert(!m_open);
            m_open = true;*/
        }
    }
}
