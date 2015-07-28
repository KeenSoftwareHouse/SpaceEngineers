using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyDestroyableObject
    {
        void OnDestroy();
        void DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0);
        float Integrity { get; }
        /// <summary>
        /// When set to true, it should use MyDamageSystem damage routing.
        /// </summary>
        bool UseDamageSystem { get; }
    }
}
