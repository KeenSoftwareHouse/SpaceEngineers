using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.ModAPI;
using Havok;
using Sandbox.Game.Entities.Blocks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyMotorBase : IMyMotorBase
    {
        IMyCubeGrid IMyMotorBase.RotorGrid { get { return TopGrid; } }

        IMyCubeBlock IMyMotorBase.Rotor { get { return TopBlock; } }

        event Action<IMyMotorBase> IMyMotorBase.AttachedEntityChanged
        {
            add { AttachedEntityChanged += GetDelegate(value); }
            remove { AttachedEntityChanged -= GetDelegate(value); }
        }

        Action<MyMechanicalConnectionBlockBase> GetDelegate(Action<ModAPI.IMyMotorBase> value)
        {
            return (Action<MyMechanicalConnectionBlockBase>)Delegate.CreateDelegate(typeof(Action<MyMechanicalConnectionBlockBase>), value.Target, value.Method);
        }

        bool ModAPI.Ingame.IMyMotorBase.IsAttached
        {
            get { return m_isAttached; }
        }

        bool ModAPI.Ingame.IMyMotorBase.PendingAttachment
        {
            get { return (m_connectionState.Value.TopBlockId.HasValue && m_connectionState.Value.TopBlockId.Value == 0); }
        }

        void ModAPI.IMyMotorBase.Attach(ModAPI.IMyMotorRotor rotor)
        {
            if (rotor != null)
            {
                MatrixD masterToSlave = rotor.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                m_connectionState.Value = new State() { TopBlockId = rotor.EntityId, MasterToSlave = masterToSlave };
            }
        }

        void ModAPI.Ingame.IMyMotorBase.Attach()
        {
            m_connectionState.Value = new State() { TopBlockId = 0};
        }

        void ModAPI.Ingame.IMyMotorBase.Detach()
        {
            m_connectionState.Value = new State() { TopBlockId = null};
        }
    }
}
