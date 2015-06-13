using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyDestroyableObject
    {
        void OnDestroy();
        void DoDamage(float damage, MyDamageType damageType, bool sync, MyHitInfo? hitInfo = null);
        float Integrity { get; }
    }
}
