using Medieval.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace Medieval.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_Dx11VoxelMaterialDefinition))]
    public class MyDx11VoxelMaterialDefinition : MyVoxelMaterialDefinition
    {
        public string ColorMetalXZnY;
        public string ColorMetalY;
        public string NormalGlossXZnY;
        public string NormalGlossY;
        public string ExtXZnY;
        public string ExtY;
        public string ColorMetalXZnYFar1;
        public string ColorMetalYFar1;
        public string NormalGlossXZnYFar1;
        public string NormalGlossYFar1;
        public string ExtXZnYFar1;
        public string ExtYFar1;
        public string ColorMetalXZnYFar2;
        public string ColorMetalYFar2;
        public string NormalGlossXZnYFar2;
        public string NormalGlossYFar2;
        public string ExtXZnYFar2;
        public string ExtYFar2;
        private float Scale;
        private float ScaleFar1;
        private float ScaleFar2;
        private float ScaleFar3;
        private float ScaleFar4;

        public string   FoliageTextureArray1;
        public string   FoliageTextureArray2;
        public float    FoliageDensity;
        public Vector2  FoliageScale;
        public float    FoliageRandomRescaleMult;

        public byte BiomeValueMin;
        public byte BiomeValueMax;

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            var myOb = (MyObjectBuilder_Dx11VoxelMaterialDefinition)ob;

            this.ColorMetalXZnY              = myOb.ColorMetalXZnY;
            this.ColorMetalY                 = myOb.ColorMetalY;
            this.NormalGlossXZnY             = myOb.NormalGlossXZnY;
            this.NormalGlossY                = myOb.NormalGlossY;
            this.ExtXZnY = myOb.ExtXZnY;
            this.ExtY = myOb.ExtY;
            this.ColorMetalXZnYFar1  = myOb.ColorMetalXZnYFar1;
            this.ColorMetalYFar1     = myOb.ColorMetalYFar1;     
            this.NormalGlossXZnYFar1 = myOb.NormalGlossXZnYFar1;
            this.NormalGlossYFar1    = myOb.NormalGlossYFar1;
            this.ExtXZnYFar1         = myOb.ExtXZnYFar1;
            this.ExtYFar1            = myOb.ExtYFar1;
            this.ColorMetalXZnYFar2  = myOb.ColorMetalXZnYFar2;
            this.ColorMetalYFar2     = myOb.ColorMetalYFar2;
            this.NormalGlossXZnYFar2 = myOb.NormalGlossXZnYFar2;
            this.NormalGlossYFar2    = myOb.NormalGlossYFar2;
            this.ExtXZnYFar2         = myOb.ExtXZnYFar2;
            this.ExtYFar2            = myOb.ExtYFar2;

            this.Scale       = myOb.Scale;
            this.ScaleFar1 = myOb.ScaleFar1;
            this.ScaleFar2 = myOb.ScaleFar2;
            this.ScaleFar3 = myOb.ScaleFar3;
            this.ScaleFar4 = myOb.ScaleFar4;

            this.FoliageTextureArray1 = myOb.FoliageTextureArray1;
            this.FoliageTextureArray2 = myOb.FoliageTextureArray2;
            this.FoliageDensity = myOb.FoliageDensity;
            this.FoliageScale = myOb.FoliageScale;
            this.FoliageRandomRescaleMult = myOb.FoliageRandomRescaleMult;

            this.BiomeValueMin = myOb.BiomeValueMin;
            this.BiomeValueMax = myOb.BiomeValueMax;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_Dx11VoxelMaterialDefinition)base.GetObjectBuilder();

            ob.ColorMetalXZnY              = this.ColorMetalXZnY;
            ob.ColorMetalY                 = this.ColorMetalY;
            ob.NormalGlossXZnY             = this.NormalGlossXZnY;
            ob.NormalGlossY                = this.NormalGlossY;
            ob.ExtXZnY = this.ExtXZnY;
            ob.ExtY = this.ExtY;
            
            ob.ColorMetalXZnYFar1        = this.ColorMetalXZnYFar1;
            ob.ColorMetalYFar1           = this.ColorMetalYFar1;
            ob.NormalGlossXZnYFar1       = this.NormalGlossXZnYFar1;
            ob.NormalGlossYFar1          = this.NormalGlossYFar1;
            ob.ExtXZnYFar1               = this.ExtXZnYFar1;
            ob.ExtYFar1                  = this.ExtYFar1;

            ob.ColorMetalXZnYFar2 = this.ColorMetalXZnYFar2;
            ob.ColorMetalYFar2 = this.ColorMetalYFar2;
            ob.NormalGlossXZnYFar2 = this.NormalGlossXZnYFar2;
            ob.NormalGlossYFar2 = this.NormalGlossYFar2;
            ob.ExtXZnYFar2 = this.ExtXZnYFar2;
            ob.ExtYFar2 = this.ExtYFar2;

            ob.Scale                     = this.Scale;
            ob.ScaleFar1                 = this.ScaleFar1;
            ob.ScaleFar2 = this.ScaleFar2;
            ob.ScaleFar3 = this.ScaleFar3;
            ob.ScaleFar4 = this.ScaleFar4;

            ob.FoliageTextureArray1      = this.FoliageTextureArray1;
            ob.FoliageTextureArray2      = this.FoliageTextureArray2;
            ob.FoliageDensity            = this.FoliageDensity;
            ob.FoliageScale              = this.FoliageScale;
            ob.FoliageRandomRescaleMult  = this.FoliageRandomRescaleMult;

            ob.BiomeValueMin = this.BiomeValueMin;
            ob.BiomeValueMax = this.BiomeValueMax;

            return ob;
        }

        public void FillString(ref string stringToFill, ref string defaultValue, ref string alternative)
        {
            if (string.IsNullOrEmpty(defaultValue))
            {
                stringToFill = alternative;
            }
            else
            {
                stringToFill = defaultValue;
            }
        }

        public override void CreateRenderData(out MyRenderVoxelMaterialData renderData)
        {
            base.CreateRenderData(out renderData);

            renderData.ColorMetalXZnY               = this.ColorMetalXZnY;
            FillString(ref renderData.ColorMetalY, ref this.ColorMetalY, ref ColorMetalXZnY);
            renderData.NormalGlossXZnY              = this.NormalGlossXZnY;
            FillString(ref renderData.NormalGlossY, ref this.NormalGlossY, ref NormalGlossXZnY);
            renderData.ExtXZnY = this.ExtXZnY;
            FillString(ref renderData.ExtY, ref this.ExtY, ref ExtXZnY);

            FillString(ref renderData.ColorMetalXZnYFar1, ref ColorMetalXZnYFar1, ref ColorMetalXZnY);
            FillString(ref renderData.ColorMetalYFar1, ref ColorMetalYFar1, ref renderData.ColorMetalXZnYFar1);
            FillString(ref renderData.NormalGlossXZnYFar1, ref this.NormalGlossXZnYFar1, ref NormalGlossXZnY);
            FillString(ref renderData.NormalGlossYFar1, ref NormalGlossYFar1, ref renderData.NormalGlossXZnYFar1);
            FillString(ref renderData.ExtXZnYFar1, ref this.ExtXZnYFar1, ref ExtXZnY);
            FillString(ref renderData.ExtYFar1, ref ExtYFar1, ref renderData.ExtXZnYFar1);

            FillString(ref renderData.ColorMetalXZnYFar2, ref ColorMetalXZnYFar2, ref renderData.ColorMetalXZnYFar1);
            FillString(ref renderData.ColorMetalYFar2, ref ColorMetalYFar2, ref renderData.ColorMetalXZnYFar2);
            FillString(ref renderData.NormalGlossXZnYFar2, ref this.NormalGlossXZnYFar2, ref renderData.NormalGlossXZnYFar1);
            FillString(ref renderData.NormalGlossYFar2, ref NormalGlossYFar2, ref renderData.NormalGlossXZnYFar2);
            FillString(ref renderData.ExtXZnYFar2, ref this.ExtXZnYFar2, ref renderData.ExtXZnYFar1);
            FillString(ref renderData.ExtYFar2, ref ExtYFar2, ref renderData.ExtXZnYFar2); 

            renderData.Scale           = this.Scale;
            renderData.ScaleFar1            = this.ScaleFar1;
            renderData.ScaleFar2 = this.ScaleFar2;
            renderData.ScaleFar3 = this.ScaleFar3;
            renderData.ScaleFar4 = this.ScaleFar4;


            renderData.ExtensionTextureArray1        = this.FoliageTextureArray1;
            renderData.ExtensionTextureArray2      = this.FoliageTextureArray2;
            renderData.ExtensionDensity             = this.FoliageDensity;
            renderData.ExtensionScale               = this.FoliageScale;
            renderData.ExtensionRandomRescaleMult   = this.FoliageRandomRescaleMult;
        }

    }
}
