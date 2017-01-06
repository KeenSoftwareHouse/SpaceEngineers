using System.Collections.Generic;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "GBuffer Debug")]
    class MyGuiScreenDebugRenderGBufferDebug : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderGBufferDebug()
        {
            RecreateControls(true);
        }

        private List<MyGuiControlCheckbox> m_cbs = new List<MyGuiControlCheckbox>();

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("GBuffer Debug", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddLabel("Gbuffer", Color.Yellow.ToVector4(), 1.2f);
            m_cbs.Clear();
            m_cbs.Add(AddCheckBox("Base color", MyRenderProxy.Settings.DisplayGbufferColor, (x) => MyRenderProxy.Settings.DisplayGbufferColor = x.IsChecked));
            m_cbs.Add(AddCheckBox("Albedo", MyRenderProxy.Settings.DisplayGbufferAlbedo, (x) => MyRenderProxy.Settings.DisplayGbufferAlbedo = x.IsChecked));
            m_cbs.Add(AddCheckBox("Normals", MyRenderProxy.Settings.DisplayGbufferNormal, (x) => MyRenderProxy.Settings.DisplayGbufferNormal = x.IsChecked));
            m_cbs.Add(AddCheckBox("Normals view", MyRenderProxy.Settings.DisplayGbufferNormalView, (x) => MyRenderProxy.Settings.DisplayGbufferNormalView = x.IsChecked));
            m_cbs.Add(AddCheckBox("Glossiness", MyRenderProxy.Settings.DisplayGbufferGlossiness, (x) => MyRenderProxy.Settings.DisplayGbufferGlossiness = x.IsChecked));
            m_cbs.Add(AddCheckBox("Metalness", MyRenderProxy.Settings.DisplayGbufferMetalness, (x) => MyRenderProxy.Settings.DisplayGbufferMetalness = x.IsChecked));
            m_cbs.Add(AddCheckBox("NDotL", MyRenderProxy.Settings.DisplayNDotL, (x) => MyRenderProxy.Settings.DisplayNDotL = x.IsChecked));
            m_cbs.Add(AddCheckBox("LOD", MyRenderProxy.Settings.DisplayGbufferLOD, (x) => MyRenderProxy.Settings.DisplayGbufferLOD = x.IsChecked));
            m_cbs.Add(AddCheckBox("Mipmap", MyRenderProxy.Settings.DisplayMipmap, (x) => MyRenderProxy.Settings.DisplayMipmap = x.IsChecked));
            m_cbs.Add(AddCheckBox("Ambient occlusion", MyRenderProxy.Settings.DisplayGbufferAO, (x) => MyRenderProxy.Settings.DisplayGbufferAO = x.IsChecked));
            m_cbs.Add(AddCheckBox("Emissive", MyRenderProxy.Settings.DisplayEmissive, (x) => MyRenderProxy.Settings.DisplayEmissive = x.IsChecked));
            m_cbs.Add(AddCheckBox("Edge mask", MyRenderProxy.Settings.DisplayEdgeMask, (x) => MyRenderProxy.Settings.DisplayEdgeMask = x.IsChecked));
            m_cbs.Add(AddCheckBox("Depth", MyRenderProxy.Settings.DisplayDepth, (x) => MyRenderProxy.Settings.DisplayDepth = x.IsChecked));
            m_cbs.Add(AddCheckBox("Stencil", MyRenderProxy.Settings.DisplayStencil, (x) => MyRenderProxy.Settings.DisplayStencil = x.IsChecked));
            m_currentPosition.Y += 0.01f;

            m_cbs.Add(AddCheckBox("Reprojection test", MyRenderProxy.Settings.DisplayReprojectedDepth, (x) => MyRenderProxy.Settings.DisplayReprojectedDepth = x.IsChecked));
            m_currentPosition.Y += 0.01f;

            AddLabel("Environment light", Color.Yellow.ToVector4(), 1.2f);
            m_cbs.Add(AddCheckBox("Ambient diffuse", MyRenderProxy.Settings.DisplayAmbientDiffuse, (x) => MyRenderProxy.Settings.DisplayAmbientDiffuse = x.IsChecked));
            m_cbs.Add(AddCheckBox("Ambient specular", MyRenderProxy.Settings.DisplayAmbientSpecular, (x) => MyRenderProxy.Settings.DisplayAmbientSpecular = x.IsChecked));
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderGBufferDebug";
        }

        bool m_radioUpdate;
        protected override void ValueChanged(Sandbox.Graphics.GUI.MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();

            if (m_radioUpdate)
                return;

            m_radioUpdate = true;
            foreach (var item in m_cbs)
            {
                if (item != sender)
                    item.IsChecked = false;
            }
            m_radioUpdate = false;
        }
    }

#endif
}
