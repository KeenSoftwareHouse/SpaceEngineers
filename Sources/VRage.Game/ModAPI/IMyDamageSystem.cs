using System;

namespace VRage.Game.ModAPI
{
    /// <summary>
    /// This delegate is used to handle damage before it's applied to an object.  This returns a modified damage that is used in DoDamage.  Return damage if no change.
    /// </summary>
    /// <param name="target">The object that is damaged</param>
    /// <param name="damage">Amount of damage being applied</param>
    /// <param name="damageType">Type of damage being applied</param>
    /// <param name="attackerId">The entity ID of the attacker</param>
    /// <returns>Modified damage.  Return damage parameter if damage is not modified.</returns>
    public delegate void BeforeDamageApplied(object target, ref MyDamageInformation info);

    /// <summary>
    ///  Standard priority values
    /// </summary>
    public enum MyDamageSystemPriority
    {
        Critical=100,
        Normal=500,
        Low=1000
    }

    public interface IMyDamageSystem
    {
        /// <summary>
        /// Registers a handler for when an object in game is destroyed.
        /// </summary>
        /// <param name="priority">Priority level.  Lower means higher priority.</param>
        /// <param name="handler">Actual handler delegate</param>
        void RegisterDestroyHandler(int priority, Action<object, MyDamageInformation> handler);

        /// <summary>
        /// Registers a handler that is called before an object in game is damaged.  The damage can be modified in this handler.
        /// </summary>
        /// <param name="priority">Priority level.  Lower means higher priority.</param>
        /// <param name="handler">Actual handler delegate</param>
        void RegisterBeforeDamageHandler(int priority, BeforeDamageApplied handler);

        /// <summary>
        /// Registers a handler that is called after an object in game is damaged.
        /// </summary>
        /// <param name="priority">Priority level.  Lower means higher priority.</param>
        /// <param name="handler">Actual handler delegate</param>
        void RegisterAfterDamageHandler(int priority, Action<object, MyDamageInformation> handler);
    }
}
