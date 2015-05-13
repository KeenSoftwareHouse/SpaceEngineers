/*
 * 
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
This source file is part of GIMPACT Library.

For the latest info, see http://gimpact.sourceforge.net/

Copyright (c) 2007 Francisco Leon Najera. C.C. 80087371.
email: projectileman@yahoo.com


This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using BulletXNA.LinearMath;

namespace BulletXNA.BulletCollision
{
    public class PrimitiveTriangle
    {
        public IndexedVector3[] m_vertices = new IndexedVector3[3];
    }
}
