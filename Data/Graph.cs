using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalityApp.Data
{
    public sealed class Graph<T>
    {
        private readonly Dictionary<T, List<(T To, int W)>> _adj = new();

        public void AddEdge(T a, T b, int w = 1, bool undirected = true)
        {
            if (!_adj.TryGetValue(a, out var la)) _adj[a] = la = new();
            la.Add((b, w));
            if (undirected)
            {
                if (!_adj.TryGetValue(b, out var lb)) _adj[b] = lb = new();
                lb.Add((a, w));
            }
        }

        public IReadOnlyList<T> Bfs(T start, T goal)
        {
            var q = new Queue<T>();
            var prev = new Dictionary<T, T?>();
            var seen = new HashSet<T>();
            q.Enqueue(start); seen.Add(start); prev[start] = default;

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                if (EqualityComparer<T>.Default.Equals(u, goal)) break;
                if (!_adj.TryGetValue(u, out var edges)) continue;
                foreach (var (v, _) in edges)
                {
                    if (seen.Add(v)) { prev[v] = u; q.Enqueue(v); }
                }
            }

            if (!prev.ContainsKey(goal)) return Array.Empty<T>();

            var path = new List<T>();
            for (var at = goal; ;)
            {
                path.Add(at);
                if (!prev.TryGetValue(at, out var p) || EqualityComparer<T?>.Default.Equals(p, default)) break;
                at = p!;
            }
            path.Reverse();
            return path;
        }

        // Simple Kruskal MST for undirected graphs
        public IReadOnlyList<(T A, T B, int W)> KruskalMst(IEqualityComparer<T>? cmp = null)
        {
            cmp ??= EqualityComparer<T>.Default;
            var edges = _adj
                .SelectMany(kv => kv.Value.Select(e => (A: kv.Key, B: e.To, W: e.W)))
                .Where(e => Comparer<T>.Default.Compare(e.A, e.B) <= 0)
                .OrderBy(e => e.W)
                .ToList();

            var nodes = _adj.Keys.ToList();
            var parent = nodes.ToDictionary(n => n, n => n, cmp);

            T Find(T x) => !cmp.Equals(parent[x], x) ? parent[x] = Find(parent[x]) : x;
            bool Union(T a, T b) { a = Find(a); b = Find(b); if (cmp.Equals(a, b)) return false; parent[a] = b; return true; }

            var mst = new List<(T A, T B, int W)>();
            foreach (var (a, b, w) in edges)
                if (Union(a, b)) mst.Add((a, b, w));
            return mst;
        }
    }
}
