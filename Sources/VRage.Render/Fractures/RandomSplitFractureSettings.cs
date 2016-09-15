using System.ComponentModel;

namespace VRageRender.Fractures
{
    public class RandomSplitFractureSettings : MyFractureSettings
    {
        public RandomSplitFractureSettings()
        {
            NumObjectsOnLevel1 = 2;
            NumObjectsOnLevel2 = 0;
            SplitPlane = "";
        }

        [DisplayName("Objects on level 1")]
        [Description("Objects on level 1")] 
        [Category("Random split")]
        public int NumObjectsOnLevel1 { get; set; }

        [DisplayName("Objects on level 2")]
        [Description("Objects on level 2")]
        [Category("Random split")]
        public int NumObjectsOnLevel2 { get; set; }

        [DisplayName("Random range")]
        [Description("Random range")]
        [Category("Random split")]
        public float RandomRange { get; set; }

        [DisplayName("Random seed 1")]
        [Description("Random seed 1")]
        [Category("Random split")]
        public int RandomSeed1 { get; set; }

        [DisplayName("Random seed 2")]
        [Description("Random seed 2")]
        [Category("Random split")]
        public int RandomSeed2 { get; set; }

        [DisplayName("Split plane")]
        [Description("Split plane")]
        [Category("Random split")]
        [Editor("Telerik.WinControls.UI.PropertyGridBrowseEditor, Telerik.WinControls.UI",
        "Telerik.WinControls.UI.BaseInputEditor, Telerik.WinControls.UI")]
        public string SplitPlane { get; set; }
    }
}
