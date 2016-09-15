using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
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
using Sandbox.ModAPI;
using System;

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

        Random m_random;

        public override string GetName()
        {
            return "Ales";
        }

        public MyAlesDebugInputComponent()
        {
            m_random = new Random();
            AddShortcut(MyKeys.U, true, false, false, false,
               () => "Reload particles",
               delegate
               {
                   ReloadParticleDefinition();
                   return true;
               });
            AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Spawn local GPSs", delegate { SpamSpawnGPSs(true); return true; });
            AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Spawn GPSs", delegate { SpamSpawnGPSs(false); return true; });
            
        }

        private void SpamSpawnGPSs(bool isLocal)
        {
            var gps = MyAPIGateway.Session.GPS.Create("abc", "123", new Vector3D(), true, true);
            for (int i = 1; i < 1000; i++)
            {
                var point = new Vector3D(m_random.Next(-10000, 10000));
                gps.Coords = point;
                gps.Name = point.GetHash().ToString();
                gps.UpdateHash();
                gps.DiscardAt = new TimeSpan(0, 0, 5);
                if (isLocal)
                    MyAPIGateway.Session.GPS.AddLocalGps(gps);
                else
                    MyAPIGateway.Session.GPS.AddGps(MySession.Static.LocalPlayerId, gps);
            }
        }

        private void TravelToWaypointClient()
        {

        }


        private void ReloadParticleDefinition()
        {
            MyDefinitionManager.Static.ReloadParticles();
        }

    }
}
