using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum MyRenderExceptionEnum
    {
        Unassigned,
        DriverNotInstalled,
        GpuNotSupported
    }

    public class MyRenderException : Exception
    {
        MyRenderExceptionEnum m_type;

        public MyRenderExceptionEnum Type { get { return m_type; } }

        public MyRenderException(string message, MyRenderExceptionEnum type = MyRenderExceptionEnum.Unassigned)
            : base(message)
        {
            m_type = type;
        }
    }
}
