using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace VRage.Audio
{
    [ProtoBuf.ProtoContract]
    public struct MyCueId
    {
        [ProtoBuf.ProtoMember]
        public readonly MyStringHash Hash;

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
    }
}
