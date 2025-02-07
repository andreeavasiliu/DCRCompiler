public class DCRGraph
{
    public string Id { get; set; } = "0";
    public string Title { get; set; }

    public List<Relationship> Relationships { get; set; } = new();

    public Dictionary<string, Event> Events { get; set; } = new();
    
    public HashSet<string> IncludedEvents { get; set; } = new();
    public HashSet<string> PendingEvents { get; set; } = new();
    public HashSet<string> ExecutedEvents { get; set; } = new();

    private StateUpdateCompiler UpdateCompiler = null!;

    public List<Relationship> Conditions => Relationships.Where(r => r.Type is RelationshipType.Condition).ToList();
    public List<Relationship> Responses => Relationships.Where(r => r.Type is RelationshipType.Response).ToList();
    public List<Relationship> Inclusions => Relationships.Where(r => r.Type is RelationshipType.Include).ToList();
    public List<Relationship> Exclusions => Relationships.Where(r => r.Type is RelationshipType.Exclude).ToList();
    public List<Relationship> Milestones => Relationships.Where(r => r.Type is RelationshipType.Milestone).ToList();
    public List<DcrExpression> Expressions { get; set; } = new List<DcrExpression>();
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

    public void Initialize()
    {
        UpdateCompiler = new StateUpdateCompiler(this);

        // Precompile logic for each event
        foreach (var e in Events.Values)
        {
            e.CompiledLogic = UpdateCompiler.GenerateLogicForEvent(e.Id);
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
    public bool CanExecuteEvent(string eventId)
    {
        var eventToExecute = Events.FirstOrDefault(e => e.Key == eventId).Value;
        if (eventToExecute == null || !IncludedEvents.Contains(eventId))
            return false;

        // Check conditions
        foreach (var condition in Relationships.Where(r => r.Type == RelationshipType.Condition && r.TargetId == eventId))
        {
            if (!ExecutedEvents.Contains(condition.SourceId))
                return false;
        }

        // Check milestone constraints
        foreach (var milestone in Relationships.Where(r => r.Type == RelationshipType.Milestone && r.TargetId == eventId))
        {
            if (!ExecutedEvents.Contains(milestone.SourceId))
                return false;
        }
        foreach (var relation in Relationships.Where(r => r.TargetId == eventId && !string.IsNullOrEmpty(r.GuardExpressionId)))
        {
            var expression = Expressions.FirstOrDefault(e => e.Id == relation.GuardExpressionId);
             if (expression != null)
            {
                var variables = Values.Where(v => expression.Value.Contains(v.Key)).ToDictionary(v => v.Key, v => v.Value);
                if (!EvaluateExpression(expression.Value, variables))
                    return false;
            }
        }


        return true;
    }

    private bool EvaluateExpression(string expressionValue, Dictionary<string, object?>? variables)
    {
        var expression = new NCalc.Expression(expressionValue);

        foreach (var variable in variables)
        {
            expression.Parameters[variable.Key] = variable.Value;
        }

        return (bool)expression.Evaluate();
    }
}

public class Relationship
{
    public string SourceId { get; private set; }
    public string TargetId { get; private set; }
    public RelationshipType Type { get; private set; }
    public string? GuardExpressionId { get; set; }
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
}


public enum RelationshipType
{
    Condition,
    Response,
    Include,
    Exclude,
    Milestone
}
