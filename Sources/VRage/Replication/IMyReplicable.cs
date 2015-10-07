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
        /// Gets dependency which must be replicated first.
        /// </summary>
        IMyReplicable GetDependency();

        /// <summary>
        /// Gets priority related to client.
        /// When priority is lower than zero, it means the object is not relevant for client.
        /// Default priority is 1.0f.
        /// </summary>
        float GetPriority(MyClientStateBase client);

        /// <summary>
        /// Serializes object for replication to client.
        /// </summary>
        void OnSave(BitStream stream);
        
        /// <summary>
        /// Client deserializes object and adds it to proper collection (e.g. MyEntities).
        /// Loading done handler can be called synchronously or asynchronously (but always from Update thread).
        /// </summary>
        void OnLoad(BitStream stream, Action loadingDoneHandler);

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
