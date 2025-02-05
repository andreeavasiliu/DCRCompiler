public class DCRGraph
{
    public Guid Id { get; set; }
    public string Title { get; set; }

    public List<Relationship> Relationships { get; set; } = new();

    public Dictionary<string, Event> Events { get; set; } = new();
    
    public HashSet<string> IncludedEvents { get; set; } = new();
    public HashSet<string> PendingEvents { get; set; } = new();
    public HashSet<string> ExecutedEvents { get; set; } = new();

    private StateUpdateCompiler jitGenerator = null!;

    public List<Relationship> Conditions => Relationships.Where(r => r.Type is RelationshipType.Condition).ToList();
    public List<Relationship> Responses => Relationships.Where(r => r.Type is RelationshipType.Response).ToList();
    public List<Relationship> Inclusions => Relationships.Where(r => r.Type is RelationshipType.Include).ToList();
    public List<Relationship> Exclusions => Relationships.Where(r => r.Type is RelationshipType.Exclude).ToList();
    public List<Relationship> Milestones => Relationships.Where(r => r.Type is RelationshipType.Milestone).ToList();

    public DCRGraph(string title)
    {
        this.Title = title;
    }

    public void Initialize()
    {
        jitGenerator = new StateUpdateCompiler(this);

        // Precompile logic for each event
        foreach (var e in Events.Values)
        {
            e.CompiledLogic = jitGenerator.GenerateLogicForEvent(e.Id);
        }
    }
     public bool IsEventEnabled(string eventId)
    {
        if (!Events.ContainsKey(eventId))
            return false;

        var e = Events[eventId];

        // An event must be included to be enabled
        if (!e.Included)
            return false;

        // Check conditions: all conditions must be satisfied
        foreach (var condition in Conditions)
        {
            if (condition.TargetId == eventId && Events[condition.SourceId].Included && !Events[condition.SourceId].Executed)
                return false;
        }

        return true;
    }
}

public class Relationship
{
    public string SourceId { get; private set; }
    public string TargetId { get; private set; }
    public RelationshipType Type { get; private set; }

    public Relationship( string source, string target, RelationshipType relationshipType)
    {
        SourceId = source;
        TargetId = target;
        Type = relationshipType;
    }
}

public enum RelationshipType
{
    Condition,
    Response,
    Include,
    Exclude,
    Milestone
}
