using System;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CompoundBlockTemplateDefinition))]
    public class MyCompoundBlockTemplateDefinition : MyDefinitionBase
    {
        public class MyCompoundBlockRotationBinding
        {
            public MyStringId BuildTypeReference;
            public MyBlockOrientation[] Rotations;
        }

        public class MyCompoundBlockBinding
        {
            public MyStringId BuildType;
            public bool Multiple;
            public MyCompoundBlockRotationBinding[] RotationBinds;
        }

        public MyCompoundBlockBinding[] Bindings;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CompoundBlockTemplateDefinition;
            MyDebug.AssertDebug(ob != null);

            if (ob.Bindings != null)
            {
                this.Bindings = new MyCompoundBlockBinding[ob.Bindings.Length];

                for (int i = 0; i < ob.Bindings.Length; ++i)
                {
                    MyCompoundBlockBinding binding = new MyCompoundBlockBinding();

                    binding.BuildType = MyStringId.GetOrCompute(ob.Bindings[i].BuildType != null ? ob.Bindings[i].BuildType.ToLower() : null);

                    binding.Multiple = ob.Bindings[i].Multiple;

                    if (ob.Bindings[i].RotationBinds != null && ob.Bindings[i].RotationBinds.Length > 0)
                    {
                        binding.RotationBinds = new MyCompoundBlockRotationBinding[ob.Bindings[i].RotationBinds.Length];

                        for (int rotationBind = 0; rotationBind < ob.Bindings[i].RotationBinds.Length; ++rotationBind)
                        {
                            if (ob.Bindings[i].RotationBinds[rotationBind].Rotations != null && ob.Bindings[i].RotationBinds[rotationBind].Rotations.Length > 0)
                            {
                                binding.RotationBinds[rotationBind] = new MyCompoundBlockRotationBinding();
                                binding.RotationBinds[rotationBind].BuildTypeReference = MyStringId.GetOrCompute(ob.Bindings[i].RotationBinds[rotationBind].BuildTypeReference != null ? ob.Bindings[i].RotationBinds[rotationBind].BuildTypeReference.ToLower() : null);

                                binding.RotationBinds[rotationBind].Rotations = new MyBlockOrientation[ob.Bindings[i].RotationBinds[rotationBind].Rotations.Length];

                                for (int rotNo = 0; rotNo < ob.Bindings[i].RotationBinds[rotationBind].Rotations.Length; ++rotNo)
                                {
                                    binding.RotationBinds[rotationBind].Rotations[rotNo] = ob.Bindings[i].RotationBinds[rotationBind].Rotations[rotNo];
                                }
                            }
                        }
                    }

                    this.Bindings[i] = binding;
                }
            }
            else {
                this.Bindings = null;
            }
        }

    }
}
