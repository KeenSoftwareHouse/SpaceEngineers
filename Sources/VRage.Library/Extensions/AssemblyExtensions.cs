using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Reflection
{
    public static class AssemblyExtensions
    {
#if !XB1
        public static ProcessorArchitecture ToProcessorArchitecture(this PortableExecutableKinds peKind)
        {
            switch (peKind &~PortableExecutableKinds.ILOnly)
            {
                case PortableExecutableKinds.PE32Plus:
                    return ProcessorArchitecture.Amd64;

                case PortableExecutableKinds.Required32Bit:
                    return ProcessorArchitecture.X86;

                case PortableExecutableKinds.Unmanaged32Bit:
                    return ProcessorArchitecture.X86;

                default:
                    return (peKind & PortableExecutableKinds.ILOnly) != 0 ? ProcessorArchitecture.MSIL : ProcessorArchitecture.None;
            }
        }
        public static PortableExecutableKinds GetPeKind(this Assembly assembly)
        {
            PortableExecutableKinds peKind;
            ImageFileMachine img;
            assembly.ManifestModule.GetPEKind(out peKind, out img);
            return peKind;
        }
#endif
        public static ProcessorArchitecture GetArchitecture(this Assembly assembly)
        {
#if XB1
			return ProcessorArchitecture.Amd64;
#else
            return assembly.GetPeKind().ToProcessorArchitecture();
#endif
        }

        public static ProcessorArchitecture TryGetArchitecture(string assemblyName)
        {
#if XB1
			return ProcessorArchitecture.Amd64;
#else
            try
            {
                return AssemblyName.GetAssemblyName(assemblyName).ProcessorArchitecture;
            }
            catch
            {
                return ProcessorArchitecture.None;
            }
#endif
        }

        public static ProcessorArchitecture TryGetArchitecture(this Assembly assembly)
        {
#if XB1
			return ProcessorArchitecture.Amd64;

#else
            try
            {
                return assembly.GetArchitecture();
            }
            catch
            {
                return ProcessorArchitecture.None;
            }
#endif
        }
    }
}
