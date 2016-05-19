using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilder
{
    // TODO: Unify factory management and loading here
    // Also abstract serialization, MP and whatever
    public class MyObjectFactories
    {
        public static void RegisterFromAssembly(Assembly assembly)
        {
            MyObjectBuilderSerializer.RegisterFromAssembly(assembly);
            MyObjectBuilderType.RegisterFromAssembly(assembly);
            MyDefinitionManagerBase.RegisterTypesFromAssembly(assembly);
        }
    }
}
