using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyRemoteControl : IMyShipController
    {
        void AddWaypoint(string name, Vector3 coords);
        void RemoveWaypoint(string name);


    }


    public enum FlightMode : int
    {
        Patrol = 0,
        Circle = 1,
        OneWay = 2,
    }
}
