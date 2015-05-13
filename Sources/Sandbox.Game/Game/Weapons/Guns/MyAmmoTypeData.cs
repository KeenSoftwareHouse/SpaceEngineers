using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Weapons.Guns
{
    // Wrappers for specific ammo types. Used in MyGunBase as a part
    // of creating projectiles/missiles.

    public abstract class MyAmmoTypeData
    {
    }

    class MyProjectileData : MyAmmoTypeData
    {
    }

    class MyMissileData : MyAmmoTypeData
    {
    }
}
