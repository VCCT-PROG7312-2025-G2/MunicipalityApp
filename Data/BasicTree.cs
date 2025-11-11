using System.Collections.Generic;

namespace MunicipalityApp.Data
{
    public sealed class BasicTreeNode<TKey, TValue>
    {
        public TKey Key { get; }
        public List<TValue> Values { get; } = new();
        public List<BasicTreeNode<TKey, TValue>> Children { get; } = new();

        public BasicTreeNode(TKey key) => Key = key;

        public BasicTreeNode<TKey, TValue> AddChild(TKey key)
        {
            var n = new BasicTreeNode<TKey, TValue>(key);
            Children.Add(n);
            return n;
        }
    }
}
