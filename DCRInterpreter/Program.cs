using DCR.Workflow;
using System.Diagnostics;
using System.Xml.Linq;

class Program
{

    static void Main(string[] args)
    {
        string xmlFilePath = "spawn_bench.xml";
        //string xmlFilePath = "job_interview.xml";
        //string xmlFilePath2 = "DCR-interpreter.xml";
        //string xmlFilePath = "the_ultimate_test.xml";

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: File '{xmlFilePath}' not found.");
            return;
        }
        DoBenchmark(XDocument.Load(xmlFilePath));
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

    static void BenchBinary(XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;
        DCRGraph pregraph = DCRInterpreter.ParseDCRGraphFromXml(original);
        var bin = DCRFastInterpreter.Serialize(pregraph);

        while (execute.ElapsedMilliseconds <= maxtime)
        {
            parse.Start();
            var graph = DCRFastInterpreter.Deserialize(bin);
            graph.Initialize();
            parse.Stop();

            execute.Start();

            graph.ExecuteEvent("listspawn");

            // graph.ExecuteEvent("application:full_name", "Jim Bean");
            // graph.ExecuteEvent("application:email_addr", "jimbean@test.test");
            // graph.ExecuteEvent("application:phone_number", "1234567890");
            // graph.ExecuteEvent("application:dob", "1990-01-01");
            // graph.ExecuteEvent("application:personal_info");
            // graph.ExecuteEvent("application:resume", "resume.pdf");
            // graph.ExecuteEvent("application:upload_doc");
            // graph.ExecuteEvent("application:position", "4");
            // graph.ExecuteEvent("application:avail_start_date", "2023-10-01");
            // graph.ExecuteEvent("application:applic_details");
            // graph.ExecuteEvent("application:A16");
            // graph.ExecuteEvent("application:A15");
            // graph.ExecuteEvent("rev_applic", "2");
            // graph.ExecuteEvent("A3", "12/25/2015 10:30:00 AM");

            //graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            //graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            //graph.ExecuteEvent("reason", "Tired");
            //graph.ExecuteEvent("vacation_request");
            //graph.ExecuteEvent("approved", "true");
            //graph.ExecuteEvent("employee_name", "Jim Bean");
            //graph.ExecuteEvent("employee_email", "jim@bean.org");
            //graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            //graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            //graph.ExecuteEvent("reason", "Tired");
            //graph.ExecuteEvent("vacation_request");
            //graph.ExecuteEvent("approved", "true");
            //graph.ExecuteEvent("review_request");
            //graph.ExecuteEvent("submit_to_hr");
            execute.Stop();
            loop++;
        }
        Measure(ConsoleColor.Cyan, "Iterpreter w/ Binary Parsing", execute.Elapsed, parse.Elapsed, loop, 1);
    }

    static void BenchRuntime(Runtime runtime, XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;

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
            loop++;
        }
        Measure(ConsoleColor.Yellow, "Workflow", execute.Elapsed, parse.Elapsed, loop, 9);
    }

    static void BenchInterp(XDocument original, int maxtime)
    {
        Stopwatch execute = new Stopwatch();
        Stopwatch parse = new Stopwatch();
        int loop = 0;
        while (execute.ElapsedMilliseconds <= maxtime)
        {
            parse.Start();
            DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(original);
            graph.Initialize();
            parse.Stop();

            execute.Start();
            var init = graph.Events.Count();
            graph.ExecuteEvent("listspawn");
            var spanwTotal =  graph.Events.Count() - init;

            Console.WriteLine($"Spawned {spanwTotal} new events"); 
            Console.WriteLine($"SpawnedInstances: {graph.SpawnedInstances.Count()}");

            // graph.ExecuteEvent("application:full_name", "Jim Bean");
            // graph.ExecuteEvent("application:email_addr", "jimbean@test.test");
            // graph.ExecuteEvent("application:phone_number", "1234567890");
            // graph.ExecuteEvent("application:dob", "1990-01-01");
            // graph.ExecuteEvent("application:personal_info");
            // graph.ExecuteEvent("application:resume", "resume.pdf");
            // graph.ExecuteEvent("application:upload_doc");
            // graph.ExecuteEvent("application:position", "4");
            // graph.ExecuteEvent("application:avail_start_date", "2023-10-01");
            // graph.ExecuteEvent("application:applic_details");
            // graph.ExecuteEvent("application:A16");
            // graph.ExecuteEvent("application:A15");
            // graph.ExecuteEvent("rev_applic", "2");
            // graph.ExecuteEvent("A3", "12/25/2015 10:30:00 AM");

            //graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            //graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            //graph.ExecuteEvent("reason", "Tired");
            //graph.ExecuteEvent("vacation_request");
            //graph.ExecuteEvent("approved", "true");
            //graph.ExecuteEvent("employee_name", "Jim Bean");
            //graph.ExecuteEvent("employee_email", "jim@bean.org");
            //graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            //graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            //graph.ExecuteEvent("reason", "Tired");
            //graph.ExecuteEvent("vacation_request");
            //graph.ExecuteEvent("approved", "true");
            //graph.ExecuteEvent("review_request");
            //graph.ExecuteEvent("submit_to_hr");

            execute.Stop();
            loop++;
        }
        Measure(ConsoleColor.Green, "Iterpreter", execute.Elapsed, parse.Elapsed, loop, 1);
    }

    static void Measure(ConsoleColor consoleColor, string ExecutionName, TimeSpan execution, TimeSpan parsing, int loop, int eventsperloop)
    {
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            execution.Hours, execution.Minutes, execution.Seconds, execution.Milliseconds / 10);

        string elapsedParseTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            parsing.Hours, parsing.Minutes, parsing.Seconds, parsing.Milliseconds / 10);
        var executedCount = loop * eventsperloop;
        Console.ForegroundColor = consoleColor;
        Console.WriteLine($"{ExecutionName} executed:");
        Console.WriteLine($"        the entire graph {loop} times!");
        Console.WriteLine($"        that is {executedCount} total activities");
        Console.WriteLine($"        and took {elapsedTime} to finish");
        Console.WriteLine($"        averaging {execution.TotalMilliseconds / executedCount}ms per event");
        Console.WriteLine($"            *parsing time was not included in this time");
        Console.WriteLine();
        Console.WriteLine($"Parsing {loop} times took {elapsedParseTime} to finish");
        Console.WriteLine($"        which means {parsing.TotalMilliseconds / loop}ms per parse");
        Console.WriteLine();
    }
}