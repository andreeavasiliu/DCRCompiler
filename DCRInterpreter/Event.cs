using System;

public class Event
{
    public string Id { get; set; }
    public bool Executed { get; set; }
    public bool Included { get; set; }
    public bool Pending { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public EventType Type { get; set; } = EventType.Task;
    public object? Data { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> ReadRoles { get; set; } = new();
    public Action<DCRGraph> CompiledLogic { get; set; } = null!;
    public List<Event> Children { get; set; } = new();
    public Event? Parent { get; set; }
    public Event(string id)
    {
        Id = id;
        Executed = false;
        Included = false;
        Pending = false;
    }
}

public enum EventType
{
    Task,
    Form
}
