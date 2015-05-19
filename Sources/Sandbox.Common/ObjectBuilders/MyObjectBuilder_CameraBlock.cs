using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CameraBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsActive;

        //By default set to maximum FOV value
        //Will get clamped to actual FOV in init
        [ProtoMember]
        public float Fov = 90;
    }
}
