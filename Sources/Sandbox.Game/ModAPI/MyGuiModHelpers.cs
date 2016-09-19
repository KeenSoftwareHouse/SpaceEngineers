using System;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Sandbox.ModAPI
{
    public class MyGuiModHelpers : IMyGui
    {
        public event Action<object> GuiControlCreated
        {
            add { MyGuiSandbox.GuiControlCreated += GetDelegate( value ); }
            remove { MyGuiSandbox.GuiControlCreated -= GetDelegate( value ); }
        }

        public event Action<object> GuiControlRemoved
        {
            add { MyGuiSandbox.GuiControlRemoved += GetDelegate( value ); }
            remove { MyGuiSandbox.GuiControlRemoved -= GetDelegate( value ); }
        }

        public string ActiveGamePlayScreen
        {
            get { return MyGuiScreenGamePlay.ActiveGameplayScreen.Name; }
        }

        public IMyEntity InteractedEntity
        {
            get { return (IMyEntity)MyGuiScreenTerminal.InteractedEntity; }
        }

        public MyTerminalPageEnum GetCurrentScreen
        {
            get { return MyGuiScreenTerminal.GetCurrentScreen(); }
        }

        public bool ChatEntryVisible
        {
            get
            {
                if (MyGuiScreenChat.Static == null || MyGuiScreenChat.Static.ChatTextbox == null)
                    return false;

                return MyGuiScreenChat.Static.ChatTextbox.Visible;
            }
        }

        Action<object> GetDelegate( Action<object> value )
        {
            return (Action<object>)Delegate.CreateDelegate( typeof(Action<object>), value.Target, value.Method );
        }
    }
}
