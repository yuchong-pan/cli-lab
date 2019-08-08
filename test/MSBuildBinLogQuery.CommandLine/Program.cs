﻿using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Logging.Query.Construction;
using Microsoft.Build.Logging.Query.Graph;

namespace Microsoft.Build.Logging.Query.Commandline
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintErrorMessage("Exactly one argument is required");
            }

            if (!File.Exists(args[0]))
            {
                PrintErrorMessage($"File not found: {args[0]}");
            }

            using var binaryLogReader = new BinaryLogReader(args[0]);
            var events = binaryLogReader.ReadEvents();

            var graphBuilder = new GraphBuilder();
            graphBuilder.HandleEvents(events.ToArray());

            PrintProjectNodes(graphBuilder.Build);
        }

        private static void PrintProjectNodes(Component.Build build)
        {
            PrintBuild(build);

            var projectGraph = new DirectedAcyclicGraph<ProjectNode_BeforeThis>(build.Projects.Values.Select(project => project.Node_BeforeThis));

            PrintProjectGraph(projectGraph);
            PrintProjectTopologicalOrdering(projectGraph);
            PrintReachableProjects(projectGraph);
            PrintReversedProjectGraph(projectGraph);
        }

        private static void PrintErrorMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            Environment.Exit(1);
        }

        private static void PrintProjectGraph(DirectedAcyclicGraph<ProjectNode_BeforeThis> graph, string header = "project graph:")
        {
            Console.WriteLine(header);

            Console.WriteLine("  graph TD");

            foreach (var node in graph.Nodes)
            {
                foreach (var adjacentNode in node.AdjacentNodes)
                {
                    Console.WriteLine($"  {node.ProjectInfo.Id} --> {adjacentNode.ProjectInfo.Id}");
                }
            }

            Console.WriteLine();
        }

        private static void PrintReversedProjectGraph(DirectedAcyclicGraph<ProjectNode_BeforeThis> graph, string header = "reversed project graph:")
        {
            PrintProjectGraph(graph.Reverse(), header);
        }

        private static void PrintProjectTopologicalOrdering(DirectedAcyclicGraph<ProjectNode_BeforeThis> graph, string header = "project topological ordering:")
        {
            var topologicalSortResult = graph.TopologicalSort(out var topologicalOrdering) ? "Success" : "Failed";
            Console.WriteLine($"{header}: {topologicalSortResult}");

            foreach (var project in topologicalOrdering)
            {
                Console.WriteLine($"  #{project.ProjectInfo.Id}");
            }

            Console.WriteLine();
        }

        private static void PrintReachableProjects(DirectedAcyclicGraph<ProjectNode_BeforeThis> graph, string header = "reachable projects:")
        {
            var reachableCalculationResult = graph.GetReachableNodes(out var reachables) ? "Success" : "Failed";
            Console.WriteLine($"{header}: {reachableCalculationResult}");

            foreach (var pair in reachables)
            {
                var reachableNodes = string.Join(", ", pair.Value.Select(node => $"#{node.ProjectInfo.Id}"));
                Console.WriteLine($"  #{pair.Key.ProjectInfo.Id}: {reachableNodes}");
            }

            Console.WriteLine();
        }

        private static void PrintBuild(Component.Build build, string header = "build:", bool printTargetGraph = false)
        {
            Console.WriteLine(header);

            foreach (var project in build.Projects.Values)
            {
                Console.WriteLine($"  project #{project.Id}: {project.ProjectFile}");

                foreach (var target in project.TargetsByName.Values)
                {
                    Console.WriteLine($"    target #{target.Id} {target.Name}");

                    if (printTargetGraph)
                    {
                        Console.WriteLine($"      directly before this: {string.Join(";", target.Node_BeforeThis.AdjacentNodes.Select(beforeThis => beforeThis.TargetInfo.Name))}");
                        Console.WriteLine($"      directly after this: {string.Join(";", target.Node_AfterThis.AdjacentNodes.Select(afterThis => afterThis.TargetInfo.Name))}");
                    }

                    foreach (var task in target.Tasks.Values)
                    {
                        Console.WriteLine($"      task #{task.Id} {task.Name}");
                    }
                }
            }

            Console.WriteLine();
        }
    }
}