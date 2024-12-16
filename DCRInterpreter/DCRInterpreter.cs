public class DCRInterpreter
{
    public DCRGraph Graph;

    public DCRInterpreter(DCRGraph graph)
    {
        Graph = graph;
    }

    // Check if an event is enabled
    public bool IsEventEnabled(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");

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

        // Check milestones: blocked if any milestone source is pending
        foreach (var milestone in Graph.Milestones)
        {
            if (milestone.TargetId == eventId && Graph.Events[milestone.SourceId].Included && Graph.Events[milestone.SourceId].Pending)
                return false;
        }

        return true;
    }

    // Execute an event and update states
    public void ExecuteEvent(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");

        var e = Graph.Events[eventId];

        if (!IsEventEnabled(eventId))
            throw new InvalidOperationException($"Event {eventId} is not enabled.");

        // Mark the event as executed
        e.Executed = true;

        // Process response rules: mark target events as pending
        foreach (var response in Graph.Responses)
        {
            if (response.SourceId == eventId)
            {
                var targetEvent = Graph.Events[response.TargetId];
                targetEvent.Pending = true;
            }
        }

        // Process inclusion rules: include target events
        foreach (var inclusion in Graph.Inclusions)
        {
            if (inclusion.SourceId == eventId)
            {
                var targetEvent = Graph.Events[inclusion.TargetId];
                targetEvent.Included = true;
            }
        }

        // Process exclusion rules: exclude target events
        foreach (var exclusion in Graph.Exclusions)
        {
            if (exclusion.SourceId == eventId)
            {
                var targetEvent = Graph.Events[exclusion.TargetId];
                targetEvent.Included = false;
            }
        }

        // Clear pending state for this event after execution
        e.Pending = false;
    }
}
