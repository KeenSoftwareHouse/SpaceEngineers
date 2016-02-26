using System;
using System.Collections.Generic;
using VRageMath;

namespace VRage.Game
{
    class MyMeshHelper
    {
        private static readonly int C_BUFFER_CAPACITY = 5000;
        private static List<Vector3D> m_tmpVectorBuffer = new List<Vector3D>(C_BUFFER_CAPACITY);
        /// <summary>
        /// GenerateSphere
        /// </summary>
        /// <param name="vctPos"></param>
        /// <param name="radius"></param>
        /// <param name="steps"></param>
        public static void GenerateSphere(ref MatrixD worldMatrix, float radius, int steps, List<Vector3D> vertices)
        {
            System.Diagnostics.Debug.Assert(steps * steps < C_BUFFER_CAPACITY);

            m_tmpVectorBuffer.Clear();
            //Now are variables for this is as followed. n is the current vertex we are working
            //with. While a and b are used to control our loops.
            int n = 0;

            //Assign our b loop to go through 90 degrees in intervals of our variable space
            float space = 360 / steps;
            float limitBeta = 90 - space;
            float limitAlpha = 360 - space;
            Vector3D vctTmp;

            //@ generate hafSphere
            for( float beta = 0; beta <= limitBeta; beta += space)
            {
                //Assign our a loop to go through 360 degrees in intervals of our variable space
                for( float alpha = 0; alpha <= limitAlpha; alpha += space)
                {
                    //Start editing our vertex.
                    vctTmp.X = (float)(radius * Math.Sin(MathHelper.ToRadians(alpha)) * Math.Sin(MathHelper.ToRadians(beta)));
                    vctTmp.Y = (float)(radius * Math.Cos(MathHelper.ToRadians(alpha)) * Math.Sin(MathHelper.ToRadians(beta)));
                    vctTmp.Z = (float)(radius * Math.Cos(MathHelper.ToRadians(beta)));
                    m_tmpVectorBuffer.Add(vctTmp);
                    //Then start working with the next vertex
                    n++;

                    //Then we do the same calculations as before, only adding the space variable
                    //to the b values.
                    vctTmp.X = (float)(radius * Math.Sin(MathHelper.ToRadians(alpha)) * Math.Sin(MathHelper.ToRadians(beta + space)));
                    vctTmp.Y = (float)(radius * Math.Cos(MathHelper.ToRadians(alpha)) * Math.Sin(MathHelper.ToRadians(beta + space)));
                    vctTmp.Z = (float)(radius * Math.Cos(MathHelper.ToRadians(beta + space)));
                    m_tmpVectorBuffer.Add(vctTmp);
                    n++;

                    //Then we do the same calculations as the first, only adding the space variable
                    //to the a values.
                    vctTmp.X = (float)(radius * Math.Sin(MathHelper.ToRadians(alpha + space)) * Math.Sin(MathHelper.ToRadians(beta)));
                    vctTmp.Y = (float)(radius * Math.Cos(MathHelper.ToRadians(alpha + space)) * Math.Sin(MathHelper.ToRadians(beta)));
                    vctTmp.Z = (float)(radius * Math.Cos(MathHelper.ToRadians(beta)));
                    m_tmpVectorBuffer.Add(vctTmp);
                    n++;

                    //Then we do the same calculations as the first again, only adding the space variable
                    //to both the b and the a values.
                    vctTmp.X = (float)(radius * Math.Sin(MathHelper.ToRadians(alpha + space)) * Math.Sin(MathHelper.ToRadians(beta + space)));
                    vctTmp.Y = (float)(radius * Math.Cos(MathHelper.ToRadians(alpha + space)) * Math.Sin(MathHelper.ToRadians(beta + space)));
                    vctTmp.Z = (float)(radius * Math.Cos(MathHelper.ToRadians(beta + space)));
                    m_tmpVectorBuffer.Add(vctTmp);
                    n++;
                }
            }

            if (m_tmpVectorBuffer.Count * 2 > vertices.Capacity)
            {
                System.Diagnostics.Debug.Assert(false); //Too mach vertices
                return;
            }

            int offset = m_tmpVectorBuffer.Count;
            foreach (Vector3D vct in m_tmpVectorBuffer)
                vertices.Add(vct);

            //@ coz stupid C#
            foreach (Vector3D vct in m_tmpVectorBuffer)
            {
                Vector3D vctTmpZNeg = new Vector3D(vct.X, vct.Y, -vct.Z);
                vertices.Add(vctTmpZNeg);
            }


            for(int i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = Vector3D.Transform(vertices[i], worldMatrix);
            }
        }
    }
}
