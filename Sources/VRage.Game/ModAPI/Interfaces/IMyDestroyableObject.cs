using VRage.Utils;

namespace VRage.Game.ModAPI.Interfaces
{
    public interface IMyDestroyableObject
    {
        void OnDestroy();
        bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0);// returns true if damage could be applied
        float Integrity { get; }
        /// <summary>
        /// When set to true, it should use MyDamageSystem damage routing.
        /// </summary>
        bool UseDamageSystem { get; }
    }
}
