using VRageMath.PackedVector;

using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using SharpDX;


    internal class MyEffectPerlinNoiseBase : MyEffectBase
    {
        EffectHandle m_permTexture;
        EffectHandle m_permTexture2d;
        EffectHandle m_gradTexture;
        EffectHandle m_permGradTexture;
        EffectHandle m_permGrad4dTexture;
        EffectHandle m_gradTexture4d;

        static Texture permTexture;
        static Texture permTexture2d;
        static Texture gradTexture;
        static Texture permGradTexture;
        static Texture permGrad4dTexture;
        static Texture gradTexture4d;

        // permutation table
        static int[] perm = new int[] { 151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        // gradients for 3d noise
        static float[,] g3 =  
        {
            {1,1,0},
            {-1,1,0},
            {1,-1,0},
            {-1,-1,0},
            {1,0,1},
            {-1,0,1},
            {1,0,-1},
            {-1,0,-1}, 
            {0,1,1},
            {0,-1,1},
            {0,1,-1},
            {0,-1,-1},
            {1,1,0},
            {0,-1,1},
            {-1,1,0},
            {0,-1,-1}
        };

        // gradients for 4D noise
        static float[,] g4 = 
        {
	        {0, -1, -1, -1},
	        {0, -1, -1, 1},
	        {0, -1, 1, -1},
	        {0, -1, 1, 1},
	        {0, 1, -1, -1},
	        {0, 1, -1, 1},
	        {0, 1, 1, -1},
	        {0, 1, 1, 1},
	        {-1, -1, 0, -1},
	        {-1, 1, 0, -1},
	        {1, -1, 0, -1},
	        {1, 1, 0, -1},
	        {-1, -1, 0, 1},
	        {-1, 1, 0, 1},
	        {1, -1, 0, 1},
	        {1, 1, 0, 1},
        	
	        {-1, 0, -1, -1},
	        {1, 0, -1, -1},
	        {-1, 0, -1, 1},
	        {1, 0, -1, 1},
	        {-1, 0, 1, -1},
	        {1, 0, 1, -1},
	        {-1, 0, 1, 1},
	        {1, 0, 1, 1},
	        {0, -1, -1, 0},
	        {0, -1, -1, 0},
	        {0, -1, 1, 0},
	        {0, -1, 1, 0},
	        {0, 1, -1, 0},
	        {0, 1, -1, 0},
	        {0, 1, 1, 0},
	        {0, 1, 1, 0}
        };

        private void GeneratePermTexture()
        {
            if (permTexture != null)
                permTexture.Dispose();

            permTexture = new Texture(MyRender.GraphicsDevice, 256, 1, 0, Usage.Dynamic, Format.L8, Pool.Default);
            byte[] data = new byte[256 * 1];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    data[x + (y * 256)] = (byte)(perm[x]);// / 255.0);
                }
            }
            
            SharpDX.DataStream ds;
            DataRectangle dr = permTexture.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            permTexture.UnlockRectangle(0);

        }                     

        // 2d permutation texture for optimized version
        int perm2d(int i)
        {
            return perm[i % 256];
        }

        private void GeneratePermTexture2d()
        {           
            if (permTexture2d != null)
                permTexture2d.Dispose();
            permTexture2d = new Texture(MyRender.GraphicsDevice, 256, 256, 0, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
            Color[] data = new Color[256 * 256];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    int A = perm2d(x) + y;
                    int AA = perm2d(A);
                    int AB = perm2d(A + 1);
                    int B = perm2d(x + 1) + y;
                    int BA = perm2d(B);
                    int BB = perm2d(B + 1);
                    //data[x + (y * 256)] = new Color((byte)(AA / 255.0), (byte)(AB / 255.0),
                    //                                (byte)(BA / 255.0), (byte)(BB / 255.0));
                    
                    //XNA Color = r g b a

                    //need a r g b
                    data[x + (y * 256)] = new Color((byte)(BA), (byte)(AB), (byte)(AA), (byte)(BB));
                }
            }

            SharpDX.DataStream ds;
            DataRectangle dr = permTexture2d.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            permTexture2d.UnlockRectangle(0);

        }

        private void GenerateGradTexture()
        {                                                                                       //NormalizedByte4
            gradTexture = new Texture(MyRender.GraphicsDevice, 16, 1, 0, Usage.Dynamic, Format.Q8W8V8U8, Pool.Default);
            NormalizedByte4[] data = new NormalizedByte4[16 * 1];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    data[x + (y * 16)] = new NormalizedByte4(g3[x, 0], g3[x, 1], g3[x, 2], 1);
                }
            }

            SharpDX.DataStream ds;
            DataRectangle dr = gradTexture.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            gradTexture.UnlockRectangle(0);
        }

        // permuted gradient texture for optimized version
        private void GeneratePermGradTexture()
        {
            if (permGradTexture != null)
                permGradTexture.Dispose();
            //NormalizedByte4
            permGradTexture = new Texture(MyRender.GraphicsDevice, 256, 1, 0, Usage.Dynamic, Format.Q8W8V8U8, Pool.Default);
            
                 
            NormalizedByte4[] data = new NormalizedByte4[256 * 1];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    //data[x + (y * 256)] = new NormalizedByte4(g3[perm[x] % 16, 0], g3[perm[x] % 16, 1], g3[perm[x] % 16, 2], 1);
                    data[x + (y * 256)] = new NormalizedByte4(g3[perm[x] % 16, 0], g3[perm[x] % 16, 1], g3[perm[x] % 16, 2], 1);
                }
            }      
                 
            /*
            byte[] data = new byte[256 * 1 * 4];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    data[(x + (y * 256)) * 4 + 0] = (byte)(255.0f * ((g3[perm[x] % 16, 0] + 1) / 2.0f));
                    data[(x + (y * 256)) * 4 + 1] = (byte)(255.0f * ((g3[perm[x] % 16, 1] + 1) / 2.0f));
                    data[(x + (y * 256)) * 4 + 2] = (byte)(255.0f * ((g3[perm[x] % 16, 2] + 1) / 2.0f));
                    data[(x + (y * 256)) * 4 + 3] = 1;
                }
            } */   
           
            SharpDX.DataStream ds;
            DataRectangle dr = permGradTexture.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            permGradTexture.UnlockRectangle(0);
        }
            
            

        private void GeneratePermGrad4dTexture()
        {                                                                                     //NormalizedByte4
            permGrad4dTexture = new Texture(MyRender.GraphicsDevice, 256, 1, 0, Usage.Dynamic, Format.Q8W8V8U8, Pool.Default);
            NormalizedByte4[] data = new NormalizedByte4[256 * 1];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    data[x + (y * 256)] = new NormalizedByte4(g4[perm[x] % 32, 0], g4[perm[x] % 32, 1], g4[perm[x] % 32, 2], g4[perm[x] % 32, 3]);
                }
            }

            SharpDX.DataStream ds;
            DataRectangle dr = permGrad4dTexture.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            permGrad4dTexture.UnlockRectangle(0);

            
        }

        private void GenerateGradTexture4d()
        {
            if (gradTexture4d != null)
                gradTexture4d.Dispose();

            //NormalizedByte4
            gradTexture4d = new Texture(MyRender.GraphicsDevice, 32, 1, 0, Usage.Dynamic, Format.Q8W8V8U8, Pool.Default);
            NormalizedByte4[] data = new NormalizedByte4[32 * 1];
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    data[x + (y * 32)] = new NormalizedByte4(g4[x, 0], g4[x, 1], g4[x, 2], g4[x, 3]);
                    //data[31 - x + (y * 32)] = new NormalizedByte4(0, 0, 0, 0);
                }
            }

            SharpDX.DataStream ds;
            DataRectangle dr = gradTexture4d.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(data);
            gradTexture4d.UnlockRectangle(0);
                       
        }

        public MyEffectPerlinNoiseBase(Effect xnaEffect)
            : base(xnaEffect)
        {
            Init();
        }

        public MyEffectPerlinNoiseBase(string asset)
            : base(asset)
        {
            Init();
        }


        void DisposeTextures()
        {
            if (permTexture != null)
            {
                permTexture.Dispose();
                permTexture2d.Dispose();
                gradTexture.Dispose();
                permGradTexture.Dispose();
                permGrad4dTexture.Dispose();
                gradTexture4d.Dispose();
            }
        }

        private void Init()
        {
            m_permTexture = m_D3DEffect.GetParameter(null, "permTexture");
            m_permTexture2d = m_D3DEffect.GetParameter(null, "permTexture2d");
            m_gradTexture = m_D3DEffect.GetParameter(null, "gradTexture");
            m_permGradTexture = m_D3DEffect.GetParameter(null, "permGradTexture");
            m_permGrad4dTexture = m_D3DEffect.GetParameter(null, "permGrad4dTexture");
            m_gradTexture4d = m_D3DEffect.GetParameter(null, "gradTexture4d");

            DisposeTextures();

            if (permTexture == null || permTexture.IsDisposed)
            {
                GeneratePermTexture();
                GeneratePermTexture2d();
                GenerateGradTexture();
                GeneratePermGradTexture();
                GeneratePermGrad4dTexture();
                GenerateGradTexture4d();
            }
                     
            m_D3DEffect.SetTexture(m_permTexture, permTexture);
            m_D3DEffect.SetTexture(m_permTexture2d, permTexture2d);
            m_D3DEffect.SetTexture(m_gradTexture,gradTexture);
            m_D3DEffect.SetTexture(m_permGradTexture,permGradTexture);
            m_D3DEffect.SetTexture(m_permGrad4dTexture,permGrad4dTexture);
            m_D3DEffect.SetTexture(m_gradTexture4d, gradTexture4d);
        }

        public void Update()
        {/*
            GeneratePermTexture();
            GeneratePermTexture2d();
            GeneratePermGradTexture();
            GenerateGradTexture4d();

            m_D3DEffect.SetTexture(m_permTexture, permTexture);
            m_D3DEffect.SetTexture(m_permTexture2d, permTexture2d);
            m_D3DEffect.SetTexture(m_gradTexture, gradTexture);
            m_D3DEffect.SetTexture(m_permGradTexture, permGradTexture);
            m_D3DEffect.SetTexture(m_permGrad4dTexture, permGrad4dTexture);
            m_D3DEffect.SetTexture(m_gradTexture4d, gradTexture4d);*/
        }

        public override void Dispose()
        {
            DisposeTextures();

            base.Dispose();
        }
    }
}
