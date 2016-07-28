using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using VRage.Animations;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Game.Models;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;

using VRage;
using VRage.Collections;
using VRage;
using VRage.Audio;
using VRage.Plugins;
using VRage.Utils;
using VRage.Data;
using VRage.Filesystem.FindFilesRegEx;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Library.Utils;
using Sandbox.Engine.Networking;
using Sandbox.Game.AI.Pathfinding;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.Components;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Game.EntityComponents;
using Sandbox.Graphics.GUI;


namespace Sandbox.Game.Gui
{
    class MyAlesDebugInputComponent : MyDebugComponent
    {

        public override string GetName()
        {
            return "Ales";
        }

        public MyAlesDebugInputComponent()
        {
            AddShortcut(MyKeys.U, true, false, false, false,
               () => "Reload particles",
               delegate
               {
                   ReloadParticleDefinition();
                   return true;
               });
        }



        private void ReloadParticleDefinition()
        {
            MyDefinitionManager.Static.ReloadParticles();
        }

    }
}
