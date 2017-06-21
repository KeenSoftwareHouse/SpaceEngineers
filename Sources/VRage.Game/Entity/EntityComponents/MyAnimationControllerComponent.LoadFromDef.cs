using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRageRender.Animations;
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
        private struct MyAnimationVirtualNodeData
        {
            public bool ExceptTarget;
            public string AnyNodePrefix;
        }
        private class MyAnimationVirtualNodes
        {
            public readonly Dictionary<string, MyAnimationVirtualNodeData> NodesAny = new Dictionary<string, MyAnimationVirtualNodeData>();
        }
        private static readonly char[] m_boneListSeparators = {' '};

        // Initialize this animation controller from given object builder.
        // param forceReloadMwm: (Re)load MWM files even if they are in cache.
        // Returns true on success.
        public static bool InitFromDefinition(this VRage.Game.Components.MyAnimationControllerComponent thisController,
            MyAnimationControllerDefinition animControllerDefinition, bool forceReloadMwm = false)
        {
            bool result = true;
            thisController.Clear();

            thisController.SourceId = animControllerDefinition.Id;
            
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
                        layer.Mode = VRageRender.Animations.MyAnimationStateMachine.MyBlendingMode.Add;
                        break;
                    case VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationLayer.MyLayerMode.Replace:
                        layer.Mode = VRageRender.Animations.MyAnimationStateMachine.MyBlendingMode.Replace;
                        break;
                    default:
                        Debug.Fail("Unknown layer mode.");
                        layer.Mode = VRageRender.Animations.MyAnimationStateMachine.MyBlendingMode.Replace;
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
                MyAnimationVirtualNodes virtualNodes = new MyAnimationVirtualNodes();
                result = InitLayerNodes(layer, objBuilderLayer.StateMachine, animControllerDefinition, thisController.Controller, layer.Name + "/",
                    virtualNodes, forceReloadMwm) && result;
                layer.SetState(layer.Name + "/" + objBuilderLayer.InitialSMNode);
                layer.SortTransitions();
            }

            foreach (var footIkChain in animControllerDefinition.FootIkChains)
                thisController.InverseKinematics.RegisterFootBone(footIkChain.FootBone, footIkChain.ChainLength, footIkChain.AlignBoneWithTerrain);
            foreach (var ignoredBone in animControllerDefinition.IkIgnoredBones)
                thisController.InverseKinematics.RegisterIgnoredBone(ignoredBone);

            if (result)
                thisController.MarkAsValid();
            return result;
        }

        // Initialize state machine of one layer.
        private static bool InitLayerNodes(MyAnimationStateMachine layer, string stateMachineName, MyAnimationControllerDefinition animControllerDefinition,
            MyAnimationController animationController, string currentNodeNamePrefix, MyAnimationVirtualNodes virtualNodes, bool forceReloadMwm)
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
                    if (!InitLayerNodes(layer, objBuilderNode.StateMachineName, animControllerDefinition, animationController, absoluteNodeName + "/", virtualNodes, forceReloadMwm))
                        result = false;
                }
                else
                {
                    var smNode = new VRageRender.Animations.MyAnimationStateMachineNode(absoluteNodeName);
                    if (objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.PassThrough
                        || objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.Any
                        || objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.AnyExceptTarget)
                    {
                        smNode.PassThrough = true;
                    }
                    else
                    {
                        smNode.PassThrough = false;
                    }

                    if (objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.Any
                        || objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.AnyExceptTarget)
                    {
                        virtualNodes.NodesAny.Add(absoluteNodeName, new MyAnimationVirtualNodeData()
                        {
                            AnyNodePrefix = currentNodeNamePrefix,
                            ExceptTarget = (objBuilderNode.Type == MyObjectBuilder_AnimationSMNode.MySMNodeType.AnyExceptTarget)
                        });
                    }

                    layer.AddNode(smNode);

                    if (objBuilderNode.AnimationTree != null)
                    {
                        var smNodeAnimTree = InitNodeAnimationTree(objBuilderNode.AnimationTree.Child, forceReloadMwm);
                        smNode.RootAnimationNode = smNodeAnimTree;
                    }
                    else
                    {
                        smNode.RootAnimationNode = new MyAnimationTreeNodeDummy();
                    }
                }
            }

            // 2nd step: generate transitions
            if (objBuilderStateMachine.Transitions != null)
            foreach (var objBuilderTransition in objBuilderStateMachine.Transitions)
            {
                string absoluteNameNodeFrom = currentNodeNamePrefix + objBuilderTransition.From;
                string absoluteNameNodeTo = currentNodeNamePrefix + objBuilderTransition.To;

                MyAnimationVirtualNodeData virtualNodeData;
                if (virtualNodes.NodesAny.TryGetValue(absoluteNameNodeFrom, out virtualNodeData))
                {
                    // nodes of type "any":
                    // "any" node is source: we create transitions directly from all nodes
                    // "any" node is target: we will use "any" node as pass through
                    foreach (var nodeFromCandidate in layer.AllNodes)
                    {
                        if (nodeFromCandidate.Key.StartsWith(virtualNodeData.AnyNodePrefix) // select nodes in the same SM
                            && nodeFromCandidate.Key != absoluteNameNodeFrom)    // disallow from "any" to the same "any"
                        {
                            // create transition if target is different from source or when we don't care about it
                            if (!virtualNodeData.ExceptTarget || absoluteNameNodeTo != nodeFromCandidate.Key)
                                CreateTransition(layer, animationController, nodeFromCandidate.Key, absoluteNameNodeTo, objBuilderTransition);
                        }
                    }
                }

                CreateTransition(layer, animationController, absoluteNameNodeFrom, absoluteNameNodeTo, objBuilderTransition);
            }

            return result;
        }

        private static void CreateTransition(MyAnimationStateMachine layer, MyAnimationController animationController,
            string absoluteNameNodeFrom, string absoluteNameNodeTo, MyObjectBuilder_AnimationSMTransition objBuilderTransition)
        {
            int conditionConjunctionIndex = 0;
            do
            {
                // generate transition for each condition conjunction
                var transition = layer.AddTransition(absoluteNameNodeFrom, absoluteNameNodeTo,
                    new VRageRender.Animations.MyAnimationStateMachineTransition()) as
                    VRageRender.Animations.MyAnimationStateMachineTransition;
                // if ok, fill in conditions
                if (transition != null)
                {
                    transition.Name =
                        MyStringId.GetOrCompute(objBuilderTransition.Name != null ? objBuilderTransition.Name.ToLower() : null);
                    transition.TransitionTimeInSec = objBuilderTransition.TimeInSec;
                    transition.Sync = objBuilderTransition.Sync;
                    transition.Priority = objBuilderTransition.Priority;
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

        // Convert one condition from object builder to in-game representation.
        private static MyCondition<float> ParseOneCondition(MyAnimationController animationController,
            MyObjectBuilder_AnimationSMCondition objBuilderCondition)
        {
            MyCondition<float> condition;

            objBuilderCondition.ValueLeft = objBuilderCondition.ValueLeft != null ? objBuilderCondition.ValueLeft.ToLower() : "0";
            objBuilderCondition.ValueRight = objBuilderCondition.ValueRight != null ? objBuilderCondition.ValueRight.ToLower() : "0";
            double lhs, rhs;
            if (Double.TryParse(objBuilderCondition.ValueLeft, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lhs))
            {
                if (Double.TryParse(objBuilderCondition.ValueRight, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rhs))
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
                if (Double.TryParse(objBuilderCondition.ValueRight, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rhs))
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
        private static MyAnimationTreeNode InitNodeAnimationTree(VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationTreeNode objBuilderNode, bool forceReloadMwm)
        {
            // ------- tree node track -------
            var objBuilderNodeTrack = objBuilderNode as VRage.Game.ObjectBuilders.MyObjectBuilder_AnimationTreeNodeTrack;
            if (objBuilderNodeTrack != null)
            {
                var nodeTrack = new MyAnimationTreeNodeTrack();
                MyModel modelAnimation = objBuilderNodeTrack.PathToModel != null ? MyModels.GetModelOnlyAnimationData(objBuilderNodeTrack.PathToModel, forceReloadMwm) : null;
                if (modelAnimation != null && modelAnimation.Animations != null && modelAnimation.Animations.Clips != null && modelAnimation.Animations.Clips.Count > 0)
                {
                    VRageRender.Animations.MyAnimationClip selectedClip = modelAnimation.Animations.Clips.FirstOrDefault(clipItem => clipItem.Name == objBuilderNodeTrack.AnimationName);
                    selectedClip = selectedClip ?? modelAnimation.Animations.Clips[0]; // fallback
                    if (selectedClip == null)
                    {
                        Debug.Fail("File '" + objBuilderNodeTrack.PathToModel + "' does not contain animation clip '" 
                            + objBuilderNodeTrack.AnimationName + "'.");
                    }
                    nodeTrack.SetClip(selectedClip);
                    nodeTrack.Loop = objBuilderNodeTrack.Loop;
                    nodeTrack.Speed = objBuilderNodeTrack.Speed;
                    nodeTrack.Interpolate = objBuilderNodeTrack.Interpolate;
                    nodeTrack.SynchronizeWithLayer = objBuilderNodeTrack.SynchronizeWithLayer;
                }
                else if (objBuilderNodeTrack.PathToModel != null)
                {
                    MyLog.Default.Log(MyLogSeverity.Error, "Cannot load MWM track {0}.", objBuilderNodeTrack.PathToModel);
                    Debug.Fail("Cannot load MWM track " + objBuilderNodeTrack.PathToModel);
                }
                return nodeTrack;
            }
            // ------ tree node mix -----------------------
            var objBuilderNodeMix1D = objBuilderNode as MyObjectBuilder_AnimationTreeNodeMix1D;
            if (objBuilderNodeMix1D != null)
            {
                var nodeMix1D = new MyAnimationTreeNodeMix1D();
                if (objBuilderNodeMix1D.Children != null)
                {
                    foreach (var mappingObjBuilder in objBuilderNodeMix1D.Children)
                    {
                        MyAnimationTreeNodeMix1D.MyParameterNodeMapping mapping = new MyAnimationTreeNodeMix1D.MyParameterNodeMapping()
                        {
                            ParamValueBinding = mappingObjBuilder.Param,
                            Child = InitNodeAnimationTree(mappingObjBuilder.Node, forceReloadMwm)
                        };
                        nodeMix1D.ChildMappings.Add(mapping);
                    }
                    nodeMix1D.ChildMappings.Sort((x, y) => x.ParamValueBinding.CompareTo(y.ParamValueBinding));
                }
                nodeMix1D.ParameterName = MyStringId.GetOrCompute(objBuilderNodeMix1D.ParameterName);
                nodeMix1D.Circular = objBuilderNodeMix1D.Circular;
                nodeMix1D.Sensitivity = objBuilderNodeMix1D.Sensitivity;
                nodeMix1D.MaxChange = objBuilderNodeMix1D.MaxChange ?? float.PositiveInfinity;
                if (nodeMix1D.MaxChange <= 0.0f)
                    nodeMix1D.MaxChange = float.PositiveInfinity;
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
