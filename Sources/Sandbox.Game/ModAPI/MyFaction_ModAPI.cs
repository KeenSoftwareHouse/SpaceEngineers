using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.Common;
using VRage.Game.ModAPI;
using VRage.Game;

namespace Sandbox.Game.World
{
    public partial class MyFaction : IMyFaction
    {
        long IMyFaction.FactionId
        {
            get
            {
            return FactionId;
            }
        }

        string IMyFaction.Tag
        {
            get
            {
                return Tag;
            }
        }

        string IMyFaction.Name
        {
            get 
            {
                return Name;
            }
        }

        string IMyFaction.Description
        {
            get
            {
                return Description;
            }
        }

        string IMyFaction.PrivateInfo
        {
            get 
            {
                return PrivateInfo;
            }
        }

        bool IMyFaction.AutoAcceptMember
        {
            get 
            {
                return AutoAcceptMember;
            }
        }

        bool IMyFaction.AutoAcceptPeace
        {
            get
            {
                return AutoAcceptPeace;
            }
        }

        bool IMyFaction.AcceptHumans
        {
            get
            {
                return AcceptHumans;
            }
        }

        long IMyFaction.FounderId
        {
            get
            {
                return FounderId;
            }
        }

        bool IMyFaction.IsFounder(long playerId)
        {
            return IsFounder(playerId);
        }

        bool IMyFaction.IsLeader(long playerId)
        {
            return IsLeader(playerId);
        }

        bool IMyFaction.IsMember(long playerId)
        {
            return IsMember(playerId);
        }

        bool IMyFaction.IsNeutral(long playerId)
        {
            return IsNeutral(playerId);
        }


        VRage.Collections.DictionaryReader<long, MyFactionMember> IMyFaction.Members
        {
            get { return Members;}
        }

        VRage.Collections.DictionaryReader<long, MyFactionMember> IMyFaction.JoinRequests
        {
            get { return JoinRequests; }
        }
    }
}
