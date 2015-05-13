using System;
using VRageMath;
using VRage.Utils;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Entities
{
    class MyCameraHeadShake
    {
        public MyCameraHeadShake()
        {
            m_Shake = false;
            m_CurrentShakeDirPower = 0.0f;
            m_CurrentShakePosPower = 0.0f;
        }

        //m_MaxShake - tahle konstanta definuje kdy je shake = 1 tedy prevadi hodnoty realne ciselne do cisla se kterym pak pracuju
        //m_MaxShakePos - tohle je maximalni cislo o ktere se muzu posunovat pri shaku z,y,z
        //m_MaxShakeDir - tohle je maximalni cislo o ktere muzu zmenit dir x, z
        //m_Damping - tohle cislo, ktere utlumuje ten shake. Tzn to co hledas na zastaveni shaku. Snizuje to aktualni mozny shake. Je to value* m_Damping vkazdem snimku
        //m_OffConstant - pri tomhle ciste se shake vypne
        //m_DirReduction - o tohle se snizi power pro zmenu diru, protoze ta je potreba byt mensi, jinak kamera strasne moc lita
        //m_Reduction - tohle je redukce pro random nasobic, aby to nebylo prilis zbesile

        public float m_MaxShake = 15.0f;      //this constant defines when shake = 1, converts real values into values we work with
        public float m_MaxShakePosX = 1.0f;    //maximal shake pos diference 
        public float m_MaxShakePosY = 1.0f;//maximal shake pos diference 
        public float m_MaxShakePosZ = 1.0f;//maximal shake pos diference 
        public float m_MaxShakeDir = 0.2f;    //maximal shake dir diference x, z
        public float m_Reduction = 0.2f;      //reduction for random multiplier
        public float m_Damping = 0.95f;       //shake speed reduction. by wich we multiply every tick
        public float m_OffConstant = 0.01f;   //reaching this constant shake turns off
        public float m_DirReduction = 0.35f;  //this is reduction for random multiplier

        private bool m_Shake = false;
        private Vector3 m_ShakePos;
        private Vector3 m_ShakeDir;
        private float m_CurrentShakePosPower;
        private float m_CurrentShakeDirPower;

        public bool Shake { get { return m_Shake; } set { m_Shake = value; } }
        public Vector3 ShakePos { get { return m_ShakePos; } }
        public Vector3 ShakeDir { get { return m_ShakeDir; } }

        public bool ShakeActive() { return m_Shake; }

        public void AddShake(float shakePower)
        {
            if (MyUtils.IsZero(shakePower))
                return;

            if (MyUtils.IsZero(m_MaxShake))
                return;
            
            float pow = (shakePower / m_MaxShake);
            //MySandboxGame.Log.WriteLine(pow.ToString());
            
            if(m_CurrentShakePosPower < pow)
                m_CurrentShakePosPower = pow;
            if (m_CurrentShakeDirPower < pow * m_DirReduction)
                m_CurrentShakeDirPower = pow * m_DirReduction;            

            m_ShakePos = new Vector3(m_CurrentShakePosPower * m_MaxShakePosX, m_CurrentShakePosPower * m_MaxShakePosY, m_CurrentShakePosPower * m_MaxShakePosZ);

            m_ShakeDir = new Vector3(m_CurrentShakeDirPower * m_MaxShakeDir, 0.0f, m_CurrentShakeDirPower * m_MaxShakeDir);

            m_Shake = true;
        }

        public void UpdateShake(float timeStep,ref Vector3 outPos, ref Vector3 outDir)
        {
            if (!m_Shake)
            {
                return;
            }

            // camera shake (dir,up,pos)            
            m_ShakePos.X *= MyUtils.GetRandomSign();
            m_ShakePos.Y *= MyUtils.GetRandomSign();
            m_ShakePos.Z *= MyUtils.GetRandomSign();

            outPos.X += m_ShakePos.X * (Math.Abs(m_ShakePos.X)) * m_Reduction;
            outPos.Y += m_ShakePos.Y * (Math.Abs(m_ShakePos.Y)) * m_Reduction;
            outPos.Z += m_ShakePos.Z * (Math.Abs(m_ShakePos.Z)) * m_Reduction;

            m_ShakeDir.X *= MyUtils.GetRandomSign();
            m_ShakeDir.Y *= MyUtils.GetRandomSign();
            m_ShakeDir.Z *= MyUtils.GetRandomSign();

            outDir.X += m_ShakeDir.X * (Math.Abs(m_ShakeDir.X)) * 100;
            outDir.Y += m_ShakeDir.Y * (Math.Abs(m_ShakeDir.Y)) * 100;
            outDir.Z += m_ShakeDir.Z * (Math.Abs(m_ShakeDir.Z)) * 100;

            outDir *= m_Reduction;

            m_CurrentShakePosPower *= (float) Math.Pow(m_Damping, timeStep * 60.0f);
            m_CurrentShakeDirPower *= (float) Math.Pow(m_Damping, timeStep * 60.0f);

            if (m_CurrentShakeDirPower < 0.0f)
            {
                m_CurrentShakeDirPower = 0.0f;
            }

            if (m_CurrentShakePosPower < 0.0f)
            {
                m_CurrentShakePosPower = 0.0f;
            }

            m_ShakePos = new Vector3(m_CurrentShakePosPower * m_MaxShakePosX, m_CurrentShakePosPower * m_MaxShakePosY, m_CurrentShakePosPower * m_MaxShakePosZ);
            m_ShakeDir = new Vector3(m_CurrentShakeDirPower * m_MaxShakeDir, 0.0f, m_CurrentShakeDirPower * m_MaxShakeDir);

            if (m_CurrentShakeDirPower < m_OffConstant && m_CurrentShakePosPower < m_OffConstant)
            {
                m_CurrentShakeDirPower = 0.0f;
                m_CurrentShakePosPower = 0.0f;
                m_Shake = false;
            }
        }

        
    }
}
