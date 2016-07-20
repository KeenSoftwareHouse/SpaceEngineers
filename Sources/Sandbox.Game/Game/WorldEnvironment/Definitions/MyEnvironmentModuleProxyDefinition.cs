using System;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilder;
using VRage.Utils;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentModuleProxyDefinition))]
    public class MyEnvironmentModuleProxyDefinition : MyDefinitionBase
    {
        public Type ModuleType;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_EnvironmentModuleProxyDefinition)builder;

            ModuleType = MyGlobalTypeMetadata.Static.GetType(ob.QualifiedTypeName, false);

            /*if (ModuleType == null && !Context.IsBaseGame)
            {
                ModuleType = MySession.Static.ScriptManager.GetScriptType(Context, ob.QualifiedTypeName);
            }*/

            if (ModuleType == null)
            {
                MyLog.Default.Error("Could not find module type {0}!", ob.QualifiedTypeName);
                throw new ArgumentException("Could not find module type;");
            }
        }
    }
}
