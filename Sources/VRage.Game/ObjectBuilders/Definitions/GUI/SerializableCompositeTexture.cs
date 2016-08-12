using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage;
using VRage.Data;
using VRageMath;

namespace ObjectBuilders.Definitions.GUI
{
    public struct SerializableCompositeTexture
    {
        [ModdableContentFile("dds")]
        public string LeftTop;

        [ModdableContentFile("dds")]
        public string LeftCenter;

        [ModdableContentFile("dds")]
        public string LeftBottom;

        [ModdableContentFile("dds")]
        public string CenterTop;

        [ModdableContentFile("dds")]
        public string Center;

        [ModdableContentFile("dds")]
        public string CenterBottom;

        [ModdableContentFile("dds")]
        public string RightTop;

        [ModdableContentFile("dds")]
        public string RightCenter;

        [ModdableContentFile("dds")]
        public string RightBottom;

        public SerializableVector2 Size;
        public MyObjectBuilder_GuiSkinDefinition.PaddingDefinition BorderSizes;
    }
}
