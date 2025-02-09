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

        var loop = 50;
        DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(doc);
        // Initialize and precompile logic for events
        graph.Initialize();
        DCRInterpreter interpreter = new DCRInterpreter(graph);

        BenchRuntime(runtime, model, loop);
        BenchInterp(interpreter, graph, loop);
        Console.ReadKey();

    }

    static void BenchRuntime(Runtime runtime, Model original, int loop)
    {
        Stopwatch stopwatch = new Stopwatch();

        int executedCount = 0;
        
        // Start the stopwatch
        stopwatch.Start();
        for (int i = 0; i < loop; i++)
        {
            var model = new Model(original);

            runtime.Execute(model, model["employee_name"], DCR.Core.Data.value.NewString("Jim Bean"));
            runtime.Execute(model, model["employee_email"], DCR.Core.Data.value.NewString("jim@bean.org"));
            runtime.Execute(model, model["start_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.Now));
            runtime.Execute(model, model["end_date"], DCR.Core.Data.value.NewDate(DateTimeOffset.Now));
            runtime.Execute(model, model["reason"], DCR.Core.Data.value.NewString("Tired"));
            runtime.Execute(model, model["vacation_request"]);
            runtime.Execute(model, model["approved"], DCR.Core.Data.value.NewBool(true));
            runtime.Execute(model, model["review_request"]);
            runtime.Execute(model, model["submit_to_hr"]);
        }
        // Stop the stopwatch
        stopwatch.Stop();

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.WriteLine($"RunTime Workflow {executedCount} executions:" + elapsedTime);
    }

    static void BenchInterp(DCRInterpreter interpreter, DCRGraph original, int loop)
    {
        Stopwatch stopwatch2 = new Stopwatch();
        int executedCount = 0;
        // Start the stopwatch
        stopwatch2.Start();
        for (int i = 0; i < loop; i++)
        {
            var graph = original;
            graph.ExecuteEvent("employee_name", "Jim Bean");
            graph.ExecuteEvent("employee_email", "jim@bean.org");
            graph.ExecuteEvent("start_date", DateTimeOffset.Now.ToString());
            graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            graph.ExecuteEvent("reason", "Tired");
            graph.ExecuteEvent("vacation_request");
            graph.ExecuteEvent("approved", "true");
            graph.ExecuteEvent("review_request");
            graph.ExecuteEvent("submit_to_hr");
        }
        // Stop the stopwatch
        stopwatch2.Stop();

        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch2.Elapsed;

        // Format and display the TimeSpan value
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        Console.WriteLine($"RunTime Interpreter {executedCount} executions:" + elapsedTime);
    }

}