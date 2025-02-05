using System.Xml.Linq;

public class DCRInterpreter
{
    private DCRGraph Graph;

    public DCRInterpreter(DCRGraph graph)
    {
        Graph = graph;
    }

    public void ExecuteEvent(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");

        var e = Graph.Events[eventId];

        if (!IsEventEnabled(eventId))
            throw new InvalidOperationException($"Event {eventId} is not enabled.");

        // Mark as executed
        e.Executed = true;

        // Execute precompiled logic using DynamicMethod
        e.CompiledLogic(Graph);
    }

    public bool IsEventEnabled(string eventId)
    {
        var e = Graph.Events[eventId];

        if (!e.Included) return false;

        foreach (var condition in Graph.Conditions)
        {
            if (condition.TargetId == eventId && !Graph.Events[condition.SourceId].Executed)
                return false;
        }

        return true;
    }
    public static DCRGraph ParseDCRGraphFromXml(XDocument doc)
    {
        // Initialize a new DCRGraph instance with a title (can be extracted from XML if available)
        string title = doc.Root?.Attribute("title")?.Value ?? "Untitled DCR Graph";
        DCRGraph graph = new DCRGraph(title);

        // Parse events from <event> elements
        foreach (var eventElement in doc.Descendants("event"))
        {
            string? id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
            {
                Event newEvent = new Event(id)
                {
                    Type = (eventElement.Attribute("type")?.Value) switch { "nesting" or "subprocess" or "form" or "template" => EventType.Form, _ =>  EventType.Task },
                    Data = eventElement.Element("data")?.Value,
                    Roles = eventElement.Element("roles")?.Elements("role").Select(r => r.Value).ToList() ?? new List<string>(),
                    ReadRoles = eventElement.Element("readRoles")?.Elements("role").Select(r => r.Value).ToList() ?? new List<string>()
                };

                graph.Events[id] = newEvent;
            }
        }

        // Parse relationships from XML and add them to the graph
        void ParseRelationships(string elementName, RelationshipType type)
        {
            foreach (var relationshipElement in doc.Descendants(elementName))
            {
                string? sourceId = relationshipElement.Attribute("sourceId")?.Value;
                string? targetId = relationshipElement.Attribute("targetId")?.Value;

                if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                {
                    Relationship relationship = new Relationship(sourceId, targetId, type);
                    graph.Relationships.Add(relationship);
                }
            }
        }

        // Parse all relationship types
        ParseRelationships("response", RelationshipType.Response);
        ParseRelationships("condition", RelationshipType.Condition);
        ParseRelationships("inclusion", RelationshipType.Include);
        ParseRelationships("exclusion", RelationshipType.Exclude);
        ParseRelationships("milestone", RelationshipType.Milestone);

        // Parse markings for executed, included, and pending events
        var executedEvents = new HashSet<string>(
            doc.Descendants("executed").Descendants("event")
               .Select(e => e.Attribute("id")?.Value).Where(id => !string.IsNullOrEmpty(id))!);

        var includedEvents = new HashSet<string>(
            doc.Descendants("included").Descendants("event")
               .Select(e => e.Attribute("id")?.Value).Where(id => !string.IsNullOrEmpty(id))!);

        var pendingEvents = new HashSet<string>(
            doc.Descendants("pendingResponses").Descendants("event")
               .Select(e => e.Attribute("id")?.Value).Where(id => !string.IsNullOrEmpty(id))!);

        // Update event states based on markings
        foreach (var id in graph.Events.Keys)
        {
            var e = graph.Events[id];
            e.Executed = executedEvents.Contains(id);
            e.Included = includedEvents.Contains(id);
            e.Pending = pendingEvents.Contains(id);

            // Add to DCRGraph's marking sets for quick access
            if (e.Executed) graph.ExecutedEvents.Add(id);
            if (e.Included) graph.IncludedEvents.Add(id);
            if (e.Pending) graph.PendingEvents.Add(id);
        }

        // Return the fully constructed DCRGraph
        return graph;
    }

}
