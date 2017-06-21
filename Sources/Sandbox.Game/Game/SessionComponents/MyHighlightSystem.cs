using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Network;
using VRageMath;
using VRageRender;
using VRageRender.Import;

namespace Sandbox.Game.SessionComponents
{

    /// <summary>
    /// System designed to propagate highlights over the network. 
    /// The replication happens only for server calls.
    /// Client side cannot ask for highlights on other clients.
    /// </summary>
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyHighlightSystem : MySessionComponentBase
    {
        private static MyHighlightSystem m_static;

        public struct MyHighlightData
        {
            /// <summary>
            /// Id of entity that should be highlighted.
            /// </summary>
            public long EntityId;
            /// <summary>
            /// Color of highlight overlay.
            /// </summary>
            public Color? OutlineColor;
            /// <summary>
            /// Overlay thickness.
            /// </summary>
            public int Thickness;
            /// <summary>
            /// Number of frames between pulses.
            /// </summary>
            public ulong PulseTimeInFrames;
            /// <summary>
            /// Id of player that should do the highlight.
            /// (For non local players its send to client)
            /// </summary>
            public long PlayerId;
            /// <summary>
            /// When set to true the system does not use the 
            /// IMyUseObject logic to process the highlight.
            /// </summary>
            public bool IgnoreUseObjectData;
            /// <summary>
            /// Specify there the names of the subparts that would be highlighted
            /// instead of the full model.
            /// Format: "subpart_1;subpart_2"
            /// </summary>
            public string SubPartNames;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="entityId">Id of entity that should be highlighted.</param>
            /// <param name="thickness">Overlay thickness.</param>
            /// <param name="pulseTimeInFrames">Number of frames between the pulses.</param>
            /// <param name="outlineColor">Color of overlay.</param>
            /// <param name="ignoreUseObjectData">Used to ignore IMyUseObject logic for highlighting.</param>
            /// <param name="playerId">Id of receiving player.</param>
            /// <param name="subPartNames">Names of subparts that should be highlighted instead of the full model.</param>
            public MyHighlightData(long entityId = 0, int thickness = -1, ulong pulseTimeInFrames = 0,
                Color? outlineColor = null, bool ignoreUseObjectData = false, long playerId = -1, string subPartNames = null)
            {
                EntityId = entityId;
                Thickness = thickness;
                OutlineColor = outlineColor;
                PulseTimeInFrames = pulseTimeInFrames;
                PlayerId = playerId;
                IgnoreUseObjectData = ignoreUseObjectData;
                SubPartNames = subPartNames;
            }
        }

        // Multiplayer message implementation
        struct HighlightMsg
        {
            // Render message parameters
            public MyHighlightData  Data;
            // Exclusive key value
            public int              ExclusiveKey;
            // Exclusivity flag
            public bool             IsExclusive;
        }   

        // Server side key counter (Just cannot be 0)
        private static int m_exclusiveKeyCounter = 10;

        // local exclusive highlights
        private readonly Dictionary<int, long>          m_exclusiveKeysToIds = new Dictionary<int, long>();
        // Local highlighted ids (EntityIds)
        private readonly HashSet<long>                  m_highlightedIds = new HashSet<long>(); 
        // This could be removed probably
        private readonly MyHudSelectedObject            m_highlightCalculationHelper = new MyHudSelectedObject();
        // Utility list for subpart indicies
        private readonly List<uint>                     m_subPartIndicies = new List<uint>();

        public event Action<MyHighlightData> HighlightRejected;
        public event Action<MyHighlightData> HighlightAccepted;
        public event Action<MyHighlightData,int> ExclusiveHighlightRejected;
        public event Action<MyHighlightData,int> ExclusiveHighlightAccepted;
        
        // Constructor that only registers the static member.
        public MyHighlightSystem() { m_static = this; }

        /// <summary>
        /// Requests highlight from render proxy. The call is handled localy for
        /// default player id. Only server can use the player id to propagate the 
        /// Highlight calls to clients.
        /// </summary>
        /// <param name="data">Highlight data wrapper.</param>
        /// <param name="playerId">Player Identity Id.</param>
        public void RequestHighlightChange(MyHighlightData data)
        {
            ProcessRequest(
                data,
                -1,
                false);
        }

        /// <summary>
        /// Requests Exclusive highlight from render proxy. The call is handled localy for
        /// default player id. Only server can use the player id to propagate the 
        /// Highlight calls to clients.
        /// Uses Exclusive key as lock accessor. Can be obtained from ExclusiveHighlightAccepted event.
        /// </summary>
        /// <param name="data">Highlight data wrapper.</param>
        /// <param name="exclusiveKey">Exclusive key.</param>
        /// <param name="playerId">Player Identity Id.</param>
        public void RequestHighlightChangeExclusive(MyHighlightData data, int exclusiveKey = -1)
        {
            ProcessRequest(
                data,
                exclusiveKey,
                true);
        }

        /// <summary>
        /// Determines whenever is the entity highlighted by the system or not.
        /// </summary>
        /// <param name="entityId">Id of entity.</param>
        /// <returns>Highlighted.</returns>
        public bool IsHighlighted(long entityId)
        {
            return m_highlightedIds.Contains(entityId);
        }

        /// <summary>
        /// Is the entity locked for highlights by the system?
        /// </summary>
        /// <param name="entityId">Id of the entity.</param>
        /// <returns>Reserved value.</returns>
        public bool IsReserved(long entityId)
        {
            return m_exclusiveKeysToIds.ContainsValue(entityId);
        }

        // Determines if the highlight is local or global request.
        // Sends message to eighter render proxy or server.
        private void ProcessRequest(MyHighlightData data, int exclusiveKey, bool isExclusive)
        {
            // It is a local highlight
            if(data.PlayerId == -1)
            {
                data.PlayerId = MySession.Static.LocalPlayerId;
            }

            if ((MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer) && data.PlayerId != MySession.Static.LocalPlayerId)
            {
                // Stop right here if id does not exist.
                MyPlayer.PlayerId _playerId;
                if (!MySession.Static.Players.TryGetPlayerId(data.PlayerId, out _playerId)) return;

                var msg =  new HighlightMsg()
                {
                    Data = data, ExclusiveKey = exclusiveKey, IsExclusive = isExclusive
                };
                // Server knows nothing, needs to ask clients for exclusive key. OnRejected or OnAccepted will give him answer.
                MyMultiplayer.RaiseStaticEvent(s => OnHighlightOnClient, msg, new EndpointId(_playerId.SteamId));
            }
            else
            {
                var enableHighlight = data.Thickness > -1;
                // Discard invalid requests
                long storedEntityId;
                if(m_exclusiveKeysToIds.ContainsValue(data.EntityId))
                { 
                    if(!m_exclusiveKeysToIds.TryGetValue(exclusiveKey, out storedEntityId) || storedEntityId != data.EntityId)
                    {
                        if(HighlightRejected != null)
                            HighlightRejected(data);

                        return;
                    }
                }

                // Give the exclusive request new local Key
                if (isExclusive)
                {
                    if (exclusiveKey == -1)
                    {
                        // Get new key from the counter
                        exclusiveKey = m_exclusiveKeyCounter++;
                    }
                    
                    if(enableHighlight)
                    { 
                        if(!m_exclusiveKeysToIds.ContainsKey(exclusiveKey))
                            m_exclusiveKeysToIds.Add(exclusiveKey, data.EntityId);
                    }
                    else
                    {
                        m_exclusiveKeysToIds.Remove(exclusiveKey);
                    }
                    
                    MakeLocalHighlightChange(data);

                    if(ExclusiveHighlightAccepted != null)
                        ExclusiveHighlightAccepted(data, exclusiveKey);
                }
                else
                {
                    MakeLocalHighlightChange(data);

                    if (HighlightAccepted != null)
                        HighlightAccepted(data);   
                }
            }
        }

        // No network logic involved. highlighs the object.
        private void MakeLocalHighlightChange(MyHighlightData data)
        {
            if (data.Thickness > -1)
            {
                // Highlight On
                m_highlightedIds.Add(data.EntityId);
            }
            else
            {
                // Highlight Off
                m_highlightedIds.Remove(data.EntityId);    
            }

            MyEntity entity;
            if(!MyEntities.TryGetEntityById(data.EntityId, out entity))
            {
                //Debug.Fail("Highlight system: Entity was not found.");
                return;
            }

            if (!data.IgnoreUseObjectData)
            {
                var useObject = entity as IMyUseObject;
                var useComp = entity.Components.Get<MyUseObjectsComponentBase>();
                // Entities can derive from IMyUseObject or have useObject component.
                if (useObject != null || useComp != null)
                {
                    if (useComp != null)
                    {
                        // Has UseObjectComp
                        List<IMyUseObject> useObjectTmpList = new List<IMyUseObject>();
                        useComp.GetInteractiveObjects(useObjectTmpList);

                        for (var index = 0; index < useObjectTmpList.Count; index++)
                        {
                            HighlightUseObject(useObjectTmpList[index], data);
                        }

                        if (useObjectTmpList.Count > 0)
                        {
                            if (HighlightAccepted != null)
                                HighlightAccepted(data);

                            return;
                        }
                    }
                    else
                    {
                        // Is useObject
                        HighlightUseObject(useObject, data);
                        if (HighlightAccepted != null)
                            HighlightAccepted(data);

                        return;
                    }
                }
            }

            // Collect subPart indicies
            m_subPartIndicies.Clear();
            CollectSubPartIndicies(entity);

            // No use object just use the model for highlight
            MyRenderProxy.UpdateModelHighlight(
                (uint)entity.Render.GetRenderObjectID(),
                null,
                m_subPartIndicies.ToArray(),
                data.OutlineColor,
                data.Thickness,
                data.PulseTimeInFrames);

            if(HighlightAccepted != null)
                HighlightAccepted(data);
        }

        // Fills subPart list with indicies of all subpart indicies
        private void CollectSubPartIndicies(MyEntity currentEntity)
        {
            foreach (var subpart in currentEntity.Subparts.Values)
            {
                CollectSubPartIndicies(subpart);
                m_subPartIndicies.AddRange(subpart.Render.RenderObjectIDs);
            }
        }

        private StringBuilder m_highlightAttributeBuilder = new StringBuilder();
        // Local helper method for use object highlighting.
        private void HighlightUseObject(IMyUseObject useObject, MyHighlightData data)
        {
            // Use the helper to perform preprocessing 
            m_highlightCalculationHelper.HighlightAttribute = null;

            if (useObject.Dummy != null)
            {
                // For objects with dummy prepare dummy data
                object attributeData;
                useObject.Dummy.CustomData.TryGetValue(MyModelDummy.ATTRIBUTE_HIGHLIGHT, out attributeData);
                string highlightAttribute = attributeData as string;

                if(highlightAttribute == null)
                     return;

                if (data.SubPartNames != null)
                {
                    // When the SubPartName hints are present check the attributes.
                    // Use only those who should be rendered.
                    m_highlightAttributeBuilder.Clear();

                    var splits = data.SubPartNames.Split(';');
                    foreach (var split in splits)
                    {
                        if (highlightAttribute.Contains(split))
                        {
                            m_highlightAttributeBuilder.Append(split).Append(';');
                        }
                    }

                    if(m_highlightAttributeBuilder.Length > 0)
                        m_highlightAttributeBuilder.TrimEnd(1);
                    m_highlightCalculationHelper.HighlightAttribute = m_highlightAttributeBuilder.ToString();
                }
                else
                {
                    // Use whole set of attributes.
                    m_highlightCalculationHelper.HighlightAttribute = highlightAttribute;
                }

                if(string.IsNullOrEmpty(m_highlightCalculationHelper.HighlightAttribute))
                    return;
            }

            m_highlightCalculationHelper.Highlight(useObject);

            // Send message to renderer
            MyRenderProxy.UpdateModelHighlight(
                (uint)m_highlightCalculationHelper.InteractiveObject.RenderObjectID,
                m_highlightCalculationHelper.SectionNames,
                m_highlightCalculationHelper.SubpartIndices,
                data.OutlineColor,
                data.Thickness,
                data.PulseTimeInFrames,
                m_highlightCalculationHelper.InteractiveObject.InstanceID);
        }

        // OnClient logic for handling highlights. Called by server only.
        [Event, Reliable, Client]
        private static void OnHighlightOnClient(HighlightMsg msg)
        {
            // Check the exclusive locks
            if(m_static.m_exclusiveKeysToIds.ContainsValue(msg.Data.EntityId))
            {
                long id;
                if(!m_static.m_exclusiveKeysToIds.TryGetValue(msg.ExclusiveKey, out id) || id != msg.Data.EntityId)
                {
                    // The exclusive key is not valid or is used for different entityId
                    if(m_static.HighlightRejected != null)
                        m_static.HighlightRejected(msg.Data);

                    MyMultiplayer.RaiseStaticEvent(s => OnRequestRejected, msg, MyEventContext.Current.Sender);
                    return;
                }
            }

            m_static.MakeLocalHighlightChange(msg.Data);
            // Handle the exclusive key
            if (msg.IsExclusive)
            {
                var enableHighlight = msg.Data.Thickness > -1;
                // Default value we need to generate one.
                if (msg.ExclusiveKey == -1)
                {
                    msg.ExclusiveKey = m_exclusiveKeyCounter++;

                    if (enableHighlight && !m_static.m_exclusiveKeysToIds.ContainsKey(msg.ExclusiveKey))
                    {
                        // Adding highlight/Updating
                        m_static.m_exclusiveKeysToIds.Add(msg.ExclusiveKey, msg.Data.EntityId);
                    }
                }

                if (!enableHighlight)
                {
                    // Removing Highlight
                    m_static.m_exclusiveKeysToIds.Remove(msg.ExclusiveKey);
                }
            }

            MyMultiplayer.RaiseStaticEvent(s => OnRequestAccepted, msg, MyEventContext.Current.Sender);
        }

        // Server logic called by clients when highlight gets rejected.
        [Event, Reliable, Server]
        private static void OnRequestRejected(HighlightMsg msg)
        {
            if (msg.IsExclusive)
            {
                m_static.NotifyExclusiveHighlightRejected(msg.Data, msg.ExclusiveKey);
            }
            else
            {
                if(m_static.HighlightRejected != null)
                    m_static.HighlightRejected(msg.Data);
            }
        }

        // Serve side logic for client accepted requests.
        [Event, Reliable, Server]
        private static void OnRequestAccepted(HighlightMsg msg)
        {
            if (msg.IsExclusive)
            {
                m_static.NotifyExclusiveHighlightAccepted(msg.Data, msg.ExclusiveKey);
            }
            else
            {
                m_static.NotifyHighlightAccepted(msg.Data);
            }
        }

        private void NotifyHighlightAccepted(MyHighlightData data)
        {
            if(HighlightAccepted == null)
                return;

            HighlightAccepted(data);

            // unsubscribe all
            foreach (var @delegate in HighlightAccepted.GetInvocationList())
            {
                HighlightAccepted -= (Action<MyHighlightData>)@delegate;
            }
        }

        private void NotifyExclusiveHighlightAccepted(MyHighlightData data, int exclusiveKey)
        {
            if(ExclusiveHighlightAccepted == null)
                return;

            ExclusiveHighlightAccepted(data, exclusiveKey);

            // unsubscribe all
            foreach (var @delegate in ExclusiveHighlightAccepted.GetInvocationList())
            {
                ExclusiveHighlightAccepted -= (Action<MyHighlightData, int>)@delegate;
            }
        }

        private void NotifyExclusiveHighlightRejected(MyHighlightData data, int exclusiveKey)
        {
            if(ExclusiveHighlightRejected == null)
                return;

            ExclusiveHighlightRejected(data, exclusiveKey);

            // unsubscribe all
            foreach (var @delegate in ExclusiveHighlightRejected.GetInvocationList())
            {
                ExclusiveHighlightRejected -= (Action<MyHighlightData, int>)@delegate;
            }
        }
    }
}
