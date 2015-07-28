using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;

namespace Sandbox.ModAPI
{
	public delegate void CharacterMovementStateDelegate(MyCharacterMovementEnum oldState, MyCharacterMovementEnum newState);

	public interface IMyCharacter
    {
        float EnvironmentOxygenLevel { get; }
		float BaseMass { get; }
		float CurrentMass { get; }

		event CharacterMovementStateDelegate OnMovementStateChanged;

		void Kill(object killData = null);
    }
}
