using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems.Electricity
{
    public interface IMyRechargeSocketOwner
    {
        MyRechargeSocket RechargeSocket { get; }
    }
}
