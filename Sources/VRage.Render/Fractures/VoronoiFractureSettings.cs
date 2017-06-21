using System.ComponentModel;

namespace VRageRender.Fractures
{
    public class VoronoiFractureSettings : MyFractureSettings
    {
        public VoronoiFractureSettings()
        {
            Seed = 123456;
            NumSitesToGenerate = 8;
            NumIterations = 1;
            SplitPlane = "";
        }

        [DisplayName("Seed")]
        [Description("Seed")]
        [Category("Voronoi")]
        public int Seed { get; set; }

        [DisplayName("Sites to generate")]
        [Description("Sites to generate")]
        [Category("Voronoi")]
        public int NumSitesToGenerate { get; set; }

        [DisplayName("Iterations")]
        [Description("Iterations")]
        [Category("Voronoi")]
        public int NumIterations { get; set; }

        [DisplayName("Split plane")]
        [Description("Split plane")]
        [Category("Voronoi")]
        [Editor("Telerik.WinControls.UI.PropertyGridBrowseEditor, Telerik.WinControls.UI",
        "Telerik.WinControls.UI.BaseInputEditor, Telerik.WinControls.UI")]

        public string SplitPlane { get; set; }
    }
}
