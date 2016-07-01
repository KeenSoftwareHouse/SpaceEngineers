using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;

namespace VRage.Game.Definitions.SessionComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_ClipboardDefinition))]
    public class MyClipboardDefinition : MySessionComponentDefinition
    {
        /// <summary>
        /// Defines settings for pasting.
        /// </summary>
        public MyPlacementSettings PastingSettings;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_ClipboardDefinition)builder;

            PastingSettings = ob.PastingSettings;
        }
    }
}
