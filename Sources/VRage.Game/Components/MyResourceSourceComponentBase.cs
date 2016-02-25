using MyDefinitionId = VRage.Game.MyDefinitionId;

namespace VRage.Game.Components
{
	public abstract class MyResourceSourceComponentBase : MyEntityComponentBase
	{
	    /// <summary>
	    /// Currently used power output of the producer in MW or litres/h.
	    /// </summary>
	    public abstract float CurrentOutputByType(MyDefinitionId resourceTypeId);

	    /// <summary>
	    /// Maximum power output of the producer in MW or litres/h.
	    /// </summary>
	    public abstract float MaxOutputByType(MyDefinitionId resourceTypeId);

	    /// <summary>
	    /// Max resource output defined in definition in MW or litres/h.
	    /// </summary>
	    public abstract float DefinedOutputByType(MyDefinitionId resourceTypeId);

	    /// <summary>
	    /// Resource production is enabled
	    /// </summary>
	    public abstract bool ProductionEnabledByType(MyDefinitionId resourceTypeId);
	}
}
