using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Rpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RpcAttribute : Attribute
    {
    }
}
