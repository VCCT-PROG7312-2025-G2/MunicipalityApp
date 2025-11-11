using System;
using System.Collections.Generic;

namespace MunicipalityApp.Data
{
    public sealed class Bst<TKey, TValue> where TKey : IComparable<TKey>
    {
        private sealed class Node
        {
            public TKey Key;
            public List<TValue> Values = new();
            public Node? Left, Right;
            public Node(TKey key, TValue value) { Key = key; Values.Add(value); }
        }

        private Node? _root;

        public void Insert(TKey key, TValue value)
        {
            if (_root == null) { _root = new Node(key, value); return; }
            var cur = _root;
            while (true)
            {
                int cmp = key.CompareTo(cur.Key);
                if (cmp < 0)
                {
                    if (cur.Left == null) { cur.Left = new Node(key, value); return; }
                    cur = cur.Left;
                }
                else if (cmp > 0)
                {
                    if (cur.Right == null) { cur.Right = new Node(key, value); return; }
                    cur = cur.Right;
                }
                else { cur.Values.Add(value); return; }
            }
        }

        public IEnumerable<TValue> Range(TKey? min, TKey? max)
        {
            var stack = new Stack<Node?>();
            var cur = _root;
            while (stack.Count > 0 || cur != null)
            {
                while (cur != null) { stack.Push(cur); cur = cur.Left; }
                cur = stack.Pop();
                bool geMin = min == null || cur.Key.CompareTo(min) >= 0;
                bool leMax = max == null || cur.Key.CompareTo(max) <= 0;
                if (geMin && leMax) foreach (var v in cur.Values) yield return v;
                if (max != null && cur.Key.CompareTo(max) > 0) break;
                cur = cur.Right;
            }
        }
    }
}
