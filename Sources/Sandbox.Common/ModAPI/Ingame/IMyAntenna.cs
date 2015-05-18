using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyRadioAntenna : IMyFunctionalBlock
    {
        float Radius {get;}

        string LastReceivedMessage {get;}
        void SendMessage(string shipreceiver, string pbreceiver, string message);
    }
}
