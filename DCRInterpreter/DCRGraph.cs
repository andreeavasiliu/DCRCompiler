public class DCRGraph
{
    public Dictionary<string, Event> Events { get; set; }
    public List<(string SourceId, string TargetId)> Responses { get; set; }
    public List<(string SourceId, string TargetId)> Conditions { get; set; }
    public List<(string SourceId, string TargetId)> Inclusions { get; set; }
    public List<(string SourceId, string TargetId)> Exclusions { get; set; }
    public List<(string SourceId, string TargetId)> Milestones { get; set; }

    private JITCodeGenerator jitGenerator;

    public DCRGraph()
    {
        Events = new Dictionary<string, Event>();
        Responses = new List<(string, string)>();
        Conditions = new List<(string, string)>();
        Inclusions = new List<(string, string)>();
        Exclusions = new List<(string, string)>();
    }

    public void Initialize()
    {
        jitGenerator = new JITCodeGenerator(this);

        // Precompile logic for each event
        foreach (var e in Events.Values)
        {
            e.CompiledLogic = jitGenerator.GenerateLogicForEvent(e.Id);
        }
    }
}
