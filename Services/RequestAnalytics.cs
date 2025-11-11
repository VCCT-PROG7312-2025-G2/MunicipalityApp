using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalityApp.Data;
using MunicipalityApp.Models;

namespace MunicipalityApp.Services
{
    public interface IRequestAnalytics
    {
        // AVL (date range)
        IEnumerable<IssueReport> RangeByDate(DateOnly? from, DateOnly? to);

        // Red-Black (id index)
        IReadOnlyList<IssueReport> GetById(Guid id);
        IEnumerable<IssueReport> OrderedById(Guid? from, Guid? to);

        // BST (location index)
        IEnumerable<IssueReport> LocationsBetween(string? min, string? max);

        // Basic tree (hierarchy)
        BasicTreeNode<string, IssueReport> BuildStatusCategoryTree();

        // --- NEW for Part 3 ---
        IReadOnlyList<IssueReport> TopUrgent(int count);                          // Heap
        IReadOnlyList<IssueStatus> WorkflowPath(IssueStatus current, IssueStatus goal); // Graph + BFS
        IReadOnlyList<(string A, string B, int W)> DepartmentMst();               // MST

        // Rebuild all indexes
        void Rebuild();
    }

    public sealed class RequestAnalytics : IRequestAnalytics
    {
        private readonly IIssueStore _store;
        private readonly object _gate = new();

        private AvlTree<long, IssueReport>? _byTicks;     // AVL by CreatedAt
        private RedBlackTree<Guid, IssueReport>? _byId;   // RBT by GUID
        private Bst<string, IssueReport>? _byLocation;    // BST by location (lexicographic)

        // NEW DS
        private PriorityQueue<IssueReport, int>? _urgent; // Heap
        private Graph<IssueStatus>? _workflow;            // Status graph
        private IReadOnlyList<(string A, string B, int W)>? _deptMst; // MST edges

        private static readonly Guid GuidMax = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
        private int _lastIssueCount = -1;

        public RequestAnalytics(IIssueStore store) { _store = store; }

        public void Rebuild()
        {
            lock (_gate)
            {
                // AVL date index
                var avl = new AvlTree<long, IssueReport>();
                foreach (var it in _store.All())
                    avl.Insert(it.CreatedAt.Ticks, it);
                _byTicks = avl;

                // Red-Black id index
                var rbt = new RedBlackTree<Guid, IssueReport>();
                foreach (var it in _store.All())
                    rbt.Insert(it.Id, it);
                _byId = rbt;

                // BST location index (lowercased)
                var bst = new Bst<string, IssueReport>();
                foreach (var it in _store.All())
                {
                    var key = (it.Location ?? "").Trim().ToLowerInvariant();
                    if (key.Length == 0) key = "~";
                    bst.Insert(key, it);
                }
                _byLocation = bst;

                // HEAP: urgency (lower priority value = more urgent)
                var pq = new PriorityQueue<IssueReport, int>();
                foreach (var it in _store.All())
                {
                    var ageHours = (int)Math.Clamp((DateTime.UtcNow - it.CreatedAt).TotalHours, 0, int.MaxValue);
                    var statusPenalty = it.Status switch
                    {
                        IssueStatus.Submitted => 0,
                        IssueStatus.Assigned => 10,
                        IssueStatus.InProgress => 20,
                        IssueStatus.Resolved => 10000,
                        _ => 0
                    };
                    var priority = statusPenalty + (10000 - ageHours);
                    pq.Enqueue(it, priority);
                }
                _urgent = pq;

                // WORKFLOW graph
                var g = new Graph<IssueStatus>();
                g.AddEdge(IssueStatus.Submitted, IssueStatus.Assigned, 1, undirected: false);
                g.AddEdge(IssueStatus.Assigned, IssueStatus.InProgress, 1, undirected: false);
                g.AddEdge(IssueStatus.InProgress, IssueStatus.Resolved, 1, undirected: false);
                // optional rework edges
                g.AddEdge(IssueStatus.InProgress, IssueStatus.Assigned, 2, undirected: false);
                g.AddEdge(IssueStatus.Assigned, IssueStatus.Submitted, 3, undirected: false);
                _workflow = g;

                // MST over categories (departments)
                var dg = new Graph<string>();
                var cats = _store.CategoryCounts.Keys.DefaultIfEmpty("General").ToList();
                for (int i = 0; i < cats.Count - 1; i++)
                    dg.AddEdge(cats[i], cats[i + 1], 1 + (i % 3));
                if (cats.Count >= 4) dg.AddEdge(cats[0], cats[3], 2);
                if (cats.Count >= 5) dg.AddEdge(cats[1], cats[4], 3);
                _deptMst = dg.KruskalMst().Select(e => (e.A, e.B, e.W)).ToList();

                _lastIssueCount = _store.All().Count();
            }
        }

        private void Ensure()
        {
            lock (_gate)
            {
                var current = _store.All().Count();
                if (_byTicks == null || _byId == null || _byLocation == null || _urgent == null || _workflow == null || _deptMst == null || current != _lastIssueCount)
                    Rebuild();
            }
        }

        // ---- AVL: date range ----
        public IEnumerable<IssueReport> RangeByDate(DateOnly? from, DateOnly? to)
        {
            Ensure();
            long minTicks = from.HasValue ? from.Value.ToDateTime(TimeOnly.MinValue).Ticks : long.MinValue;
            long maxTicks = to.HasValue ? to.Value.ToDateTime(TimeOnly.MaxValue).Ticks : long.MaxValue;
            return _byTicks!.Range(minTicks, maxTicks);
        }


        // ---- RBT: id lookup / ordered ----
        public IReadOnlyList<IssueReport> GetById(Guid id)
        {
            Ensure();
            return _byId!.Get(id).ToList();
        }

        public IEnumerable<IssueReport> OrderedById(Guid? from, Guid? to)
        {
            Ensure();
            if (!from.HasValue && !to.HasValue) return _byId!.InOrder();
            var a = from ?? Guid.Empty;
            var b = to ?? GuidMax;
            return _byId!.Range(a, b);
        }

        // ---- BST: location range ----
        public IEnumerable<IssueReport> LocationsBetween(string? min, string? max)
        {
            Ensure();
            var a = string.IsNullOrWhiteSpace(min) ? string.Empty : min.Trim().ToLowerInvariant();
            var b = string.IsNullOrWhiteSpace(max) ? "\uFFFF" : max.Trim().ToLowerInvariant();
            return _byLocation!.Range(a, b);
        }

        // ---- Basic tree: Status -> Category -> Issues ----
        public BasicTreeNode<string, IssueReport> BuildStatusCategoryTree()
        {
            Ensure();
            var root = new BasicTreeNode<string, IssueReport>("Requests");
            foreach (IssueStatus s in Enum.GetValues(typeof(IssueStatus)))
            {
                var sNode = root.AddChild(s.ToString());
                var byCat = _store.All()
                                  .Where(x => x.Status == s)
                                  .GroupBy(x => x.Category ?? "Other")
                                  .OrderBy(g => g.Key);
                foreach (var g in byCat)
                {
                    var cNode = sNode.AddChild(g.Key);
                    cNode.Values.AddRange(g);
                }
            }
            return root;
        }

        // ---- Heap / Graph / MST accessors ----
        public IReadOnlyList<IssueReport> TopUrgent(int count)
        {
            Ensure();
            var tmp = new PriorityQueue<IssueReport, int>(_urgent!.UnorderedItems);
            var list = new List<IssueReport>(Math.Max(0, count));
            while (list.Count < count && tmp.TryDequeue(out var it, out _)) list.Add(it);
            return list;
        }

        public IReadOnlyList<IssueStatus> WorkflowPath(IssueStatus current, IssueStatus goal)
        {
            Ensure();
            return _workflow!.Bfs(current, goal);
        }

        public IReadOnlyList<(string A, string B, int W)> DepartmentMst()
        {
            Ensure();
            return _deptMst!;
        }
    }
}
