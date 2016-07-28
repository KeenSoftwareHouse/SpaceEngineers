using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Scripting
{
    public class MyIngameScripting : IMyIngameScripting
    {
        public static MyIngameScripting Static { get; internal set; }

        static MyIngameScripting()
        {
            Static = new MyIngameScripting();
        }

        public MyIngameScripting()
        {
            ScriptBlacklist = MyScriptCompiler.Static.Whitelist;
        }

        public static IMyScriptBlacklist ScriptBlacklist;

        IMyScriptBlacklist IMyIngameScripting.ScriptBlacklist
        {
            get { return ScriptBlacklist; }
        }

        public void Clean()
        {
            ScriptBlacklist = null;
            Static = null;
        }
    }
}
