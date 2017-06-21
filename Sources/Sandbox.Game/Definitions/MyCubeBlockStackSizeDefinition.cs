using ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CubeBlockStackSizeDefinition))]
    public class MyCubeBlockStackSizeDefinition: MyDefinitionBase
    {
        public Dictionary<MyDefinitionId, MyFixedPoint> BlockMaxStackSizes;

        public MyCubeBlockStackSizeDefinition()
        {
            BlockMaxStackSizes = new Dictionary<MyDefinitionId, MyFixedPoint>(MyDefinitionId.Comparer);
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_CubeBlockStackSizeDefinition;
            if (ob != null)
            {
                if (ob.Blocks != null)
                {
                    foreach (var item in ob.Blocks)
                    {
                        MyObjectBuilderType typeId;
                        if (item.TypeId != null)
                        {
                            typeId = MyObjectBuilderType.Parse(item.TypeId);
                        }
                        else
                        {
                            string error = "\"TypeId\" must be defined in a block item for " + builder.Id;
                            System.Diagnostics.Debug.Assert(false, error);
                            Sandbox.MySandboxGame.Log.WriteLine(error);
                            throw new ArgumentException(error);
                        }

                        if (item.SubtypeId != null)
                        {
                            BlockMaxStackSizes.Add(new MyDefinitionId(typeId, item.SubtypeId), item.MaxStackSize);
                        }
                        else
                        {
                            string error = "\"SubtypeId\" must be defined in a block item for " + builder.Id;
                            System.Diagnostics.Debug.Assert(false, error);
                            Sandbox.MySandboxGame.Log.WriteLine(error);
                            throw new ArgumentException(error);
                        }
                    }
                }
            }
        }
    }
}
