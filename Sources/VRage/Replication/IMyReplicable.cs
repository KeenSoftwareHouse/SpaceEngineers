using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;

namespace VRage.Network
{
    public interface IMyReplicable : IMyNetObject
    {
        /// <summary>
        /// Child replicables are strongly dependent on parent.
        /// When parent is replicated, children are replicated, priority is never checked for children.
        /// Parent can change during replicable runtime, HasToBeChild can not.
        /// </summary>
        bool HasToBeChild { get; }

        /// <summary>
        /// Gets parent which must be replicated first.
        /// </summary>
        IMyReplicable GetParent();

        /// <summary>
        /// Gets priority related to client.
        /// When priority is lower than zero, it means the object is not relevant for client.
        /// Default priority is 1.0f.
        /// </summary>
        float GetPriority(MyClientInfo client,bool cached);

        /// <summary>
        /// Serializes object for replication to client.
        /// </summary>
        bool OnSave(BitStream stream);
        
        /// <summary>
        /// Client deserializes object and adds it to proper collection (e.g. MyEntities).
        /// Loading done handler can be called synchronously or asynchronously (but always from Update thread).
        /// </summary>
        void OnLoad(BitStream stream, Action<bool> loadingDoneHandler);

        /// <summary>
        /// Client deserializes object and adds it to proper collection (e.g. MyEntities).
        /// Loading done handler can be called synchronously or asynchronously (but always from Update thread).
        /// </summary>
        void OnLoadBegin(BitStream stream, Action<bool> loadingDoneHandler);

        /// <summary>
        /// Called on client when server destroyed this replicable.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Returns state groups for replicable in a list.
        /// This method can has to return objects in same order every time (e.g. first terminal, second physics etc).
        /// It does not have to return same instances every time.
        /// </summary>
        void GetStateGroups(List<IMyStateGroup> resultList);

        bool IsReadyForReplication { get; }
        Dictionary<IMyReplicable, Action> ReadyForReplicationAction { get; }

        /// <summary>
        /// Root replicables always have spatial representation. 
        /// </summary>
        /// <returns></returns>
        BoundingBoxD GetAABB();

        /// <summary>
        /// Called when root replicable AABB changed
        /// </summary>
        Action<IMyReplicable> OnAABBChanged { get; set; }

        /// <summary>
        /// Dependend replicables, which might not be in AABB of this replicable. Ie. all relayed antennas are depended 
        /// on mycharacter and need to be synced with him. 
        /// </summary>
        /// <returns></returns>
        HashSet<IMyReplicable> GetDependencies();
    }
}
