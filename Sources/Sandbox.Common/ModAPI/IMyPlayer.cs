using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyPlayer
    {
        IMyNetworkClient Client { get; }
        Sandbox.Common.MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId);

        HashSet<long> Grids { get; }
        void AddGrid(long gridEntityId);
        void RemoveGrid(long gridEntityId);
        IMyEntityController Controller { get; }
        VRageMath.Vector3 GetPosition();
        ulong SteamUserId { get; }
        string DisplayName { get; }
        long PlayerID { get; }
        long IdentityId { get; }
    }
}
