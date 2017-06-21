using Sandbox.Common;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 400)]
    class MyHudCameraOverlay : VRage.Game.Components.MySessionComponentBase
    {
        private static string m_textureName;
        public static string TextureName
        {
            get
            {
                return m_textureName;
            }
            set
            {
                MyDebug.AssertDebug(!Enabled || value != null, "TextureName should not be null if the overlay is active");
                m_textureName = value;
            }
        }

        private static bool m_enabled = false;
        public static bool Enabled 
        {
            get
            {
                return m_enabled;
            }
            set
            {
                if (m_enabled != value)
                {
                    MyDebug.AssertDebug(m_textureName != null, "No texture set");
                    m_enabled = value;
                }
            }
        }

        public MyHudCameraOverlay() { }

        protected override void UnloadData()
        {
            base.UnloadData();
            Enabled = false;
        }

        public override void Draw()
        {
            base.Draw();

            if (!Enabled)
            {
                return;
            }

            MyDebug.AssertDebug(m_textureName != null, "TextureName should not be null when drawing overlay");
            DrawFullScreenSprite();
        }

        private static void DrawFullScreenSprite()
        {
            var fullScreenRect = MyGuiManager.GetFullscreenRectangle();
            var rect = new RectangleF(fullScreenRect.X, fullScreenRect.Y, fullScreenRect.Width, fullScreenRect.Height);
            Rectangle? source = null;
            Vector2 origin = Vector2.Zero;
            VRageRender.MyRenderProxy.DrawSprite(m_textureName, ref rect, false, ref source, Color.White, 0f, Vector2.UnitX, ref origin, SpriteEffects.None, 0f);
        }
    }
}
