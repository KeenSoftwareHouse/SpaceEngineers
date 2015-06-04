using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Sandbox.Common;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    public partial class MyShipController : IMyShipController
    {
        IMyEntity Sandbox.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.DrawHud(ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            if (camera is IMyCameraController)
                DrawHud(camera as IMyCameraController, playerId);
        }

        bool IMyShipController.IsUnderControl
        {
            get { return ControllerInfo.Controller != null; }
        }

        bool IMyShipController.ControlWheels
        {
            get { return ControlWheels; }
        }

        bool IMyShipController.ControlThrusters
        {
            get { return ControlThrusters; }
        }

        bool IMyShipController.HandBrake
        {
            get { return CubeGrid.GridSystems.WheelSystem.HandBrake; }
        }

        bool IMyShipController.DampenersOverride
        {
            get
            {
                if (GridThrustSystem == null)
                {
                    Debug.Fail("Alex Florea: Grid thrust system should not be null!");
                    return false;
                }
                else
                {
                    return GridThrustSystem.DampenersEnabled;
                }
            }
        }

        bool IMyShipController.IsMain
        {
            get { return IsMainCockpit; }
        }

        void IMyShipController.NotifyPilot(string message, int displayTimeMs, MyFontEnum font)
        {
            if (ControllerInfo.Controller == null)
                return;

            message = (message.Length > 500 ? message.Substring(0, 500) : message);
            displayTimeMs = (displayTimeMs > 30000 ? 30000 : displayTimeMs);

            if (ControllerInfo.ControllingIdentityId == MySession.LocalPlayerId)
                NotifyPilot(message, displayTimeMs, font);
            else
                SyncObject.SendPilotNotification(ControllerInfo.Controller.Player, message, (ushort)displayTimeMs, font);
        }

        public void NotifyPilot(string message, int displayTimeMs, MyFontEnum font)
        {
            var notification = new MyHudNotification(Sandbox.Game.Localization.MySpaceTexts.CustomText, displayTimeMs, font);
            notification.SetTextFormatArguments(message);
            MyHud.Notifications.Add(notification);
        }
    }
}
