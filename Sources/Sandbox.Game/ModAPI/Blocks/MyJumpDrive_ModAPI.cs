using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyJumpDrive : Sandbox.ModAPI.IMyJumpDrive
    {
        bool IMyJumpDrive.IsCharging
        {
            get { return m_isRecharging; }
        }

        void IMyJumpDrive.SetCharging(bool set)
        {
            SetRecharging(set);
        }

        bool IMyJumpDrive.IsFull
        {
            get { return IsFull; }
        }

        bool IMyJumpDrive.CanJump
        {
            get { return CanJump; }
        }

        float IMyJumpDrive.TimeUntilCharged
        {
            get { return m_timeRemaining; }
        }

        float IMyJumpDrive.CurrentStoredPower
        {
            get { return m_storedPower; }
        }

        float IMyJumpDrive.MaxStoredPower
        {
            get { return BlockDefinition.PowerNeededForJump; }
        }

        float IMyJumpDrive.JumpDistancePercent
        {
            get { return m_jumpDistanceRatio; }
        }

        void IMyJumpDrive.SetJumpDistancePercent(float percent)
        {
            SetJumpDistanceRatio(percent);
        }

        void IMyJumpDrive.JumpTo(Vector3D coords)
        {
            if (CanJump)
            {
                CubeGrid.GridSystems.JumpSystem.RequestJump(String.Empty, coords, skipDialog: true);
            }
        }

        string IMyJumpDrive.TargetName
        {
            get
            {
                if (m_jumpTarget != null)
                    return m_jumpTarget.Name;

                return null;
            }
        }

        Vector3D? IMyJumpDrive.TargetCoords
        {
            get
            {
                if (m_jumpTarget != null)
                    return m_jumpTarget.Coords;

                return null;
            }
        }

        double IMyJumpDrive.GetMinJumpDistance()
        {
            return MyGridJumpDriveSystem.MIN_JUMP_DISTANCE;
        }

        double IMyJumpDrive.GetMaxJumpDistance()
        {
            return CubeGrid.GridSystems.JumpSystem.GetMaxJumpDistance();
        }
    }
}
