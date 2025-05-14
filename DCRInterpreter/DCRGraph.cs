public class DCRGraph
{
    public string Id { get; set; } = "0";
    public string Title { get; set; }

    public List<Relationship> Relationships { get; set; } = new();

    public Dictionary<string, Event> Events { get; set; } = new();

    public HashSet<string> IncludedEvents => Events.Values.Where(e => e.Included).Select(e => e.Id).ToHashSet(); // I'm not manually updating this in IL
    public HashSet<string> PendingEvents => Events.Values.Where(e => e.Pending).Select(e => e.Id).ToHashSet();
    public HashSet<string> ExecutedEvents => Events.Values.Where(e => e.Executed).Select(e => e.Id).ToHashSet();

    private StateUpdateCompiler UpdateCompiler = null!;
    public IEnumerable<Event> Robots => Events.Where(e => e.Value.Included && e.Value.Pending && e.Value.IsRobot()).Select(e => e.Value);
    public IEnumerable<Relationship> Conditions => Relationships.Where(r => r.Type is RelationshipType.Condition);
    public IEnumerable<Relationship> Responses => Relationships.Where(r => r.Type is RelationshipType.Response);
    public IEnumerable<Relationship> Inclusions => Relationships.Where(r => r.Type is RelationshipType.Include);
    public IEnumerable<Relationship> Exclusions => Relationships.Where(r => r.Type is RelationshipType.Exclude);
    public IEnumerable<Relationship> Milestones => Relationships.Where(r => r.Type is RelationshipType.Milestone);
    public IEnumerable<Relationship> Updates => Relationships.Where(r => r.Type is RelationshipType.Update);
    public IEnumerable<Relationship> Spawns => Relationships.Where(r => r.Type is RelationshipType.Spawn);
    public Dictionary<string, DcrExpression> Expressions { get; set; } = new Dictionary<string, DcrExpression>();
    public Dictionary<string, DCRGraph> Templates => Events.Where(e => e.Value.Template != null)
        .ToDictionary(e => e.Key, e => e.Value.Template!);
    public Dictionary<int, List<string>> SpawnedInstances { get; set; } = new(); // InstanceID -> [EventIDs]
    public Dictionary<string, object?> Values
    {
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


    public static bool EvaluateExpression(string expressionValue, Dictionary<string, object?>? variables)
    {
        Dictionary<string, object?>? paramList = new();
        if (variables != null)
            foreach (var item in variables)
            {
                if (item.Value == null)
                {
                    paramList.Add(item.Key, null);
                    continue;
                }
                ;
                if (int.TryParse(item.Value.ToString(), out var i))
                {
                    paramList.Add(item.Key, i);
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
                if (DateTimeOffset.TryParse(item.Value.ToString(), out var dt))
                {
                    paramList.Add(item.Key, dt);
                    continue;
                }
                paramList.Add(item.Key, item.Value);

            }
        Dictionary<string, object?>? newParamList = new();
        foreach (var item in paramList)
        {
            if (item.Key.Contains(":"))
            {
                var newKey = item.Key.Split(':')[1];
                newParamList.Add(newKey, item.Value);
                expressionValue = expressionValue.Replace(item.Key, newKey);
            }
            newParamList.Add(item.Key, item.Value);
        }
        var expression = new NCalc.Expression(expressionValue, NCalc.ExpressionOptions.AllowNullOrEmptyExpressions);
        expression.Parameters = newParamList;
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
    public void AddSpawnedInstance(string templateId)
    {
        var instanceId = SpawnedInstances.Count;
        SpawnedInstances[instanceId] = new List<string>();
        // Copy initial state from template
        var template = Templates[templateId];
        foreach (var e in template.Events.Values)
        {
            var newID = $"{templateId}:{instanceId}:{e.Id}";
            var newEvent = new Event(newID)
            {
                InstanceId = instanceId,
                Label = e.Label,
                Description = e.Description,
                Type = e.Type,
                Data = e.Data,
                Roles = e.Roles.ToList(),
                ReadRoles = e.ReadRoles.ToList(),
                Included = e.Included,
                Pending = e.Pending,
                Executed = e.Executed,
                Children = e.Children.ToList(),
                Parent = e.Parent
            };
            Events[newID] = newEvent;
            SpawnedInstances[instanceId].Add(newID);
        }
        // Copy relationships from template
        foreach (var r in template.Relationships)
        {
            var targetEvent = Events.FirstOrDefault(e => e.Value.Id == r.TargetId);
            var targetId = !targetEvent.Equals(default(KeyValuePair<string, Event>)) ? targetEvent.Key : $"{templateId}:{instanceId}:{r.TargetId}";
            var sourceEvent = Events.FirstOrDefault(e => e.Value.Id == r.SourceId);
            var sourceId = !sourceEvent.Equals(default(KeyValuePair<string, Event>)) ? sourceEvent.Key : $"{templateId}:{instanceId}:{r.SourceId}";
            var newRelationship = new Relationship(
                targetId,
                sourceId,
                r.Type
            )
            {
                GuardExpression = r.GuardExpression
            };
            Relationships.Add(newRelationship);
        }
    }

}

public class Relationship
{
    public string SourceId { get; private set; }
    public string TargetId { get; private set; }
    public RelationshipType Type { get; private set; }
    public string? GuardExpressionId => GuardExpression?.Id;
    public DcrExpression? GuardExpression { get; set; }

    public Relationship( string source, string target, RelationshipType relationshipType)
    {
        SourceId = source;
        TargetId = target;
        Type = relationshipType;
    }
}
public class DcrExpression
{
    public string Id { get; set; } // Unique identifier for the expression
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
    Update,
    Spawn
}
