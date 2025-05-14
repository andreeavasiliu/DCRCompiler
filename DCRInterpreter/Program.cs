using DCR.Workflow;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.Diagnostics;
using System.Numerics;
using System.Xml.Linq;
using static DCR.Core.Generator;

class Program
{

    static void Main(string[] args)
    {

        string xmlFilePath = "DCR-interpreter.xml";
        //string xmlFilePath = "the_ultimate_test.xml";

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: File '{xmlFilePath}' not found.");
            return;
        }
        DoBenchmark(XDocument.Load(xmlFilePath));
        //DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(XDocument.Load(xmlFilePath));
    }

    static void DoBenchmark(XDocument doc)
    {
        var runtime = DCR.Workflow.Runtime.Create(builder =>
        {
            builder.WithOptions(options =>
                options.UpdateModelLog = true
            );
        });

        var maxmilisec = 10000; //60000ms = 60 sec

        //BenchRuntime(runtime, doc, maxmilisec);
        BenchInterp(doc, maxmilisec);
        BenchBinary(doc, maxmilisec);
        Console.ReadKey();

    }


    public class EventFormatter : IMessagePackFormatter<Event?>
    {
        public void Serialize(ref MessagePackWriter writer, Event? value, MessagePackSerializerOptions options)
        {
            // TODO: Consider handling null values gracefully (writer.WriteNil())
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            // Write an array header to store the values in order
            writer.WriteArrayHeader(10);  // We have 10 fields to serialize

            // Serialize the fields: Id, Executed, Included, Pending, Label, Description, etc.
            writer.Write(value.Id);
            writer.Write(value.Executed);
            writer.Write(value.Included);
            writer.Write(value.Pending);
            writer.Write(value.Label);
            writer.Write(value.Description);
            writer.Write((int)value.Type); // Serialize enum as integer
            options.Resolver.GetFormatterWithVerify<object?>().Serialize(ref writer, value.Data, options);
            options.Resolver.GetFormatterWithVerify<List<string>>().Serialize(ref writer, value.Roles, options);
            options.Resolver.GetFormatterWithVerify<List<string>>().Serialize(ref writer, value.ReadRoles, options);
            var kids = value.Children.Select(x => x.Id).ToList();
            options.Resolver.GetFormatterWithVerify<List<string>>().Serialize(ref writer, kids, options);
        }

        public Event? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);
            if (reader.IsNil)
            {
                return null;
            }
            reader.ReadArrayHeader(); // Read the array header

            // Read the fields in the correct order as serialized
            var id = reader.ReadString();
            var executed = reader.ReadBoolean();
            var included = reader.ReadBoolean();
            var pending = reader.ReadBoolean();
            var label = reader.ReadString();
            var description = reader.ReadString();
            var eventType = (EventType)reader.ReadInt32();
            var data = options.Resolver.GetFormatterWithVerify<object?>().Deserialize(ref reader, options);
            var roles = options.Resolver.GetFormatterWithVerify<List<string>>().Deserialize(ref reader, options);
            var readRoles = options.Resolver.GetFormatterWithVerify<List<string>>().Deserialize(ref reader, options);
            var kIds = options.Resolver.GetFormatterWithVerify<List<string>>().Deserialize(ref reader, options);

            // Create the Event object
            var ev = new Event(id!)
            {
                Executed = executed,
                Included = included,
                Pending = pending,
                Label = label,
                Description = description,
                Type = eventType,
                Data = data,
                Roles = roles,
                ReadRoles = readRoles,
                kIds = kIds,
            };

            return ev;
        }
    }

    public class CustomResolver : IFormatterResolver
    {
        public static readonly CustomResolver Instance = new();

        private static readonly IMessagePackFormatter<Event?> eventFormatter = new EventFormatter();

        private static readonly IFormatterResolver fallbackResolver =
            MessagePack.Resolvers.CompositeResolver.Create(
                // Register your formatter here only once
                new IMessagePackFormatter[] { eventFormatter },

                // Then use standard for the rest
                new IFormatterResolver[] { MessagePack.Resolvers.StandardResolver.Instance }
            );

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            // Just delegate. No manual checks.
            return fallbackResolver.GetFormatter<T>();
        }
    }

    static void RemakeTree(DCRGraph grph)
    {
        foreach(var evt in grph.Events)
        {
            foreach(var kid in evt.Value.kIds)
            {
                evt.Value.Children.Add(grph.Events[kid]);
                grph.Events[kid].Parent = evt.Value;
            }
        }
    }
    static void BenchBinary(XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;
        int executedCount = 0;


        var options = MessagePackSerializerOptions.Standard.WithResolver(CustomResolver.Instance);

        while (execute.ElapsedMilliseconds <= maxtime)
        {
            DCRGraph pregraph = DCRInterpreter.ParseDCRGraphFromXml(original);
            foreach (var kp in pregraph.Events)
            {
                var binEvt = MessagePackSerializer.Serialize<Event>(kp.Value, options);
                var Evt = MessagePackSerializer.Deserialize<Event>(binEvt, options);
            }
            var binDict = MessagePackSerializer.Serialize(pregraph.Events, options);
            var Dict = MessagePackSerializer.Deserialize<Dictionary<string, Event>>(binDict, options);


            var bin = MessagePackSerializer.Serialize<DCRGraph>(pregraph, options);
            parse.Start();
            var graph = MessagePackSerializer.Deserialize<DCRGraph>(bin,options);
            RemakeTree(graph);
            graph.Initialize();
            parse.Stop();

            execute.Start();

            graph.ExecuteEvent("employee_name", "Jim Bean");
            graph.ExecuteEvent("employee_email", "jim@bean.org");
            graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            graph.ExecuteEvent("reason", "Tired");
            graph.ExecuteEvent("vacation_request");
            graph.ExecuteEvent("approved", "true");
            graph.ExecuteEvent("review_request");
            graph.ExecuteEvent("submit_to_hr");


            execute.Stop();
            executedCount += 9;
            loop++;
        }
        // Format and display the TimeSpan value
        TimeSpan ts = execute.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        TimeSpan ts2 = parse.Elapsed;
        string elapsedParseTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts2.Hours, ts2.Minutes, ts2.Seconds, ts2.Milliseconds / 10);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"RunTime Interpreted executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
        Console.WriteLine($"            *parsing time was not included in this time");
        Console.WriteLine();
        Console.WriteLine($"Parsing with Binary {loop} times took {elapsedParseTime} to finish");
        Console.WriteLine();
    }

    static void BenchRuntime(Runtime runtime, XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;
        int executedCount = 0;

        while (execute.ElapsedMilliseconds <= maxtime)
        {
            parse.Start();
            var model = runtime.Parse(original);
            parse.Stop();

            execute.Start();

            runtime.Execute(model, model["employee_name"], DCR.Core.Data.value.NewString("Jim Bean"));
            runtime.Execute(model, model["employee_email"], DCR.Core.Data.value.NewString("jim@bean.org"));
            runtime.Execute(model, model["start_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.MinValue));
            runtime.Execute(model, model["end_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.Now));
            runtime.Execute(model, model["reason"], DCR.Core.Data.value.NewString("Tired"));
            runtime.Execute(model, model["vacation_request"]);
            runtime.Execute(model, model["approved"], DCR.Core.Data.value.NewBool(true));
            runtime.Execute(model, model["review_request"]);
            runtime.Execute(model, model["submit_to_hr"]);

            execute.Stop();
            executedCount += 9;
            loop++;
        }


        // Format and display the TimeSpan value
        TimeSpan ts = execute.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        TimeSpan ts2 = parse.Elapsed;
        string elapsedParseTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts2.Hours, ts2.Minutes, ts2.Seconds, ts2.Milliseconds / 10);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"RunTime Workflow executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
        Console.WriteLine($"            *parsing time was not included in this time");
        Console.WriteLine();
        Console.WriteLine($"Parsing with Workflow {loop} times took {elapsedParseTime} to finish");
        Console.WriteLine();
    }

    static void BenchInterp(XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;
        int executedCount = 0;
        while (execute.ElapsedMilliseconds <= maxtime)
        {
            parse.Start();
            DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(original);
            graph.Initialize();
            parse.Stop();

            execute.Start();

            graph.ExecuteEvent("employee_name", "Jim Bean");
            graph.ExecuteEvent("employee_email", "jim@bean.org");
            graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            graph.ExecuteEvent("reason", "Tired");
            graph.ExecuteEvent("vacation_request");
            graph.ExecuteEvent("approved", "true");
            graph.ExecuteEvent("review_request");
            graph.ExecuteEvent("submit_to_hr");


            execute.Stop();
            executedCount += 9;
            loop++;
        }
        // Format and display the TimeSpan value
        TimeSpan ts = execute.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        TimeSpan ts2 = parse.Elapsed;
        string elapsedParseTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts2.Hours, ts2.Minutes, ts2.Seconds, ts2.Milliseconds / 10);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"RunTime Interpreted executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
        Console.WriteLine($"            *parsing time was not included in this time");
        Console.WriteLine();
        Console.WriteLine($"Parsing with Interpreter {loop} times took {elapsedParseTime} to finish");
        Console.WriteLine();
    }

}