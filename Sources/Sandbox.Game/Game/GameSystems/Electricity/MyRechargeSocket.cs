using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;


namespace Sandbox.Game.GameSystems.Electricity
{
    public class MyRechargeSocket
    {
        private MyPowerDistributor m_powerDistributor;
        private IMyPowerConsumer m_pluggedInConsumer;

        public MyPowerDistributor PowerDistributor
        {
            get { return m_powerDistributor; }
            set
            {
                if (m_powerDistributor != value)
                {
                    if (m_pluggedInConsumer != null && m_powerDistributor != null)
                        m_powerDistributor.RemoveConsumer(m_pluggedInConsumer);
                    m_powerDistributor = value;
                    if (m_pluggedInConsumer != null && m_powerDistributor != null)
                        m_powerDistributor.AddConsumer(m_pluggedInConsumer);
                }
            }
        }

        public void PlugIn(IMyPowerConsumer consumer)
        {
            if (m_pluggedInConsumer == consumer)
                return;
            Debug.Assert(m_pluggedInConsumer == null, "Consumer already plugged in.");
            m_pluggedInConsumer = consumer;
            if (m_powerDistributor != null)
                m_powerDistributor.AddConsumer(consumer);
        }

        public void Unplug()
        {
            Debug.Assert(m_pluggedInConsumer != null, "Consumer not plugged in.");
            if (m_powerDistributor != null)
                m_powerDistributor.RemoveConsumer(m_pluggedInConsumer);
            m_pluggedInConsumer = null;
        }
    }
}
