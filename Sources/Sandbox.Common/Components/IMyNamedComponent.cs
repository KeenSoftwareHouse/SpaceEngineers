using VRage;

namespace Sandbox.Common
{
    public interface IMyNamedComponent
    {
        string GetName();

        bool Enabled
        {
            get;
            set;
        }
    }
}
