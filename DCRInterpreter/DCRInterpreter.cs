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
}
