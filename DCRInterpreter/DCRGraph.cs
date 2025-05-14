using MessagePack;

[MessagePackObject]
public class DCRGraph
{
    [Key(0)]
    public string Id { get; set; } = "0";
    [Key(1)]
    public string Title { get; set; }
    [Key(2)]
    public List<Relationship> Relationships { get; set; } = new();
    [Key(3)]
    public Dictionary<string, Event> Events { get; set; } = new();

    [IgnoreMember]
    public HashSet<string> IncludedEvents => Events.Values.Where(e => e.Included).Select(e => e.Id).ToHashSet(); // I'm not manually updating this in IL
    [IgnoreMember]
    public HashSet<string> PendingEvents => Events.Values.Where(e => e.Pending).Select(e => e.Id).ToHashSet();
    [IgnoreMember]
    public HashSet<string> ExecutedEvents => Events.Values.Where(e=>e.Executed).Select(e=>e.Id).ToHashSet();
    [IgnoreMember]
    private StateUpdateCompiler UpdateCompiler = null!;
    [IgnoreMember]
    public IEnumerable<Event> Robots => Events.Where(e => e.Value.Included && e.Value.Pending && e.Value.IsRobot()).Select(e => e.Value);
    [IgnoreMember]
    public IEnumerable<Relationship> Conditions => Relationships.Where(r => r.Type is RelationshipType.Condition);
    [IgnoreMember]
    public IEnumerable<Relationship> Responses => Relationships.Where(r => r.Type is RelationshipType.Response);
    [IgnoreMember]
    public IEnumerable<Relationship> Inclusions => Relationships.Where(r => r.Type is RelationshipType.Include);
    [IgnoreMember]
    public IEnumerable<Relationship> Exclusions => Relationships.Where(r => r.Type is RelationshipType.Exclude);
    [IgnoreMember]
    public IEnumerable<Relationship> Milestones => Relationships.Where(r => r.Type is RelationshipType.Milestone);
    [Key(4)]
    public Dictionary<string, DcrExpression> Expressions { get; set; } = new Dictionary<string, DcrExpression>();
    [IgnoreMember]
    public Dictionary<string, object?> Values {
        get
        {
            var values = new Dictionary<string, object?>();
            foreach (var e in Events)
            {
                values.Add(e.Key, e.Value.Data);
            }
            return values;
        }
    }

    public DCRGraph(string title)
    {
        this.Title = title;
    }

    public DCRGraph(DCRGraph other)
    {
        this.Title = other.Title;
        this.Id = other.Id;
        this.Relationships = new(other.Relationships);
        this.Expressions = new(other.Expressions);
        this.Events = new(other.Events);
        Initialize();
    }
    public void Initialize()
    {
        UpdateCompiler = new StateUpdateCompiler(this);

        // Precompile logic for each event
        foreach (var e in Events.Values)
        {
            e.CompiledLogic = UpdateCompiler.GenerateLogicForEvent(e.Id);
        }
    }

    // public bool CanExecuteEvent(string eventId)
    // {
    //     var eventToExecute = Events.FirstOrDefault(e => e.Key == eventId).Value;
    //     if (eventToExecute == null || !eventToExecute.Included)
    //         return false;

    //     // Check conditions
    //     foreach (var condition in Conditions.Where(r => r.TargetId == eventId))
    //     {
    //         if (!ExecutedEvents.Contains(condition.SourceId))
    //             return false;
    //     }

    //     // Check milestone constraints
    //     foreach (var milestone in Milestones.Where(r => r.TargetId == eventId))
    //     {
    //         if (!ExecutedEvents.Contains(milestone.SourceId))
    //             return false;
    //     }

    //     if(eventToExecute.Parent != null)
    //     {
    //         return CanExecuteEvent(eventToExecute.Parent.Id);
    //     }

    //     return true;
    // }

    public static bool EvaluateExpression(string expressionValue, Dictionary<string, object?>? variables)
    {
        var expression = new NCalc.Expression(expressionValue, NCalc.ExpressionOptions.AllowNullOrEmptyExpressions);
        Dictionary<string, object?>? paramList = new();
        if(variables != null)
        foreach (var item in variables)
        {
            if (item.Value == null) 
            {
                paramList.Add(item.Key, null);
                continue;
            };
            if (int.TryParse(item.Value.ToString(), out var i)) 
            {
                paramList.Add(item.Key,i);
                continue;
            }
            if (bool.TryParse(item.Value.ToString(), out var b))
            {
                paramList.Add(item.Key, b);
                continue;
            }
            if (float.TryParse(item.Value.ToString(), out var f))
            {
                paramList.Add(item.Key, f);
                continue;
            }
            if(DateTimeOffset.TryParse(item.Value.ToString(), out var dt))
            {
                paramList.Add(item.Key, dt);
                continue;
            }
            paramList.Add(item.Key, item.Value);

        }
        expression.Parameters = paramList;
        var x = (bool)expression.Evaluate();
        return x;
    }

    public List<string> ExecuteEvent(string eventId, string? data = null)
    {
        if (!Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");

        var e = Events[eventId];
        // Execute precompiled logic using DynamicMethod
        return e.CompiledLogic(this, data);
    }
}
[MessagePackObject]
public class Relationship
{
    [Key(0)]
    public string SourceId { get; set; }
    [Key(1)]
    public string TargetId { get; set; }
    [Key(2)]
    public RelationshipType Type { get;  set; }
    [IgnoreMember]
    public string? GuardExpressionId => GuardExpression?.Id;
    [Key(3)]
    public DcrExpression? GuardExpression { get; set; }

    public Relationship( string source, string target, RelationshipType relationshipType)
    {
        SourceId = source;
        TargetId = target;
        Type = relationshipType;
    }
}
[MessagePackObject]
public class DcrExpression
{
    [Key(0)]
    public string Id { get; set; } // Unique identifier for the expression
    [Key(1)]
    public string Value { get; set; } // The actual expression (e.g., "count(global) > 1")

    public bool Evaluate(DCRGraph graph)
    {
        var values = graph.Values.Where(v => Value.Contains(v.Key)).ToDictionary(v => v.Key, v => v.Value);
        return DCRGraph.EvaluateExpression(Value, values);
    }
}


public enum RelationshipType
{
    Condition,
    Response,
    Include,
    Exclude,
    Milestone,
    Update
}
