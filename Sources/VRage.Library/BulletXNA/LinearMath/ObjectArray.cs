/*
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
 * Bullet Continuous Collision Detection and Physics Library
 * Copyright (c) 2003-2008 Erwin Coumans  http://www.bulletphysics.com/
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose, 
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;

namespace BulletXNA.LinearMath
{
    //This is an implementation of the ObjectArray class which attempts to grow itself to a certain size if it's indexed out of bounds?
    public class ObjectArray<T>
        where T : new()
    {
        // Fields
        private const int _defaultCapacity = 4;
        private static T[] _emptyArray;
        private T[] _items;
        private int _size;
        private int _version;

        // Methods
        static ObjectArray()
        {
            ObjectArray<T>._emptyArray = new T[0];
        }

        public ObjectArray()
        {
            this._items = ObjectArray<T>._emptyArray;
        }

        public T[] GetRawArray()
        {
            return _items;
        }

        public ObjectArray(int capacity)
        {
            if (capacity < 0)
            {
                throw new Exception("ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_SmallCapacity");
            }
            this._items = new T[capacity];
        }

        public void Add(T item)
        {
            if (this._size == this._items.Length)
            {
                this.EnsureCapacity(this._size + 1);
            }
            this._items[this._size++] = item;
            this._version++;
        }

        public void Swap(int index0, int index1)
        {
            T temp = _items[index0];
            _items[index0] = _items[index1];
            _items[index1] = temp;
        }

        public void Resize(int newsize)
        {
            Resize(newsize, true);
        }

        public void Resize(int newsize, bool allocate)
        {
            int curSize = Count;

            if (newsize < curSize)
            {
                if (allocate)
                {
                    for (int i = newsize; i < curSize; i++)
                    {
                        this._items[i] = new T();
                    }
                }
                else
                {
                    for (int i = newsize; i < curSize; i++)
                    {
                        this._items[i] = default(T);
                    }
                }
            }
            else
            {
                if (newsize > Count)
                {
                    Capacity = newsize;
                }
                if (allocate)
                {
                    for (int i = curSize; i < newsize; i++)
                    {
                        this._items[i] = new T();
                    }
                }

            }

            this._size = newsize;
        }

        public void Clear()
        {
            if (this._size > 0)
            {
                Array.Clear(this._items, 0, this._size);
                this._size = 0;
            }
            this._version++;
        }

        private void EnsureCapacity(int min)
        {
            if (this._items.Length < min)
            {
                int num = (this._items.Length == 0) ? 4 : (this._items.Length * 2);
                if (num < min)
                {
                    num = min;
                }
                this.Capacity = num;
            }
        }

        public int Capacity
        {
            get
            {
                return this._items.Length;
            }
            set
            {
                if (value != this._items.Length)
                {
                    if (value < this._size)
                    {
                        throw new Exception("ExceptionResource ArgumentOutOfRange_SmallCapacity");
                    }
                    if (value > 0)
                    {
                        T[] destinationArray = new T[value];
                        if (this._size > 0)
                        {
                            Array.Copy(this._items, 0, destinationArray, 0, this._size);
                        }
                        this._items = destinationArray;
                    }
                    else
                    {
                        this._items = ObjectArray<T>._emptyArray;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                return this._size;
            }
        }

        public T this[int index]
        {
            get
            {
                //checkAndGrow(index);
                int diff = index + 1 - _size;
                for (int i = 0; i < diff; ++i)
                {
                    Add(new T());
                }

                if (index >= this._size)
                {
                    throw new Exception("ThrowHelper.ThrowArgumentOutOfRangeException()");
                }
                return this._items[index];
            }
            set
            {
                int diff = index + 1 - _size;
                for (int i = 0; i < diff; ++i)
                {
                    Add(new T());
                }

                //checkAndGrow(index);
                if (index >= this._size)
                {
                    throw new Exception("ThrowHelper.ThrowArgumentOutOfRangeException()");
                }
                this._items[index] = value;
                this._version++;
            }
        }
    }
}
