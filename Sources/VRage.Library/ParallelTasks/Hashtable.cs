using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{

	public class HashTableEnumerator<TKey, TData>
	: IEnumerator<KeyValuePair<TKey, TData>>
	{
		int currentIndex = -1;
		Hashtable<TKey, TData> table;

		public HashTableEnumerator(Hashtable<TKey, TData> table)
		{
			this.table = table;
		}

		public KeyValuePair<TKey, TData> Current
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
			HashtableNode<TKey,TData> node;
			do
			{
				currentIndex++;
				if (table.array.Length <= currentIndex)
					return false;

				node = table.array[currentIndex];
			} while (node.Token != HashtableToken.Used);

			Current = new KeyValuePair<TKey, TData>(node.Key, node.Data);
			return true;
		}

		public void Reset()
		{
			currentIndex = -1;
		}
	}

	public enum HashtableToken
	{
		Empty,
		Used,
		Deleted
	}

    public struct HashtableNode<TKey,TData>
    {
        public TKey Key;
        public TData Data;
        public HashtableToken Token;

		public HashtableNode(TKey key, TData data, HashtableToken token)
		{
			Key = key;
			Data = data;
			Token = token;
		}
    }



	public class GetHashCode_HashTable<TKey>
	{
		public static int GetHashCode(TKey v) //where TKey: class
		{
			return v.GetHashCode();
		}
	}

    public class Hashtable<TKey, TData>
        : IEnumerable<KeyValuePair<TKey, TData>>
    {

        // MartinG@DigitalRune: Use EqualityComparer.Equals() instead of object.Equals(). 
        // object.Equals() casts value types to object and can therefore create "garbage".
        private static readonly EqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;

#if UNSHARPER_TMP
		public HashtableNode<TKey, TData>[] array;
#else
        public volatile HashtableNode<TKey,TData>[] array;
#endif
        SpinLock writeLock;

        static readonly HashtableNode<TKey,TData> DeletedNode = new HashtableNode<TKey,TData>( default(TKey), default(TData), HashtableToken.Deleted);

        /// <summary>
        /// Initializes a new instance of the <see cref="Hashtable&lt;Key, Data&gt;"/> class.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the table.</param>
        public Hashtable(int initialCapacity)
        {
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException("initialCapacity", "cannot be < 1");
            array = new HashtableNode<TKey,TData>[initialCapacity];
            writeLock = new SpinLock();
        }

        /// <summary>
        /// Adds an item to this hashtable.
        /// </summary>
        /// <param name="key">The key at which to add the item.</param>
        /// <param name="data">The data to add.</param>
        public void Add(TKey key, TData data)
        {
#if UNSHARPER_TMP
			try
			{
				writeLock.Enter();
				bool inserted = Insert(array, key, data);

				if (!inserted)
				{
					Resize();
					Insert(array, key, data);
				}
				writeLock.Exit();
			}
			catch
			{
				writeLock.Exit();
			}
#else
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
#endif
        }

        private void Resize()
        {
            var newArray = new HashtableNode<TKey,TData>[array.Length * 2];
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];
                if (item.Token == HashtableToken.Used)
                    Insert(newArray, item.Key, item.Data);
            }

            array = newArray;
        }

        private bool Insert(HashtableNode<TKey,TData>[] table, TKey key, TData data)
        {
			var initialHash = Math.Abs(GetHashCode_HashTable<TKey>.GetHashCode(key)) % table.Length;
            var hash = initialHash;
            bool inserted = false;
            do
            {
                var node = table[hash];
                // if node is empty, or marked with a tombstone
                if (node.Token == HashtableToken.Empty || node.Token == HashtableToken.Deleted || KeyComparer.Equals(key, node.Key))
                {
                    table[hash] = new HashtableNode<TKey,TData>()
                    {
                        Key = key,
                        Data = data,
                        Token = HashtableToken.Used
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
        public void UnsafeSet(TKey key, TData value)
        {
            HashtableNode<TKey,TData>[] table;
            bool inserted = false;

            do
            {
                table = array;
				var initialHash = Math.Abs(GetHashCode_HashTable<TKey>.GetHashCode(key)) % table.Length;
                var hash = initialHash;

                do
                {
                    var node = table[hash];
                    if (KeyComparer.Equals(key, node.Key))
                    {
                        table[hash] = new HashtableNode<TKey,TData>()
                        {
                            Key = key,
                            Data = value,
                            Token = HashtableToken.Used
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

        private bool Find(TKey key, out HashtableNode<TKey,TData> node)
        {
            node = new HashtableNode<TKey,TData>();
            var table = array;
			var initialHash = Math.Abs(GetHashCode_HashTable<TKey>.GetHashCode(key)) % table.Length;
            var hash = initialHash;
            
            do
            {
                HashtableNode<TKey,TData> n = table[hash];
                if (n.Token == HashtableToken.Empty)
                    return false;
                if (n.Token == HashtableToken.Deleted || !KeyComparer.Equals(key, n.Key))
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
        public bool TryGet(TKey key, out TData data)
        {
            HashtableNode<TKey,TData> n;
            if (Find(key, out n))
            {
                data = n.Data;
                return true;
            }
            else
            {
                data = default(TData);
                return false;
            }
        }

        /// <summary>
        /// Removes the data at the specified key location.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Remove(TKey key)
        {
            try
            {
                writeLock.Enter();


                HashtableNode<TKey,TData>[] table = array;
				var initialHash = Math.Abs(GetHashCode_HashTable<TKey>.GetHashCode(key)) % table.Length;
                var hash = initialHash;

                do
                {
                    HashtableNode<TKey,TData> n = table[hash];
                    if (n.Token == HashtableToken.Empty)
                        return;
                    if (n.Token == HashtableToken.Deleted || !KeyComparer.Equals(key, n.Key))
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
        public IEnumerator<KeyValuePair<TKey, TData>> GetEnumerator()
        {
            return new HashTableEnumerator<TKey,TData>(this);
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
