using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.FileSystem;
using VRage.Game.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Definitions.Animation
{
    [MyDefinitionType(typeof(MyObjectBuilder_AnimationControllerDefinition), typeof(MyAnimationControllerDefinitionPostprocess))]
    public class MyAnimationControllerDefinition : MyDefinitionBase
    {
        // animation layers
        public List<MyObjectBuilder_AnimationLayer> Layers = new List<MyObjectBuilder_AnimationLayer>();
        // state machines (referenced by layers)
        public List<MyObjectBuilder_AnimationSM> StateMachines = new List<MyObjectBuilder_AnimationSM>();
        // ik bone chains - feet
        public List<MyObjectBuilder_AnimationFootIkChain> FootIkChains = new List<MyObjectBuilder_AnimationFootIkChain>();
        // ik - ignored bones
        public List<string> IkIgnoredBones = new List<string>();

        // init from object builder
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_AnimationControllerDefinition;
            Debug.Assert(ob != null);

            if (ob.Layers != null)
                Layers.AddRange(ob.Layers);
            if (ob.StateMachines != null)
                StateMachines.AddRange(ob.StateMachines);
            if (ob.FootIkChains != null)
                FootIkChains.AddRange(ob.FootIkChains);
            if (ob.IkIgnoredBones != null)
                IkIgnoredBones.AddRange(ob.IkIgnoredBones);
        }

        // generate object builder
        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var builder = MyDefinitionManagerBase.GetObjectFactory().CreateObjectBuilder<MyObjectBuilder_AnimationControllerDefinition>(this);

            builder.Id = Id;
            builder.Description = (DescriptionEnum.HasValue) ? DescriptionEnum.Value.ToString() : DescriptionString;
            builder.DisplayName = (DisplayNameEnum.HasValue) ? DisplayNameEnum.Value.ToString() : DisplayNameString;
            builder.Icons = Icons;
            builder.Public = Public;
            builder.Enabled = Enabled;
            builder.AvailableInSurvival = AvailableInSurvival;

            builder.StateMachines = StateMachines.ToArray();
            builder.Layers = Layers.ToArray();
            builder.FootIkChains = FootIkChains.ToArray();
            builder.IkIgnoredBones = IkIgnoredBones.ToArray();

            return builder;
        }

        public void Clear()
        {
            Layers.Clear();
            StateMachines.Clear();
            FootIkChains.Clear();
            IkIgnoredBones.Clear();
        }
    }

    internal class MyAnimationControllerDefinitionPostprocess : MyDefinitionPostprocessor
    {
        public override void AfterLoaded(ref Bundle definitions)
        {
            foreach (var def in definitions.Definitions)
            {
                MyAnimationControllerDefinition animationController = def.Value as MyAnimationControllerDefinition;
                if (animationController == null || animationController.StateMachines == null || def.Value.Context.IsBaseGame
                    || def.Value.Context == null || def.Value.Context.ModPath == null)
                    continue;

                foreach (var sm in animationController.StateMachines)
                    foreach (var node in sm.Nodes)
                        if (node.AnimationTree != null && node.AnimationTree.Child != null)
                            ResolveMwmPaths(def.Value.Context, node.AnimationTree.Child);
            }
        }

        private void ResolveMwmPaths(MyModContext modContext, MyObjectBuilder_AnimationTreeNode objBuilderNode)
        {
            // ------- tree node track -------
            var objBuilderNodeTrack = objBuilderNode as VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationTreeNodeTrack;
            if (objBuilderNodeTrack != null && objBuilderNodeTrack.PathToModel != null)
            {
                string testMwmPath = Path.Combine(modContext.ModPath, objBuilderNodeTrack.PathToModel);
                if (MyFileSystem.FileExists(testMwmPath))
                {
                    objBuilderNodeTrack.PathToModel = testMwmPath;
                }
            }
            // ------ tree node mix -----------------------
            var objBuilderNodeMix1D = objBuilderNode as MyObjectBuilder_AnimationTreeNodeMix1D;
            if (objBuilderNodeMix1D != null)
            {
                if (objBuilderNodeMix1D.Children != null)
                {
                    foreach (var mappingObjBuilder in objBuilderNodeMix1D.Children)
                        if (mappingObjBuilder.Node != null)
                            ResolveMwmPaths(modContext, mappingObjBuilder.Node);
                }
            }
            // ------ tree node add -----------------------
            var objBuilderNodeAdd = objBuilderNode as MyObjectBuilder_AnimationTreeNodeAdd;
            if (objBuilderNodeAdd != null)
            {
                if (objBuilderNodeAdd.BaseNode.Node != null)
                    ResolveMwmPaths(modContext, objBuilderNodeAdd.BaseNode.Node);
                if (objBuilderNodeAdd.AddNode.Node != null)
                    ResolveMwmPaths(modContext, objBuilderNodeAdd.AddNode.Node);
            }
        }

        public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
        {
        }

        public override void OverrideBy(ref Bundle currentDefinitions, ref Bundle overrideBySet)
        {
            foreach (var def in overrideBySet.Definitions)
            {
                MyAnimationControllerDefinition modifyingAnimationController = def.Value as MyAnimationControllerDefinition;
                if (def.Value.Enabled && modifyingAnimationController != null)
                {
                    bool justCopy = true;
                    if (currentDefinitions.Definitions.ContainsKey(def.Key))
                    {
                        MyAnimationControllerDefinition originalAnimationController = currentDefinitions.Definitions[def.Key] as MyAnimationControllerDefinition;
                        if (originalAnimationController != null)
                        {
                            foreach (var sm in modifyingAnimationController.StateMachines)
                            {
                                bool found = false;
                                foreach (var smOrig in originalAnimationController.StateMachines)
                                    if (sm.Name == smOrig.Name)
                                    {
                                        smOrig.Nodes = sm.Nodes;
                                        smOrig.Transitions = sm.Transitions;
                                        found = true;
                                        break;
                                    }

                                if (!found)
                                    originalAnimationController.StateMachines.Add(sm);
                            }

                            foreach (var layer in modifyingAnimationController.Layers)
                            {
                                bool found = false;
                                foreach (var layerOrig in originalAnimationController.Layers)
                                    if (layer.Name == layerOrig.Name)
                                    {
                                        layerOrig.Name = layer.Name;
                                        layerOrig.BoneMask = layer.BoneMask;
                                        layerOrig.InitialSMNode = layer.InitialSMNode;
                                        layerOrig.StateMachine = layer.StateMachine;
                                        layerOrig.Mode = layer.Mode;
                                        found = true;
                                    }

                                if (!found)
                                    originalAnimationController.Layers.Add(layer);
                            }

                            // TODO: IK?

                            justCopy = false;
                        }
                    }

                    if (justCopy)
                    {
                        currentDefinitions.Definitions[def.Key] = def.Value;
                    }
                }
            }
        }
    }
}
