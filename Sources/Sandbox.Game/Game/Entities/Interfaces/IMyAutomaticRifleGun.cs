using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Weapons
{
    public interface IMyAutomaticRifleGun : IMyEntity, IMyHandheldGunObject<MyGunBase>, IMyGunBaseUser
    {
    }
}
