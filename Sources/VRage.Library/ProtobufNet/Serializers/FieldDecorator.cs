﻿#if !XB1 // XB1_NOPROTOBUF
#if !NO_RUNTIME
using System;

using ProtoBuf.Meta;

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;
#endif



namespace ProtoBuf.Serializers
{
    sealed class FieldDecorator : ProtoDecoratorBase
    {

        public override Type ExpectedType { get { return forType; } }
        private readonly FieldInfo field;
        private readonly Type forType;
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return false; } }
        public FieldDecorator(Type forType, FieldInfo field, IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(field != null);
            this.forType = forType;
            this.field = field;
        }
#if !FEAT_IKVM
        public override void Write(object value, ProtoWriter dest)
        {
            Helpers.DebugAssert(value != null);
            value = field.GetValue(value);
            if(value != null) Tail.Write(value, dest);
        }
        public override object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value != null);
            object newValue = Tail.Read((Tail.RequiresOldValue ? field.GetValue(value) : null), source);
            if(newValue != null) field.SetValue(value,newValue);
            return null;
        }
#endif

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(field);
            ctx.WriteNullCheckedTail(field.FieldType, Tail, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadAddress(loc, ExpectedType);
                if (Tail.RequiresOldValue)
                {
                    ctx.CopyValue();
                    ctx.LoadValue(field);  
                }
                // value is either now on the stack or not needed
                ctx.ReadNullCheckedTail(field.FieldType, Tail, null);

                if (Tail.ReturnsValue)
                {
                    if (field.FieldType.IsValueType)
                    {
                        // stack is now the return value
                        ctx.StoreValue(field);
                    }
                    else
                    {
                        
                        Compiler.CodeLabel hasValue = ctx.DefineLabel(), allDone = ctx.DefineLabel();
                        ctx.CopyValue();
                        ctx.BranchIfTrue(hasValue, true); // interpret null as "don't assign"

                        // no value, discard
                        ctx.DiscardValue();
                        ctx.DiscardValue();
                        ctx.Branch(allDone, true);

                        ctx.MarkLabel(hasValue);
                        ctx.StoreValue(field);
                        ctx.MarkLabel(allDone);
                    }
                }
                else
                {
                    ctx.DiscardValue();
                }
            }
        }
#endif
    }
}
#endif
#endif // !XB1
