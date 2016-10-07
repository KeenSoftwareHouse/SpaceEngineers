using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;

namespace VRage.Game.Definitions.SessionComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_CubeBuilderDefinition))]
    public class MyCubeBuilderDefinition : MySessionComponentDefinition
    {
        public float DefaultBlockBuildingDistance;
        public float MaxBlockBuildingDistance;
        public float MinBlockBuildingDistance;

        public double BuildingDistSurvivalCharacter;
        public double BuildingDistSurvivalShip;

        /// <summary>
        /// Defines settings for building mode.
        /// </summary>
        public MyPlacementSettings BuildingSettings;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_CubeBuilderDefinition)builder;

            DefaultBlockBuildingDistance = ob.DefaultBlockBuildingDistance;
            MaxBlockBuildingDistance = ob.MaxBlockBuildingDistance;
            MinBlockBuildingDistance = ob.MinBlockBuildingDistance;

            BuildingDistSurvivalCharacter = ob.BuildingDistSurvivalCharacter;
            BuildingDistSurvivalShip = ob.BuildingDistSurvivalShip;

            BuildingSettings = ob.BuildingSettings;

        }
    }
}
