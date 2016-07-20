using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyMotorBase : IMyFunctionalBlock, ModAPI.Ingame.IMyMotorBase
    {
        /// <summary>
        /// Gets the grid attached to the rotor part
        /// </summary>
        IMyCubeGrid RotorGrid { get; }

        /// <summary>
        /// Gets the attached rotor part entity
        /// </summary>
        IMyCubeBlock Rotor { get; }

        /// <summary>
        /// When the rotor head is attached or detached from the base
        /// </summary>
        /// <remarks>This event can be called in three states:
        /// <list type="number">
        /// <item>Stator is detached from rotor</item>
        /// <item>Stator is looking for rotor attachment</item>
        /// <item>Stator is attached to rotor</item>
        /// </list>
        /// The looking and attached states will both return <b>true</b> for <see cref="Sandbox.ModAPI.Ingame.IMyMotorBase.IsAttached">IsAttached</see>.</para>
        /// To determine which state it is, use the <see cref="Sandbox.ModAPI.Ingame.IMyMotorBase.PendingAttachment">PendingAttachment</see> property: <b>true</b> means awaiting attachment, <b>false</b> means the rotor is attached.
        /// </remarks>
        event Action<IMyMotorBase> AttachedEntityChanged;

        /// <summary>
        /// Gets the dummy position, to aid in attachment
        /// </summary>
        /// <remarks>Gets the location where the top rotor piece will attach.</remarks>
        Vector3 DummyPosition { get; }

        /// <summary>
        /// Attaches a specified nearby rotor/wheel to the stator/suspension block
        /// </summary>
        /// <param name="rotor">Entity to attach</param>
        /// <remarks>The rotor to attach must already be in position before calling this method.</remarks>
        void Attach(IMyMotorRotor rotor);
    }
}
