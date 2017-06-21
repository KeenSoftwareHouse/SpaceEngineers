using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Multiplayer
{
    partial class MyFactionCollection : IMyFactionCollection
    {

        bool IMyFactionCollection.FactionTagExists(string tag, IMyFaction doNotCheck)
        {
            return FactionTagExists(tag, doNotCheck);
        }

        bool IMyFactionCollection.FactionNameExists(string name, IMyFaction doNotCheck)
        {
            return FactionNameExists(name, doNotCheck);
        }

        IMyFaction IMyFactionCollection.TryGetFactionById(long factionId)
        {
            return TryGetFactionById(factionId);
        }

        IMyFaction IMyFactionCollection.TryGetPlayerFaction(long playerId)
        {
            return TryGetPlayerFaction(playerId);
        }

        IMyFaction IMyFactionCollection.TryGetFactionByTag(string tag)
        {
            return TryGetFactionByTag(tag);
        }

        IMyFaction IMyFactionCollection.TryGetFactionByName(string name)
        {
            foreach (var entry in m_factions)
            {
                var faction = entry.Value;

                if (string.Equals(name, faction.Name, StringComparison.OrdinalIgnoreCase))
                    return faction;
            }

            return null;
        }

        void IMyFactionCollection.AddPlayerToFaction(long playerId, long factionId)
        {
            AddPlayerToFaction(playerId, factionId);
        }

        void IMyFactionCollection.KickPlayerFromFaction(long playerId)
        {
            KickPlayerFromFaction(playerId);
        }

        MyRelationsBetweenFactions IMyFactionCollection.GetRelationBetweenFactions(long factionId1, long factionId2)
        {
            return GetRelationBetweenFactions(factionId1, factionId2);
        }

        bool IMyFactionCollection.AreFactionsEnemies(long factionId1, long factionId2)
        {
            return AreFactionsEnemies( factionId1,  factionId2);
        }

        bool IMyFactionCollection.IsPeaceRequestStateSent(long myFactionId, long foreignFactionId)
        {
            return IsPeaceRequestStateSent(myFactionId,  foreignFactionId);
        }

        bool IMyFactionCollection.IsPeaceRequestStatePending(long myFactionId, long foreignFactionId)
        {
            return IsPeaceRequestStatePending(myFactionId,  foreignFactionId);
        }

        void IMyFactionCollection.RemoveFaction(long factionId)
        {
             RemoveFaction(factionId);
        }

        void IMyFactionCollection.SendPeaceRequest(long fromFactionId, long toFactionId)
        {
            SendPeaceRequest(fromFactionId, toFactionId);
        }

        void IMyFactionCollection.CancelPeaceRequest(long fromFactionId, long toFactionId)
        {
            CancelPeaceRequest(fromFactionId, toFactionId);
        }

        void IMyFactionCollection.AcceptPeace(long fromFactionId, long toFactionId)
        {
            AcceptPeace( fromFactionId,  toFactionId);
        }

        void IMyFactionCollection.DeclareWar(long fromFactionId, long toFactionId)
        {
            DeclareWar(fromFactionId, toFactionId);
        }

        void IMyFactionCollection.SendJoinRequest(long factionId, long playerId)
        {
           SendJoinRequest( factionId,  playerId);
        }

        void IMyFactionCollection.CancelJoinRequest(long factionId, long playerId)
        {
            CancelJoinRequest(factionId, playerId);
        }

        void IMyFactionCollection.AcceptJoin(long factionId, long playerId)
        {
            AcceptJoin( factionId,  playerId);
        }

        void IMyFactionCollection.KickMember(long factionId, long playerId)
        {
            KickMember(factionId, playerId);
        }

        void IMyFactionCollection.PromoteMember(long factionId, long playerId)
        {
            PromoteMember(factionId, playerId);
        }

        void IMyFactionCollection.DemoteMember(long factionId, long playerId)
        {
            DemoteMember(factionId,  playerId);
        }

        void IMyFactionCollection.MemberLeaves(long factionId, long playerId)
        {
            MemberLeaves(factionId, playerId);
        }

        event Action<long, bool, bool> IMyFactionCollection.FactionAutoAcceptChanged
        {
            add { FactionAutoAcceptChanged += value; }
            remove { FactionAutoAcceptChanged -= value; }
        }

        void IMyFactionCollection.ChangeAutoAccept(long factionId, long playerId, bool autoAcceptMember, bool autoAcceptPeace)
        {
            ChangeAutoAccept( factionId,  playerId,  autoAcceptMember,  autoAcceptPeace);
        }

        void IMyFactionCollection.EditFaction(long factionId, string tag, string name, string desc, string privateInfo)
        {
            EditFaction( factionId,  tag,  name,  desc,  privateInfo);
        }

        void IMyFactionCollection.CreateFaction(long founderId, string tag, string name, string desc, string privateInfo)
        {
            CreateFaction(founderId,  tag,  name,  desc,  privateInfo);
        }

        event Action<long> IMyFactionCollection.FactionCreated
        {
            add { FactionCreated += value; }
            remove { FactionCreated -= value; }
        }

        MyObjectBuilder_FactionCollection IMyFactionCollection.GetObjectBuilder()
        {
            return GetObjectBuilder();
        }


        event Action<long> IMyFactionCollection.FactionEdited
        {
            add { FactionEdited += value; }
            remove { FactionEdited -= value; }
        }
    }
}
