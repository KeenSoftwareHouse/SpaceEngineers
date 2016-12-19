using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Gui
{
    public class MultilineData
    {
        public bool completed;
        public int lines;
        public string data;
        public int charactersDisplayed = 0;
    }

    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Questlog : MyObjectBuilder_Base
    {
        public List<MultilineData> LineData = new List<MultilineData>();
        public string Title = String.Empty;
    }
}
