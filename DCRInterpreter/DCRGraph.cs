using System.Xml.Linq;

public class DCRGraph
{
    public Dictionary<string, Event> Events { get; set; }
    public List<(string SourceId, string TargetId)> Responses { get; set; }
    public List<(string SourceId, string TargetId)> Conditions { get; set; }
    public List<(string SourceId, string TargetId)> Inclusions { get; set; }
    public List<(string SourceId, string TargetId)> Exclusions { get; set; }
    public List<(string SourceId, string TargetId)> Milestones { get; set; } // New for milestone rules

    public DCRGraph()
    {
        Events = new Dictionary<string, Event>();
        Responses = new List<(string, string)>();
        Conditions = new List<(string, string)>();
        Inclusions = new List<(string, string)>();
        Exclusions = new List<(string, string)>();
        Milestones = new List<(string, string)>();
    }
    public void ParseFromXml(string xmlContent)
    {
        XDocument doc = XDocument.Parse(xmlContent);

        // Initialize event states from <marking>
        var executedEvents = new HashSet<string>();
        var includedEvents = new HashSet<string>();
        var pendingEvents = new HashSet<string>();

        foreach (var eventElement in doc.Descendants("executed").Descendants("event"))
        {
            string id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                executedEvents.Add(id);
        }

        foreach (var eventElement in doc.Descendants("included").Descendants("event"))
        {
            string id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                includedEvents.Add(id);
        }

        foreach (var eventElement in doc.Descendants("pendingResponses").Descendants("event"))
        {
            string id = eventElement.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id))
                pendingEvents.Add(id);
        }

        // Create events and set their initial states
        foreach (var id in includedEvents)
        {
            if (!Events.ContainsKey(id))
                Events[id] = new Event(id);
            Events[id].Included = true;
        }

        foreach (var id in executedEvents)
        {
            if (!Events.ContainsKey(id))
                Events[id] = new Event(id);
            Events[id].Executed = true;
        }

        foreach (var id in pendingEvents)
        {
            if (!Events.ContainsKey(id))
                Events[id] = new Event(id);
            Events[id].Pending = true;
        }
    }
}
