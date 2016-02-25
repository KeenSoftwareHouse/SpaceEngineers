
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Graphics.GUI;
using System.Text;

using VRageMath;
using System;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ReactorDefinition))]
    public class MyReactorDefinition : MyPowerProducerDefinition
    {
        public float InventoryMaxVolume;
        public Vector3 InventorySize;
        public MyInventoryConstraint InventoryConstraint;

        public MyDefinitionId FuelId;
        public MyPhysicalItemDefinition FuelDefinition;
        public MyObjectBuilder_PhysicalObject FuelItem;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var generatorBuilder = builder as MyObjectBuilder_ReactorDefinition;
            MyDebug.AssertDebug(generatorBuilder != null, "Initializing thrust definition using wrong object builder.");
            InventorySize = generatorBuilder.InventorySize;
            InventoryMaxVolume = InventorySize.X * InventorySize.Y * InventorySize.Z;

            FuelId = generatorBuilder.FuelId;
            FuelDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(FuelId);
            MyDebug.AssertDebug(FuelDefinition != null);

            FuelItem = MyObjectBuilderSerializer.CreateNewObject(generatorBuilder.FuelId) as MyObjectBuilder_PhysicalObject;
            MyDebug.AssertDebug(FuelItem != null);

            //StringBuilder constraintTooltip = new StringBuilder();
            //constraintTooltip.Append(FuelDefinition.DisplayNameText);
            //InventoryConstraint = new MyInventoryConstraint(constraintTooltip).Add(FuelId);
            String constraintTooltip = FuelDefinition.DisplayNameText;
            InventoryConstraint = new MyInventoryConstraint(constraintTooltip).Add(FuelId);
        }
    }
}
