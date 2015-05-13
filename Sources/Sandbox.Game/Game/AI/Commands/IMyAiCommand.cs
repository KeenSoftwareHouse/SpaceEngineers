using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Commands
{
    public interface IMyAiCommand
    {
        void InitCommand(MyAiCommandDefinition definition);
        void ActivateCommand();
    }
}
