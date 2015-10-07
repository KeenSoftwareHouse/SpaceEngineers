using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;

namespace VRage.Game.Systems
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyScriptedSystemAttribute : Attribute
    {
        public readonly string ScriptName;

        public MyScriptedSystemAttribute(string scriptName)
        {
            ScriptName = scriptName;
        }
    }


    public abstract class MyGroupScriptBase
    {
        public MyGroupScriptBase()
        {
        }

        public abstract void ProcessObjects(ListReader<MyDefinitionId> objects);
    }
}
