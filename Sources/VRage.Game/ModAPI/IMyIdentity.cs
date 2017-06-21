using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyIdentity
    {
        long PlayerId { get; }
        long IdentityId { get; }
        string DisplayName { get; }
        string Model { get; }
        Vector3? ColorMask { get; }
        bool IsDead { get; }
    }
}
