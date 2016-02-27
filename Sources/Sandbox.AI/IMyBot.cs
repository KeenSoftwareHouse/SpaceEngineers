using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.AI
{
    public interface IMyBot
    {
        void Cleanup();
        void GetAvailableActions(ActionCollection actions);
    }
}
