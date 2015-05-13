using Sandbox.Common;
using Sandbox.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.Components
{
    public static class MyRenderComponentExtensions
    {
        public static MyModel GetModel(this MyRenderComponentBase obj)
        {
            return (MyModel)obj.ModelStorage;
        }
    }
}
