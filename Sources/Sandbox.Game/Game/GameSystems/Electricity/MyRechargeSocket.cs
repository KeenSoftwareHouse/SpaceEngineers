using System.Diagnostics;
using Sandbox.Game.EntityComponents;


namespace Sandbox.Game.GameSystems.Electricity
{
    public class MyRechargeSocket
    {
        private MyResourceDistributorComponent m_resourceDistributor;
        private MyResourceSinkComponent m_pluggedInConsumer;

		public MyResourceDistributorComponent ResourceDistributor
        {
            get { return m_resourceDistributor; }
            set
            {
                if (m_resourceDistributor != value)
                {
                    if (m_pluggedInConsumer != null && m_resourceDistributor != null)
                        m_resourceDistributor.RemoveSink(m_pluggedInConsumer);
                    m_resourceDistributor = value;
                    if (m_pluggedInConsumer != null && m_resourceDistributor != null)
                        m_resourceDistributor.AddSink(m_pluggedInConsumer);
                }
            }
        }

		public void PlugIn(MyResourceSinkComponent consumer)
        {
            if (m_pluggedInConsumer == consumer)
                return;
            Debug.Assert(m_pluggedInConsumer == null, "Consumer already plugged in.");
            m_pluggedInConsumer = consumer;
			if (m_resourceDistributor != null)
			{
				m_resourceDistributor.AddSink(consumer);
				consumer.Update();
			}
        }

        public void Unplug()
        {
            Debug.Assert(m_pluggedInConsumer != null, "Consumer not plugged in.");
            if (m_resourceDistributor != null)
                m_resourceDistributor.RemoveSink(m_pluggedInConsumer);
            m_pluggedInConsumer = null;
        }
    }
}
