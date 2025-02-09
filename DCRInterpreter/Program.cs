using DCR.Workflow;
using System.Diagnostics;
using System.Xml.Linq;

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
        var model = runtime.Parse(doc);

        var maxmilisec = 1000; //60000ms = 60 sec
        DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(doc);
        // Initialize and precompile logic for events
        graph.Initialize();

        BenchRuntime(runtime, model, maxmilisec);
        BenchInterp(graph, maxmilisec);
        Console.ReadKey();

    }

    static void BenchRuntime(Runtime runtime, Model original, int maxtime)
    {
        Stopwatch stopwatch = new Stopwatch();

        int loop = 0;
        int executedCount = 0;
        
        while (stopwatch.ElapsedMilliseconds <= maxtime)
        {
            
            var model = new Model(original);

            stopwatch.Start();

            runtime.Execute(model, model["employee_name"], DCR.Core.Data.value.NewString("Jim Bean"));
            runtime.Execute(model, model["employee_email"], DCR.Core.Data.value.NewString("jim@bean.org"));
            runtime.Execute(model, model["start_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.MinValue));
            runtime.Execute(model, model["end_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.Now));
            runtime.Execute(model, model["reason"], DCR.Core.Data.value.NewString("Tired"));
            runtime.Execute(model, model["vacation_request"]);
            runtime.Execute(model, model["approved"], DCR.Core.Data.value.NewBool(true));
            runtime.Execute(model, model["review_request"]);
            runtime.Execute(model, model["submit_to_hr"]);

            stopwatch.Stop();
            executedCount += 9;
            loop++;
        }

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"RunTime Workflow executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
    }

    static void BenchInterp(DCRGraph original, int maxtime)
    {
        Stopwatch stopwatch2 = new Stopwatch();
        int loop = 0;
        int executedCount = 0;
        while (stopwatch2.ElapsedMilliseconds <= maxtime)
        {
            DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(XDocument.Load("DCR-interpreter.xml")); //esta stupido
            graph.Initialize();
            //var graph = new DCRGraph(original); //its still a reference not a copy...
            stopwatch2.Start();

            graph.ExecuteEvent("employee_name", "Jim Bean");
            graph.ExecuteEvent("employee_email", "jim@bean.org");
            graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            graph.ExecuteEvent("reason", "Tired");
            graph.ExecuteEvent("vacation_request");
            graph.ExecuteEvent("approved", "true");
            graph.ExecuteEvent("review_request");
            graph.ExecuteEvent("submit_to_hr");

            
            stopwatch2.Stop();
            executedCount += 9;
            loop++;
        }

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch2.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"RunTime Interpreted executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
    }

}