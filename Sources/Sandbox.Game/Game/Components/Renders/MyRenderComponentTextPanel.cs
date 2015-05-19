using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.Components;
using VRageRender;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;

namespace Sandbox.Game.Components
{
    class MyRenderComponentTextPanel : MyRenderComponent
    {
        const string PANEL_MATERIAL_NAME = "ScreenArea";
        #region overrides
        public override void OnAddedToContainer(MyComponentContainer container)
        {
            base.OnAddedToContainer(container);
        }

        public void ChangeTexture(string path)
        {
            MyRenderProxy.ChangeMaterialTexture(this.RenderObjectIDs[0], PANEL_MATERIAL_NAME, path);
        }
        public void RenderTextToTexture(long entityId,string text, float scale, Color fontColor, Color backgroundColor, int textureResolution,int aspectRatio, MyFontEnum font)
        {
            MyRenderProxy.RenderTextToTexture(RenderObjectIDs[0], entityId, PANEL_MATERIAL_NAME, text, scale, fontColor, backgroundColor, textureResolution, aspectRatio, (int)font);
        }
        public void ReleaseRenderTexture()
        {
            MyRenderProxy.ReleaseRenderTexture(Entity.EntityId,RenderObjectIDs[0]);
        }
        public override void ReleaseRenderObjectID(int index)
        {
            if (m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyEntities.RemoveRenderObjectFromMap(m_renderObjectIDs[index]);
                VRageRender.MyRenderProxy.ReleaseRenderTexture(Entity.EntityId,m_renderObjectIDs[index]);
                VRageRender.MyRenderProxy.RemoveRenderObject(m_renderObjectIDs[index]);
                m_renderObjectIDs[index] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }
        override public void UpdateRenderEntity(Vector3 colorMaskHSV)
        {
            base.UpdateRenderEntity(colorMaskHSV);
            (Entity as MyTextPanel).OnColorChanged();
        }
        #endregion
    }
}
