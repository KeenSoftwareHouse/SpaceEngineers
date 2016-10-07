using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    public struct VoxelMapChange
    {
        public Dictionary<byte, byte> Changes;
    }

    [MyDefinitionType(typeof(MyObjectBuilder_VoxelMaterialModifierDefinition), typeof(Postprocessor))]
    public class MyVoxelMaterialModifierDefinition : MyDefinitionBase
    {
        public MyDiscreteSampler<VoxelMapChange> Options;

        private MyObjectBuilder_VoxelMaterialModifierDefinition m_ob;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            m_ob = (MyObjectBuilder_VoxelMaterialModifierDefinition)builder;
        }

        private class Postprocessor : MyDefinitionPostprocessor
        {
            public override void AfterLoaded(ref Bundle definitions)
            {
            }

            public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
            {
                foreach (var def in definitions.Values)
                {
                    var vmdef = (MyVoxelMaterialModifierDefinition)def;

                    vmdef.Options = new MyDiscreteSampler<VoxelMapChange>(vmdef.m_ob.Options.Select(x => new VoxelMapChange
                    {
                        Changes = x.Changes == null ? null : x.Changes.ToDictionary(
                            y => MyDefinitionManager.Static.GetVoxelMaterialDefinition(y.From).Index,
                            y => MyDefinitionManager.Static.GetVoxelMaterialDefinition(y.To).Index)
                    }), vmdef.m_ob.Options.Select(x => x.Chance));

                    Debug.Assert(vmdef.Options.Initialized);

                    vmdef.m_ob = null;
                }
            }
        }
    }
}
