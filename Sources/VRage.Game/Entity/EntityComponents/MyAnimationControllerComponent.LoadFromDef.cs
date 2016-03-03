using System;
using System.Diagnostics;
using System.Linq;
using VRage.Animations;
using VRage.Animations.AnimationNodes;
using VRage.FileSystem;
using VRage.Game.Definitions.Animation;
using VRage.Game.Models;
using VRage.Game.ObjectBuilders;
using VRage.Generics.StateMachine;
using VRage.Utils;

namespace VRage.Game.Components
{
    // Extension on MyAnimationControllerComponent
    // Contains implementatiomn of loading from object builder, which currently depends on the sandbox
    // 
    public static class MyAnimationControllerComponentLoadFromDef
    {
        private static readonly char[] m_boneListSeparators = {' '};

        // Initialize this animation controller from given object builder.
        // Returns true on success.
        public static bool InitFromDefinition(this VRage.Game.Components.MyAnimationControllerComponent thisController,
            MyAnimationControllerDefinition animControllerDefinition)
        {
            bool result = true;
            thisController.Clear();
            foreach (var objBuilderLayer in animControllerDefinition.Layers)
            {
                var layer = thisController.Controller.CreateLayer(objBuilderLayer.Name);
                if (layer == null)
                {
                    continue;
                }
                switch (objBuilderLayer.Mode)
                {
                    case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationLayer.MyLayerMode.Add:
                        layer.Mode = VRage.Animations.MyAnimationStateMachine.MyBlendingMode.Add;
                        break;
                    case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationLayer.MyLayerMode.Replace:
                        layer.Mode = VRage.Animations.MyAnimationStateMachine.MyBlendingMode.Replace;
                        break;
                    default:
                        Debug.Fail("Unknown layer mode.");
                        layer.Mode = VRage.Animations.MyAnimationStateMachine.MyBlendingMode.Replace;
                        break;
                }
                if (objBuilderLayer.BoneMask != null)
                {
                    string[] boneMaskArray = objBuilderLayer.BoneMask.Split(m_boneListSeparators);
                    foreach (string s in boneMaskArray)
                        layer.BoneMaskStrIds.Add(MyStringId.GetOrCompute(s));
                }
                else
                {
                    layer.BoneMaskStrIds.Clear();
                }
                layer.BoneMask = null; // this will build itself in animation controller when we know all character bones
                result = InitLayerNodes(layer, objBuilderLayer.StateMachine, animControllerDefinition, thisController.Controller) && result;
                layer.SetState(objBuilderLayer.InitialSMNode);
                // todo: bone mask
            }
            return result;
        }

        // Initialize state machine of one layer.
        private static bool InitLayerNodes(VRage.Animations.MyAnimationStateMachine layer, string stateMachineName, MyAnimationControllerDefinition animControllerDefinition,
            VRage.Animations.MyAnimationController animationController, string currentNodeNamePrefix = "")
        {
            var objBuilderStateMachine = animControllerDefinition.StateMachines.FirstOrDefault(x => x.Name == stateMachineName);
            if (objBuilderStateMachine == null)
            {
                Debug.Fail("Animation state machine " + stateMachineName + " was not found.");
                return false;
            }

            bool result = true;
            // 1st step: generate nodes
            if (objBuilderStateMachine.Nodes != null)
            foreach (var objBuilderNode in objBuilderStateMachine.Nodes)
            {
                string absoluteNodeName = currentNodeNamePrefix + objBuilderNode.Name;
                if (objBuilderNode.StateMachineName != null)
                {
                    // embedded state machine, copy its nodes
                    if (!InitLayerNodes(layer, objBuilderNode.StateMachineName, animControllerDefinition, animationController, absoluteNodeName + "/"))
                        result = false;
                }
                else
                {
                    if (objBuilderNode.AnimationTree == null)
                    {
                        // no animation tree, just skip
                        continue;
                    }

                    var smNode = new VRage.Animations.MyAnimationStateMachineNode(absoluteNodeName);
                    layer.AddNode(smNode);

                    var smNodeAnimTree = InitNodeAnimationTree(objBuilderNode.AnimationTree.Child);
                    smNode.RootAnimationNode = smNodeAnimTree;
                }
            }

            // 2nd step: generate transitions
            if (objBuilderStateMachine.Transitions != null)
            foreach (var objBuilderTransition in objBuilderStateMachine.Transitions)
            {
                int conditionConjunctionIndex = 0;
                do
                {
                    // generate transition for each condition conjunction
                    var transition = layer.AddTransition(objBuilderTransition.From, objBuilderTransition.To,
                        new VRage.Animations.MyAnimationStateMachineTransition()) as
                        VRage.Animations.MyAnimationStateMachineTransition;
                    // if ok, fill in conditions
                    if (transition != null)
                    {
                        transition.Name = MyStringId.GetOrCompute(objBuilderTransition.Name);
                        transition.TransitionTimeInSec = objBuilderTransition.TimeInSec;
                        transition.Sync = objBuilderTransition.Sync;
                        if (objBuilderTransition.Conditions != null &&
                            objBuilderTransition.Conditions[conditionConjunctionIndex] != null)
                        {
                            var conjunctionOfConditions =
                                objBuilderTransition.Conditions[conditionConjunctionIndex].Conditions;
                            foreach (var objBuilderCondition in conjunctionOfConditions)
                            {
                                var condition = ParseOneCondition(animationController, objBuilderCondition);
                                if (condition != null)
                                    transition.Conditions.Add(condition);
                            }
                        }
                    }
                    conditionConjunctionIndex++;
                } while (objBuilderTransition.Conditions != null &&
                         conditionConjunctionIndex < objBuilderTransition.Conditions.Length);
            }

            return result;
        }

        // Convert one condition from object builder to in-game representation.
        private static MyCondition<float> ParseOneCondition(MyAnimationController animationController,
            MyObjectBuilder_AnimationSMCondition objBuilderCondition)
        {
            MyCondition<float> condition;
            if (objBuilderCondition.ValueLeft == null || objBuilderCondition.ValueRight == null)
            {
                Debug.Fail("Missing operand in transition condition.");
                return null;
            }

            double lhs, rhs;
            if (Double.TryParse(objBuilderCondition.ValueLeft, out lhs))
            {
                if (Double.TryParse(objBuilderCondition.ValueRight, out rhs))
                {
                    condition =
                        new VRage.Generics.StateMachine.MyCondition<float>(
                            animationController.Variables,
                            ConvertOperation(objBuilderCondition.Operation), (float) lhs,
                            (float) rhs);
                }
                else
                {
                    condition =
                        new VRage.Generics.StateMachine.MyCondition<float>(
                            animationController.Variables,
                            ConvertOperation(objBuilderCondition.Operation), (float) lhs,
                            objBuilderCondition.ValueRight);
                }
            }
            else
            {
                if (Double.TryParse(objBuilderCondition.ValueRight, out rhs))
                {
                    condition =
                        new VRage.Generics.StateMachine.MyCondition<float>(
                            animationController.Variables,
                            ConvertOperation(objBuilderCondition.Operation),
                            objBuilderCondition.ValueLeft, (float) rhs);
                }
                else
                {
                    condition =
                        new VRage.Generics.StateMachine.MyCondition<float>(
                            animationController.Variables,
                            ConvertOperation(objBuilderCondition.Operation),
                            objBuilderCondition.ValueLeft, objBuilderCondition.ValueRight);
                }
            }
            return condition;
        }

        // Initialize animation tree of the state machine node.
        private static VRage.Animations.MyAnimationTreeNode InitNodeAnimationTree(VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationTreeNode objBuilderNode)
        {
            // ------- tree node track -------
            var objBuilderNodeTrack = objBuilderNode as VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationTreeNodeTrack;
            if (objBuilderNodeTrack != null)
            {
                var nodeTrack = new VRage.Animations.AnimationNodes.MyAnimationTreeNodeTrack();
                MyModel modelAnimation = MyModels.GetModelOnlyAnimationData(objBuilderNodeTrack.PathToModel);
                if (modelAnimation != null)
                {
                    VRage.Animations.MyAnimationClip selectedClip = modelAnimation.Animations.Clips.FirstOrDefault(clipItem => clipItem.Name == objBuilderNodeTrack.AnimationName);
                    if (selectedClip == null)
                    {
                        Debug.Fail("File '" + objBuilderNodeTrack.PathToModel + "' does not contain animation clip '" 
                            + objBuilderNodeTrack.AnimationName + "'.");
                    }
                    nodeTrack.SetClip(selectedClip);
                    nodeTrack.Loop = objBuilderNodeTrack.Loop;
                    nodeTrack.Speed = objBuilderNodeTrack.Speed;
                    nodeTrack.Interpolate = objBuilderNodeTrack.Interpolate;
                }
                return nodeTrack;
            }
            // ------ tree node mix -----------------------
            var objBuilderNodeMix1D = objBuilderNode as MyObjectBuilder_AnimationTreeNodeMix1D;
            if (objBuilderNodeMix1D != null)
            {
                var nodeMix1D = new VRage.Animations.AnimationNodes.MyAnimationTreeNodeMix1D();
                if (objBuilderNodeMix1D.Children != null)
                {
                    foreach (var mappingObjBuilder in objBuilderNodeMix1D.Children)
                    {
                        MyAnimationTreeNodeMix1D.MyParameterNodeMapping mapping = new MyAnimationTreeNodeMix1D.MyParameterNodeMapping()
                        {
                            ParamValueBinding = mappingObjBuilder.Param,
                            Child = InitNodeAnimationTree(mappingObjBuilder.Node)
                        };
                        nodeMix1D.ChildMappings.Add(mapping);
                    }
                    nodeMix1D.ChildMappings.Sort((x, y) => x.ParamValueBinding.CompareTo(y.ParamValueBinding));
                }
                nodeMix1D.ParameterName = MyStringId.GetOrCompute(objBuilderNodeMix1D.ParameterName);
                return nodeMix1D;
            }
            // ------ tree node add -----------------------
            var objBuilderNodeAdd = objBuilderNode as MyObjectBuilder_AnimationTreeNodeAdd;
            if (objBuilderNodeAdd != null)
            {
                Debug.Fail("Addition node: currently unsupported type of animation tree node.");
            }
            return null;
        }

        // Find animation definition
        private static bool TryGetAnimationDefinition(string animationSubtypeName, 
            out MyAnimationDefinition animDefinition)
        {
            if (animationSubtypeName == null)
            {
                animDefinition = null;
                return false;
            }

            MyDefinitionManagerBase.Static.TryGetDefinition<MyAnimationDefinition>(MyStringHash.GetOrCompute(animationSubtypeName), 
                out animDefinition);
            if (animDefinition == null)
            {
                //Try backward compatibility
                //Backward compatibility
                string oldPath = System.IO.Path.Combine(MyFileSystem.ContentPath, animationSubtypeName);
                if (MyFileSystem.FileExists(oldPath))
                {
                    animDefinition = new MyAnimationDefinition()
                    {
                        AnimationModel = oldPath,
                        ClipIndex = 0,
                    };
                    return true;
                }

                animDefinition = null;
                return false;
            }

            return true;
        }

        private static VRage.Generics.StateMachine.MyCondition<float>.MyOperation ConvertOperation(VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType operation)
        {
            switch (operation)
            {
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.AlwaysFalse:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.AlwaysFalse;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.AlwaysTrue:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.AlwaysTrue;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.Equal:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.Equal;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.Greater:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.Greater;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.GreaterOrEqual:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.GreaterOrEqual;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.Less:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.Less;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.LessOrEqual:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.LessOrEqual;
                case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationSMCondition.MyOperationType.NotEqual:
                    return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.NotEqual;
                default:
                    Debug.Fail("Unknown or null operator in transition condition!");
                    break;
            }
            return VRage.Generics.StateMachine.MyCondition<float>.MyOperation.AlwaysFalse;
        }

    }
}
