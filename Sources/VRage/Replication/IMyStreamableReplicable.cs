using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public interface IMyStreamableReplicable
    {
        void Serialize(BitStream stream);
        void LoadDone(BitStream stream);
        void LoadCancel();

        IMyStateGroup GetStreamingStateGroup();

        float GetPriority(MyClientInfo state,bool cached);

        float PriorityScale();

        bool NeedsToBeStreamed
        {
            get;
        }
    }
}