using System.Collections.Generic;

namespace VRage.Game.ModAPI
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
