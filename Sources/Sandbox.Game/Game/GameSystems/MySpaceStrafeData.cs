using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Game.GameSystems
{
    public static class MySpaceStrafeDataStatic
    {
        public static MySpaceStrafeData Default = new MySpaceStrafeData();
        private static Dictionary<string, MySpaceStrafeData> presets = new Dictionary<string, MySpaceStrafeData>();

        public static void Reset()
        {
            presets.Clear();
        }

        public static void SavePreset(string key, MySpaceStrafeData preset)
        {
            presets[key] = preset;
        }

        public static MySpaceStrafeData LoadPreset(string key)
        {
            if (presets.ContainsKey(key))
                return presets[key];
            else
                return new MySpaceStrafeData();
        }
    }

    public class MySpaceStrafeData
    {
        public float Height = 10f;
        public float Depth = 5f;
        public float Width = 10f;
        public bool AvoidCollisions = true;
        public float SpeedLimit = 25f;
        public bool RotateToPlayer = true;
        public float PlayerYAxisOffset = 0.9f;
        public int WaypointDelayMsMin = 1000;
        public int WaypointDelayMsMax = 3000;
        public float WaypointThresholdDistance = 0.5f;
        public float PlayerTargetDistance = 200f;
        public float MaxManeuverDistance = 250f;
        public int WaypointMaxTime = 15000;
        public int LostTimeMs = 20000;
        public float MinStrafeDistance = 2f;
        public bool UseStaticWeaponry = true;
        public float StaticWeaponryUsage = 300f;
        public bool UseTools = true;
        public float ToolsUsage = 5f;
        public bool UseKamikazeBehavior = true;
        public bool CanBeDisabled = true;
        public float KamikazeBehaviorDistance = 75f;
        public string AlternativeBehavior = "";
        public bool UsePlanetHover = false;
        public float PlanetHoverMin = 2f;
        public float PlanetHoverMax = 25f;

        public MySpaceStrafeData() { }

        public MySpaceStrafeData(MyObjectBuilder_DroneBehaviorDefinition definition)
        {
            Height = definition.StrafeHeight;
            Depth = definition.StrafeDepth;
            Width = definition.StrafeWidth;
            AvoidCollisions = definition.AvoidCollisions;
            SpeedLimit = definition.SpeedLimit;
            RotateToPlayer = definition.RotateToPlayer;
            PlayerYAxisOffset = definition.PlayerYAxisOffset;
            WaypointDelayMsMin = definition.WaypointDelayMsMin;
            WaypointDelayMsMax = definition.WaypointDelayMsMax;
            WaypointThresholdDistance = definition.WaypointThresholdDistance;
            PlayerTargetDistance = definition.TargetDistance;
            MaxManeuverDistance = definition.MaxManeuverDistance;
            WaypointMaxTime = definition.WaypointMaxTime;
            LostTimeMs = definition.LostTimeMs;
            MinStrafeDistance = definition.MinStrafeDistance;
            UseStaticWeaponry = definition.UseStaticWeaponry;
            StaticWeaponryUsage = definition.StaticWeaponryUsage;
            UseKamikazeBehavior = definition.UseRammingBehavior;
            KamikazeBehaviorDistance = definition.RammingBehaviorDistance;
            AlternativeBehavior = definition.AlternativeBehavior;
            UseTools = definition.UseTools;
            ToolsUsage = definition.ToolsUsage;
            UsePlanetHover = definition.UsePlanetHover;
            PlanetHoverMin = definition.PlanetHoverMin;
            PlanetHoverMax = definition.PlanetHoverMax;
        }
    }
}
