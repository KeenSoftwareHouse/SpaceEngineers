using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LargeTurretBaseDefinition))]
    public class MyLargeTurretBaseDefinition : MyWeaponBlockDefinition
    {
        public string OverlayTexture;
        public bool AiEnabled;
        public int MinElevationDegrees;
        public int MaxElevationDegrees;
        public int MinAzimuthDegrees;
        public int MaxAzimuthDegrees;
        public bool IdleRotation;
        public float MaxRangeMeters;
        public float RotationSpeed;
        public float ElevationSpeed;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obLargeTurret = builder as MyObjectBuilder_LargeTurretBaseDefinition;
            MyDebug.AssertDebug(obLargeTurret != null, "Initializing turret base definition using wrong object builder!");

            OverlayTexture = obLargeTurret.OverlayTexture;
            AiEnabled = obLargeTurret.AiEnabled;
            MinElevationDegrees = obLargeTurret.MinElevationDegrees;
            MaxElevationDegrees = obLargeTurret.MaxElevationDegrees;
            MinAzimuthDegrees = obLargeTurret.MinAzimuthDegrees;
            MaxAzimuthDegrees = obLargeTurret.MaxAzimuthDegrees;
            IdleRotation = obLargeTurret.IdleRotation;
            MaxRangeMeters = obLargeTurret.MaxRangeMeters;
            RotationSpeed = obLargeTurret.RotationSpeed;
            ElevationSpeed = obLargeTurret.ElevationSpeed;
        }
    }
}
