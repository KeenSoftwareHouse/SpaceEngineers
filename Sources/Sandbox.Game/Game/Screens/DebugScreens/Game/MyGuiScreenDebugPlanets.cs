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
using VRageRender;
using VRageRender.Voxels;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1

    [MyDebugScreen("Game", "Planets")]
    class MyGuiScreenDebugPlanets : MyGuiScreenDebugBase
    {
        private float[] m_lodRanges;
        private static bool m_massive = false;
        private static MyGuiScreenDebugPlanets m_instance;

        public MyGuiScreenDebugPlanets()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPlanets";
        }

        MyClipmapScaleEnum ScaleGroup
        {
            get { return m_massive ? MyClipmapScaleEnum.Massive : MyClipmapScaleEnum.Normal; }
        }

        static bool Massive
        {
            get { return m_massive; }
            set
            {
                if (m_massive != value)
                {
                    m_instance.RecreateControls(false);
                    m_massive = value;
                }
            }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            BackgroundColor = new Vector4(1f, 1f, 1f, 0.5f);

            m_instance = this;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);
            //AddCheckBox("Enable frozen seas ", null, MemberHelper.GetMember(() => MyFakes.ENABLE_PLANET_FROZEN_SEA));
            //AddSlider("Sea level : ", 0f, 200f, null, MemberHelper.GetMember(() => MyCsgHeightmapHelpers.FROZEN_OCEAN_LEVEL));
            AddCheckBox("Debug draw areas: ", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_FLORA_BOXES));

            AddCheckBox("Massive", this, MemberHelper.GetMember(() => m_massive));


            m_lodRanges = new float[MyRenderConstants.RenderQualityProfile.LodClipmapRanges[(int)ScaleGroup].Length];

            for (int i = 0; i < m_lodRanges.Length; i++)
            {
                m_lodRanges[i] = MyClipmap.LodRangeGroups[(int)ScaleGroup][i];
            }

            for (int i = 0; i < m_lodRanges.Length; i++)
            {
                int lod = i;
                AddSlider("LOD " + i, m_lodRanges[i], 0, (i < 4 ? 1000 : i < 7 ? 10000 : 300000), (s) => { ChangeValue(s.Value, lod); });
            }

        }

        private void ChangeValue(float value, int lod)
        {
            m_lodRanges[lod] = value;


            var lods = MyRenderConstants.RenderQualityProfile.LodClipmapRanges;

            for (int i = 0; i < m_lodRanges.Length; i++)
            {
                lods[(int)ScaleGroup][i] = m_lodRanges[i];
            }
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

           
           // MyClipmap.UpdateLodRanges(lods);
        }
    }

#endif
}
