using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Serialization
{
    public static class MySerializerNetObject
    {
        private static INetObjectResolver m_netObjectResolver;

        public static INetObjectResolver NetObjectResolver { get { return m_netObjectResolver; } }

        public struct ResolverToken : IDisposable
        {
            INetObjectResolver m_previousResolver;

            public ResolverToken(INetObjectResolver newResolver)
            {
                m_previousResolver = m_netObjectResolver;
                m_netObjectResolver = newResolver;
            }

            public void Dispose()
            {
                m_netObjectResolver = m_previousResolver;
                m_previousResolver = null;
            }
        }

        public static ResolverToken Using(INetObjectResolver netObjectResolver)
        {
            return new ResolverToken(netObjectResolver);
        }
    }

    public class MySerializerNetObject<T> : MySerializer<T>
        where T : class, IMyNetObject
    {
        public override void Clone(ref T value)
        {
            throw new NotSupportedException();
        }

        public override bool Equals(ref T a, ref T b)
        {
            return a == b;
        }

        public override void Read(BitStream stream, out T value, MySerializeInfo info)
        {
            value = null;
            MySerializerNetObject.NetObjectResolver.Resolve<T>(stream, ref value);
        }

        public override void Write(BitStream stream, ref T value, MySerializeInfo info)
        {
            MySerializerNetObject.NetObjectResolver.Resolve<T>(stream, ref value);
        }
    }
}
