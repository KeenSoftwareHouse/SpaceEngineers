using System;
using VRage.Collections;

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
