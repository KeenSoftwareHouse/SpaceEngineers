using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;
using Sandbox.Common.Components;

namespace Sandbox.Game.Components
{
    public class MyRenderComponentLight : MyRenderComponentCubeBlock
    {
        #region light properties
        private bool m_emissiveMaterialDirty;
        private Color m_bulbColor = Color.Black;
        private float m_currentLightPower = 0; //0..1

        public float CurrentLightPower
        {
            get { return m_currentLightPower; }
            set
            {
                if (m_currentLightPower != value)
                {
                    m_currentLightPower = value;
                    m_emissiveMaterialDirty = true;
                }
            }
        }
        public Color BulbColor
        {
            get { return m_bulbColor; }
            set
            {
                if (m_bulbColor != value)
                {
                    m_bulbColor = value;
                    m_emissiveMaterialDirty = true;
                }
            }
        }
        #endregion

        #region overrides
        public override void Draw()
        {
            base.Draw();
            UpdateEmissiveMaterial();
        }
        #endregion

        private void UpdateEmissiveMaterial()
        {
            if (m_emissiveMaterialDirty)
            {
                VRageRender.MyRenderProxy.UpdateModelProperties(
                  RenderObjectIDs[0],
                  0,
                  m_model.AssetName,
                  -1,
                  "Emissive",
                  null,
                  BulbColor,
                  null,
                  null,
                  CurrentLightPower);
                m_emissiveMaterialDirty = false;
            }
        }
    }
}
