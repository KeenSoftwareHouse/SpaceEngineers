using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageRender;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game.Components;
using VRage.Game;
using VRage.Utils;
using Sandbox.Definitions;

namespace Sandbox.Game.Components
{
    class MyRenderComponentTextPanel : MyRenderComponent
    {
        const string PANEL_MATERIAL_NAME = "ScreenArea";
        private MyTextPanel m_textPanel;

        public MyRenderComponentTextPanel(MyTextPanel panel)
        {
            m_textPanel = panel;
        }

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
        }

        public void ChangeTexture(string path)
        {
            if (RenderObjectIDs[0] != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyRenderProxy.ChangeMaterialTexture(this.RenderObjectIDs[0], PANEL_MATERIAL_NAME, path);
                MyRenderProxy.UpdateModelProperties(this.RenderObjectIDs[0], 0, -1, PANEL_MATERIAL_NAME, null, null, 1);
            }
        }
        public void RenderTextToTexture(string text, float scale, Color fontColor, Color backgroundColor, int textureResolution, int aspectRatio)
        {
            string offscreenTexture = "LCDOffscreenTexture_" + m_textPanel.EntityId;

            int width = textureResolution * aspectRatio;
            int height = textureResolution;
            MyRenderProxy.CreateGeneratedTexture(offscreenTexture, width, height);
            MyRenderProxy.DrawString((int)MyDefinitionManager.Static.GetFontSafe(m_textPanel.Font.SubtypeName).Id.SubtypeId, Vector2.Zero, fontColor, text, scale, float.PositiveInfinity, offscreenTexture);
            MyRenderProxy.RenderOffscreenTextureToMaterial(RenderObjectIDs[0], PANEL_MATERIAL_NAME, offscreenTexture, backgroundColor);
        }
        public override void ReleaseRenderObjectID(int index)
        {
            if (m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyEntities.RemoveRenderObjectFromMap(m_renderObjectIDs[index]);
                VRageRender.MyRenderProxy.RemoveRenderObject(m_renderObjectIDs[index]);
                m_renderObjectIDs[index] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }
        override public void UpdateRenderEntity(Vector3 colorMaskHSV)
        {
            base.UpdateRenderEntity(colorMaskHSV);
            (Container.Entity as MyTextPanel).OnColorChanged();
        }

        #endregion
    }
}
