using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Plugins
{
    public interface IPlugin : IDisposable
    {
        void Init(object gameInstance);
        void Update();
    }
}
