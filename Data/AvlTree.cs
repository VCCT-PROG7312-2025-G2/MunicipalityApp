using System;
using System.Collections.Generic;

namespace MunicipalityApp.Data
{
    public sealed class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private sealed class Node
        {
            public TKey Key;
            public List<TValue> Values = new();
            public Node? Left, Right;
            public int Height = 1;
            public Node(TKey key, TValue value) { Key = key; Values.Add(value); }
        }

        private Node? _root;

        public void Insert(TKey key, TValue value) => _root = Insert(_root, key, value);

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

        // --- internals ---
        private static int H(Node? n) => n?.Height ?? 0;
        private static void Update(Node n) => n.Height = Math.Max(H(n.Left), H(n.Right)) + 1;
        private static int Bal(Node n) => H(n.Left) - H(n.Right);

        private static Node RotR(Node y)
        {
            var x = y.Left!;
            var t2 = x.Right;
            x.Right = y; y.Left = t2;
            Update(y); Update(x); return x;
        }
        private static Node RotL(Node x)
        {
            var y = x.Right!;
            var t2 = y.Left;
            y.Left = x; x.Right = t2;
            Update(x); Update(y); return y;
        }

        private static Node Insert(Node? n, TKey key, TValue value)
        {
            if (n == null) return new Node(key, value);

            int c = key.CompareTo(n.Key);
            if (c < 0) n.Left = Insert(n.Left, key, value);
            else if (c > 0) n.Right = Insert(n.Right, key, value);
            else { n.Values.Add(value); return n; }

            Update(n);
            int b = Bal(n);

            if (b > 1 && key.CompareTo(n.Left!.Key) < 0) return RotR(n);
            if (b < -1 && key.CompareTo(n.Right!.Key) > 0) return RotL(n);
            if (b > 1 && key.CompareTo(n.Left!.Key) > 0) { n.Left = RotL(n.Left!); return RotR(n); }
            if (b < -1 && key.CompareTo(n.Right!.Key) < 0) { n.Right = RotR(n.Right!); return RotL(n); }

            return n;
        }
    }
}
