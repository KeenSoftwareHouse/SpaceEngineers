using System.Linq;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LCDFontDefinition))]
    public class MyLCDFontDefinition : MyDefinitionBase
    {
        public string FontDataPath;
        public string[] FontTexturePathes;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var LCDFontBuilder = builder as MyObjectBuilder_LCDFontDefinition;
            if (LCDFontBuilder != null)
            {
                this.FontDataPath = LCDFontBuilder.FontDataPath;
                if (LCDFontBuilder.FontTextures != null)
                {
                    this.FontTexturePathes = LCDFontBuilder.FontTextures.Select(_ => _.Path).ToArray();
                }
                else
                {
                    this.FontTexturePathes = new string[0];
                }
            }
        }
    }
}