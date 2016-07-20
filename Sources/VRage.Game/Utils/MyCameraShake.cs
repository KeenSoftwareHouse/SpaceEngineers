using System;
using VRage.Utils;
using VRageMath;

namespace VRage.Game.Utils
{
    public class MyCameraShake
    {
        public MyCameraShake()
        {
            m_shakeEnabled = false;
            m_currentShakeDirPower = 0.0f;
            m_currentShakePosPower = 0.0f;
        }

        //m_MaxShake - tahle konstanta definuje kdy je shake = 1 tedy prevadi hodnoty realne ciselne do cisla se kterym pak pracuju
        //m_MaxShakePos - tohle je maximalni cislo o ktere se muzu posunovat pri shaku z,y,z
        //m_MaxShakeDir - tohle je maximalni cislo o ktere muzu zmenit dir x, z
        //m_Damping - tohle cislo, ktere utlumuje ten shake. Tzn to co hledas na zastaveni shaku. Snizuje to aktualni mozny shake. Je to value* m_Damping vkazdem snimku
        //m_OffConstant - pri tomhle ciste se shake vypne
        //m_DirReduction - o tohle se snizi power pro zmenu diru, protoze ta je potreba byt mensi, jinak kamera strasne moc lita
        //m_Reduction - tohle je redukce pro random nasobic, aby to nebylo prilis zbesile

        public float MaxShake = 15.0f;      //this constant defines when shake = 1, converts real values into values we work with
        public float MaxShakePosX = 1.0f;    //maximal shake pos diference 
        public float MaxShakePosY = 1.0f;//maximal shake pos diference 
        public float MaxShakePosZ = 1.0f;//maximal shake pos diference 
        public float MaxShakeDir = 0.2f;    //maximal shake dir diference x, z
        public float Reduction = 0.2f;      //reduction for random multiplier
        public float Dampening = 0.95f;       //shake speed reduction. by wich we multiply every tick
        public float OffConstant = 0.01f;   //reaching this constant shake turns off
        public float DirReduction = 0.35f;  //this is reduction for random multiplier

        private bool m_shakeEnabled = false;
        private Vector3 m_shakePos;
        private Vector3 m_shakeDir;
        private float m_currentShakePosPower;
        private float m_currentShakeDirPower;

        public bool ShakeEnabled { get { return m_shakeEnabled; } set { m_shakeEnabled = value; } }
        public Vector3 ShakePos { get { return m_shakePos; } }
        public Vector3 ShakeDir { get { return m_shakeDir; } }

        public bool ShakeActive() { return m_shakeEnabled; }

        public void AddShake(float shakePower)
        {
            if (MyUtils.IsZero(shakePower))
                return;

            if (MyUtils.IsZero(MaxShake))
                return;
            
            float pow = (shakePower / MaxShake);
            //MySandboxGame.Log.WriteLine(pow.ToString());
            
            if(m_currentShakePosPower < pow)
                m_currentShakePosPower = pow;
            if (m_currentShakeDirPower < pow * DirReduction)
                m_currentShakeDirPower = pow * DirReduction;            

            m_shakePos = new Vector3(m_currentShakePosPower * MaxShakePosX, m_currentShakePosPower * MaxShakePosY, m_currentShakePosPower * MaxShakePosZ);

            m_shakeDir = new Vector3(m_currentShakeDirPower * MaxShakeDir, 0.0f, m_currentShakeDirPower * MaxShakeDir);

            m_shakeEnabled = true;
        }

        public void UpdateShake(float timeStep, out Vector3 outPos, out Vector3 outDir)
        {
            if (!m_shakeEnabled)
            {
                outPos = Vector3.Zero;
                outDir = Vector3.Zero;
                return;
            }

            // camera shake (dir,up,pos)            
            m_shakePos.X *= MyUtils.GetRandomSign();
            m_shakePos.Y *= MyUtils.GetRandomSign();
            m_shakePos.Z *= MyUtils.GetRandomSign();

            outPos.X = m_shakePos.X * (Math.Abs(m_shakePos.X)) * Reduction;
            outPos.Y = m_shakePos.Y * (Math.Abs(m_shakePos.Y)) * Reduction;
            outPos.Z = m_shakePos.Z * (Math.Abs(m_shakePos.Z)) * Reduction;

            m_shakeDir.X *= MyUtils.GetRandomSign();
            m_shakeDir.Y *= MyUtils.GetRandomSign();
            m_shakeDir.Z *= MyUtils.GetRandomSign();

            outDir.X = m_shakeDir.X * (Math.Abs(m_shakeDir.X)) * 100;
            outDir.Y = m_shakeDir.Y * (Math.Abs(m_shakeDir.Y)) * 100;
            outDir.Z = m_shakeDir.Z * (Math.Abs(m_shakeDir.Z)) * 100;

            outDir *= Reduction;

            m_currentShakePosPower *= (float) Math.Pow(Dampening, timeStep * 60.0f);
            m_currentShakeDirPower *= (float) Math.Pow(Dampening, timeStep * 60.0f);

            if (m_currentShakeDirPower < 0.0f)
            {
                m_currentShakeDirPower = 0.0f;
            }

            if (m_currentShakePosPower < 0.0f)
            {
                m_currentShakePosPower = 0.0f;
            }

            m_shakePos = new Vector3(m_currentShakePosPower * MaxShakePosX, m_currentShakePosPower * MaxShakePosY, m_currentShakePosPower * MaxShakePosZ);
            m_shakeDir = new Vector3(m_currentShakeDirPower * MaxShakeDir, 0.0f, m_currentShakeDirPower * MaxShakeDir);

            if (m_currentShakeDirPower < OffConstant && m_currentShakePosPower < OffConstant)
            {
                m_currentShakeDirPower = 0.0f;
                m_currentShakePosPower = 0.0f;
                m_shakeEnabled = false;
            }
        }
    }
}
