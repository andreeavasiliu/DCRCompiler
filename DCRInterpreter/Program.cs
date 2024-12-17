using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DCR.Workflow;
using Microsoft.Extensions.Logging;
using static DCR.Core.Data.BuiltinModule;
class Program
{
    static void Main(string[] args)
    {
        string xmlFilePath = "DCR_interpreter.xml";

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: File '{xmlFilePath}' not found.");
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            Becnh(doc);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void Becnh(XDocument doc)
    {
        var runtime = Runtime.Create(builder =>
        {
            builder.WithOptions(options =>
                options.UpdateModelLog = true
            );
        });
        var model = runtime.Parse(doc);

        var loop = 500000;
        DCRGraph graph = ParseDCRGraphFromXml(doc);
        // Initialize and precompile logic for events
        graph.Initialize();
        DCRInterpreter interpreter = new DCRInterpreter(graph);

        BenchRuntime(runtime, model, loop);
        BenchInterp(interpreter, graph, loop);

    }

    static void BenchRuntime(Runtime runtime, Model model, int loop)
    {
        Stopwatch stopwatch = new Stopwatch();

        int executedCount = 0;
        // Start the stopwatch
        stopwatch.Start();
        for (int i = 0; i < loop; i++)
        {
            foreach (var item in model.AllActivities)
            {
                if (item.IsEnabled)
                {
                    executedCount++;
                    model.Execute(item);
                }

                else
                {
                    Console.WriteLine($"Event {item.Id} is not enabled.");
                }
            }
        }
        // Stop the stopwatch
        stopwatch.Stop();

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.WriteLine($"RunTime Workflow {executedCount} executions:" + elapsedTime);
    }

    static void BenchInterp(DCRInterpreter interpreter, DCRGraph graph, int loop)
    {
        Stopwatch stopwatch2 = new Stopwatch();
        int executedCount = 0;
        // Start the stopwatch
        stopwatch2.Start();
        for (int i = 0; i < loop; i++)
        {
            foreach (var eventId in graph.Events.Keys)
            {
                if (interpreter.IsEventEnabled(eventId))
                {
                    executedCount++;
                    interpreter.ExecuteEvent(eventId);
                }
                else
                {
                    Console.WriteLine($"Event {eventId} is not enabled.");
                }
            }
        }
        // Stop the stopwatch
        stopwatch2.Stop();

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch2.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.WriteLine($"RunTime Interpreter {executedCount} executions:" + elapsedTime);
    }

    static DCRGraph ParseDCRGraphFromXml(XDocument doc)
    {
        // Initialize a new DCRGraph instance
        DCRGraph graph = new DCRGraph();

        // Parse events from <event> elements
        foreach (var eventElement in doc.Descendants("event"))
        {
            string? id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
            {
                graph.Events[id] = new Event(id);
            }
        }

        // Parse responses from <response> elements
        foreach (var responseElement in doc.Descendants("response"))
        {
            string? sourceId = responseElement.Attribute("sourceId")?.Value;
            string? targetId = responseElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
            {
                graph.Responses.Add((sourceId, targetId));
            }
        }

        // Parse conditions from <condition> elements
        foreach (var conditionElement in doc.Descendants("condition"))
        {
            string? sourceId = conditionElement.Attribute("sourceId")?.Value;
            string? targetId = conditionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
            {
                graph.Conditions.Add((sourceId, targetId));
            }
        }

        // Parse inclusions from <inclusion> elements
        foreach (var inclusionElement in doc.Descendants("inclusion"))
        {
            string? sourceId = inclusionElement.Attribute("sourceId")?.Value;
            string? targetId = inclusionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
            {
                graph.Inclusions.Add((sourceId, targetId));
            }
        }

        // Parse exclusions from <exclusion> elements
        foreach (var exclusionElement in doc.Descendants("exclusion"))
        {
            string? sourceId = exclusionElement.Attribute("sourceId")?.Value;
            string? targetId = exclusionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
            {
                graph.Exclusions.Add((sourceId, targetId));
            }
        }

        // Parse milestones from <milestone> elements
        foreach (var milestoneElement in doc.Descendants("milestone"))
        {
            string? sourceId = milestoneElement.Attribute("sourceId")?.Value;
            string? targetId = milestoneElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
            {
                graph.Milestones.Add((sourceId, targetId));
            }
        }

        // Initialize event states from <marking>
        var executedEvents = new HashSet<string>();
        var includedEvents = new HashSet<string>();
        var pendingEvents = new HashSet<string>();

        foreach (var executedEvent in doc.Descendants("executed").Descendants("event"))
        {
            string? id = executedEvent.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                executedEvents.Add(id);
        }

        foreach (var includedEvent in doc.Descendants("included").Descendants("event"))
        {
            string? id = includedEvent.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                includedEvents.Add(id);
        }

        foreach (var pendingEvent in doc.Descendants("pendingResponses").Descendants("event"))
        {
            string? id = pendingEvent.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                pendingEvents.Add(id);
        }

        // Set initial states for events
        foreach (var id in graph.Events.Keys)
        {
            var e = graph.Events[id];

            e.Executed = executedEvents.Contains(id);
            e.Included = includedEvents.Contains(id);
            e.Pending = pendingEvents.Contains(id);
        }

        return graph;
    }


}