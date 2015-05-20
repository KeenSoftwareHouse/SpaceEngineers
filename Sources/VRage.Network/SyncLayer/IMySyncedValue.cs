﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public interface IMySyncedValue
    {
        void SetParent(MySyncedClass mySyncedClass);

        void Serialize(BitStream bs);
        void Deserialize(BitStream bs);

        void SerializeDefault(BitStream bs);
        void DeserializeDefault(BitStream bs);

        bool IsDirty { get; }
        bool IsDefault();
    }
}
