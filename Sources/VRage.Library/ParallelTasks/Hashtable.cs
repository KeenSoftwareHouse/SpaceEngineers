using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    /// <summary>
    /// A thread safe hashtable.
    /// </summary>
    /// <typeparam name="Key">The type of item to use as keys.</typeparam>
    /// <typeparam name="Data">The type of data stored.</typeparam>
    public class Hashtable<Key, Data>
        : IEnumerable<KeyValuePair<Key, Data>>
    {
        struct Node
        {
            public Key Key;
            public Data Data;
            public Token Token;
        }

        enum Token
        {
            Empty,
            Used,
            Deleted
        }

        class Enumerator
            : IEnumerator<KeyValuePair<Key, Data>>
        {
            int currentIndex = -1;
            Hashtable<Key, Data> table;

            public Enumerator(Hashtable<Key, Data> table)
            {
                this.table = table;
            }

            public KeyValuePair<Key, Data> Current
            {
                get;
                private set;
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                Node node;
                do
                {
                    currentIndex++;
                    if (table.array.Length <= currentIndex)
                        return false;

                    node = table.array[currentIndex];
                } while (node.Token != Hashtable<Key,Data>.Token.Used);

                Current = new KeyValuePair<Key,Data>(node.Key, node.Data);
                return true;
            }

            public void Reset()
            {
                currentIndex = -1;
            }
        }


        // MartinG@DigitalRune: Use EqualityComparer.Equals() instead of object.Equals(). 
        // object.Equals() casts value types to object and can therefore create "garbage".
        private static readonly EqualityComparer<Key> KeyComparer = EqualityComparer<Key>.Default;

        volatile Node[] array;
        SpinLock writeLock;

        static readonly Node DeletedNode = new Node() { Key = default(Key), Data = default(Data), Token = Token.Deleted };

        /// <summary>
        /// Initializes a new instance of the <see cref="Hashtable&lt;Key, Data&gt;"/> class.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the table.</param>
        public Hashtable(int initialCapacity)
        {
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException("initialCapacity", "cannot be < 1");
            array = new Hashtable<Key, Data>.Node[initialCapacity];
            writeLock = new SpinLock();
        }

        /// <summary>
        /// Adds an item to this hashtable.
        /// </summary>
        /// <param name="key">The key at which to add the item.</param>
        /// <param name="data">The data to add.</param>
        public void Add(Key key, Data data)
        {
            try
            {
                writeLock.Enter();
                bool inserted = Insert(array, key, data);

                if (!inserted)
                {
                    Resize();
                    Insert(array, key, data);
                }
            }
            finally
            {
                writeLock.Exit();
            }
        }

        private void Resize()
        {
            var newArray = new Node[array.Length * 2];
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];
                if (item.Token == Token.Used)
                    Insert(newArray, item.Key, item.Data);
            }

            array = newArray;
        }

        private bool Insert(Node[] table, Key key, Data data)
        {
            var initialHash = Math.Abs(key.GetHashCode()) % table.Length;
            var hash = initialHash;
            bool inserted = false;
            do
            {
                var node = table[hash];
                // if node is empty, or marked with a tombstone
                if (node.Token == Token.Empty || node.Token == Token.Deleted || KeyComparer.Equals(key, node.Key))
                {
                    table[hash] = new Node()
                    {
                        Key = key,
                        Data = data,
                        Token = Token.Used
                    };
                    inserted = true;
                    break;
                }
                else
                    hash = (hash + 1) % table.Length;
            } while (hash != initialHash);

            return inserted;
        }

        /// <summary>
        /// Sets the value of the item at the specified key location.
        /// This is only guaranteed to work correctly if no other thread is modifying the same key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The new value.</param>
        public void UnsafeSet(Key key, Data value)
        {
            Node[] table;
            bool inserted = false;

            do
            {
                table = array;
                var initialHash = Math.Abs(key.GetHashCode()) % table.Length;
                var hash = initialHash;

                do
                {
                    var node = table[hash];
                    if (KeyComparer.Equals(key, node.Key))
                    {
                        table[hash] = new Node()
                        {
                            Key = key,
                            Data = value,
                            Token = Token.Used
                        };
                        inserted = true;
                        break;
                    }
                    else
                        hash = (hash + 1) % table.Length;
                } while (hash != initialHash);
            } while (table != array);

            // MartinG@DigitalRune: I have moved the Add() outside the loop because it uses a 
            // write-lock and the loop above should run without locks.
            if (!inserted)
                Add(key, value);
        }

        private bool Find(Key key, out Node node)
        {
            node = new Hashtable<Key, Data>.Node();
            var table = array;
            var initialHash = Math.Abs(key.GetHashCode()) % table.Length;
            var hash = initialHash;
            
            do
            {
                Node n = table[hash];
                if (n.Token == Token.Empty)
                    return false;
                if (n.Token == Token.Deleted || !KeyComparer.Equals(key, n.Key))
                    hash = (hash + 1) % table.Length;
                else
                {
                    node = n;
                    return true;
                }
            } while (hash != initialHash);

            return false;
        }

        /// <summary>
        /// Tries to get the data at the specified key location.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="data">The data at the key location.</param>
        /// <returns><c>true</c> if the data was found; else <c>false</c>.</returns>
        public bool TryGet(Key key, out Data data)
        {
            Node n;
            if (Find(key, out n))
            {
                data = n.Data;
                return true;
            }
            else
            {
                data = default(Data);
                return false;
            }
        }

        /// <summary>
        /// Removes the data at the specified key location.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Remove(Key key)
        {
            try
            {
                writeLock.Enter();


                Node[] table = array;
                var initialHash = Math.Abs(key.GetHashCode()) % table.Length;
                var hash = initialHash;

                do
                {
                    Node n = table[hash];
                    if (n.Token == Token.Empty)
                        return;
                    if (n.Token == Token.Deleted || !KeyComparer.Equals(key, n.Key))
                        hash = (hash + 1) % table.Length;
                    else
                        table[hash] = DeletedNode;
                } while (hash != initialHash);      // MartinG@DigitalRune: Stop when all entries are checked!
            }
            finally
            {
                writeLock.Exit();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<Key, Data>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
