using System;

public class Event
{
    public string Id { get; set; }
    public bool Executed { get; set; }
    public bool Included { get; set; }
    public bool Pending { get; set; }

    public Event(string id)
    {
        Id = id;
        Executed = false;
        Included = false;
        Pending = false;
    }
}
