using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyJumpDrive : Sandbox.ModAPI.Ingame.IMyJumpDrive
    {
        /// <summary>
        /// Set the stored power level of the jump drive.
        /// </summary>
        /// <param name="filledRatio">0.0 to 1.0</param>
        void SetStoredPower(float filledRatio);
    }
}
