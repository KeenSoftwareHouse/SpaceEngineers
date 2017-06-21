using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1
    [MyDebugScreen("Game", "Travel")]
    class MyGuiScreenDebugTravel : MyGuiScreenDebugBase
    {
        //in milions km
        static Dictionary<string, Vector3> s_travelPoints = new Dictionary<string, Vector3>()
        {
            { "Mercury", new Vector3(-39, 0.0f, 46) },
            { "Venus", new Vector3(-2, 0.0f, 108) },
            { "Earth", new Vector3(101, 0, -111) },
            { "Moon", new Vector3(101, 0, -111) + new Vector3(-0.015f, 0.0f, -0.2f) },
            { "Mars", new Vector3(-182, 0, 114) },
            { "Jupiter", new Vector3(-778f, 0.0f, 155.6f) },
            { "Saturn", new Vector3(1120f, 0.0f, -840f) },
            { "Uranus", new Vector3(-2700f, 0.0f, -1500f) },
            { "Zero", new Vector3(0f, 0.0f, 0f) },
            { "Billion", new Vector3(1E3f) },
            { "BillionFlat0", new Vector3(999f, 1E3f, 1E3f) },
            { "BillionFlat1", new Vector3(1001f, 1E3f, 1E3f) },
        };

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugDrawSettings";
        }

        public MyGuiScreenDebugTravel()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Travel", Color.Yellow.ToVector4());
            AddShareFocusHint();

            foreach (var travelPair in s_travelPoints)
            {
                AddButton(new StringBuilder(travelPair.Key), (button) => TravelTo(travelPair.Value));
            }
        }

        void TravelTo(Vector3 positionInMilions)
        {
            Vector3D pos = (Vector3D)positionInMilions * 1E6;

            MyMultiplayer.TeleportControlledEntity(pos);
        }

    }
#endif
}
