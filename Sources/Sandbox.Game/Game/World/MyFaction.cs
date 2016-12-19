using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sandbox.Engine.Utils;
using VRage.Collections;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Common;
using System.Diagnostics;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.ModAPI;


namespace Sandbox.Game.World
{
    public partial class MyFaction 
    {
        private Dictionary<long, MyFactionMember> m_members;      // contains all (leaders and members)
        private Dictionary<long, MyFactionMember> m_joinRequests; // members that want to join this faction

        public long   FactionId { get; private set; }
        public string Tag;
        public string Name;
        public string Description;
        public string PrivateInfo; // for members only

        public long FounderId { get; private set; }

        public bool AutoAcceptMember;
        public bool AutoAcceptPeace;
        public bool AcceptHumans;
        public bool EnableFriendlyFire = true;

        public DictionaryReader<long, MyFactionMember> Members      { get { return new DictionaryReader<long, MyFactionMember>(m_members); } }
        public DictionaryReader<long, MyFactionMember> JoinRequests { get { return new DictionaryReader<long, MyFactionMember>(m_joinRequests); } }
        
        public MyFaction(long id, string tag, string name, string desc, string privateInfo, long creatorId)
        {
            FactionId   = id;
            Tag         = tag;
            Name        = name;
            Description = desc;
            PrivateInfo = privateInfo;
            FounderId = creatorId;

            AutoAcceptMember = false;
            AutoAcceptPeace  = false;
            AcceptHumans = true;

            m_members      = new Dictionary<long, MyFactionMember>();
            m_joinRequests = new Dictionary<long, MyFactionMember>();

            m_members.Add(creatorId, new MyFactionMember(creatorId, true, true));
        }

        public MyFaction(MyObjectBuilder_Faction obj)
        {
            FactionId   = obj.FactionId;
            Tag         = obj.Tag;
            Name        = obj.Name;
            Description = obj.Description;
            PrivateInfo = obj.PrivateInfo;

            AutoAcceptMember = obj.AutoAcceptMember;
            AutoAcceptPeace  = obj.AutoAcceptPeace;
            EnableFriendlyFire = obj.EnableFriendlyFire;
            AcceptHumans = obj.AcceptHumans;

            m_members = new Dictionary<long, MyFactionMember>(obj.Members.Count);

            foreach (var member in obj.Members)
            {
                m_members.Add(member.PlayerId, member);
                if (member.IsFounder)
                    FounderId = member.PlayerId;
            }

            if (obj.JoinRequests != null)
            {
                m_joinRequests = new Dictionary<long, MyFactionMember>(obj.JoinRequests.Count);

                foreach (var request in obj.JoinRequests)
                    m_joinRequests.Add(request.PlayerId, request);
            }
            else
            {
                m_joinRequests = new Dictionary<long, MyFactionMember>();
            }

            // Fix the faction settings if it was created from definition
            var factionDef = MyDefinitionManager.Static.TryGetFactionDefinition(Tag);
            if (factionDef != null)
            {
                AutoAcceptMember = factionDef.AutoAcceptMember;
                AcceptHumans = factionDef.AcceptHumans;
                EnableFriendlyFire = factionDef.EnableFriendlyFire;
                Name = factionDef.DisplayNameText;
                Description = factionDef.DescriptionText;
            }

            CheckAndFixFactionRanks();
        }

        public bool IsFounder(long playerId)
        {
            MyFactionMember member;
            if (m_members.TryGetValue(playerId, out member))
            {
                return member.IsFounder;
            }
            return false;
        }

        public bool IsLeader(long playerId)
        {
            MyFactionMember member;
            if (m_members.TryGetValue(playerId, out member))
            {
                return member.IsLeader;
            }
            return false;
        }

        public bool IsMember(long playerId)
        {
            MyFactionMember member;
            return m_members.TryGetValue(playerId, out member);
        }

        public bool IsNeutral(long playerId)
        {
             IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(playerId);

            if (faction != null)
                return MySession.Static.Factions.GetRelationBetweenFactions(FactionId, faction.FactionId) == MyRelationsBetweenFactions.Neutral;

            return false;
        }

        public bool IsEveryoneNpc()
        {
            foreach (var member in m_members)
            {
                if (!Sync.Players.IdentityIsNpc(member.Key))
                {
                    return false;
                }
            }
            return true;
        }


        public void AddJoinRequest(long playerId)
        {
            m_joinRequests[playerId] = new MyFactionMember(playerId, false);
        }

        public void CancelJoinRequest(long playerId)
        {
            m_joinRequests.Remove(playerId);
        }

        public void AcceptJoin(long playerId, bool autoaccept = false)
        {
            MySession.Static.Factions.AddPlayerToFactionInternal(playerId, FactionId);

            if (m_joinRequests.ContainsKey(playerId))
            {
                m_members[playerId] = m_joinRequests[playerId];
                m_joinRequests.Remove(playerId);
            }
            else if (AutoAcceptMember || autoaccept)
                m_members[playerId] = new MyFactionMember(playerId, false);
        }

        public void KickMember(long playerId)
        {
            m_members.Remove(playerId);
            MySession.Static.Factions.KickPlayerFromFaction(playerId);
            CheckAndFixFactionRanks();
        }

        public void PromoteMember(long playerId)
        {
            MyFactionMember memberToPromote;
            if (m_members.TryGetValue(playerId, out memberToPromote))
            {
                memberToPromote.IsLeader = true;
                m_members[playerId] = memberToPromote;
            }
        }

        public void DemoteMember(long playerId)
        {
            MyFactionMember memberToDemote;
            if (m_members.TryGetValue(playerId, out memberToDemote))
            {
                memberToDemote.IsLeader = false;
                m_members[playerId] = memberToDemote;
            }
        }

        public void PromoteToFounder(long playerId)
        {
            MyFactionMember memberToPromote;
            if (m_members.TryGetValue(playerId, out memberToPromote))
            {
                memberToPromote.IsLeader = true;
                memberToPromote.IsFounder = true;
                m_members[playerId] = memberToPromote;
                FounderId = playerId;
            }
        }

        public void CheckAndFixFactionRanks()
        {
            if (HasFounder())
                return;
                        
            foreach (var member in m_members)
            {
                if (member.Value.IsLeader)
                {
                    PromoteToFounder(member.Key);
                    return;
                }
            }

            if (m_members.Count > 0)
                PromoteToFounder(m_members.Keys.FirstOrDefault());
        }

        private bool HasFounder()
        {
            MyFactionMember founder;
            if (m_members.TryGetValue(FounderId, out founder))
            {
                return founder.IsFounder;
            }
            return false;
        }

        public MyObjectBuilder_Faction GetObjectBuilder()
        {
            var builder = new MyObjectBuilder_Faction();

            builder.FactionId   = FactionId;
            builder.Tag         = Tag;
            builder.Name        = Name;
            builder.Description = Description;
            builder.PrivateInfo = PrivateInfo;

            builder.AutoAcceptMember = AutoAcceptMember;
            builder.AutoAcceptPeace  = AutoAcceptPeace;
            builder.EnableFriendlyFire = EnableFriendlyFire;

            builder.Members = new List<MyObjectBuilder_FactionMember>(Members.Count());
            foreach (var member in Members)
                builder.Members.Add(member.Value);

            builder.JoinRequests = new List<MyObjectBuilder_FactionMember>(JoinRequests.Count());
            foreach (var request in JoinRequests)
                builder.JoinRequests.Add(request.Value);

            return builder;
        }
    }
}
