using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Network
{
    public abstract class CallSite
    {
        public readonly MySynchronizedTypeInfo OwnerType;
        public readonly uint Id;
        public readonly MethodInfo MethodInfo;
        public readonly CallSiteFlags CallSiteFlags;

        public bool HasClientFlag { get { return (CallSiteFlags & CallSiteFlags.Client) == CallSiteFlags.Client; } }
        public bool HasServerFlag { get { return (CallSiteFlags & CallSiteFlags.Server) == CallSiteFlags.Server; } }
        public bool HasBroadcastFlag { get { return (CallSiteFlags & CallSiteFlags.Broadcast) == CallSiteFlags.Broadcast; } }
        public bool HasBroadcastExceptFlag { get { return (CallSiteFlags & CallSiteFlags.BroadcastExcept) == CallSiteFlags.BroadcastExcept; } }
        public bool HasRefreshReplicableFlag { get { return (CallSiteFlags & CallSiteFlags.RefreshReplicable) == CallSiteFlags.RefreshReplicable; } }
        public bool IsReliable { get { return (CallSiteFlags & CallSiteFlags.Reliable) == CallSiteFlags.Reliable; } }
        public bool IsBlocking { get { return (CallSiteFlags & CallSiteFlags.Blocking) == CallSiteFlags.Blocking; } }

        public CallSite(MySynchronizedTypeInfo owner, uint id, MethodInfo info, CallSiteFlags flags)
        {
            OwnerType = owner;
            Id = id;
            MethodInfo = info;
            CallSiteFlags = flags;
        }

        public abstract bool Invoke(BitStream stream, object obj, bool validate);

        public override string ToString()
        {
            return String.Format("{0}.{1}", MethodInfo.DeclaringType.Name, MethodInfo.Name);
        }
    }
}
