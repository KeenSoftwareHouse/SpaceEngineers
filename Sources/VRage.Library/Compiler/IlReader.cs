#if !XB1 // XB1_NOILREADER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace VRage.Compiler
{
#if UNSHARPER
	public class IlReader
	{
		public class IlInstruction
		{
		}
	}

#else
    /// <summary>
    /// Reads method body and returns instructions
    /// </summary>
    public class IlReader
    {
        public class IlInstruction
        {
            public OpCode OpCode;
            public object Operand;
            public long Offset;
            public long LocalVariableIndex;
            public string FormatOperand()
            {
                switch (OpCode.OperandType)
                {
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                        {
                            if (Operand is MethodInfo)
                            {
                                var methodInfo = ((MethodInfo)Operand);
                                var sig = methodInfo.ToString().Substring(methodInfo.ReturnType.Name.ToString().Length + 1);
                                return String.Format("{0} {1}::{2}", methodInfo.ReturnType, methodInfo.DeclaringType, sig);
                            }
                            else if (Operand is ConstructorInfo)
                            {
                                var constInfo = ((ConstructorInfo)Operand);
                                var sig = constInfo.ToString().Substring("Void".Length + 1);
                                return String.Format("{0}::{1}", constInfo.DeclaringType, sig);
                            }
                            return Operand.ToString();
                        }

                    case OperandType.InlineNone:
                        return String.Empty;

                    default:
                        return Operand.ToString();
                }
            }

            public override string ToString()
            {
                return OpCode + " " + FormatOperand();
            }
        }

        private BinaryReader stream;
        private OpCode[] singleByteOpCode;
        private OpCode[] doubleByteOpCode;
        private byte[] instructions;
        private IList<LocalVariableInfo> locals;
        private ParameterInfo[] parameters;
        private Type[] typeArgs = null;
        private Type[] methodArgs = null;
        private MethodBase currentMethod = null;
        private List<IlInstruction> ilInstructions = null;

        public IlReader()
        {
            CreateOpCodes();
        }

        private void CreateOpCodes()
        {
            singleByteOpCode = new OpCode[225];
            doubleByteOpCode = new OpCode[31];

            FieldInfo[] fields = GetOpCodeFields();

            for (int i = 0; i < fields.Length; i++)
            {
                OpCode code = (OpCode)fields[i].GetValue(null);

                if (code.OpCodeType == OpCodeType.Nternal)
                    continue;

                if (code.Size == 1)
                    singleByteOpCode[code.Value] = code;
                else
                    doubleByteOpCode[code.Value & 0xff] = code;
            }
        }

        public List<IlInstruction> ReadInstructions(MethodBase method)
        {
            ilInstructions = new List<IlInstruction>();
            this.currentMethod = method;

            var body = method.GetMethodBody();
            parameters = method.GetParameters();
            if (body == null)
                return ilInstructions;
            locals = body.LocalVariables;
            instructions = method.GetMethodBody().GetILAsByteArray();
            var str = new ByteStream(instructions, instructions.Length);
            stream = new BinaryReader(str);

            if (!(typeof(ConstructorInfo).IsAssignableFrom(method.GetType())))
                methodArgs = method.GetGenericArguments();

            if (method.DeclaringType != null)
                typeArgs = method.DeclaringType.GetGenericArguments();

            IlInstruction instruction = null;

            while (stream.BaseStream.Position < stream.BaseStream.Length)
            {
                instruction = new IlInstruction();
                bool isDoubleByte = false;
                OpCode code = ReadOpCode(ref isDoubleByte);
                instruction.OpCode = code;
                instruction.Offset = stream.BaseStream.Position-1;
                if (isDoubleByte)
                {
                    instruction.Offset--;
                }
                instruction.Operand = ReadOperand(code, method.Module,ref instruction.LocalVariableIndex);
                ilInstructions.Add(instruction);
            }

            return ilInstructions;
        }

        private object ReadOperand(OpCode code, Module module, ref long localVariableIndex)
        {
            object operand = null;
            
            switch (code.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineSwitch:
                    int length = stream.ReadInt32();
                    int[] branches = new int[length];
                    int[] offsets = new int[length];
                    for (int i = 0; i < length; i++)
                    {
                        offsets[i] = stream.ReadInt32();
                    }
                    for (int i = 0; i < length; i++)
                    {
                        branches[i] = (int)stream.BaseStream.Position + offsets[i];
                    }

                    operand = (object) branches; // Just forget to save readed offsets
                    break;
                case OperandType.ShortInlineBrTarget:
                    if (code.FlowControl != FlowControl.Branch && code.FlowControl != FlowControl.Cond_Branch)
                    {
                        operand = stream.ReadSByte();
                    }
                    else
                    {
                        operand = stream.ReadSByte() + stream.BaseStream.Position;
                    }
                    break;
                case OperandType.InlineBrTarget:
                    operand = stream.ReadInt32()+ stream.BaseStream.Position;;
                    break;
                case OperandType.ShortInlineI:
                    if (code == OpCodes.Ldc_I4_S)
                        operand = (sbyte)stream.ReadByte();
                    else
                        operand = stream.ReadByte();
                    break;
                case OperandType.InlineI:
                    operand = stream.ReadInt32();
                    break;
                case OperandType.ShortInlineR:
                    operand = stream.ReadSingle();
                    break;
                case OperandType.InlineR:
                    operand = stream.ReadDouble();
                    break;
                case OperandType.InlineI8:
                    operand = stream.ReadInt64();
                    break;
                case OperandType.InlineSig:
                    operand = module.ResolveSignature(stream.ReadInt32());
                    break;
                case OperandType.InlineString:
                    operand = module.ResolveString(stream.ReadInt32());
                    break;
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                    operand = module.ResolveMember(stream.ReadInt32(), typeArgs, methodArgs);
                    break;
                case OperandType.ShortInlineVar:
                    {
                        int index = stream.ReadByte();
                        operand = GetVariable(code, index);
                        localVariableIndex = index;
                    }
                    break;
                case OperandType.InlineVar:
                    {
                        int index = stream.ReadUInt16();
                        operand = GetVariable(code, index);
                        localVariableIndex = index;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

            return operand;
        }

        private OpCode ReadOpCode(ref bool isDoubleByte)
        {
            isDoubleByte = false;
            byte instruction = stream.ReadByte();
            if (instruction != 254)
                return singleByteOpCode[instruction];
            else
            {
                isDoubleByte = true;
                return doubleByteOpCode[stream.ReadByte()];
            }
        }

        private object GetVariable(OpCode code, int index)
        {
            if (code.Name.Contains("loc"))
                return locals[index];

            if (!currentMethod.IsStatic)
                index--;

            return parameters[index];
        }

        private FieldInfo[] GetOpCodeFields()
        {
            return typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        }

        public IList<LocalVariableInfo> Locals 
        {
            get { return locals; }
        }
    }
#endif
}
#endif // !XB1

