using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
namespace VRage.Game.ModAPI
{
	public delegate void CharacterMovementStateDelegate(MyCharacterMovementEnum oldState, MyCharacterMovementEnum newState);

    public interface IMyCharacter : IMyEntity, IMyControllableEntity, IMyCameraController, IMyDestroyableObject, IMyDecalProxy
    {
        float EnvironmentOxygenLevel { get; }
		float BaseMass { get; }
		float CurrentMass { get; }

        /// <summary>
        /// Returns the amount of energy the suit has, values will range between 0 and 1, where 0 is no charge and 1 is full charge.
        /// </summary>
        float SuitEnergyLevel { get; }

        /// <summary>
        /// Returns the amount of gas left in the suit, values will range between 0 and 1, where 0 is no gas and 1 is full gas.
        /// </summary>
        /// <param name="gasDefinitionId">Definition Id of the gas. Common example: new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen")</param>
        /// <returns></returns>
        float GetSuitGasFillLevel(MyDefinitionId gasDefinitionId);
        
        /// <summary>
        /// Returns true is this character is a player character, otherwise false.
        /// </summary>
        bool IsPlayer { get; }

        /// <summary>
        /// Returns true is this character is an AI character, otherwise false.
        /// </summary>
        bool IsBot { get; }

		event CharacterMovementStateDelegate OnMovementStateChanged;

		void Kill(object killData = null);

        /// <summary>
        /// Trigger animation event in the new animation system.
        /// If there is a transition leading from current animation state having same name as this event, 
        /// animation state machine will change state accordingly.
        /// If not, nothing happens.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="sync">Synchronize over network</param>
	    void TriggerCharacterAnimationEvent(string eventName, bool sync);
    }
}
