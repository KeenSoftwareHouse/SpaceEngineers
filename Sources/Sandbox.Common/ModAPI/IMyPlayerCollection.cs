using Sandbox.Common;
using System;
using System.Collections.Generic;
using VRage.ModAPI;

namespace Sandbox.ModAPI
{
    public interface IMyPlayerCollection
    {
        void ExtendControl(ModAPI.Interfaces.IMyControllableEntity entityWithControl, IMyEntity entityGettingControl);
        void GetPlayers(List<IMyPlayer> players, Func<IMyPlayer, bool> collect = null);
        bool HasExtendedControl(ModAPI.Interfaces.IMyControllableEntity firstEntity, IMyEntity secondEntity);
        void ReduceControl(ModAPI.Interfaces.IMyControllableEntity entityWhichKeepsControl, IMyEntity entityWhichLoosesControl);
        void RemoveControlledEntity(IMyEntity entity);
        void TryExtendControl(ModAPI.Interfaces.IMyControllableEntity entityWithControl, IMyEntity entityGettingControl);
        bool TryReduceControl(ModAPI.Interfaces.IMyControllableEntity entityWhichKeepsControl, IMyEntity entityWhichLoosesControl);
        void SetControlledEntity(ulong steamUserId, IMyEntity entity);
        long Count { get; }
        IMyPlayer GetPlayerControllingEntity(IMyEntity entity);
        void GetAllIdentites(List<IMyIdentity> identities, Func<IMyIdentity, bool> collect = null);
        //void WriteDebugInfo();
    }
}
