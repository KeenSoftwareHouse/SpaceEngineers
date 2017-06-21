using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace VRage.Audio
{
    [ProtoBuf.ProtoContract]
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
    public struct MyCueId
#else  // XB1
    public struct MyCueId : IMySetGetMemberDataHelper
#endif // XB1
    {
        [ProtoBuf.ProtoMember]
        public MyStringHash Hash;

        public MyCueId(MyStringHash hash)
        {
            Hash = hash;
        }

        public bool IsNull
        {
            get { return Hash == MyStringHash.NullOrEmpty; }
        }

        public static bool operator ==(MyCueId r, MyCueId l) { return r.Hash == l.Hash; }
        public static bool operator !=(MyCueId r, MyCueId l) { return r.Hash != l.Hash; }

        public override bool Equals(object obj)
        {
            return (obj is MyCueId) && ((MyCueId)obj).Hash.Equals(Hash);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override string ToString()
        {
            return Hash.ToString();
        }
        
        public class ComparerType : IEqualityComparer<MyCueId>
        {
            bool IEqualityComparer<MyCueId>.Equals(MyCueId x, MyCueId y)
            {
                return x.Hash == y.Hash;
            }

            int IEqualityComparer<MyCueId>.GetHashCode(MyCueId obj)
            {
                return obj.Hash.GetHashCode();
            }
        }
        public static readonly ComparerType Comparer = new ComparerType();

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public object GetMemberData(MemberInfo m)
        {
            if (m.Name == "Hash")
                return Hash;

            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
            return null;
        }
#endif // XB1
    }
}
