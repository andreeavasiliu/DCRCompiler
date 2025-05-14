using DCR.Workflow;
using System.Diagnostics;
using System.Xml.Linq;
using static DCR.Core.Generator;

class Program
{
   
    static void Main(string[] args)
    {
        string xmlFilePath = "job_interview.xml";
        //string xmlFilePath = "DCR-interpreter.xml";
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
        Console.ReadKey();

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

            graph.ExecuteEvent("application:full_name", "Jim Bean");
            graph.ExecuteEvent("application:email_addr", "jimbean@test.test");
            graph.ExecuteEvent("application:phone_number", "1234567890");
            graph.ExecuteEvent("application:dob", "1990-01-01");
            graph.ExecuteEvent("application:personal_info");
            graph.ExecuteEvent("application:resume", "resume.pdf");
            graph.ExecuteEvent("application:upload_doc");
            graph.ExecuteEvent("application:position", "4");
            graph.ExecuteEvent("application:avail_start_date", "2023-10-01");
            graph.ExecuteEvent("application:applic_details");
            graph.ExecuteEvent("application:A16");
            graph.ExecuteEvent("application:A15");
            graph.ExecuteEvent("rev_applic", "2");
            graph.ExecuteEvent("A3", "12/25/2015 10:30:00 AM");

            // graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            // graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            // graph.ExecuteEvent("reason", "Tired");
            // graph.ExecuteEvent("vacation_request");
            // graph.ExecuteEvent("approved", "true");
            // graph.ExecuteEvent("employee_name", "Jim Bean");
            // graph.ExecuteEvent("employee_email", "jim@bean.org");
            // graph.ExecuteEvent("start_date", DateTimeOffset.MinValue.ToString());
            // graph.ExecuteEvent("end_date", DateTimeOffset.Now.ToString());
            // graph.ExecuteEvent("reason", "Tired");
            // graph.ExecuteEvent("vacation_request");
            // graph.ExecuteEvent("approved", "true");
            // graph.ExecuteEvent("review_request");
            // graph.ExecuteEvent("submit_to_hr");

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