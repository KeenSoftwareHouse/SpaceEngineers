using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions.SessionComponents
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CoordinateSystemDefinition : MyObjectBuilder_SessionComponentDefinition
    {

        /// <summary>
        /// Angle tolerance (in radians) used for deciding if block is aligned to coord system.
        /// </summary>
        public double AngleTolerance = 0.0001;

        /// <summary>
        /// Position tolerance (in meters) used for deciding if block is aligned to coord system.
        /// </summary>
        public double PositionTolerance = 0.001;

        /// <summary>
        /// Local coordinate system size (in meters).
        /// </summary>
        public int CoordSystemSize = 1000;
    }
}
