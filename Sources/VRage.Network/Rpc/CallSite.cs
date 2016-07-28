#if !XB1
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Rpc
{
    public abstract class CallSite
    {
        public ushort Id;
        public MethodInfo MethodInfo;
        public Type[] Arguments;

        public abstract void Invoke(BitStream stream);
        public abstract void Build(MethodInfo info, ushort id, CallSiteCache cache);

        public static CallSite Create(MethodInfo info, ushort id, CallSiteCache cache)
        {
            Type[] args = new Type[7];
            var parameters = info.GetParameters();
            int i = 0;
            if(!info.IsStatic)
            {
                args[i] = info.DeclaringType;
                i++;
            }
            for (int x = 0; x < parameters.Length; x++, i++)
            {
                args[i] = parameters[x].ParameterType;
            }
            for (; i < args.Length; i++)
            {
                args[i] = typeof(DBNull);
            }
            var type = typeof(CallSite<,,,,,,>).MakeGenericType(args);
            var result = (CallSite)Activator.CreateInstance(type);
            result.Arguments = args;
            result.Build(info, id, cache);
            return result;
        }
    }
}
#endif // !XB1
