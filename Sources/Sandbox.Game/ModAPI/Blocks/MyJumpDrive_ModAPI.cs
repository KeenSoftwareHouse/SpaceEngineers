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
        bool IMyJumpDrive.Recharge
        {
            get { return m_isRecharging; }
        }

        void IMyJumpDrive.SetRecharge(bool set)
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

        bool IMyJumpDrive.IsJumping
        {
            get { return IsJumping; }
        }

        float IMyJumpDrive.JumpCountdown
        {
            get
            {
                long? ticks = CubeGrid.GridSystems.JumpSystem.GetJumpElapsedTicks();

                return (ticks.HasValue ? 0 : (float)TimeSpan.FromTicks(ticks.Value).TotalSeconds);
            }
        }

        public bool CanUserJump(long userId)
        {
            return CanJumpAndHasAccess(userId);
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
            JumpTo(coords, 0);
        }

        public void JumpTo(Vector3D coords, long userId)
        {
            if (CanJump)
            {
                if (userId == 0)
                    userId = OwnerId;

                CubeGrid.GridSystems.JumpSystem.RequestJump(String.Empty, coords, userId, skipDialog: true);
            }
        }

        void IMyJumpDrive.AbortJump()
        {
            CubeGrid.GridSystems.JumpSystem.RequestAbort();
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

        double IMyJumpDrive.GetTotalMass()
        {
            return CubeGrid.GridSystems.JumpSystem.GetMass();
        }

        double IMyJumpDrive.GetMinJumpDistance()
        {
            return MyGridJumpDriveSystem.MIN_JUMP_DISTANCE;
        }

        double IMyJumpDrive.GetMaxJumpDistance()
        {
            return CubeGrid.GridSystems.JumpSystem.GetMaxJumpDistance(this.OwnerId);
        }
    }
}
