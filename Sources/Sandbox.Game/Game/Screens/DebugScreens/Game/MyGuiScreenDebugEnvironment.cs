using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Game", "Environment")]
    public class MyGuiScreenDebugEnvironment : MyGuiScreenDebugBase
    {
        public static Action DeleteEnvironmentItems;

        public MyGuiScreenDebugEnvironment()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderEnvironment";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddShareFocusHint();
            Spacing = 0.01f;

            // Environment controll
            AddCaption("World Environment", Color.Yellow.ToVector4());

            AddCaption("Debug Tools:", Color.Yellow.ToVector4());

            AddCheckBox("Update Environment Sectors", () => MyPlanetEnvironmentSessionComponent.EnableUpdate,
                x => MyPlanetEnvironmentSessionComponent.EnableUpdate = x);

            AddButton("Refresh Sectors", x => RefreshSectors());

            // Debug draw
            AddLabel("Debug Draw Options:", Color.White, 1);

            AddCheckBox("Debug Draw Sectors", () => MyPlanetEnvironmentSessionComponent.DebugDrawSectors,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawSectors = x);

            AddCheckBox("Debug Draw Clipmap Proxies", () => MyPlanetEnvironmentSessionComponent.DebugDrawProxies,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawProxies = x);

            AddCheckBox("Debug Draw Dynamic Clusters", () => MyPlanetEnvironmentSessionComponent.DebugDrawDynamicObjectClusters,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawDynamicObjectClusters = x);

            AddCheckBox("Debug Draw Collision Boxes", () => MyPlanetEnvironmentSessionComponent.DebugDrawCollisionCheckers,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawCollisionCheckers = x);

            AddCheckBox("Debug Draw Providers", () => MyPlanetEnvironmentSessionComponent.DebugDrawEnvironmentProviders,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawEnvironmentProviders = x);

            AddCheckBox("Debug Draw Active Sector Items", () => MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorItems,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorItems = x);

            AddCheckBox("Debug Draw Active Sector Provider", () => MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorProvider,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorProvider = x);

            AddSlider("Sector Name Draw Distance:", new MyGuiSliderPropertiesExponential(1, 1000), () => MyPlanetEnvironmentSessionComponent.DebugDrawDistance,
                x => MyPlanetEnvironmentSessionComponent.DebugDrawDistance = x);
        }

        private void RefreshSectors()
        {
            foreach (var planet in MyPlanets.GetPlanets())
            {
                var component = planet.Components.Get<MyPlanetEnvironmentComponent>();
                component.CloseAll();
            }
        }
    }
}