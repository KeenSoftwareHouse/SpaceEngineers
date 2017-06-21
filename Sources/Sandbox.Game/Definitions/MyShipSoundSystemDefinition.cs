using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipSoundSystemDefinition))]
    public class MyShipSoundSystemDefinition : MyDefinitionBase
    {
        public float MaxUpdateRange = 2000f;
        public float MaxUpdateRange_sq = 2000 * 2000;
        public float WheelsCallbackRangeCreate_sq = 500 * 500;
        public float WheelsCallbackRangeRemove_sq = 750 * 750;
        public float FullSpeed = 96f;
        public float FullSpeed_sq = 96 * 96;
        public float SpeedThreshold1 = 32;
        public float SpeedThreshold2 = 64;
        public float LargeShipDetectionRadius = 15f;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ShipSoundSystemDefinition;
            MyDebug.AssertDebug(ob != null);

            this.MaxUpdateRange = ob.MaxUpdateRange;
            this.FullSpeed = ob.FullSpeed;
            this.FullSpeed_sq = ob.FullSpeed * ob.FullSpeed;
            this.SpeedThreshold1 = (ob.FullSpeed * 0.33f);
            this.SpeedThreshold2 = (ob.FullSpeed * 0.66f);
            this.LargeShipDetectionRadius = ob.LargeShipDetectionRadius;
            this.MaxUpdateRange_sq = ob.MaxUpdateRange * ob.MaxUpdateRange;
            this.WheelsCallbackRangeCreate_sq = ob.WheelStartUpdateRange * ob.WheelStartUpdateRange;
            this.WheelsCallbackRangeRemove_sq = ob.WheelStopUpdateRange * ob.WheelStopUpdateRange;
        }
    }
}