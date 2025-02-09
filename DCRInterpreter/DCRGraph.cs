﻿public class DCRGraph
{
    public string Id { get; set; } = "0";
    public string Title { get; set; }

    public List<Relationship> Relationships { get; set; } = new();

    public Dictionary<string, Event> Events { get; set; } = new();
    
    public HashSet<string> IncludedEvents => Events.Values.Where(e => e.Included).Select(e => e.Id).ToHashSet(); // I'm not manually updating this in IL
    public HashSet<string> PendingEvents => Events.Values.Where(e => e.Pending).Select(e => e.Id).ToHashSet();
    public HashSet<string> ExecutedEvents => Events.Values.Where(e=>e.Executed).Select(e=>e.Id).ToHashSet();

    private StateUpdateCompiler UpdateCompiler = null!;
    public IEnumerable<Event> Robots => Events.Where(e => e.Value.Included && e.Value.Pending && e.Value.IsRobot()).Select(e => e.Value);
    public IEnumerable<Relationship> Conditions => Relationships.Where(r => r.Type is RelationshipType.Condition);
    public IEnumerable<Relationship> Responses => Relationships.Where(r => r.Type is RelationshipType.Response);
    public IEnumerable<Relationship> Inclusions => Relationships.Where(r => r.Type is RelationshipType.Include);
    public IEnumerable<Relationship> Exclusions => Relationships.Where(r => r.Type is RelationshipType.Exclude);
    public IEnumerable<Relationship> Milestones => Relationships.Where(r => r.Type is RelationshipType.Milestone);
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
        if (eventToExecute == null || !eventToExecute.Included)
            return false;

        // Check conditions
        foreach (var condition in Conditions.Where(r => r.TargetId == eventId))
        {
            if (!ExecutedEvents.Contains(condition.SourceId))
                return false;
        }

        // Check milestone constraints
        foreach (var milestone in Milestones.Where(r => r.TargetId == eventId))
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

        if (!IsEventEnabled(eventId))
            return new List<string>();
        if (!CanExecuteEvent(eventId))
            return new List<string>();
        // Execute precompiled logic using DynamicMethod
        return e.CompiledLogic(this, data);
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
