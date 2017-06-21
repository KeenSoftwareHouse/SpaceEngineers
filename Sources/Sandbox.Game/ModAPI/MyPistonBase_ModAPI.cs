using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.ModAPI.Ingame;
using Havok;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    partial class MyPistonBase : Sandbox.ModAPI.IMyPistonBase
    {
        IMyCubeGrid ModAPI.IMyPistonBase.TopGrid { get { return TopGrid; } }

        IMyCubeBlock ModAPI.IMyPistonBase.Top { get { return TopBlock; } }

        float IMyPistonBase.Velocity
        {
            get { return Velocity; }
        }

        float IMyPistonBase.MinLimit
        {
            get { return MinLimit; }
        }

        float IMyPistonBase.MaxLimit
        {
            get { return MaxLimit; }
        }

        bool IMyPistonBase.IsAttached
        {
            get { return m_isAttached; }
        }

        bool IMyPistonBase.IsLocked
        {
            get { return m_isWelding || m_welded; }
        }

        bool IMyPistonBase.PendingAttachment
        {
            get { return m_connectionState.Value.TopBlockId == 0; }
        }

        Action<MyMechanicalConnectionBlockBase> GetDelegate(Action<ModAPI.IMyPistonBase> value)
        {
            return (Action<MyMechanicalConnectionBlockBase>)Delegate.CreateDelegate(typeof(Action<MyMechanicalConnectionBlockBase>), value.Target, value.Method);
        }

        event Action<bool> ModAPI.IMyPistonBase.LimitReached
        {
            add { LimitReached += value; }
            remove { LimitReached -= value; }
        }

        event Action<ModAPI.IMyPistonBase> ModAPI.IMyPistonBase.AttachedEntityChanged
        {
            add { AttachedEntityChanged += GetDelegate(value); }
            remove { AttachedEntityChanged -= GetDelegate(value); }
        }

        void ModAPI.IMyPistonBase.Attach(ModAPI.IMyPistonTop top)
        {
            if (top != null)
                m_connectionState.Value = new State() { TopBlockId = top.EntityId };
        }

        void ModAPI.Ingame.IMyPistonBase.Attach()
        {
            m_connectionState.Value = new State() { TopBlockId = 0 };
        }

        void ModAPI.Ingame.IMyPistonBase.Detach()
        {
            m_connectionState.Value = new State() { TopBlockId = null };
        }
    }
}
