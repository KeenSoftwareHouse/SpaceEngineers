using VRageMath;

namespace VRageRender.Import
{
    // Summary:
    //     Represents bone data for a model.
    public sealed class MyModelBone
    {
        //
        // Summary:
        //     Gets the index of this bone in the Bones collection.
        public int Index;
        //
        // Summary:
        //     Gets the name of this bone.
        public string Name;
        //
        // Summary:
        //     Gets the parent of this bone.
        public int Parent;
        //
        // Summary:
        //     Gets or sets the matrix used to transform this bone relative to its parent
        //     bone.
        public Matrix Transform;

        public override string ToString()
        {
            return Name + " (" + Index +")";
        }
    }
}
