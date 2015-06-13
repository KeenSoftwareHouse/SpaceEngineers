using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Game.GameSystems.Electricity
{
    public interface IMyPowerConsumer
    {
        MyPowerReceiver PowerReceiver { get; }
    }
}
