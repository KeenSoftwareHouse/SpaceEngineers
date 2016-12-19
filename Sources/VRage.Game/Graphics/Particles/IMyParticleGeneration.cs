using System.Collections.Generic;
using System.Xml;
using VRageRender.Animations;
using VRageMath;

namespace VRage.Game
{
    public interface IMyParticleGeneration
    {
        void Close();
        void Deallocate();

        IMyParticleGeneration CreateInstance(MyParticleEffect effect);

        string Name { get; set; }
        MyConstPropertyBool Enabled { get; }
        MatrixD EffectMatrix { get; set; }

        IEnumerable<IMyConstProperty> GetProperties();
        MyParticleEmitter GetEmitter();
        MyParticleEffect GetEffect();

        void Serialize(XmlWriter writer);
        void Deserialize(XmlReader reader);

        void Clear();
        void Done();

        IMyParticleGeneration Duplicate(MyParticleEffect effect);
        void Update();

        int GetParticlesCount();
        float GetBirthRate();

        void MergeAABB(ref BoundingBoxD aabb);

        void SetDirty();
        void SetAnimDirty();
        void SetPositionDirty();
        
        bool Show { get; set; }

        void Draw(List<VRageRender.MyBillboard> collectedBillboards);
        void PrepareForDraw();
    }
}
