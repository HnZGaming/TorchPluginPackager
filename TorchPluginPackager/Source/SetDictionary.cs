using System.Collections.Generic;
using System.Linq;

namespace TorchPluginPackager
{
    public sealed class SetDictionary<K, V>
    {
        readonly Dictionary<K, HashSet<V>> _self;

        public SetDictionary()
        {
            _self = new Dictionary<K, HashSet<V>>();
        }

        public IEnumerable<V> GetValues(K key)
        {
            if (_self.TryGetValue(key, out var values))
            {
                return values;
            }

            return Enumerable.Empty<V>();
        }

        public bool Contains(K key, V value)
        {
            return _self.TryGetValue(key, out var values) && values.Contains(value);
        }

        public bool Add(K key, V value)
        {
            if (!_self.TryGetValue(key, out var values))
            {
                values = new HashSet<V>();
                values.Add(value);
                _self.Add(key, values);
                return true;
            }

            return values.Add(value);
        }

        public void Clear(K key)
        {
            if (_self.TryGetValue(key, out var values))
            {
                values.Clear();
            }
        }
    }
}