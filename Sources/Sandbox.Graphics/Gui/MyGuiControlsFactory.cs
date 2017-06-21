#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game;
using VRage.Game.Common;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlTypeAttribute : MyFactoryTagAttribute
    {
        public MyGuiControlTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    public static class MyGuiControlsFactory
    {
        static MyObjectFactory<MyGuiControlTypeAttribute, MyGuiControlBase> m_objectFactory = new MyObjectFactory<MyGuiControlTypeAttribute, MyGuiControlBase>();

        public static void RegisterDescriptorsFromAssembly(Assembly assembly)
        {
            m_objectFactory.RegisterFromAssembly(assembly);
        }

        public static MyGuiControlBase CreateGuiControl(MyObjectBuilder_Base builder)
        {
            return m_objectFactory.CreateInstance(builder.TypeId);
        }

        public static MyObjectBuilder_GuiControlBase CreateObjectBuilder(MyGuiControlBase control)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_GuiControlBase>(control);
        }
    }
}
