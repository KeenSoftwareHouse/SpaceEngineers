﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLaserAntenna : IMyFunctionalBlock
    {
        Vector3D TargetCoords
        {
            get;
        }

        void SetTargetCoords(string coords);

        void Connect();

        /// <summary>
        /// Gets the linked receiver's IMyGridTerminalSystem. Returns null if no link is established.
        /// </summary>
        /// <returns></returns>
        IMyGridTerminalSystem GetReceiverGrid();

    }
}
