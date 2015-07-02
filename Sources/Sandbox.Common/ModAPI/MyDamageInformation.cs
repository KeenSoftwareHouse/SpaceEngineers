using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// This structure contains all information about damage being done.
    /// </summary>
    public struct MyDamageInformation
    {
        public MyDamageInformation(bool isDeformation, float amount, MyDamageType type, long attackerId)
        {
            IsDeformation = isDeformation;
            Amount = amount;
            Type = type;
            AttackerId = attackerId;
        }

        public bool IsDeformation;
        public float Amount;
        public MyDamageType Type;
        public long AttackerId;
    }
}
