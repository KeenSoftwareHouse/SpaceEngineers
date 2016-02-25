using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    public interface IMyGunBaseUser
    {
        MyEntity IgnoreEntity { get; }
        MyEntity Weapon { get; }
        MyEntity Owner { get; }
        IMyMissileGunObject Launcher { get; }

        MyInventory AmmoInventory { get; }
        long OwnerId { get; }
        String ConstraintDisplayName { get; }
    }

    public static class MyGunBaseUserExtension
    {
        public static bool PutConstraint(this IMyGunBaseUser obj)
        {
            return !string.IsNullOrEmpty(obj.ConstraintDisplayName);
        }
    }
}
