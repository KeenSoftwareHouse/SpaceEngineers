using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializeException : Exception
    {
        public MySerializeErrorEnum Error;

        public MySerializeException(MySerializeErrorEnum error)
        {
            Error = error;
        }
    }
}
