using System;
using System.Collections.Generic;
using System.Linq;

namespace SampeGraph
{
    public class Graph
    {

        /// Converts a graph into another graph based on the id mappings of solution.
        public static int[][] Convert(int[][] g, IDictionary<int, int> solution)
        {
            var newG = new int[g.Length][];
            for (var i = 0; i < g.Length; i++)
            {
                newG[solution[i]] = g[i].Select(id => solution[id]).OrderBy(x => x).ToArray();
            }
            return newG;
        }

        // Determine if 2 Graphs are the same.
        public static bool Equal(int[][] graph1, int[][] graph2)
        {
           return (graph1.Length == graph2.Length) && Enumerable.Range(0, graph1.Length).All(i => graph1[i].All(graph2[i].Contains));
        }

        public static bool CheckSolution(int[][] graph1, int[][] graph2, IDictionary<int, int> mapping)
        {
            return Equal(Convert(graph1, mapping), graph2);
        }

        public static int[][] GenerateRandom(Random rand, int size, double connectRatio)
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

        public static int[][] GenerateWithRandomizedStructure(Random rand, int[][] g)
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
    }
}
