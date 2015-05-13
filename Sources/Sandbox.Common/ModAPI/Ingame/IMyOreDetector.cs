using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyOreDetector : IMyFunctionalBlock
    {
        float Range {get;}
        bool BroadcastUsingAntennas {get;}
    }
}
