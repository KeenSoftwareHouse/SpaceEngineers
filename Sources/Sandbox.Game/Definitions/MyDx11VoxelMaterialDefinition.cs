using Medieval.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
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
        public Color Far3Color;

        public float InitialScale;
	    public float ScaleMultiplier;
	    public float InitialDistance;
	    public float DistanceMultiplier;
        public float Far1Distance;
        public float Far2Distance;
        public float Far3Distance;
        public float Far1Scale;
        public float Far2Scale;
        public float Far3Scale;
        public float ExtensionDetailScale;

        public string   FoliageTextureArray1;
        public string   FoliageTextureArray2;
        public string[] FoliageColorTextureArray;
        public string[] FoliageNormalTextureArray;
        public float    FoliageDensity;
        public Vector2  FoliageScale;
        public float    FoliageRandomRescaleMult;
        public int      FoliageType;

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
            this.ColorMetalXZnYFar1  = myOb.ColorMetalXZnYFar1 != null ? myOb.ColorMetalXZnYFar1: myOb.ColorMetalXZnY;
            this.ColorMetalYFar1 = myOb.ColorMetalYFar1 != null ? myOb.ColorMetalYFar1 : myOb.ColorMetalY;
            this.NormalGlossXZnYFar1 = myOb.NormalGlossXZnYFar1 != null ? myOb.NormalGlossXZnYFar1 : myOb.NormalGlossXZnY;
            this.NormalGlossYFar1 = myOb.NormalGlossYFar1 != null ? myOb.NormalGlossYFar1 : myOb.NormalGlossY;
            this.ExtXZnYFar1 = myOb.ExtXZnYFar1 != null ? myOb.ExtXZnYFar1 : myOb.ExtXZnY;
            this.ExtYFar1 = myOb.ExtYFar1 != null ? myOb.ExtYFar1 : myOb.ExtY;
            this.ColorMetalXZnYFar2 = myOb.ColorMetalXZnYFar2 != null ? myOb.ColorMetalXZnYFar2 : myOb.ColorMetalXZnY;
            this.ColorMetalYFar2 = myOb.ColorMetalYFar2 != null ? myOb.ColorMetalYFar2 : myOb.ColorMetalY;
            this.NormalGlossXZnYFar2 = myOb.NormalGlossXZnYFar2 != null ? myOb.NormalGlossXZnYFar2 : myOb.NormalGlossXZnY;
            this.NormalGlossYFar2 = myOb.NormalGlossYFar2 != null ? myOb.NormalGlossYFar2 : myOb.NormalGlossY;
            this.ExtXZnYFar2 = myOb.ExtXZnYFar2 != null ? myOb.ExtXZnYFar2 : myOb.ExtXZnY;
            this.ExtYFar2 = myOb.ExtYFar2 != null ? myOb.ExtYFar2 : myOb.ExtY;

            this.InitialScale = myOb.InitialScale;
            this.ScaleMultiplier = myOb.ScaleMultiplier;
            this.InitialDistance = myOb.InitialDistance;
            this.DistanceMultiplier = myOb.DistanceMultiplier;
            this.Far1Distance = myOb.Far1Distance;
            this.Far2Distance = myOb.Far2Distance;
            this.Far3Distance = myOb.Far3Distance;
            this.Far1Scale = myOb.Far1Scale;
            this.Far2Scale = myOb.Far2Scale;
            this.Far3Scale = myOb.Far3Scale;
            this.Far3Color = myOb.Far3Color;
            this.ExtensionDetailScale = myOb.ExtDetailScale;

            this.FoliageTextureArray1 = myOb.FoliageTextureArray1;
            this.FoliageTextureArray2 = myOb.FoliageTextureArray2;
            this.FoliageColorTextureArray = myOb.FoliageColorTextureArray;
            this.FoliageNormalTextureArray = myOb.FoliageNormalTextureArray;

            Debug.Assert(FoliageTextureArray1 == null || FoliageColorTextureArray == null,
                "FoliageTextureArray1 and FoliageColorTextureArray cannot be used together (" + FoliageTextureArray1 + ")");

            this.FoliageDensity = myOb.FoliageDensity;
            this.FoliageScale = myOb.FoliageScale;
            this.FoliageRandomRescaleMult = myOb.FoliageRandomRescaleMult;
            this.FoliageType = myOb.FoliageType;

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

            ob.InitialScale = this.InitialScale;
            ob.ScaleMultiplier = this.ScaleMultiplier;
            ob.InitialDistance = this.InitialDistance;
            ob.DistanceMultiplier = this.DistanceMultiplier;
            ob.Far1Distance = this.Far1Distance;
            ob.Far2Distance = this.Far2Distance;
            ob.Far3Distance = this.Far3Distance;
            ob.Far1Scale = this.Far1Scale;
            ob.Far2Scale = this.Far2Scale;
            ob.Far3Scale = this.Far3Scale;
            ob.Far3Color = this.Far3Color;
            ob.ExtDetailScale = this.ExtensionDetailScale;

            ob.FoliageTextureArray1      = this.FoliageTextureArray1;
            ob.FoliageTextureArray2      = this.FoliageTextureArray2;
            ob.FoliageColorTextureArray  = this.FoliageColorTextureArray;
            ob.FoliageNormalTextureArray = this.FoliageNormalTextureArray;
            ob.FoliageDensity            = this.FoliageDensity;
            ob.FoliageScale              = this.FoliageScale;
            ob.FoliageRandomRescaleMult  = this.FoliageRandomRescaleMult;
            ob.FoliageType               = this.FoliageType;

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

            renderData.DistanceAndScale = new Vector4(InitialScale, InitialDistance, ScaleMultiplier, DistanceMultiplier);
            renderData.DistanceAndScaleFar = new Vector4(Far1Scale, Far1Distance, Far2Scale, Far2Distance);
            renderData.DistanceAndScaleFar3 = new Vector2(Far3Scale, Far3Distance);
            renderData.Far3Color = Far3Color;
            renderData.ExtensionDetailScale = ExtensionDetailScale;

            renderData.ExtensionTextureArray1       = this.FoliageTextureArray1;
            renderData.ExtensionTextureArray2       = this.FoliageTextureArray2;
            renderData.FoliageColorTextureArray     = this.FoliageColorTextureArray;
            renderData.FoliageNormalTextureArray    = this.FoliageNormalTextureArray;
            renderData.ExtensionDensity             = this.FoliageDensity;
            renderData.ExtensionScale               = this.FoliageScale;
            renderData.ExtensionRandomRescaleMult   = this.FoliageRandomRescaleMult;
            renderData.ExtensionType                = this.FoliageType;
        }

    }
}
