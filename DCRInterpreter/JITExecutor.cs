public class JITExecutor
{
    private DCRGraph Graph;

    public JITExecutor(DCRGraph graph)
    {
        Graph = graph;
    }

    // Execute an event using its precompiled DynamicMethod
    public void ExecuteEvent(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");

        var e = Graph.Events[eventId];

        if (!IsEventEnabled(eventId))
            throw new InvalidOperationException($"Event {eventId} is not enabled.");

        // Mark the event as executed
        e.Executed = true;

        // Invoke the precompiled logic for this event
        e.CompiledLogic(Graph);
    }

    // Check if an event is enabled
    public bool IsEventEnabled(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            return false;

        var e = Graph.Events[eventId];

        // An event must be included to be enabled
        if (!e.Included)
            return false;

        // Check conditions: all conditions must be satisfied
        foreach (var condition in Graph.Conditions)
        {
            if (condition.TargetId == eventId && Graph.Events[condition.SourceId].Included && !Graph.Events[condition.SourceId].Executed)
                return false;
        }

        return true;
    }
}
