using System;
using System.Linq;
using Newtonsoft.Json;

namespace SampeGraph
{
    public class Program
    {

        public static void CheckGraphs(int[][] graph1, int[][] graph2, bool showSolution)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var solution = GraphStructureSolver.Solve(graph1, graph2);
            watch.Stop();
            if (solution != null && showSolution)
            {
                Console.WriteLine("Solution:");
                Console.WriteLine("  " + JsonConvert.SerializeObject(solution));
                Console.WriteLine($"Solution valid: {Graph.CheckSolution(graph1, graph2, solution)}");
            }
            var edgeCount = graph1.Select(connections => connections.Length).Sum();
            var timePerEdge = (edgeCount > 0) ? watch.ElapsedMilliseconds / (double) edgeCount : 0.0;
            Console.WriteLine($"Stats: Time={watch.ElapsedMilliseconds} ms | Nodes={graph1.Length} | Edges={edgeCount} | Solution={solution != null} | TimePerEdge={timePerEdge}");
        }

        private static void RunSampleGraph()
        {
            Console.WriteLine("== Checking Sample Graph ==========================");
            var graph1 = JsonConvert.DeserializeObject<int[][]>("[[0,1,5,7],[0,5],[2,1,4],[0,4],[6],[7,4],[7],[6,5,1]]");
            var graph2 = JsonConvert.DeserializeObject<int[][]>("[[1,5],[1,2,3,6],[6,5],[2,1],[6],[4],[4,2,3],[7,3,5]]");
            CheckGraphs(graph1, graph2, true);
        }

        public static void RunSuccessiveGraphSizes()
        {
            Console.WriteLine("== Running successive graph sizes ======================");
            var rand = new Random();
            for (var i = 2.0; i <= 9000.0; i *= 1.25)
            {
                var g1 = Graph.GenerateRandom(rand, (int) i, 0.25);
                var g2 = Graph.GenerateWithRandomizedStructure(rand, g1);
                CheckGraphs(g1, g2, false);
            }
        }

        public static void Main(string[] args)
        {
            RunSampleGraph();
            RunSuccessiveGraphSizes();
        }
    }
}