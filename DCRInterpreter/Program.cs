using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        string xmlFilePath = "DCR_interpreter.xml";

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: File '{xmlFilePath}' not found.");
            return;
        }

        try
        {
            DCRGraph graph = ParseDCRGraphFromXml(xmlFilePath);

            JITExecutor jitExecutor = new JITExecutor(graph);

            foreach (var e in graph.Events.Values)
            {
                Console.WriteLine($"Event {e.Id}: Executed={e.Executed}, Included={e.Included}, Pending={e.Pending}");
            }

            foreach (var eventId in graph.Events.Keys)
            {
                if (graph.Events[eventId].Included)
                {
                    Console.WriteLine($"Executing event: {eventId}");
                    await jitExecutor.ExecuteEventAsync(eventId);
                    foreach (var e in graph.Events.Values)
                    {
                        Console.WriteLine($"Event {e.Id}: Executed={e.Executed}, Included={e.Included}, Pending={e.Pending}");
                    }
                }
                else
                {
                    Console.WriteLine($"Event {eventId} is not enabled.");
                }
            }

            Console.WriteLine("\nFinal Event States:");
            foreach (var e in graph.Events.Values)
            {
                Console.WriteLine($"Event {e.Id}: Executed={e.Executed}, Included={e.Included}, Pending={e.Pending}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static DCRGraph ParseDCRGraphFromXml(string filePath)
    {
        XDocument doc = XDocument.Load(filePath);

        DCRGraph graph = new DCRGraph();
        foreach (var eventElement in doc.Descendants("event"))
        {
            string id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                graph.Events[id] = new Event(id);
        }
        graph.ParseFromXml(doc.ToString());

        foreach (var responseElement in doc.Descendants("response"))
        {
            string sourceId = responseElement.Attribute("sourceId")?.Value;
            string targetId = responseElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                graph.Responses.Add((sourceId, targetId));
        }

        foreach (var conditionElement in doc.Descendants("condition"))
        {
            string sourceId = conditionElement.Attribute("sourceId")?.Value;
            string targetId = conditionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                graph.Conditions.Add((sourceId, targetId));
        }

        foreach (var inclusionElement in doc.Descendants("inclusion"))
        {
            string sourceId = inclusionElement.Attribute("sourceId")?.Value;
            string targetId = inclusionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                graph.Inclusions.Add((sourceId, targetId));
        }

        foreach (var exclusionElement in doc.Descendants("exclusion"))
        {
            string sourceId = exclusionElement.Attribute("sourceId")?.Value;
            string targetId = exclusionElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                graph.Exclusions.Add((sourceId, targetId));
        }

        foreach (var milestoneElement in doc.Descendants("milestone"))
        {
            string sourceId = milestoneElement.Attribute("sourceId")?.Value;
            string targetId = milestoneElement.Attribute("targetId")?.Value;
            if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                graph.Milestones.Add((sourceId, targetId));
        }

        return graph;
    }

}