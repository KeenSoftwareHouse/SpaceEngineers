
namespace VRageRender.Messages
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

        public virtual bool IsPersistent
        {
            get { return false; }
        }
    }

    public abstract class MySpriteDrawRenderMessage : MyRenderMessageBase
    {
        public string TargetTexture { get; set; }

        /// <summary>If it has a target offscreen texture, then the message has to be processed earlier</summary>
        public override MyRenderMessageType MessageClass
        {
            get { return TargetTexture == null ? MyRenderMessageType.Draw : MyRenderMessageType.StateChangeOnce; }
        }
    }

    public abstract class MyDebugRenderMessage : MyRenderMessageBase
    {
        public bool Persistent
        {
            get;
            set;
        }

        public override bool IsPersistent
        {
            get { return Persistent; }
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
    }
}
