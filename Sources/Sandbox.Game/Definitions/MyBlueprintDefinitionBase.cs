using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Game.World;
using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Definitions
{
    public abstract class MyBlueprintDefinitionBase : MyDefinitionBase
    {
        public struct Item
        {
            public MyDefinitionId Id;

            /// <summary>
            /// Amount of item required or produced. For discrete objects this refers to
            /// pieces. For ingots and ore, this refers to volume in m^3.
            /// </summary>
            public MyFixedPoint Amount;

            public override string ToString()
            {
                return string.Format("{0}x {1}", Amount, Id);
            }

            public static Item FromObjectBuilder(BlueprintItem obItem)
            {
                return new Item()
                {
                    Id = obItem.Id,
                    Amount = MyFixedPoint.DeserializeStringSafe(obItem.Amount)
                };
            }
        }

        public struct ProductionInfo
        {
            public MyBlueprintDefinitionBase Blueprint;
            public MyFixedPoint Amount;
        }

        public Item[] Prerequisites;
        public Item[] Results;

        public string ProgressBarSoundCue = null;

        /// <summary>
        /// Base production time in seconds, which is affected by speed increase of
        /// refinery or assembler.
        /// </summary>
        public float BaseProductionTimeInSeconds = 1.0f;

        /// <summary>
        /// Total volume of products created by one unit of blueprint. This is for production calculation purposes.
        /// </summary>
        public float OutputVolume;

        /// <summary>
        /// Whether the the blueprint's outputs have to be produced as a whole at once (because you cannot divide some output items)
        /// </summary>
        public bool Atomic;

        public MyObjectBuilderType InputItemType
        {
            get
            {
                VerifyInputItemType(Prerequisites[0].Id.TypeId);
                return Prerequisites[0].Id.TypeId;
            }
        }

        /// <summary>
        /// Postprocess initialization. Should set PostprocessNeeded to false if initialization was successful.
        /// </summary>
        public abstract void Postprocess();

        /// <summary>
        /// Whether the Postprocess method still needs to be called.
        /// </summary>
        public bool PostprocessNeeded { get; protected set; }

        [Conditional("DEBUG")]
        private void VerifyInputItemType(MyObjectBuilderType inputType)
        {
            foreach (var item in Prerequisites)
                MyDebug.AssertDebug(inputType == item.Id.TypeId, "Not all input objects are of same type. Is this on purpose?");
        }

        public override string ToString()
        {
            return Results.ToString();
        }

        /// <summary>
        /// Should return the number of added blueprints (to make building hierarchical blueprint production infos easier)
        /// </summary>
        public abstract int GetBlueprints(List<ProductionInfo> blueprints);
    }
}