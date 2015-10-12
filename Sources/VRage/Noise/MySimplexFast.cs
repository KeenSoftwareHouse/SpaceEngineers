using VRageMath;

namespace VRage.Noise
{
    public class MySimplexFast : MyModuleFast
    {
        private static Grad[] grad3 = { new Grad(1,1,0), new Grad(-1, 1,0), new Grad(1,-1, 0), new Grad(-1,-1, 0),
                                        new Grad(1,0,1), new Grad(-1, 0,1), new Grad(1, 0,-1), new Grad(-1, 0,-1),
                                        new Grad(0,1,1), new Grad( 0,-1,1), new Grad(0, 1,-1), new Grad( 0,-1,-1) };

        private int m_seedSimplex;

        private byte[] m_permSimplex = new byte[512];
        private byte[] m_gradSimplex = new byte[512];

        public override int Seed
        {
            get { return m_seedSimplex; }
            set
            {
                m_seedSimplex = value;

                var rnd = new MyRNG(m_seedSimplex);

                for (int i = 0; i < 256; i++)
                {
                    m_permSimplex[i] = (byte)rnd.NextIntRange(0f, 255f);
                    m_permSimplex[256 + i] = m_permSimplex[i];
                    m_gradSimplex[i] = (byte)(m_permSimplex[i] % 12);
                    m_gradSimplex[256 + i] = m_gradSimplex[i];
                }
            }
        }

        public double Frequency { get; set; }

        public MySimplexFast(int seed = 1, double frequency = 1.0)
        {
            Seed      = seed;
            Frequency = frequency;
        }

        public override double GetValue(double x)
        {
            x *= Frequency;

            int i0 = MathHelper.Floor(x);
            var x0 = x - i0;
            var x1 = x0 - 1.0;

            double n0, n1;

            var t0 = 1.0 - x0*x0;
            var t1 = 1.0 - x1*x1;

            t0 *= t0;
            t1 *= t1;

            n0 = t0*t0 * Dot(grad3[m_gradSimplex[ i0      & 0xFF]], x0);
            n1 = t1*t1 * Dot(grad3[m_gradSimplex[(i0 + 1) & 0xFF]], x1);

            // The maximum value of this noise is 8*(3/4)^4 = 2.53125
            // A factor of 0.395 scales to fit exactly within [-1,1]
            return 0.395*(n0 + n1);
        }

        public override double GetValue(double x, double y)
        {
            const double SKEW   = 0.3660254037844386; // ( sqrt(3) - 1 ) / 2
            const double UNSKEW = 0.2113248654051871; // ( 3 - sqrt(3) ) / 6

            x *= Frequency;
            y *= Frequency;

            var s = (x + y)*SKEW; // Hairy factor for 2D
            int i = MathHelper.Floor(x + s);
            int j = MathHelper.Floor(y + s);

            var t  = (i + j)*UNSKEW;
            var x0 = x - i + t;
            var y0 = y - j + t;

            int i1, j1;

            if   (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else           { i1 = 0; j1 = 1; } // upper triangle, YX order: (0,0)->(0,1)->(1,1)

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
                n0  = t0*t0 * Dot(grad3[m_gradSimplex[(ii + m_permSimplex[jj]) & 0xFF]], x0, y0);
            }

            if (t1 < 0.0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1  = t1*t1 * Dot(grad3[m_gradSimplex[(ii + i1 + m_permSimplex[(jj + j1) & 0xFF]) & 0xFF]], x1, y1);
            }

            if (t2 < 0.0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2  = t2*t2 * Dot(grad3[m_gradSimplex[(ii + 1 + m_permSimplex[(jj + 1) & 0xFF]) & 0xFF]], x2, y2);
            }

            return 70.0*(n0 + n1 + n2);
        }

        public override double GetValue(double x, double y, double z)
        {
            // Skewing and unskewing factors
            const double SKEW   = 0.3333333333333333; // 1 / 3
            const double UNSKEW = 0.1666666666666667; // 1 / 6

            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            // Skew the input space to determine which simplex cell we're in
            var s = (x + y + z)*SKEW; // Very nice and simple skew factor for 3D
            int i = MathHelper.Floor(x + s);
            int j = MathHelper.Floor(y + s);
            int k = MathHelper.Floor(z + s);

            var t = (i + j + k)*UNSKEW;

            // Unskew the cell origin back to (x,y,z) space
            var x0 = x - i + t; // The x,y,z distances from the cell origin
            var y0 = y - j + t;
            var z0 = z - k + t;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

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

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where c = 1/6.
            var x1 = x0 - i1  + UNSKEW; // Offsets for second corner in (x,y,z) coords
            var y1 = y0 - j1  + UNSKEW;
            var z1 = z0 - k1  + UNSKEW;
            var x2 = x0 - i2  + UNSKEW*2.0; // Offsets for third corner in (x,y,z) coords
            var y2 = y0 - j2  + UNSKEW*2.0;
            var z2 = z0 - k2  + UNSKEW*2.0;
            var x3 = x0 - 1.0 + UNSKEW*3.0; // Offsets for last corner in (x,y,z) coords
            var y3 = y0 - 1.0 + UNSKEW*3.0;
            var z3 = z0 - 1.0 + UNSKEW*3.0;

            // Work out the hashed gradient indices of the four simplex corners
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            // Calculate the contribution from the four corners
            var t0 = 0.6 - x0*x0 - y0*y0 - z0*z0;
            var t1 = 0.6 - x1*x1 - y1*y1 - z1*z1;
            var t2 = 0.6 - x2*x2 - y2*y2 - z2*z2;
            var t3 = 0.6 - x3*x3 - y3*y3 - z3*z3;

            double n0, n1, n2, n3;

            if (t0 < 0.0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0  = t0*t0 * Dot(grad3[m_gradSimplex[ii + m_permSimplex[jj + m_permSimplex[kk]]]], x0, y0, z0);
            }

            if (t1 < 0.0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1  = t1*t1 * Dot(grad3[m_gradSimplex[ii + i1 + m_permSimplex[jj + j1 + m_permSimplex[kk + k1]]]], x1, y1, z1);
            }

            if (t2 < 0.0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2  = t2*t2 * Dot(grad3[m_gradSimplex[ii + i2 + m_permSimplex[jj + j2 + m_permSimplex[kk + k2]]]], x2, y2, z2);
            }

            if (t3 < 0.0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3  = t3*t3 * Dot(grad3[m_gradSimplex[ii + 1 + m_permSimplex[jj + 1 + m_permSimplex[kk + 1]]]], x3, y3, z3);
            }

            // Add contributions from each corner to get the final noise value.
            return 32.0*(n0 + n1 + n2 + n3);
        }

        // Inner class to speed up gradient computations (array access is slower than member access)
        private class Grad
        {
            public double x, y, z;

            public Grad(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        private double Dot(Grad g, double x)
        {
            return g.x*x;
        }

        private double Dot(Grad g, double x, double y)
        {
            return g.x*x + g.y*y;
        }

        private double Dot(Grad g, double x, double y, double z)
        {
            return g.x*x + g.y*y + g.z*z;
        }
    }
}
