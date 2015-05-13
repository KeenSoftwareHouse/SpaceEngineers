using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common
{
    public struct MyControllerData
    {
        public string DisplayName;
        public bool IsDead;
        public ulong SteamId;
        public int ControllerId;
        public string Model;
    }

    public struct MyFactionMember
    {
        public long PlayerId;
        public bool IsLeader;
        public bool IsFounder;

        public MyFactionMember(long id, bool isLeader, bool isFounder = false)
        {
            PlayerId = id;
            IsLeader = isLeader;
            IsFounder = isFounder;
        }

        #region Implicit conversions

        public static implicit operator MyFactionMember(Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_FactionMember v)
        {
            return new MyFactionMember(v.PlayerId, v.IsLeader, v.IsFounder);
        }

        public static implicit operator Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_FactionMember(MyFactionMember v)
        {
            return new Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_FactionMember() { PlayerId = v.PlayerId, IsLeader = v.IsLeader, IsFounder = v.IsFounder };
        }

        #endregion

        #region Comparer

        public class FactionComparerType : IEqualityComparer<MyFactionMember>
        {
            public bool Equals(MyFactionMember x, MyFactionMember y)
            {
                return x.PlayerId != y.PlayerId;
            }

            public int GetHashCode(MyFactionMember obj)
            {
                return ((int)(obj.PlayerId >> 32) ^ (int)(obj.PlayerId));
            }
        }
        public static readonly FactionComparerType Comparer = new FactionComparerType();

        #endregion
    }
}
