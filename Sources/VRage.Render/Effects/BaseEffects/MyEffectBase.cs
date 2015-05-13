using System.IO;
using SharpDX.Direct3D;
using SharpDX.Direct3D9;
using VRage.Utils;

//  This class is wrapper for XNA effect with added functionality of holding references to commonly used parameters (light positions, fog, camera, matrixes, etc).

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Matrix = VRageMath.Matrix;
    using System;
    using System.Diagnostics;
    using VRage.Library.Utils;
    using VRage.FileSystem;

    //  Base class for all effects
    internal abstract class MyEffectBase : IDisposable
    {
        class MyIncludeProcessor : Include
        {
            DateTime m_mostUpdateDateTime;
            string m_effectPath;

            public void Reset(string effectPath)
            {
                m_effectPath = effectPath;
                m_mostUpdateDateTime = DateTime.MinValue;
            }

            public void Close(Stream stream)
            {
                stream.Close();
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                string fullFileName = Path.Combine(m_effectPath, fileName);
                DateTime sourceTime = File.GetLastWriteTime(fullFileName);

                if (sourceTime > m_mostUpdateDateTime)
                    m_mostUpdateDateTime = sourceTime;

                return new FileStream(fullFileName, FileMode.Open, FileAccess.Read);
            }

            public void Dispose()
            {
            }

            public IDisposable Shadow { get; set; }

            public DateTime MostUpdateDateTime
            {
                get { return m_mostUpdateDateTime; }
            }
        }

        protected readonly Effect m_D3DEffect;

        EffectHandle m_nearPlane;
        EffectHandle m_farPlane;

        EffectHandle m_fogDistanceNear;
        EffectHandle m_fogDistanceFar;
        EffectHandle m_fogColor;
        EffectHandle m_fogMultiplier;
        EffectHandle m_fogBacklightMultiplier;

        static MyIncludeProcessor m_includeProcessor = new MyIncludeProcessor();

        protected MyEffectBase(Effect xnaEffect)
        {
            m_D3DEffect = xnaEffect;

            Init();
        }

        protected MyEffectBase(string asset)
        {
            string sourceFX = asset + ".fx";
            string compiledFX = asset + ".fxo";

            var srcPath = Path.Combine(MyFileSystem.ContentPath, sourceFX);
            var comPath = Path.Combine(MyFileSystem.ContentPath, compiledFX);

            var srcAbsolute = srcPath;

            bool needRecompile = false;

            if (File.Exists(comPath))
            {
                if (File.Exists(srcPath) && !MyRenderProxy.IS_OFFICIAL)
                {
                    var compiledTime = File.GetLastWriteTime(comPath);
                    var sourceTime   = File.GetLastWriteTime(srcAbsolute);

                    m_includeProcessor.Reset(Path.GetDirectoryName(srcAbsolute));
                    ShaderBytecode.PreprocessFromFile(srcAbsolute, null, m_includeProcessor);
                    sourceTime = m_includeProcessor.MostUpdateDateTime > sourceTime ? m_includeProcessor.MostUpdateDateTime : sourceTime;

                    if (sourceTime > compiledTime)
                        needRecompile = true;
                }
            }
            else
            {
                if (File.Exists(srcPath))
                    needRecompile = true;
                else
                {
                    throw new FileNotFoundException("Effect not found: " + asset);
                }
            }

            ShaderFlags flags = ShaderFlags.OptimizationLevel3 | ShaderFlags.PartialPrecision | ShaderFlags.SkipValidation;
       
            if (needRecompile)
            {
//#if DEBUG
//                flags |= ShaderFlags.Debug;
//#endif
                //m_D3DEffect = Effect.FromFile(MySandboxGameDX.Static.GraphicsDevice, sourceFX, flags);

                try
                {
                    var srcDir = Path.GetDirectoryName(srcAbsolute);
                    m_includeProcessor.Reset(srcDir);
                    var result = ShaderBytecode.CompileFromFile(srcAbsolute, "fx_2_0", flags, null, m_includeProcessor);

                    ShaderBytecode shaderByteCode = result;
                    shaderByteCode.Save(Path.Combine(srcDir, Path.GetFileNameWithoutExtension(srcAbsolute) + ".fxo"));
                    result.Dispose();
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);
                    throw;
                }
            }

            using (var fs = MyFileSystem.OpenRead(comPath))
            {
                byte[] m = new byte[fs.Length];
                fs.Read(m, 0, (int)fs.Length);

                m_D3DEffect = Effect.FromMemory(MyRender.GraphicsDevice, m, flags);
            }
            Init();
        }

        private void Init()
        {
            m_nearPlane = m_D3DEffect.GetParameter(null, "NEAR_PLANE_DISTANCE");
            m_farPlane = m_D3DEffect.GetParameter(null, "FAR_PLANE_DISTANCE");

            if (m_nearPlane != null && m_farPlane != null)
            {
                m_D3DEffect.SetValue(m_farPlane, MyRenderCamera.FAR_PLANE_DISTANCE);
                m_D3DEffect.SetValue(m_nearPlane, MyRenderCamera.NEAR_PLANE_DISTANCE);
            }                   

            m_fogDistanceNear = m_D3DEffect.GetParameter(null, "FogDistanceNear");
            m_fogDistanceFar = m_D3DEffect.GetParameter(null, "FogDistanceFar");
            m_fogColor = m_D3DEffect.GetParameter(null, "FogColor");
            m_fogMultiplier = m_D3DEffect.GetParameter(null, "FogMultiplier");
            m_fogBacklightMultiplier = m_D3DEffect.GetParameter(null, "FogBacklightMultiplier");
        }

        public virtual void Dispose()
        {
            m_D3DEffect.Dispose();
        }

        public virtual void SetTextureNormal(Texture texture2D) { } 
        public virtual void SetTextureDiffuse(Texture texture2D) { }

        public virtual bool IsTextureNormalSet() { return true; }
        public virtual bool IsTextureDiffuseSet() { return true; }

        public virtual void SetDiffuseColor(VRageMath.Vector3 diffuseColor) { }

        public virtual void SetEmissivity(float emissivity) { }
        public virtual void SetEmissivityOffset(float emissivityOffset) { }
        public virtual void SetEmissivityUVAnim(Vector2 uvAnim) { }
        public virtual void SetDiffuseUVAnim(Vector2 uvAnim) { }


        public virtual void SetSpecularPower(float specularPower) { }
        public virtual void SetSpecularIntensity(float specularIntensity) { }

        public virtual void SetProjectionMatrix(ref Matrix projectionMatrix) { }
        public virtual void SetViewMatrix(ref Matrix matrix) { }

        bool begin = false;
        public virtual void Begin(int pass = 0, FX fx = FX.None)
        {
            System.Diagnostics.Debug.Assert(begin == false);
            m_D3DEffect.Begin(fx);
            m_D3DEffect.BeginPass(pass);
            begin = true;
        }

        public virtual void End()
        {
            System.Diagnostics.Debug.Assert(begin == true);
            m_D3DEffect.EndPass();
            m_D3DEffect.End();
            begin = false;
        }


        public void SetNearPlane(float near)
        {
            m_D3DEffect.SetValue(m_nearPlane, near);
        }

        public void SetFarPlane(float far)
        {
            m_D3DEffect.SetValue(m_farPlane, far);
        }

        public void SetFogDistanceNear(float fogDistanceNear)
        {
            m_D3DEffect.SetValue(m_fogDistanceNear, fogDistanceNear);
        }

        public void SetFogDistanceFar(float fogDistanceFar)
        {
            m_D3DEffect.SetValue(m_fogDistanceFar, fogDistanceFar);
        }

        public void SetFogColor(Vector3 fogColor)
        {
            m_D3DEffect.SetValue(m_fogColor, fogColor);
        }

        public void SetFogMultiplier(float fogMultiplier)
        {
            m_D3DEffect.SetValue(m_fogMultiplier, fogMultiplier);
        }

        public void SetFogBacklightMultiplier(float multiplier)
        {
            m_D3DEffect.SetValue(m_fogBacklightMultiplier, multiplier);
        }

        public Effect D3DEffect
        {
            get { return m_D3DEffect; }
        }
    }
}
