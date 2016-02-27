namespace VRage.ObjectBuilders
{
    [ProtoBuf.ProtoContract]
    public struct MyRuntimeObjectBuilderId
    {
        public static readonly MyRuntimeObjectBuilderIdComparer Comparer = new MyRuntimeObjectBuilderIdComparer();

        [ProtoBuf.ProtoMember]
        internal readonly ushort Value;

        public MyRuntimeObjectBuilderId(ushort value)
        {
            Value = value;
        }

        public bool IsValid
        {
            get { return Value != 0; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Value, (MyObjectBuilderType)this);
        }
    }
}
