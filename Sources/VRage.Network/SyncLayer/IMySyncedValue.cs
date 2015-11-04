using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public enum MySyncedDataStateEnum : byte
    {
        UpToDate = 0,
        Pending,
        Outdated,
    }

    public interface IMySyncedValue
    {
        void SetParent(MySyncedClass mySyncedClass);

        void Serialize(BitStream bs, int clientIndex);
        void Deserialize(BitStream bs);

        void SerializeDefault(BitStream bs, int clientIndex);
        void DeserializeDefault(BitStream bs);

        MySyncedDataStateEnum GetDataState(int clientIndex);
        bool IsDefault();

        void ResetPending(int clientIndex);
    }
}
