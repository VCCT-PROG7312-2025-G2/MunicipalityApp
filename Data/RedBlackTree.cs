using System;
using System.Collections.Generic;

namespace MunicipalityApp.Data
{
    public sealed class RedBlackTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private enum Color { Red, Black }

        private sealed class Node
        {
            public TKey Key;
            public List<TValue> Values = new();
            public Node? Left, Right, Parent;
            public Color Color;

            public Node(TKey key, TValue value, Color c)
            { Key = key; Values.Add(value); Color = c; }
        }

        private Node? _root;

        public void Insert(TKey key, TValue value)
        {
            var z = new Node(key, value, Color.Red);
            Node? y = null;
            var x = _root;

            while (x != null)
            {
                y = x;
                int cmp = key.CompareTo(x.Key);
                if (cmp < 0) x = x.Left;
                else if (cmp > 0) x = x.Right;
                else { x.Values.Add(value); return; }
            }

            z.Parent = y;
            if (y == null) _root = z;
            else if (key.CompareTo(y.Key) < 0) y.Left = z;
            else y.Right = z;

            InsertFix(z);
        }

        public IEnumerable<TValue> InOrder()
        {
            var st = new Stack<Node?>();
            var cur = _root;
            while (st.Count > 0 || cur != null)
            {
                while (cur != null) { st.Push(cur); cur = cur.Left; }
                cur = st.Pop();
                foreach (var v in cur.Values) yield return v;
                cur = cur.Right;
            }
        }

        public IEnumerable<TValue> Range(TKey? min, TKey? max)
        {
            var st = new Stack<Node?>();
            var cur = _root;
            while (st.Count > 0 || cur != null)
            {
                while (cur != null) { st.Push(cur); cur = cur.Left; }
                cur = st.Pop();
                bool geMin = min == null || cur.Key.CompareTo(min) >= 0;
                bool leMax = max == null || cur.Key.CompareTo(max) <= 0;
                if (geMin && leMax) foreach (var v in cur.Values) yield return v;
                if (max != null && cur.Key.CompareTo(max) > 0) break;
                cur = cur.Right;
            }
        }

        public IEnumerable<TValue> Get(TKey key)
        {
            var n = FindNode(key);
            return n == null ? Array.Empty<TValue>() : n.Values;
        }

        // --- internals ---
        private Node? FindNode(TKey key)
        {
            var x = _root;
            while (x != null)
            {
                int cmp = key.CompareTo(x.Key);
                if (cmp < 0) x = x.Left;
                else if (cmp > 0) x = x.Right;
                else return x;
            }
            return null;
        }

        private void RotateLeft(Node x)
        {
            var y = x.Right!;
            x.Right = y.Left;
            if (y.Left != null) y.Left.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null) _root = y;
            else if (x == x.Parent.Left) x.Parent.Left = y;
            else x.Parent.Right = y;
            y.Left = x;
            x.Parent = y;
        }

        private void RotateRight(Node y)
        {
            var x = y.Left!;
            y.Left = x.Right;
            if (x.Right != null) x.Right.Parent = y;
            x.Parent = y.Parent;
            if (y.Parent == null) _root = x;
            else if (y == y.Parent.Left) y.Parent.Left = x;
            else y.Parent.Right = x;
            x.Right = y;
            y.Parent = x;
        }

        private static Color C(Node? n) => n?.Color ?? Color.Black;
        private static void SetBlack(Node n) => n.Color = Color.Black;
        private static void SetRed(Node n) => n.Color = Color.Red;

        private void InsertFix(Node z)
        {
            while (C(z.Parent) == Color.Red)
            {
                if (z.Parent == z.Parent!.Parent!.Left)
                {
                    var y = z.Parent.Parent.Right;
                    if (C(y) == Color.Red)
                    {
                        SetBlack(z.Parent); SetBlack(y!); SetRed(z.Parent.Parent!);
                        z = z.Parent.Parent!;
                    }
                    else
                    {
                        if (z == z.Parent.Right) { z = z.Parent; RotateLeft(z); }
                        SetBlack(z.Parent!); SetRed(z.Parent!.Parent!);
                        RotateRight(z.Parent!.Parent!);
                    }
                }
                else
                {
                    var y = z.Parent!.Parent!.Left;
                    if (C(y) == Color.Red)
                    {
                        SetBlack(z.Parent!); SetBlack(y!); SetRed(z.Parent!.Parent!);
                        z = z.Parent!.Parent!;
                    }
                    else
                    {
                        if (z == z.Parent!.Left) { z = z.Parent!; RotateRight(z); }
                        SetBlack(z.Parent!); SetRed(z.Parent!.Parent!);
                        RotateLeft(z.Parent!.Parent!);
                    }
                }
            }
            SetBlack(_root!);
        }
    }
}
