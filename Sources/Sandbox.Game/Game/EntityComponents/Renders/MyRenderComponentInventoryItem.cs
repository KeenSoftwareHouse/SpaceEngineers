using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using VRageMath;
using VRageRender;
using Sandbox.Graphics;
using Sandbox.Game.Weapons;

using VRage.Game.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentInventoryItem:MyRenderComponent
    {
        MyBaseInventoryItemEntity m_invetoryItem;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_invetoryItem = Container.Entity as MyBaseInventoryItemEntity;
        }
        public override void Draw()
        {
            base.Draw();

            Vector3 transformedPoint = Vector3.Transform(Container.Entity.PositionComp.GetPosition(), MySector.MainCamera.ViewMatrix);
            Vector4 projectedPoint = Vector4.Transform(transformedPoint, MySector.MainCamera.ProjectionMatrix);

            if (transformedPoint.Z > 0)
            {
                projectedPoint.X *= -1;
                projectedPoint.Y *= -1;
            }
            if (projectedPoint.W <= 0) return;

            Vector2 projectedPoint2D = new Vector2(projectedPoint.X / projectedPoint.W / 2.0f + 0.5f, -projectedPoint.Y / projectedPoint.W / 2.0f + 0.5f);

            projectedPoint2D = MyGuiManager.GetHudPixelCoordFromNormalizedCoord(projectedPoint2D);

            for (int i = 0; i < m_invetoryItem.IconTextures.Length; i++ )
                MyGuiManager.DrawSprite(m_invetoryItem.IconTextures[i], projectedPoint2D, new Rectangle(0, 0, 128, 128), Color.White,
                     0, new Vector2(64, 64), new Vector2(0.5f), SpriteEffects.None, 0);
        }
        
        #endregion

    }
}
