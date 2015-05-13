using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyHudObjectiveLine
    {
        bool Visible { get; }

        string Title { get; set; }

        string CurrentObjective { get; }

        void Show();
        void Hide();

        void AdvanceObjective();

        List<string> Objectives
        {
            get;
            set;
        }
    }
}
