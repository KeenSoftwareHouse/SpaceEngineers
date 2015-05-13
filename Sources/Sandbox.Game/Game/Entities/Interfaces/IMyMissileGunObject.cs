using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public interface IMyMissileGunObject : IMyGunObject<MyGunBase>
    {
        /// <summary>
        /// Should create a missile with the given parameters, make a sound, create particle effects, etc.
        /// </summary>
        void ShootMissile(Vector3 velocity);
    }
}
