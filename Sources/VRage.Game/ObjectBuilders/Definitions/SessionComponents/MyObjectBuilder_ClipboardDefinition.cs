using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions.SessionComponents
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ClipboardDefinition : MyObjectBuilder_SessionComponentDefinition
    {
        /// <summary>
        /// Defines pasting settings.
        /// </summary>
        public MyPlacementSettings PastingSettings;
    }
}
