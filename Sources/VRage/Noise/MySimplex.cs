using VRageMath;

namespace VRage.Noise
{
    public class MySimplex : IMyModule
    {
        private int m_seed;

        private byte[] m_perm = new byte[512];

        private static double Grad(int hash, double x)
        {
            var h = hash & 15;
            var grad = 1.0 + (h & 7);

            if ((h & 8) != 0) grad = -grad;

            return grad * x;
        }

        private static double Grad(int hash, double x, double y)
        {
            // Convert low 3 bits of hash code into 8 simple gradient directions, and compute the dot product with (x,y).
            var h = hash & 7;
            var u = h < 4 ? x : y;
            var v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0 * v : 2.0 * v);
        }

        private static double Grad(int hash, double x, double y, double z)
        {
            // Convert low 4 bits of hash code into 12 simple gradient directions, and compute dot product.
            var h = hash & 0xF;
            var u = h < 8 ? x : y;
            var v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            return ((h & 0x1) != 0 ? u : -u) + ((h & 0x2) != 0 ? v : -v);
        }

        public int Seed
        {
            get { return m_seed; }
            set
            {
                m_seed = value;

                var rnd = new MyRNG(m_seed);

                for (int i = 0; i < 256; ++i)
                { 
                    m_perm[i] = (byte)rnd.NextIntRange(0f, 255f);
                    m_perm[256 + i] = m_perm[i];
                }
            }
        }

        public double Frequency { get; set; }

        public MySimplex(int seed = 1, double frequency = 1.0)
        {
            Seed      = seed;
            Frequency = frequency;
        }

        public double GetValue(double x)
        {
            x *= Frequency;

            int i0 = MathHelper.Floor(x);
            var x0 = x  - i0;
            var x1 = x0 - 1.0;

            double n0, n1;

            var t0 = 1.0 - x0*x0;
            var t1 = 1.0 - x1*x1;

            t0 *= t0;
            t1 *= t1;

            n0 = t0*t0 * Grad(m_perm[ i0      & 0xFF], x0);
            n1 = t1*t1 * Grad(m_perm[(i0 + 1) & 0xFF], x1);

            return 0.395*(n0 + n1);
        }

        public double GetValue(double x, double y)
        {
            const double SKEW   = 0.3660254037844386; // ( sqrt(3) - 1 ) / 2
            const double UNSKEW = 0.2113248654051871; // ( 3 - sqrt(3) ) / 6

            x *= Frequency;
            y *= Frequency;

            var s = (x + y)*SKEW;
            int i = MathHelper.Floor(x + s);
            int j = MathHelper.Floor(y + s);

            var t  = (i + j)*UNSKEW;
            var x0 = x - i + t;
            var y0 = y - j + t;

            int i1, j1;

            if   (x0 > y0) { i1 = 1; j1 = 0; }
            else           { i1 = 0; j1 = 1; }

            var x1 = x0 - i1  + UNSKEW;
            var y1 = y0 - j1  + UNSKEW;
            var x2 = x0 - 1.0 + UNSKEW + UNSKEW;
            var y2 = y0 - 1.0 + UNSKEW + UNSKEW;

            int ii = i & 0xFF;
            int jj = j & 0xFF;

            var t0 = 0.5 - x0*x0 - y0*y0;
            var t1 = 0.5 - x1*x1 - y1*y1;
            var t2 = 0.5 - x2*x2 - y2*y2;

            double n0, n1, n2;

            if (t0 < 0.0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0  = t0*t0 * Grad(m_perm[ii + m_perm[jj]], x0, y0);
            }

            if (t1 < 0.0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1  = t1*t1 * Grad(m_perm[ii + i1 + m_perm[jj + j1]], x1, y1);
            }

            if (t2 < 0.0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2  = t2*t2 * Grad(m_perm[ii + 1 + m_perm[jj + 1]], x2, y2);
            }

            return 40.0*(n0 + n1 + n2);
        }

        public double GetValue(double x, double y, double z)
        {
            // Skewing and unskewing factors
            const double SKEW   = 0.3333333333333333; // 1 / 3
            const double UNSKEW = 0.1666666666666667; // 1 / 6

            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            var s = (x + y + z)*SKEW;
            int i = MathHelper.Floor(x + s);
            int j = MathHelper.Floor(y + s);
            int k = MathHelper.Floor(z + s);

            var t  = (i + j + k)*UNSKEW;
            var x0 = x - i + t;
            var y0 = y - j + t;
            var z0 = z - k + t;

            int i1, j1, k1;
            int i2, j2, k2;

            if (x0 >= y0)
            {
                if      (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
                else               { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
            }
            else
            {
                if      (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
                else              { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
            }

            var x1 = x0 - i1  + UNSKEW;
            var y1 = y0 - j1  + UNSKEW;
            var z1 = z0 - k1  + UNSKEW;
            var x2 = x0 - i2  + UNSKEW + UNSKEW;
            var y2 = y0 - j2  + UNSKEW + UNSKEW;
            var z2 = z0 - k2  + UNSKEW + UNSKEW;
            var x3 = x0 - 1.0 + UNSKEW + UNSKEW + UNSKEW;
            var y3 = y0 - 1.0 + UNSKEW + UNSKEW + UNSKEW;
            var z3 = z0 - 1.0 + UNSKEW + UNSKEW + UNSKEW;

            int ii = i & 0xFF;
            int jj = j & 0xFF;
            int kk = k & 0xFF;

            var t0 = 0.6 - x0*x0 - y0*y0 - z0*z0;
            var t1 = 0.6 - x1*x1 - y1*y1 - z1*z1;
            var t2 = 0.6 - x2*x2 - y2*y2 - z2*z2;
            var t3 = 0.6 - x3*x3 - y3*y3 - z3*z3;

            double n0, n1, n2, n3;

            if (t0 < 0.0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0  = t0*t0 * Grad(m_perm[ii + m_perm[jj + m_perm[kk]]], x0, y0, z0);
            }

            if (t1 < 0.0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1  = t1*t1 * Grad(m_perm[ii + i1 + m_perm[jj + j1 + m_perm[kk + k1]]], x1, y1, z1);
            }

            if (t2 < 0.0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2  = t2*t2 * Grad(m_perm[ii + i2 + m_perm[jj + j2 + m_perm[kk + k2]]], x2, y2, z2);
            }

            if (t3 < 0.0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3  = t3*t3 * Grad(m_perm[ii +  1 + m_perm[jj +  1 + m_perm[kk + 1]]], x3, y3, z3);
            }

            return 32.0*(n0 + n1 + n2 + n3);
        }
    }
}
