using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Gui
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlImageButton: MyObjectBuilder_GuiControlBase
    {
        public string Text;

        public string TextEnum;

        public float TextScale;

        public int TextAlignment;

        public bool DrawCrossTextureWhenDisabled;

        public bool DrawRedTextureWhenDisabled;
    }
}
