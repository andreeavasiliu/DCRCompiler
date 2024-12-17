public class DCRGraph
{
    public Dictionary<string, Event> Events { get; set; }
    public List<(string SourceId, string TargetId)> Responses { get; set; }
    public List<(string SourceId, string TargetId)> Conditions { get; set; }
    public List<(string SourceId, string TargetId)> Inclusions { get; set; }
    public List<(string SourceId, string TargetId)> Exclusions { get; set; }
    public List<(string SourceId, string TargetId)> Milestones { get; set; }

    private StateUpdateCompiler jitGenerator;

    public DCRGraph()
    {
        Events = new Dictionary<string, Event>();
        Responses = new List<(string, string)>();
        Conditions = new List<(string, string)>();
        Inclusions = new List<(string, string)>();
        Exclusions = new List<(string, string)>();
        Milestones = new List<(string, string)>();

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
