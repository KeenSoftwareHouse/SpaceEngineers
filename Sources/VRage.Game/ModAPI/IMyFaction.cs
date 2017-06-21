using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VRage.Game.ModAPI
{
    public interface IMyFaction
    {
        long FactionId { get; }

        string Tag { get; }
        string Name { get; }
        string Description { get; }
        string PrivateInfo { get; }

        bool AutoAcceptMember { get; }
        bool AutoAcceptPeace { get; }
        bool AcceptHumans { get; }

        long FounderId { get; }

        bool IsFounder(long playerId);

        bool IsLeader(long playerId);

        bool IsMember(long playerId);

        bool IsNeutral(long playerId);

        bool IsEveryoneNpc();

        VRage.Collections.DictionaryReader<long, MyFactionMember> Members { get; }
        VRage.Collections.DictionaryReader<long, MyFactionMember> JoinRequests { get; }

    }
}
