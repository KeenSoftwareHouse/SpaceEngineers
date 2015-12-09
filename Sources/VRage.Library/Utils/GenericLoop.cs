using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Platform
{
    public class GenericLoop
    {
        public delegate void VoidAction();

        VoidAction m_tickCallback;

        public bool IsDone = false;

        public virtual void Run(VoidAction tickCallback)
        {
            m_tickCallback = tickCallback;

            while (!IsDone)
                m_tickCallback();
        }        
    }
}
