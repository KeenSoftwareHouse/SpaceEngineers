using System;
using SharpDX;
using SharpDX.Direct3D11;
using VRage.Render11.Resources.Internal;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal interface ISamplerState : IMyPersistentResource<SamplerStateDescription>
    {
    }

    internal interface ISamplerStateInternal : ISamplerState
    {
        SamplerState Resource { get; }
    }

    namespace Internal
    {
        internal class MySamplerState : MyPersistentResource<SamplerState, SamplerStateDescription>, ISamplerStateInternal
        {
            protected override SamplerState CreateResource(ref SamplerStateDescription description)
            {
                SamplerState state = new SamplerState(MyRender11.Device, description);
                state.DebugName = Name;
                return state;
            }
        }
    }

    internal class MySamplerStateManager : MyPersistentResourceManager<MySamplerState, SamplerStateDescription>
    {
        internal static ISamplerState Default;
        internal static ISamplerState Point;
        internal static ISamplerState Linear;
        internal static ISamplerState Texture;
        internal static ISamplerState Shadowmap;
        internal static ISamplerState Alphamask;
        internal static ISamplerState AlphamaskArray;
        internal static ISamplerState CloudSampler;
        internal static ISamplerState PointHBAOClamp;
        internal static ISamplerState PointHBAOBorder;

        internal static ISamplerState[] StandardSamplers;

        protected override int GetAllocResourceCount()
        {
            return 32;
        }

        internal MySamplerStateManager()
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            Default = CreateResource("Default", ref description);

            description.AddressU = TextureAddressMode.Border;
            description.AddressV = TextureAddressMode.Border;
            description.AddressW = TextureAddressMode.Border;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            description.BorderColor = new Color4(0, 0, 0, 0);
            Alphamask = CreateResource("Alphamask", ref description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipPoint;
            description.MaximumLod = Single.MaxValue;
            Point = CreateResource("Point", ref description);

            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            Linear = CreateResource("Linear", ref description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.ComparisonMinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            description.ComparisonFunction = Comparison.LessEqual;
            Shadowmap = CreateResource("Shadowmap", ref description);

            Texture = CreateResource("Texture", ref description);
            AlphamaskArray = CreateResource("AlphamaskArray", ref description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipPoint;
            description.MaximumLod = System.Single.MaxValue;
            description.MaximumAnisotropy = 1;
            description.ComparisonFunction = Comparison.Never;
            description.BorderColor = new SharpDX.Mathematics.Interop.RawColor4(-float.MaxValue, -float.MaxValue, -float.MaxValue, -float.MaxValue);
            PointHBAOClamp = CreateResource("PointHBAOClamp", ref description);

            description.AddressU = TextureAddressMode.Border;
            description.AddressV = TextureAddressMode.Border;
            PointHBAOBorder = CreateResource("PointHBAOBorder", ref description);

            description = new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = Single.MaxValue
            };
            CloudSampler = CreateResource("CloudSampler", ref description);

            StandardSamplers = new ISamplerState[]
            {
                Default,
                Point,
                Linear,
                Texture,
                Alphamask,
                AlphamaskArray,
            };

            UpdateFiltering();
        }

        internal static void UpdateFiltering()
        {
            UpdateTextureSampler((MySamplerState) Texture, TextureAddressMode.Wrap);
            UpdateTextureSampler((MySamplerState) AlphamaskArray, TextureAddressMode.Clamp);
        }

        static void UpdateTextureSampler(MySamplerState samplerState, TextureAddressMode addressMode)
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = addressMode;
            description.AddressV = addressMode;
            description.AddressW = addressMode;
            description.MaximumLod = Single.MaxValue;

            if (MyRender11.Settings.User.AnisotropicFiltering == MyTextureAnisoFiltering.NONE)
            {
                description.Filter = Filter.MinMagMipLinear;
            }
            else
            {
                description.Filter = Filter.Anisotropic;

                switch (MyRender11.Settings.User.AnisotropicFiltering)
                {
                    case MyTextureAnisoFiltering.ANISO_1:
                        description.MaximumAnisotropy = 1;
                        break;
                    case MyTextureAnisoFiltering.ANISO_4:
                        description.MaximumAnisotropy = 4;
                        break;
                    case MyTextureAnisoFiltering.ANISO_8:
                        description.MaximumAnisotropy = 8;
                        break;
                    case MyTextureAnisoFiltering.ANISO_16:
                        description.MaximumAnisotropy = 16;
                        break;
                    default:
                        description.MaximumAnisotropy = 1;
                        break;
                }
            }

            samplerState.ChangeDescription(ref description);
        }
    }
}
