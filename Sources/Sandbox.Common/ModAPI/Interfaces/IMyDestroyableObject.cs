using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Interfaces
{
    /// <summary>
    /// This delegate is used to handle damage before it's applied to an object.  This returns a modified damage that is used in DoDamage.  Return damage if no change.
    /// </summary>
    /// <param name="target">The object that is damaged</param>
    /// <param name="damage">Amount of damage being applied</param>
    /// <param name="damageType">Type of damage being applied</param>
    /// <param name="attackerId">The entity ID of the attacker</param>
    /// <returns>Modified damage.  Return damage parameter if damage is not modified.</returns>
    public delegate float BeforeDamageApplied(object target, float damage, MyDamageType damageType, long attackerId);
    /// <summary>
    /// This delegate is used to handle deformations before they are applied to an object.
    /// </summary>
    /// <param name="target">The object that is being deformed</param>
    /// <param name="attackerId">The entity that is causing the deformation</param>
    /// <returns>true if deformation should happen, false if not</returns>
    public delegate bool BeforeDeformationApplied(object target, long attackerId);

    public interface IMyDestroyableObject
    {
        void OnDestroy();
        void DoDamage(float damage, MyDamageType damageType, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0);
        float Integrity { get; }
        
        /// <summary>
        /// Raised when an object is destroyed containing an entityid of what killed it
        /// </summary>
        event Action<object, float, MyDamageType, long> OnDestroyed;
        /// <summary>
        /// Raised before damage is applied to an object. 
        /// </summary>        
        event BeforeDamageApplied OnBeforeDamageApplied;
        /// <summary>
        /// Raised before deformation is applied to an object.  Only really applies to slimblocks
        /// </summary>
        event BeforeDeformationApplied OnBeforeDeformationApplied;
        /// <summary>
        /// Raised after damage is applied to an object
        /// </summary>
        event Action<object, float, MyDamageType, long> OnAfterDamageApplied;        
    }
}
