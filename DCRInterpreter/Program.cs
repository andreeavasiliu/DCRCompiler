using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DCR.Workflow;
using Microsoft.Extensions.Logging;
using static DCR.Core.Data.BuiltinModule;
using Gremlin.Net;
using Gremlin.Net.Driver;
class Program
{
   
    static void Main(string[] args)
    {
        string accountName = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_ENDPOINT")!;
        string accountKey = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_KEY")!;    
        var server = new GremlinServer(
                hostname: $"{accountName}.gremlin.cosmos.azure.com",
                port: 443,
                username: "/dbs/cosmicworks/colls/products",
                password: $"{accountKey}",
                enableSsl: true
            );

        using var client = new GremlinClient(
                    gremlinServer: server,
                    messageSerializer: new Gremlin.Net.Structure.IO.GraphSON.GraphSON2MessageSerializer()
                );

     
        //string xmlFilePath = "DCR_interpreter.xml";
        string xmlFilePath = "the_ultimate_test.xml";


        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: File '{xmlFilePath}' not found.");
            return;
        }

        DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(XDocument.Load(xmlFilePath));


        // try
        // {
        //     XDocument doc = XDocument.Load(xmlFilePath);
        //     Becnh(doc);
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Error: {ex.Message}");
        // }
    }

    // static void Becnh(XDocument doc)
    // {
    //     var runtime = Runtime.Create(builder =>
    //     {
    //         builder.WithOptions(options =>
    //             options.UpdateModelLog = true
    //         );
    //     });
    //     var model = runtime.Parse(doc);

    //     var loop = 500000;
    //     DCRGraph graph = ParseDCRGraphFromXml(doc);
    //     // Initialize and precompile logic for events
    //     graph.Initialize();
    //     DCRInterpreter interpreter = new DCRInterpreter(graph);

    //     BenchRuntime(runtime, model, loop);
    //     BenchInterp(interpreter, graph, loop);

    // }

    // static void BenchRuntime(Runtime runtime, Model model, int loop)
    // {
    //     Stopwatch stopwatch = new Stopwatch();

    //     int executedCount = 0;
    //     // Start the stopwatch
    //     stopwatch.Start();
    //     for (int i = 0; i < loop; i++)
    //     {
    //         foreach (var item in model.AllActivities)
    //         {
    //             if (item.IsEnabled)
    //             {
    //                 executedCount++;
    //                 model.Execute(item);
    //             }

    //             else
    //             {
    //                 Console.WriteLine($"Event {item.Id} is not enabled.");
    //             }
    //         }
    //     }
    //     // Stop the stopwatch
    //     stopwatch.Stop();

    //     // Get the elapsed time as a TimeSpan value
    //     TimeSpan ts = stopwatch.Elapsed;

    //     // Format and display the TimeSpan value
    //     string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
    //         ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
    //     Console.WriteLine($"RunTime Workflow {executedCount} executions:" + elapsedTime);
    // }

    // static void BenchInterp(DCRInterpreter interpreter, DCRGraph graph, int loop)
    // {
    //     Stopwatch stopwatch2 = new Stopwatch();
    //     int executedCount = 0;
    //     // Start the stopwatch
    //     stopwatch2.Start();
    //     for (int i = 0; i < loop; i++)
    //     {
    //         foreach (var eventId in graph.Events.Keys)
    //         {
    //             if (interpreter.IsEventEnabled(eventId))
    //             {
    //                 executedCount++;
    //                 interpreter.ExecuteEvent(eventId);
    //             }
    //             else
    //             {
    //                 Console.WriteLine($"Event {eventId} is not enabled.");
    //             }
    //         }
    //     }
    //     // Stop the stopwatch
    //     stopwatch2.Stop();

    //     // Get the elapsed time as a TimeSpan value
    //     TimeSpan ts = stopwatch2.Elapsed;

    //     // Format and display the TimeSpan value
    //     string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
    //         ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
    //     Console.WriteLine($"RunTime Interpreter {executedCount} executions:" + elapsedTime);
    // }

}