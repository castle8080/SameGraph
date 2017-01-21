using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SampeGraph
{

    public class GraphStructureSolver
    {
        public class SolutionState
        {
            public int[][] Graph1 { get; private set; }

            public int[][] Graph2 { get; private set; }

            public IDictionary<int, int> Mapping { get; private set; }

            public IDictionary<int, int> ReverseMapping { get; private set; }

            public IDictionary<int, ISet<int>> Possibilities { get; private set; }

            public SolutionState(int[][] g1, int[][] g2, IDictionary<int, int> mapping, IDictionary<int, ISet<int>> possibilities)
            {
                Graph1 = g1;
                Graph2 = g2;
                Mapping = mapping;
                ReverseMapping = Mapping.ToDictionary(pair => pair.Value, pair => pair.Key);
                Possibilities = possibilities;
                Invalid = false;
                Simplify();
            }

            public SolutionState(SolutionState old)
            {
                Graph1 = old.Graph1;
                Graph2 = old.Graph2;
                Mapping = new Dictionary<int, int>(old.Mapping);
                ReverseMapping = new Dictionary<int, int>(old.ReverseMapping);
                Possibilities = old.Possibilities.ToDictionary(pair => pair.Key, pair => (ISet<int>) new HashSet<int>(pair.Value));
                Invalid = old.Invalid;
            }

            public bool Invalid { get; set; }

            public bool Solved {
                get {
                    return !Invalid && Possibilities.Count == 0;
                }
            }

            public static SolutionState CreateInitialState(int[][] graph1, int[][] graph2)
            {
                // Generates the initial possibilities by removing values where the number of
                // connections are different.
                var idConnectionCounts = graph2
                    .Select((connections, id) => new { id, count = connections.Length })
                    .GroupBy(e => e.count)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.id).ToArray());

                var possibilities = Enumerable
                    .Range(0, graph1.Length)
                    .ToDictionary(i => i, i => (ISet<int>) new HashSet<int>(idConnectionCounts[graph1[i].Length]));

                return new SolutionState(graph1, graph2, new Dictionary<int, int>(), possibilities);
            }

            /// Gets the next entry to make a guess for.
            public KeyValuePair<int, ISet<int>>? NextEntryToSolve()
            {
                if (Invalid || Solved)
                {
                    return null;
                }
                return Possibilities.Aggregate((p1, p2) => (p1.Value.Count <= p2.Value.Count) ? p1 : p2);
            }

            public SolutionState WithPossibleMapping(int id, int toId)
            {
                var newState = new SolutionState(this);
                newState.SetMapping(id, toId);
                return newState;
            }

            private void SetMapping(int id, int toId)
            {
                //Console.WriteLine($"Setting {id} to {toId}.");
                if (!Possibilities.ContainsKey(id))
                {
                    throw new InvalidOperationException($"id {id} has already been mapped.");
                }
                if (!Possibilities[id].Contains(toId))
                {
                    throw new InvalidOperationException($"{toId} is not a possibility for {id}.");
                }

                // Set the mapping
                Mapping[id] = toId;
                ReverseMapping[toId] = id;

                // Clear out the id as a possibility
                Possibilities.Remove(id);

                // Other ids should not have the toId as a possibility any more.
                foreach (var possible in Possibilities.Values)
                {
                    possible.Remove(toId);
                }

                // Look at the connections for the id and toId to restrict their possibilties.
                foreach (var g1Conn in Graph1[id])
                {
                    if (Possibilities.ContainsKey(g1Conn))
                    {
                        Possibilities[g1Conn] = new HashSet<int>(Possibilities[g1Conn].Intersect(Graph2[toId]));
                    }
                }
            }

            private void Simplify()
            {
                while (true)
                {
                    KeyValuePair<int, int>? pair = null;
                    foreach (var entry in Possibilities)
                    {
                        if (entry.Value.Count == 0)
                        {
                            Invalid = true;
                            return;
                        }
                        else if (entry.Value.Count == 1)
                        {
                            pair = new KeyValuePair<int, int>(entry.Key, entry.Value.FirstOrDefault());
                            break;
                        }
                    }
                    if (pair == null)
                    {
                        // No changes
                        return;
                    }
                    else
                    {
                        SetMapping(pair.Value.Key, pair.Value.Value);
                    }
                }
            }
        }

        public static IDictionary<int, int> Solve(int[][] g1, int[][] g2)
        {
            if (g1.Length != g2.Length)
            {
                return null;
            }

            var states = new List<Func<SolutionState>>();
            states.Add(() => SolutionState.CreateInitialState(g1, g2));

            while (states.Any())
            {
                var state = states[states.Count - 1]();
                states.RemoveAt(states.Count - 1);

                if (state.Solved)
                {
                    return state.Mapping;
                }
                else if (!state.Invalid)
                {
                    var entry = state.NextEntryToSolve();
                    foreach (var possible in entry.Value.Value)
                    {
                        states.Add(() => state.WithPossibleMapping(entry.Value.Key, possible));
                    }
                }
            }

            return null;
        }
    }

    public class Program
    {
        private static int[][] Convert(int[][] g, IDictionary<int, int> solution)
        {
            var newG = new int[g.Length][];
            for (var i = 0; i < g.Length; i++)
            {
                newG[solution[i]] = g[i].Select(id => solution[id]).OrderBy(x => x).ToArray();
            }
            return newG;
        }

        private static bool GraphsEqual(int[][] graph1, int[][] graph2)
        {
           return (graph1.Length == graph2.Length) && Enumerable.Range(0, graph1.Length).All(i => graph1[i].All(graph2[i].Contains));
        }

        private static bool CheckSolution(int[][] graph1, int[][] graph2, IDictionary<int, int> mapping)
        {
            return GraphsEqual(Convert(graph1, mapping), graph2);
        }

        private static int[][] GenerateRandomGraph(Random rand, int size, double connectRatio)
        {
            var g = new int[size][];
            for (var i = 0; i < g.Length; i++)
            {
                var ids = new List<int>();
                for (var j = 0; j < g.Length; j++)
                {
                    if (rand.NextDouble() <= connectRatio)
                    {
                        ids.Add(j);
                    }
                }
                g[i] = ids.ToArray();
            }
            return g;
        }

        private static int[][] GenerateGraphWithRandomizedStructure(Random rand, int[][] g)
        {
            // Generate randomized mapping by shuffling and identity transform.
            var mapping = Enumerable.Range(0, g.Length).ToArray();
            for (var i = 0; i < mapping.Length; i++)
            {
                var j = rand.Next(i, mapping.Length);
                var tmp = mapping[i];
                mapping[i] = mapping[j];
                mapping[j] = tmp;
            }

            var mappingDict = Enumerable.Range(0, mapping.Length).ToDictionary(i => i, i => mapping[i]);
            return Convert(g, mappingDict);
        }

        public static void CheckGraphs(int[][] graph1, int[][] graph2, bool showSolution)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var solution = GraphStructureSolver.Solve(graph1, graph2);
            watch.Stop();
            if (solution != null && showSolution)
            {
                Console.WriteLine("Solution:");
                Console.WriteLine("  " + JsonConvert.SerializeObject(solution));
                Console.WriteLine($"Solution valid: {CheckSolution(graph1, graph2, solution)}");
            }
            var edgeCount = graph1.Select(connections => connections.Length).Sum();
            var timePerEdge = (edgeCount > 0) ? watch.ElapsedMilliseconds / (double) edgeCount : 0.0;
            Console.WriteLine($"Stats: Time={watch.ElapsedMilliseconds} ms | Nodes={graph1.Length} | Edges={edgeCount} | Solution={solution != null} | TimePerEdge={timePerEdge}");
        }

        public static void Main(string[] args)
        {
            var graph1 = JsonConvert.DeserializeObject<int[][]>("[[0,1,5,7],[0,5],[2,1,4],[0,4],[6],[7,4],[7],[6,5,1]]");
            var graph2 = JsonConvert.DeserializeObject<int[][]>("[[1,5],[1,2,3,6],[6,5],[2,1],[6],[4],[4,2,3],[7,3,5]]");
            CheckGraphs(graph1, graph2, true);

            Console.WriteLine();
            Console.WriteLine("==================================");
            Console.WriteLine("Trying larger random graphs:");
            Console.WriteLine("==================================");

            var rand = new Random();
            for (var i = 2; i <= 8192; i = i * 2)
            {
                var g1 = GenerateRandomGraph(rand, i, 0.25);
                var g2 = GenerateGraphWithRandomizedStructure(rand, g1);
                CheckGraphs(g1, g2, false);
            }
        }
    }
}
