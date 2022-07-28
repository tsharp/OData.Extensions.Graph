using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OData.Extensions.Graph.Core
{
    internal class SafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> data = new Dictionary<TKey, TValue>();

        public TValue this[TKey key] {
            get => data[key];
            set {
                if (data.ContainsKey(key))
                {
                    data[key] = value;
                }
                else
                {
                    data.Add(key, value);
                }
            } 
        }

        public ICollection<TKey> Keys => data.Keys;

        public ICollection<TValue> Values => data.Values;

        public int Count => data.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) 
            => this[key] = value;

        public void Add(KeyValuePair<TKey, TValue> item) 
            => this[item.Key] = item.Value;

        public void Clear() => data.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) 
            => data[item.Key].Equals(item.Value);

        public bool ContainsKey(TKey key) => data.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) 
            => throw new NotImplementedException();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => data.GetEnumerator();

        public bool Remove(TKey key)
            => data.Remove(key);

        public bool Remove(KeyValuePair<TKey, TValue> item)
            => data.Remove(item.Key);
            
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            => data.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => data.GetEnumerator();
    }
}
