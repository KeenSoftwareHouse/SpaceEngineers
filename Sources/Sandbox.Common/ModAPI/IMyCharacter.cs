using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
	public delegate void CharacterMovementStateDelegate(MyCharacterMovementEnum oldState, MyCharacterMovementEnum newState);

	public interface IMyCharacter
    {
        float EnvironmentOxygenLevel { get; }

		event CharacterMovementStateDelegate OnMovementStateChanged;
    }
}
