using System;
using System.Collections.Generic;
using System.IO;
using VRageRender.Animations;
using VRage.FileSystem;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.Generics;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Utils;

namespace VRage.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentAnimationSystem : MySessionComponentBase
    {
        // Static reference to this session component.
        public static MySessionComponentAnimationSystem Static = null;
        // All registered skinned entity components.
        private readonly HashSet<MyAnimationControllerComponent> m_skinnedEntityComponents = new HashSet<MyAnimationControllerComponent>();

        private readonly List<MyAnimationControllerComponent> m_skinnedEntityComponentsToAdd = new List<MyAnimationControllerComponent>(32);
        private readonly List<MyAnimationControllerComponent> m_skinnedEntityComponentsToRemove = new List<MyAnimationControllerComponent>(32);

        private readonly FastResourceLock m_lock = new FastResourceLock();

        // variables for live debugging 
        private int m_debuggingSendNameCounter = 0;
        private const int m_debuggingSendNameCounterMax = 60;
        private string m_debuggingLastNameSent = null;
        private readonly List<MyStateMachineNode> m_debuggingAnimControllerCurrentNodes = new List<MyStateMachineNode>(); 
        private readonly List<int[]> m_debuggingAnimControllerTreePath = new List<int[]>();

        public MyEntity EntitySelectedForDebug; // override default selection = controlled object

        public IEnumerable<MyAnimationControllerComponent> RegisteredAnimationComponents
        {
            get { return m_skinnedEntityComponents; }
        }

        public override void LoadData()
        {
            EntitySelectedForDebug = null;
            m_skinnedEntityComponents.Clear();
            m_skinnedEntityComponentsToAdd.Clear();
            m_skinnedEntityComponentsToRemove.Clear();
            MySessionComponentAnimationSystem.Static = this;

#if !XB1
            if (!MySessionComponentExtDebug.Static.IsHandlerRegistered(LiveDebugging_ReceivedMessageHandler))
                MySessionComponentExtDebug.Static.ReceivedMsg += LiveDebugging_ReceivedMessageHandler;
#endif // !XB1
        }

        protected override void UnloadData()
        {
            EntitySelectedForDebug = null;
            m_skinnedEntityComponents.Clear();
            m_skinnedEntityComponentsToAdd.Clear();
            m_skinnedEntityComponentsToRemove.Clear();
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("New Animation System");

            using (m_lock.AcquireExclusiveUsing())
            {
                foreach (var toRemove in m_skinnedEntityComponentsToRemove)
                {
                    if (m_skinnedEntityComponents.Contains(toRemove))
                        m_skinnedEntityComponents.Remove(toRemove);
                }
                m_skinnedEntityComponentsToRemove.Clear();

                foreach (var toAdd in m_skinnedEntityComponentsToAdd)
                {
                    m_skinnedEntityComponents.Add(toAdd);
                }
                m_skinnedEntityComponentsToAdd.Clear();
            }

            foreach (MyAnimationControllerComponent skinnedEntityComp in m_skinnedEntityComponents)
                skinnedEntityComp.Update();
            ProfilerShort.End();

#if !XB1
            LiveDebugging();
#endif // !XB1
        }

        /// <summary>
        /// Register entity component.
        /// </summary>
        internal void RegisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_skinnedEntityComponentsToAdd.Add(entityComponent);
            }
        }

        /// <summary>
        /// Unregister entity component.
        /// </summary>
        internal void UnregisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_skinnedEntityComponentsToRemove.Add(entityComponent);
            }
        }

        // --------------- LIVE DEBUGGING -----------------------------------------------------------------

#if !XB1
        private void LiveDebugging()
        {
            if (Session == null || MySessionComponentExtDebug.Static == null/* || !MySessionComponentExtDebug.Static.HasClients*/)
                return;

            MyEntity localSkinnedEntity = EntitySelectedForDebug ?? (Session.ControlledObject != null ? Session.ControlledObject.Entity as MyEntity : null);
            if (localSkinnedEntity == null)
                return;
            MyAnimationControllerComponent localSkinnedEntityAnimComponent = localSkinnedEntity.Components.Get<MyAnimationControllerComponent>();
            if (localSkinnedEntityAnimComponent == null || localSkinnedEntityAnimComponent.SourceId.TypeId.IsNull)
                return;

            // send AC name (connect / reconnect)
            m_debuggingSendNameCounter--;
            if (localSkinnedEntityAnimComponent.SourceId.SubtypeName != m_debuggingLastNameSent)
                m_debuggingSendNameCounter = 0;
            if (m_debuggingSendNameCounter <= 0)
            {
                LiveDebugging_SendControllerNameToEditor(localSkinnedEntityAnimComponent.SourceId.SubtypeName);
                m_debuggingSendNameCounter = m_debuggingSendNameCounterMax;
                m_debuggingLastNameSent = localSkinnedEntityAnimComponent.SourceId.SubtypeName;
            }

            // animation states
            LiveDebugging_SendAnimationStateChangesToEditor(localSkinnedEntityAnimComponent.Controller);
        }

        private void LiveDebugging_SendControllerNameToEditor(string subtypeName)
        {
            var msg = new MyExternalDebugStructures.ACConnectToEditorMsg()
            {
                ACName = subtypeName
            };
            MySessionComponentExtDebug.Static.SendMessageToClients(msg);
        }

        private void LiveDebugging_SendAnimationStateChangesToEditor(MyAnimationController animController)
        {
            if (animController == null)
                return;

            int layerCount = animController.GetLayerCount();
            if (layerCount != m_debuggingAnimControllerCurrentNodes.Count)
            {
                m_debuggingAnimControllerCurrentNodes.Clear();
                for (int i = 0; i < layerCount; i++)
                    m_debuggingAnimControllerCurrentNodes.Add(null);

                m_debuggingAnimControllerTreePath.Clear();
                for (int i = 0; i < layerCount; i++)
                {
                    m_debuggingAnimControllerTreePath.Add(new int[animController.GetLayerByIndex(i).VisitedTreeNodesPath.Length]);
                }
            }

            for (int i = 0; i < layerCount; i++)
            {
                var layerVisitedTreeNodesPath = animController.GetLayerByIndex(i).VisitedTreeNodesPath;
                if (animController.GetLayerByIndex(i).CurrentNode != m_debuggingAnimControllerCurrentNodes[i]
                    || !LiveDebugging_CompareAnimTreePathSeqs(layerVisitedTreeNodesPath, m_debuggingAnimControllerTreePath[i]))
                {
                    Array.Copy(layerVisitedTreeNodesPath, m_debuggingAnimControllerTreePath[i], layerVisitedTreeNodesPath.Length); // local copy
                    m_debuggingAnimControllerCurrentNodes[i] = animController.GetLayerByIndex(i).CurrentNode;
                    if (m_debuggingAnimControllerCurrentNodes[i] != null)
                    {
                        var msg =
                            MyExternalDebugStructures.ACSendStateToEditorMsg.Create(m_debuggingAnimControllerCurrentNodes[i].Name, m_debuggingAnimControllerTreePath[i]);
                        MySessionComponentExtDebug.Static.SendMessageToClients(msg);
                    }
                }
            }
        }

        private static bool LiveDebugging_CompareAnimTreePathSeqs(int[] seq1, int[] seq2)
        {
            if (seq1 == null || seq2 == null || seq1.Length != seq2.Length)
                return false;

            for (int i = 0; i < seq1.Length; i++)
            {
                if (seq1[i] != seq2[i])
                    return false;
                if (seq1[i] == 0 && seq2[i] == 0)
                    return true;
            }

            return true;
        }

        // receiving messages
        private void LiveDebugging_ReceivedMessageHandler(MyExternalDebugStructures.CommonMsgHeader messageHeader, IntPtr messageData)
        {
            MyExternalDebugStructures.ACReloadInGameMsg msgReload;
            if (MyExternalDebugStructures.ReadMessageFromPtr(ref messageHeader, messageData, out msgReload))
            {
                try
                {
                    string acContentPath = msgReload.ACContentAddress;
                    string acAddress = msgReload.ACAddress;
                    string acName = msgReload.ACName;

                    MyObjectBuilder_Definitions allDefinitions; // = null;
                    // load animation controller definition from SBC file
                    if (MyObjectBuilderSerializer.DeserializeXML(acAddress, out allDefinitions) &&
                        allDefinitions.Definitions != null &&
                        allDefinitions.Definitions.Length > 0)
                    {
                        var firstDef = allDefinitions.Definitions[0];
                        MyModContext context = new MyModContext();
                        context.Init("AnimationControllerDefinition", acAddress, acContentPath);
                        MyAnimationControllerDefinition animationControllerDefinition = new MyAnimationControllerDefinition();
                        animationControllerDefinition.Init(firstDef, context);
                        MyStringHash animSubtypeNameHash = MyStringHash.GetOrCompute(acName);

                        // post process and update in def. manager
                        MyAnimationControllerDefinition originalAnimationControllerDefinition =
                            MyDefinitionManagerBase.Static.GetDefinition<MyAnimationControllerDefinition>(
                                animSubtypeNameHash);

                        var postprocessor = MyDefinitionManagerBase.GetPostProcessor(typeof(MyObjectBuilder_AnimationControllerDefinition));
                        if (postprocessor != null)
                        {
                            MyDefinitionPostprocessor.Bundle originalBundle = new MyDefinitionPostprocessor.Bundle
                            {
                                Context = MyModContext.BaseGame,
                                Definitions = new Dictionary<MyStringHash, MyDefinitionBase>
                                {
                                    {animSubtypeNameHash, originalAnimationControllerDefinition}
                                },
                                Set = new MyDefinitionSet()
                            };
                            originalBundle.Set.AddDefinition(originalAnimationControllerDefinition);

                            MyDefinitionPostprocessor.Bundle overridingBundle = new MyDefinitionPostprocessor.Bundle
                            {
                                Context = context,
                                Definitions = new Dictionary<MyStringHash, MyDefinitionBase>
                                {
                                    {animSubtypeNameHash, animationControllerDefinition}
                                },
                                Set = new MyDefinitionSet()
                            };
                            overridingBundle.Set.AddDefinition(animationControllerDefinition);

                            // postprocess -> override existing definition in memory
                            postprocessor.AfterLoaded(ref overridingBundle);
                            postprocessor.OverrideBy(ref originalBundle, ref overridingBundle);
                        }

                        // swap animation controller for each entity
                        foreach (var component in m_skinnedEntityComponents)
                        {
                            if (component != null && component.SourceId.SubtypeName == acName)
                            {
                                component.Clear();
                                component.InitFromDefinition(originalAnimationControllerDefinition, forceReloadMwm: true); // reload from original def that was modified by postprocessor
                                if (component.ReloadBonesNeeded != null)
                                    component.ReloadBonesNeeded();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e);
                }
            }
        }
#endif // !XB1
        // --------------------------------------------------------------------------------------------

        /// <summary>
        /// Reload all mwm tracks while in-game. Mwms from cache are not used. 
        /// </summary>
        public void ReloadMwmTracks()
        {
            foreach (var component in m_skinnedEntityComponents)
            {
                MyAnimationControllerDefinition animationControllerDefinition =
                    MyDefinitionManagerBase.Static.GetDefinition<MyAnimationControllerDefinition>(MyStringHash.GetOrCompute(component.SourceId.SubtypeName));
                if (animationControllerDefinition != null)
                {
                    component.Clear();
                    component.InitFromDefinition(animationControllerDefinition, forceReloadMwm: true); // reload from original def that was modified by postprocessor
                    if (component.ReloadBonesNeeded != null)
                        component.ReloadBonesNeeded();
                }
            }
        }
        // --------------------------------------------------------------------------------------------

    }
}
