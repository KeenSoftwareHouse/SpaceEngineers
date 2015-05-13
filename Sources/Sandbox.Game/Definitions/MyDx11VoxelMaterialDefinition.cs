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

            ob.FoliageTextureArray1      = this.FoliageTextureArray1;
            ob.FoliageTextureArray2      = this.FoliageTextureArray2;
            ob.FoliageDensity            = this.FoliageDensity;
            ob.FoliageScale              = this.FoliageScale;
            ob.FoliageRandomRescaleMult  = this.FoliageRandomRescaleMult;

            ob.BiomeValueMin = this.BiomeValueMin;
            ob.BiomeValueMax = this.BiomeValueMax;

            return ob;
        }

        public override void CreateRenderData(out MyRenderVoxelMaterialData renderData)
        {
            base.CreateRenderData(out renderData);

            renderData.ColorMetalXZnY               = this.ColorMetalXZnY;
            renderData.ColorMetalY                  = this.ColorMetalY;
            renderData.NormalGlossXZnY              = this.NormalGlossXZnY;
            renderData.NormalGlossY                 = this.NormalGlossY;
            renderData.ExtXZnY = this.ExtXZnY;
            renderData.ExtY = this.ExtY;

            renderData.ColorMetalXZnYFar1   = this.ColorMetalXZnYFar1;
            renderData.ColorMetalYFar1      = this.ColorMetalYFar1;
            renderData.NormalGlossXZnYFar1  = this.NormalGlossXZnYFar1;
            renderData.NormalGlossYFar1     = this.NormalGlossYFar1;
            renderData.ExtXZnYFar1 = this.ExtXZnYFar1;
            renderData.ExtYFar1 = this.ExtYFar1;

            renderData.ColorMetalXZnYFar2 = this.ColorMetalXZnYFar2;
            renderData.ColorMetalYFar2 = this.ColorMetalYFar2;
            renderData.NormalGlossXZnYFar2 = this.NormalGlossXZnYFar2;
            renderData.NormalGlossYFar2 = this.NormalGlossYFar2;
            renderData.ExtXZnYFar2 = this.ExtXZnYFar2;
            renderData.ExtYFar2 = this.ExtYFar2;

            renderData.Scale           = this.Scale;
            renderData.ScaleFar1            = this.ScaleFar1;
            renderData.ScaleFar2 = this.ScaleFar2;

            renderData.ExtensionTextureArray1        = this.FoliageTextureArray1;
            renderData.ExtensionTextureArray2      = this.FoliageTextureArray2;
            renderData.ExtensionDensity             = this.FoliageDensity;
            renderData.ExtensionScale               = this.FoliageScale;
            renderData.ExtensionRandomRescaleMult   = this.FoliageRandomRescaleMult;
        }

    }
}
