using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;

namespace VRage.Game.Definitions.SessionComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_CoordinateSystemDefinition))]
    public class MyCoordinateSystemDefinition : MySessionComponentDefinition
    {
        public double AngleTolerance = 0.0001;
        public double PositionTolerance = 0.001;
        public int CoordSystemSize = 1000;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            MyObjectBuilder_CoordinateSystemDefinition obCoordSys = builder as MyObjectBuilder_CoordinateSystemDefinition;

            if (obCoordSys == null)
            {
                Debug.Fail("Wrong object builder!!");
            }

            this.AngleTolerance = obCoordSys.AngleTolerance;
            this.PositionTolerance = obCoordSys.PositionTolerance;
            this.CoordSystemSize = obCoordSys.CoordSystemSize;

        }
    }
}
