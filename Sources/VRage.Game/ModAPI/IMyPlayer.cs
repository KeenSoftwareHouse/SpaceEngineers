using System.Collections.Generic;

namespace VRage.Game.ModAPI
{
    public interface IMyPlayer
    {
        IMyNetworkClient Client { get; }
        VRage.Game.MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId);

        HashSet<long> Grids { get; }
        void AddGrid(long gridEntityId);
        void RemoveGrid(long gridEntityId);
        IMyEntityController Controller { get; }
        VRageMath.Vector3D GetPosition();
        ulong SteamUserId { get; }
        string DisplayName { get; }
        [System.Obsolete("use IdentityId")]
        long PlayerID { get; }
        long IdentityId { get; }
        bool IsAdmin { get; }
        bool IsPromoted { get; }
        IMyIdentity Identity { get; }
        IMyCharacter Character { get; }
        bool IsRealPlayer { get; }
        bool IsBot { get; }
    }
}
