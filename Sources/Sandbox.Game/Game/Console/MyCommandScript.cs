using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game.GUI
{
    public class MyCommandScript : MyCommand
    {
        private class MyCommandMethodArgs : MyCommandArgs
        {
            public object[] Args;
        }

        private Type m_type;
        private static StringBuilder m_cache = new StringBuilder();

        public override string Prefix()
        {
            return m_type.Name;
        }

        public MyCommandScript(Type type)
        {
            m_type = type;
            int i = 0;
            foreach (var method in type.GetMethods())
            {
                if (!method.IsPublic || !method.IsStatic)
                    continue;
                var act = new MyCommandAction()
                {
                    AutocompleteHint = GetArgsString(method),
                    Parser = (x) => ParseArgs(x, method),
                    CallAction = (x) => Invoke(x, method)
                };
                m_methods.Add(string.Format("{0}{1}",i++, method.Name), act);
            }
        }

        private StringBuilder GetArgsString(System.Reflection.MethodInfo method)
        {
            var sb = new StringBuilder();
            foreach (var arg in method.GetParameters())
                sb.Append(string.Format("{0} {1}, ", arg.ParameterType.Name, arg.Name));
            return sb;
        }

        private StringBuilder Invoke(MyCommandArgs x, System.Reflection.MethodInfo method)
        {
            m_cache.Clear();
            var args = x as MyCommandMethodArgs;
            if (args.Args != null)
            {
                m_cache.Append("Success. ");
                var retVal = method.Invoke(null, args.Args);
                if (retVal != null)
                    m_cache.Append(retVal.ToString());
            }
            else
                m_cache.Append(string.Format("Invoking {0} failed", method.Name));
            return m_cache;
        }

        private MyCommandArgs ParseArgs(List<string> x, System.Reflection.MethodInfo method)
        {
            MyCommandMethodArgs retVal = new MyCommandMethodArgs();
            var paramInfos = method.GetParameters();
            List<object> parameters = new List<object>();
            for (int i = 0; i < paramInfos.Length && i < x.Count; i++)
            {
                var paramType = paramInfos[i].ParameterType;
                var parseMet = paramType.GetMethod("TryParse", new Type[] { typeof(System.String), paramType.MakeByRefType() });
                if (parseMet != null)
                {
                    var output = Activator.CreateInstance(paramType);
                    var args = new object[] { x[i], output };
                    var par = parseMet.Invoke(null, args);
                    parameters.Add(args[1]);
                }
                else
                    parameters.Add(x[i]);
            }
            if (paramInfos.Length == parameters.Count)
                retVal.Args = parameters.ToArray();
            return retVal;
        }
    }
}
