using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
    class MyTerminalComparer : IComparer<MyTerminalBlock>, IComparer<MyBlockGroup>
    {
        public static MyTerminalComparer Static = new MyTerminalComparer();

        public int Compare(MyTerminalBlock lhs, MyTerminalBlock rhs)
        {
            int definitionDiff = (lhs.CustomName != null ? lhs.CustomName.ToString() : lhs.DefinitionDisplayNameText).CompareTo((rhs.CustomName != null ? rhs.CustomName.ToString() : rhs.DefinitionDisplayNameText));
            if (definitionDiff != 0)
                return definitionDiff;

            if (lhs.NumberInGrid != rhs.NumberInGrid)
                return lhs.NumberInGrid.CompareTo(rhs.NumberInGrid);

            return 0;
        }

        public int Compare(MyBlockGroup x, MyBlockGroup y)
        {
            return x.Name.CompareTo(y.Name);
        }
    }
}
