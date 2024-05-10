using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysProg1
{
    internal class Cache <K,T>
    {
        private object _cacheLock = new object();
        private IDictionary<K, T> cache;

        public Cache()
        {
            this.cache = new Dictionary<K, T>();
        }

        public bool TryGetValue(K key, out T value)
        {
            bool cacheHit = false;
            lock (_cacheLock)
            {
                cacheHit = cache.TryGetValue(key, out value);
            }
            return cacheHit;
        }

        public void Add(K key, T value)
        {
            lock (_cacheLock)
            {
                if (cache.ContainsKey(key))
                    return;
                cache.Add(key, value);
            }
        }
    }
}
