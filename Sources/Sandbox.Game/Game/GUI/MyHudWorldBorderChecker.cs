using Sandbox.Common;

using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game.Gui
{
    public class MyHudWorldBorderChecker
    {
        private static readonly float WARNING_DISTANCE = 600.0f;

        private MyHudNotification m_notification = new MyHudNotification(MySpaceTexts.NotificationLeavingWorld, MyHudNotification.INFINITE, MyFontEnum.Red);
        private MyHudNotification m_notificationCreative = new MyHudNotification(MySpaceTexts.NotificationLeavingWorld_Creative, MyHudNotification.INFINITE, MyFontEnum.Red);

        internal static MyHudEntityParams HudEntityParams = new MyHudEntityParams(MyTexts.Get(MySpaceTexts.HudMarker_ReturnToWorld), MyRelationsBetweenPlayerAndBlock.Enemies, float.MaxValue, MyHudIndicatorFlagsEnum.SHOW_BORDER_INDICATORS | MyHudIndicatorFlagsEnum.SHOW_TEXT);

        public bool WorldCenterHintVisible { get; private set; }

        public void Update()
        {
            if (MySession.ControlledEntity != null)
            {
                float maxDistance = MyEntities.WorldHalfExtent();

                var dist = MySession.ControlledEntity.Entity != null ? MySession.ControlledEntity.Entity.PositionComp.GetPosition().AbsMax() : 0.0f;

                if (maxDistance != 0.0f && MySession.ControlledEntity.Entity != null && maxDistance - dist < WARNING_DISTANCE)
                {
                    var d = maxDistance - dist > 0.0f ? maxDistance - dist : 0.0f;
                    if (MySession.Static.SurvivalMode)
                    {
                        m_notification.SetTextFormatArguments(d);
                        MyHud.Notifications.Add(m_notification);
                    }
                    else
                    {
                        m_notificationCreative.SetTextFormatArguments(d);
                        MyHud.Notifications.Add(m_notificationCreative);
                    }
                    WorldCenterHintVisible = true;
                }
                else
                {
                    MyHud.Notifications.Remove(m_notification);
                    MyHud.Notifications.Remove(m_notificationCreative);
                    WorldCenterHintVisible = false;
                }
            }
        }
    }
}
