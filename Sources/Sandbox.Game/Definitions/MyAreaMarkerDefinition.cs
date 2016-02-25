using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AreaMarkerDefinition))]
    public class MyAreaMarkerDefinition : MyDefinitionBase
    {
        public string Model;
		public string ColorMetalTexture;
		public string AddMapsTexture;

        public Vector3 ColorHSV;
        public Vector3 MarkerPosition;
		public int MaxNumber;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AreaMarkerDefinition;
            Model = ob.Model;
			ColorMetalTexture = ob.ColorMetalTexture;
			AddMapsTexture = ob.AddMapsTexture;
            ColorHSV = ob.ColorHSV;
            MarkerPosition = ob.MarkerPosition;
			MaxNumber = ob.MaxNumber;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_AreaMarkerDefinition;

            ob.Model = Model;
			ob.ColorMetalTexture = ColorMetalTexture;
			ob.AddMapsTexture = AddMapsTexture;
            ob.ColorHSV = ColorHSV;
            ob.MarkerPosition = MarkerPosition;
			ob.MaxNumber = MaxNumber;

            return ob;
        }
    }
}
