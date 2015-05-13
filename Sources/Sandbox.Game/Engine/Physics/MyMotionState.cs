//using System.Diagnostics;
//using Sandbox.Engine.Physics;
//using VRageMath;

//using Sandbox.Game.Entities;
//using Sandbox.Engine.Utils;
//using VRage.Utils;
//using System.Linq;
//using System.Collections.Generic;

//using VRageRender;

//namespace Sandbox.Engine.Physics
//{
//    internal class MyMotionState 
//    {
//        public MyPhysicsBody Body;
//        Matrix m_matrix;

//        public bool FireOnOnMotion = true;

//        public MyMotionState(Matrix matrix)
//        {
//            m_matrix = matrix;
//        }

//        public Matrix WorldTransform 
//        {
//            get
//            {
//                return m_matrix;
//            }
//            set
//            {
//                //m_rigidBody.WorldTransform 
//                if (Body != null && FireOnOnMotion)
//                {
//                    Body.OnMotion(Body.RigidBody, 0);
//                }
//                m_matrix = value;
//            }
//        }

//        public void GetWorldTransform(out Matrix matrix)
//        {
//            matrix = m_matrix;
//        }
//    }
//}