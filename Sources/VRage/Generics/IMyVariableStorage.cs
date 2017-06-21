using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Interface of variable storage (key-value principle).
    /// </summary>
    public interface IMyVariableStorage<T>
    {
        // Set new value for the specified key. If not present, create it. Implicit conversion is done on the way.
        void SetValue(MyStringId key, T newValue);
        // Get value for the specified key. If not found, newValue is set to null and false is returned. Implicit conversion is done on the way.
        bool GetValue(MyStringId key, out T value);
    }
}
