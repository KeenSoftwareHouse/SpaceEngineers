using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public interface IBitSerializable
    {
        /// <summary>
        /// When reading, returns false when validation was required and failed, otherwise true.
        /// </summary>
        bool Serialize(BitStream stream, bool validate);
    }
}
