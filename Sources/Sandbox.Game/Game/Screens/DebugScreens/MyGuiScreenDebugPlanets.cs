using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Gui;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Game", "Planets")]
    class MyGuiScreenDebugPlanets : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugPlanets()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPlanets";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            BackgroundColor = new Vector4(1f, 1f, 1f, 0.5f);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);
            AddCheckBox("Enable frozen seas ", null, MemberHelper.GetMember(() => MyFakes.ENABLE_PLANET_FROZEN_SEA));
            AddSlider("Sea level : ", 0f, 200f, null, MemberHelper.GetMember(() => MyCsgPrecomputedHelpres.FROZEN_OCEAN_LEVEL));
            AddButton(new StringBuilder("Run test"), onClick: RemoveAllAsteroids);
        }

        private void RemoveAllAsteroids(MyGuiControlButton sender)
        {
            MyCsgShapePlanetShapeAttributes shapeAttributes = new MyCsgShapePlanetShapeAttributes();

            shapeAttributes.Seed = 12345;
            shapeAttributes.Diameter = 60;
            shapeAttributes.Radius = 60 / 2.0f;
            shapeAttributes.DeviationScale = 0.003f;
            float maxHillSize = 10;

            float planetHalfDeviation = (shapeAttributes.Diameter * shapeAttributes.DeviationScale) / 2.0f;
            float hillHalfDeviation = planetHalfDeviation * maxHillSize;
            float canyonHalfDeviation = 1;


            float averagePlanetRadius = shapeAttributes.Radius - hillHalfDeviation;

            float outerRadius = averagePlanetRadius + hillHalfDeviation;
            float innerRadius = averagePlanetRadius - canyonHalfDeviation;

            float atmosphereRadius = MathHelper.Max(outerRadius, averagePlanetRadius * 1.08f);
            float minPlanetRadius = MathHelper.Min(innerRadius, averagePlanetRadius - planetHalfDeviation * 2 * 2.5f);

            MyCsgShapePlanetMaterialAttributes materialAttributes = new MyCsgShapePlanetMaterialAttributes();
            materialAttributes.OreStartDepth = innerRadius;
            materialAttributes.OreEndDepth = innerRadius;
            materialAttributes.OreEndDepth = MathHelper.Max(materialAttributes.OreEndDepth, 0);
            materialAttributes.OreStartDepth = MathHelper.Max(materialAttributes.OreStartDepth, 0);

            materialAttributes.OreProbabilities = new MyOreProbability[10];

            for (int i = 0; i < 10; ++i)
            {
                materialAttributes.OreProbabilities[i] = new MyOreProbability();
                materialAttributes.OreProbabilities[i].OreName = "Ice_01";
                materialAttributes.OreProbabilities[i].CummulativeProbability = 0.0f;
            }

            shapeAttributes.AveragePlanetRadius = averagePlanetRadius;

            IMyStorageDataProvider dataProvider = MyCompositeShapeProvider.CreatePlanetShape(0, ref shapeAttributes, maxHillSize, ref materialAttributes);
            IMyStorage storage = new MyOctreeStorage(dataProvider, MyVoxelCoordSystems.FindBestOctreeSize(shapeAttributes.Diameter));
            MyStorageDataCache cache = new MyStorageDataCache();
            cache.Resize(storage.Size);
            Vector3I start = Vector3I.Zero;
            Vector3I end = storage.Size;
            storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 1, ref start, ref end);
            dataProvider.ReleaseHeightMaps();
        }
    }
}
