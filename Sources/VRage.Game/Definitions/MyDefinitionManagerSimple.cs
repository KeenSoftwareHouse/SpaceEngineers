using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Game.Definitions;

namespace VRage.Game
{
    /// <summary>
    /// Simple definition manager class that allows loading of definitions from files
    /// and support type overrides (e.g. for loading subset of EnvironmentDefinition)
    /// </summary>
    public class MyDefinitionManagerSimple : MyDefinitionManagerBase
    {
        private Dictionary<string, string> m_overrideMap = new Dictionary<string, string>();

        /// <param name="typeOverride">The xst:type atrribute overridden</param>
        public void AddDefinitionOverride(Type overridingType, string typeOverride)
        {
            var attribute = overridingType.GetCustomAttribute(typeof(MyDefinitionTypeAttribute), false) as MyDefinitionTypeAttribute;
            if (attribute == null)
                throw new Exception("Missing type attribute in definition");

            string overridingTypeName;
            var xmlTypeAttribute = attribute.ObjectBuilderType.GetCustomAttribute(typeof(XmlTypeAttribute), false) as XmlTypeAttribute;
            if (xmlTypeAttribute == null)
                overridingTypeName = attribute.ObjectBuilderType.Name;
            else
                overridingTypeName = xmlTypeAttribute.TypeName;

            m_overrideMap[typeOverride] = overridingTypeName;
        }

        public void LoadDefinitions(string path)
        {
            bool result = false;
            MyObjectBuilder_Definitions builder = null;
            using (var fileStream = MyFileSystem.OpenRead(path))
            {
                if (fileStream != null)
                {
                    using (var readStream = fileStream.UnwrapGZip())
                    {
                        if (readStream != null)
                        {
                            MyObjectBuilder_Base obj;
                            result = MyObjectBuilderSerializer.DeserializeXML(readStream, out obj, typeof(MyObjectBuilder_Definitions), m_overrideMap);
                            builder = obj as MyObjectBuilder_Definitions;
                        }
                    }
                }
            }

            if (!result)
                throw new Exception("Error while reading \"" + path + "\"");

            if (builder.Definitions != null)
            {
                foreach (MyObjectBuilder_DefinitionBase definitionBuilder in builder.Definitions)
                {
                    MyObjectBuilderType.RemapType(ref definitionBuilder.Id, m_overrideMap);
                    MyDefinitionBase definition = GetObjectFactory().CreateInstance(definitionBuilder.TypeId);
                    definition.Init(builder.Definitions[0], new MyModContext());
                    m_definitions.AddDefinition(definition);
                }
            }
        }

        public override MyDefinitionSet GetLoadingSet()
        {
            throw new NotImplementedException();
        }
    }
}
