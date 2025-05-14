using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;

public class DCRInterpreter
{
    private DCRGraph Graph;

    public DCRInterpreter(DCRGraph graph)
    {
        Graph = graph;
    }

    public static DCRGraph ParseDCRGraphFromXml(XDocument doc)
    {
        // Initialize a new DCRGraph instance with a title (can be extracted from XML if available)
        string title = doc.Root?.Attribute("title")?.Value ?? "Untitled DCR Graph";
        string? guid = doc.Element("dcrgraph")!.Element("meta")?.Element("graph")?.Attribute("id")?.Value;
        DCRGraph graph = new DCRGraph(title);
        // If we don't have a guid, we're possibliy inside a template
        // and we need to generate a new one
        graph.Id = guid ?? Guid.NewGuid().ToString();

        // Parse events from <event> elements
        void ParseEvent(XElement eventElement, Event? parentEvent = null)
        {
            string? id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
            {
                Event newEvent = new Event(id)
                {
                    Type = (eventElement.Attribute("type")?.Value) switch
                    {
                        "nesting" or "subprocess" or "form" => EventType.Form,
                        "template" => EventType.Template,
                        _ => EventType.Task
                    },
                    Data = eventElement.Element("data")?.Value.EscapeGremlinString(),
                    Roles = eventElement.Element("custom")?.Element("roles")?.Elements("role").Select(r => r.Value).Where(role => !string.IsNullOrEmpty(role)).ToList() ?? new List<string>(),
                    ReadRoles = eventElement.Element("custom")?.Element("readRoles")?.Elements("readRole").Select(r => r.Value).Where(role => !string.IsNullOrEmpty(role)).ToList() ?? new List<string>(),
                    Description = eventElement.Element("custom")?.Element("eventDescription")?.Value.EscapeGremlinString() ?? "",
                    Parent = parentEvent
                };

                graph.Events.Add(id, newEvent);
                if (newEvent.Type == EventType.Template) // Add "Template" to your EventType enum
                {
                    var nestedGraphElement = eventElement.Element("template")?.Element("dcrgraph");
                    if (nestedGraphElement != null)
                    {
                        // Recursively parse the nested graph
                        var nestedDoc = new XDocument(nestedGraphElement);
                        var nestedGraph = ParseDCRGraphFromXml(nestedDoc);
                        newEvent.Template = nestedGraph; // Add Template property to Event class
                    }
                }

                // Add this event as a child of its parent, if applicable
                if (parentEvent != null)
                {
                    parentEvent.Children.Add(newEvent);
                }

                // Recursively parse child events
                foreach (var childEvent in eventElement.Elements("event"))
                {
                    ParseEvent(childEvent, newEvent);
                }
            }
        }
        foreach (var eventElement in doc.Element("dcrgraph")!.Element("specification")!.Element("resources")!.Element("events")!.Elements("event"))
        {
            ParseEvent(eventElement);
        }

        // Parse relationships from XML and add them to the graph
        void ParseRelationships(string elementName, RelationshipType type)
        {
            foreach (var relationshipElement in doc.Element("dcrgraph")!.Element("specification")!.Element("constraints")!.Element($"{elementName}s")?.Elements(elementName) ?? Enumerable.Empty<XElement>())
            {
                string? sourceId = relationshipElement.Attribute("sourceId")?.Value;
                string? targetId = relationshipElement.Attribute("targetId")?.Value;

                string expressing = ""; //dcr further differentiate id type
                switch (type)
                {
                    case RelationshipType.Update:
                        expressing = "valueExpressionId";
                        break;
                    case RelationshipType.Condition:
                    case RelationshipType.Milestone:
                        expressing = "link";
                        break;
                    default:
                        expressing = "expressionId";
                        break;
                }

                string? guardId = relationshipElement.Attribute(expressing)?.Value;

                if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(targetId))
                {
                    Relationship relationship = new Relationship(sourceId, targetId, type)
                    {
                        GuardExpression = graph.Expressions.FirstOrDefault(k => k.Key == guardId).Value
                    };
                    graph.Relationships.Add(relationship);
                }
            }
        }

        void ParseLabels()
        {
            foreach (var labelElement in doc.Element("dcrgraph")!.Element("specification")!.Element("resources")!.Element("labelMappings")!.Elements("labelMapping"))
            {
                string? id = labelElement.Element("labelMapping")?.Attribute("eventId")?.Value;
                string? label = labelElement.Element("labelMapping")?.Attribute("labelId")?.Value;

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                {
                    if (graph.Events.ContainsKey(id))
                    {
                        graph.Events[id].Label = label;
                    }
                }
            }
        }
        void ParseExpressions()
        {
            foreach (var expression in doc.Element("dcrgraph")!.Element("specification")!.Element("resources")!.Element("expressions")!.Elements("expression").Where(x => x.FirstNode == null)) //why? the last part
            {
                string? id = expression.Attribute("id")?.Value;
                string? expressionString = expression.Attribute("value")?.Value;

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(expressionString))
                {
                    DcrExpression newExpression = new DcrExpression { Id = id, Value = expressionString };
                    graph.Expressions.Add(id, newExpression);
                }
            }
        }

        void SetDefaults()
        {
            foreach (var variable in doc.Element("dcrgraph")!.Element("runtime")!.Element("marking")!.Element("globalStore")!.Elements("variable"))
            {
                string? id = variable.Attribute("id")?.Value;
                string? value = variable.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(value))
                {
                    if (graph.Events.ContainsKey(id) && graph.Events[id].Data == null)
                    {
                        graph.Events[id].Data = value;
                    }
                }
            }
        }

        ParseExpressions();

        // Parse all relationship types
        //there is a + "s" added to the name. be mindful of that
        ParseRelationships("response", RelationshipType.Response);
        ParseRelationships("condition", RelationshipType.Condition);
        ParseRelationships("include", RelationshipType.Include);
        ParseRelationships("exclusion", RelationshipType.Exclude);
        ParseRelationships("milestone", RelationshipType.Milestone);
        ParseRelationships("update", RelationshipType.Update);
        ParseRelationships("templateSpawn", RelationshipType.Spawn);

        ParseLabels();
        SetDefaults();

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

        }

        // Return the fully constructed DCRGraph
        return graph;
    }

}
