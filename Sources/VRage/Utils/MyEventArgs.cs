using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Utils
{
    public class MyEventArgs : EventArgs
    {
        private Dictionary<MyStringId, object> m_args = new Dictionary<MyStringId, object>(MyStringId.Comparer);
        public Dictionary<MyStringId, object>.KeyCollection ArgNames { get { return m_args.Keys; } }

        public MyEventArgs()
        { }

        public MyEventArgs(KeyValuePair<MyStringId, object> arg)
        {
            SetArg(arg.Key, arg.Value);
        }

        public MyEventArgs(KeyValuePair<MyStringId, object>[] args)
        {
            foreach (var pair in args)
                SetArg(pair.Key, pair.Value);
        }

        public object GetArg(MyStringId argName)
        {
            return ArgNames.Contains(argName) ? m_args[argName] : null;
        }

        public void SetArg(MyStringId argName, object value)
        {
            m_args.Remove(argName);
            m_args.Add(argName, value);
        }
    }
}
