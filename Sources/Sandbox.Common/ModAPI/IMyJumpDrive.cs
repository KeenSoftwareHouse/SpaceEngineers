using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyJumpDrive : Sandbox.ModAPI.Ingame.IMyJumpDrive
    {
        /// <summary>
        /// Set the stored power level of the jump drive.
        /// </summary>
        /// <param name="filledRatio">0.0 to 1.0</param>
        void SetStoredPower(float filledRatio);

        /// <summary>
        /// If the jump drive can be operated by the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        bool CanUserJump(long userId);

        /// <summary>
        /// Starts the jump sequence towards the specified coordinates.
        /// 
        /// If the coordinates are too close or too far it will not do anything.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="userId">if 0 the jumpdrive's owner will be used</param>
        void JumpTo(Vector3D coords, long userId = 0);
    }
}
