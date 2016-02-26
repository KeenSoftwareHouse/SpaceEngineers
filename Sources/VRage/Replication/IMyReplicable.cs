using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public interface IMyReplicable : IMyNetObject
    {
        /// <summary>
        /// Child replicables are strongly dependent on parent.
        /// When parent is replicated, children are replicated, priority is never checked for children.
        /// Dependency can change during replicable runtime, IsChild can not.
        /// </summary>
        bool IsChild { get; }

        /// <summary>
        /// Gets dependency which must be replicated first.
        /// </summary>
        IMyReplicable GetDependency();

        /// <summary>
        /// Gets priority related to client.
        /// When priority is lower than zero, it means the object is not relevant for client.
        /// Default priority is 1.0f.
        /// </summary>
        float GetPriority(MyClientInfo client);

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
    }
}
