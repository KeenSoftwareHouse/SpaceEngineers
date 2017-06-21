using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

namespace Sandbox.Game.Multiplayer
{
    public partial class MyPlayerCollection : IMyPlayerCollection
    {
        long IMyPlayerCollection.Count
        {
            get { return m_players.Count; }
        }

        void IMyPlayerCollection.GetPlayers(List<IMyPlayer> players, Func<IMyPlayer, bool> collect)
        {
            foreach (var pair in m_players)
            {
                if (collect == null || collect(pair.Value))
                {
                    players.Add(pair.Value);
                }
            }
        }

        void IMyPlayerCollection.ExtendControl(IMyControllableEntity entityWithControl, IMyEntity entityGettingControl)
        {
            var e1 = entityWithControl as Sandbox.Game.Entities.IMyControllableEntity;
            var e2 = entityGettingControl as MyEntity;
            if(e1 != null && e2 != null)
                ExtendControl(e1, e2);
        }

        bool IMyPlayerCollection.HasExtendedControl(IMyControllableEntity firstEntity, IMyEntity secondEntity)
        {
            var e1 = firstEntity as Sandbox.Game.Entities.IMyControllableEntity;
            var e2 = secondEntity as MyEntity;
            if (e1 != null && e2 != null)
                return HasExtendedControl(e1, e2);
            return false;
        }

        void IMyPlayerCollection.ReduceControl(IMyControllableEntity entityWhichKeepsControl, IMyEntity entityWhichLoosesControl)
        {
            var e1 = entityWhichKeepsControl as Sandbox.Game.Entities.IMyControllableEntity;
            var e2 = entityWhichLoosesControl as MyEntity;
            if (e1 != null && e2 != null)
                ReduceControl(e1, e2);
        }

        void IMyPlayerCollection.RemoveControlledEntity(IMyEntity entity)
        {
            var e = entity as MyEntity;
            if (e != null)
                RemoveControlledEntity(e);
        }

        void IMyPlayerCollection.SetControlledEntity(ulong steamUserId, IMyEntity entity)
        {
            var e = entity as MyEntity;
            if (e != null)
                SetControlledEntity(steamUserId, e);
        }

        void IMyPlayerCollection.TryExtendControl(IMyControllableEntity entityWithControl, IMyEntity entityGettingControl)
        {
            var e1 = entityWithControl as Sandbox.Game.Entities.IMyControllableEntity;
            var e2 = entityGettingControl as MyEntity;
            if (e1 != null && e2 != null)
                TryExtendControl(e1, e2);
        }

        bool IMyPlayerCollection.TryReduceControl(IMyControllableEntity entityWhichKeepsControl, IMyEntity entityWhichLoosesControl)
        {
            var e1 = entityWhichKeepsControl as Sandbox.Game.Entities.IMyControllableEntity;
            var e2 = entityWhichLoosesControl as MyEntity;
            if (e1 != null && e2 != null)
                return TryReduceControl(e1, e2);
            return false;
        }

        IMyPlayer IMyPlayerCollection.GetPlayerControllingEntity(IMyEntity entity)
        {
            var e = entity as MyEntity;        
            if (e != null)
            {
                var controller = GetEntityController(e);
                if (controller != null)
                {
                    return controller.Player;
                }
            }
            return null;
        }


        void IMyPlayerCollection.GetAllIdentites(List<IMyIdentity> identities, Func<IMyIdentity, bool> collect)
        {
            foreach (var pair in m_allIdentities)
            {
                if (collect == null || collect(pair.Value))
                {
                    identities.Add(pair.Value);
                }
            }
        }

        long IMyPlayerCollection.TryGetIdentityId(ulong steamId)
        {
            return TryGetIdentityId(steamId);
        }

        ulong IMyPlayerCollection.TryGetSteamId(long identityId)
        {
            return TryGetSteamId(identityId);
        }
    }
}
