using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.Components;
using VRageRender;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentTextPanel : MyRenderComponent
    {
        const string PANEL_MATERIAL_NAME = "ScreenArea";
        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
        }

        public void ChangeTexture(string path)
        {
            MyRenderProxy.ChangeMaterialTexture(this.RenderObjectIDs[0], PANEL_MATERIAL_NAME, path);
            if (RenderObjectIDs[0] != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyRenderProxy.UpdateModelProperties(this.RenderObjectIDs[0], 0, null, -1, PANEL_MATERIAL_NAME, null, null, null, null, 1);
            }
        }
        public void RenderTextToTexture(long entityId,string text, float scale, Color fontColor, Color backgroundColor, int textureResolution,int aspectRatio)
        {
            MyRenderProxy.RenderTextToTexture(RenderObjectIDs[0], entityId, PANEL_MATERIAL_NAME, text, scale, fontColor, backgroundColor, textureResolution, aspectRatio);
        }
        public void ReleaseRenderTexture()
        {
            MyRenderProxy.ReleaseRenderTexture(Container.Entity.EntityId, RenderObjectIDs[0]);
        }
        public override void ReleaseRenderObjectID(int index)
        {
            if (m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyEntities.RemoveRenderObjectFromMap(m_renderObjectIDs[index]);
                VRageRender.MyRenderProxy.ReleaseRenderTexture(Container.Entity.EntityId, m_renderObjectIDs[index]);
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
