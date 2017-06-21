using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GUI.DebugInputComponents
{  
    public class MyResearchDebugInputComponent : MyDebugComponent
    {
        public MyResearchDebugInputComponent()
        {
            AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Show Your Research", ShowResearch);
            AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Toggle Pretty Mode", ShowResearchPretty);
            AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Unlock Your Research", UnlockResearch);
            AddShortcut(MyKeys.NumPad6, true, false, false, false, () => "Unlock All Research", UnlockAllResearch);
            AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Reset Your Research", ResetResearch);
            AddShortcut(MyKeys.NumPad9, true, false, false, false, () => "Reset All Research", ResetAllResearch);
        }

        public override string GetName()
        {
            return "Research";
        }

        private bool ShowResearch()
        {
            MySessionComponentResearch.Static.DEBUG_SHOW_RESEARCH = !MySessionComponentResearch.Static.DEBUG_SHOW_RESEARCH;
            return true;
        }

        private bool ShowResearchPretty()
        {
            MySessionComponentResearch.Static.DEBUG_SHOW_RESEARCH_PRETTY = !MySessionComponentResearch.Static.DEBUG_SHOW_RESEARCH_PRETTY;
            return true;
        }
        
        private bool ResetResearch()
        {
            if (MySession.Static != null && MySession.Static.LocalCharacter != null)
            MySessionComponentResearch.Static.ResetResearch(MySession.Static.LocalCharacter);
            return true;
        }

        private bool ResetAllResearch()
        {
            var players = Sync.Players.GetOnlinePlayers();
            foreach (var player in players)
            {
                var character = player.Controller.ControlledEntity as MyCharacter;
                if (character != null)
                    MySessionComponentResearch.Static.ResetResearch(character);
            }
            return true;
        }

        private bool UnlockResearch()
        {
            if (MySession.Static != null && MySession.Static.LocalCharacter != null)
                MySessionComponentResearch.Static.DebugUnlockAllResearch(MySession.Static.LocalCharacter);
            return true;
        }

        private bool UnlockAllResearch()
        {
            var players = Sync.Players.GetOnlinePlayers();
            foreach (var player in players)
            {
                var character = player.Controller.ControlledEntity as MyCharacter;
                if (character != null)
                    MySessionComponentResearch.Static.DebugUnlockAllResearch(character);
            }
            return true;
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;
            
            return base.HandleInput();
        }
    }
}
