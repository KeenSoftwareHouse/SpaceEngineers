using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    class MyRadarComponent
    {
        public float DetectionRadius { get; set; }

        public bool BroadcastUsingAntennas { get; set; }
    }
}
