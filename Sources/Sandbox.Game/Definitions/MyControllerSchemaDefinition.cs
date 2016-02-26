using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ControllerSchemaDefinition))]
    public class MyControllerSchemaDefinition : MyDefinitionBase
    {
        public class ControlGroup
        {
            public string Type;
            public string Name;
            public Dictionary<string, MyControllerSchemaEnum> ControlBinding;
        }

        public List<int> CompatibleDevices;
        public Dictionary<string, List<ControlGroup>> Schemas;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_ControllerSchemaDefinition;

            if (ob.CompatibleDeviceIds != null)
            {
                CompatibleDevices = new List<int>(ob.CompatibleDeviceIds.Count);
                byte[] tmpArr = new byte[4];
                foreach (var cont in ob.CompatibleDeviceIds)
                {
                    if (cont.Length < 8)
                    {
                        Debug.Assert(false, "Invalid device id");
                        continue;
                    }

                    if (TryGetByteArray(cont, 8, out tmpArr))
                    {
                        int packed = BitConverter.ToInt32(tmpArr, 0);
                        CompatibleDevices.Add(packed);
                    }
                    else
                    {
                        Debug.Assert(false, "Invalid device id");
                    }       
                }
            }
            
            if (ob.Schemas != null)
            {
                Schemas = new Dictionary<string, List<ControlGroup>>(ob.Schemas.Count);
                foreach (var schema in ob.Schemas)
                {
                    Debug.Assert(!Schemas.ContainsKey(schema.SchemaName));
                    if (schema.ControlGroups != null)
                    {
                        var controlGroups = new List<ControlGroup>(schema.ControlGroups.Count);
                        Schemas[schema.SchemaName] = controlGroups;

                        foreach (var controlGroup in schema.ControlGroups)
                        {
                            var newControlGroup = new ControlGroup();
                            newControlGroup.Type = controlGroup.Type;
                            newControlGroup.Name = controlGroup.Name;
                            if (controlGroup.ControlDefs != null)
                            {
                                newControlGroup.ControlBinding = new Dictionary<string, MyControllerSchemaEnum>(controlGroup.ControlDefs.Count);
                                foreach (var def in controlGroup.ControlDefs)
                                {
                                    newControlGroup.ControlBinding[def.Type] = def.Control;
                                }
                            }
                        }
                    }
                }
            }
        }
    
        private bool TryGetByteArray(string str, int count, out byte[] arr)
        {
            arr = null;
            if (count % 2 == 1)
                return false;
            if (str.Length < count)
                return false;

            arr = new byte[count / 2];
            StringBuilder sb = new StringBuilder();
            for (int i = 0, j = 0; i < count; i+=2, j++)
            {
                sb.Clear().Append(str[i]).Append(str[i + 1]);

                if (!Byte.TryParse(sb.ToString(), System.Globalization.NumberStyles.HexNumber, null, out arr[j]))
                    return false;
            }

            return true;
        }

    }
}
