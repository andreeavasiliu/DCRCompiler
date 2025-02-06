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
using Gremlin.Net.Driver.Exceptions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
   
    static void Main(string[] args)
    {
        string accountName = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_ENDPOINT")!;
        string accountKey = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_KEY")!;    
        var server = new GremlinServer(
                hostname: $"{accountName}.gremlin.cosmos.azure.com",
                port: 443,
                username: "/dbs/DCRgraph/colls/events",
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
        await CleanDatabaseAsync(client);
        await InsertGraphAsync(graph, client);
    }
    static public async Task InsertGraphAsync(DCRGraph graph, GremlinClient _client)
    {
        // Insert vertices for each event
        foreach (var eventEntry in graph.Events)
        {
            var eventId = eventEntry.Key;
            var eventObj = eventEntry.Value;

            var vertexQuery = $"g.addV('Event')" +
                              ".property('graph', 'pk')" +
                              $".property('id', '{eventId.EscapeGremlinString()}')" +
                              $".property('type', '{eventObj.Type}')" +
                              $".property('executed', {eventObj.Executed.ToString().ToLower()})" +
                              $".property('included', {eventObj.Included.ToString().ToLower()})" +
                              $".property('pending', {eventObj.Pending.ToString().ToLower()})";

            if (eventObj.Data != null)
                vertexQuery += $".property('data', '{eventObj.Data}')";

            if (eventObj.Roles.Count > 0)
                vertexQuery += $".property('roles', '{string.Join(",", eventObj.Roles)}')";

            if (eventObj.ReadRoles.Count > 0)
                vertexQuery += $".property('readRoles', '{string.Join(",", eventObj.ReadRoles)}')";

            if (!string.IsNullOrEmpty(eventObj.Description))
                vertexQuery += $".property('description', '{eventObj.Description}')";
            
            if(!string.IsNullOrEmpty(eventObj.Label))
                vertexQuery += $".property('label', '{eventObj.Label}')";

            await ExecuteQueryAsync(vertexQuery, _client);
            if (eventObj.Parent != null)
            {
                var parentEdgeQuery = $"g.V('{eventObj.Parent.Id}').addE('parentOf').to(g.V('{eventId}'))";
                await ExecuteQueryAsync(parentEdgeQuery, _client);
            }
        }

        // Insert edges for each relationship
        foreach (var relationship in graph.Relationships)
        {
            var edgeQuery = $"g.V('{relationship.SourceId}')" +
                            $".addE('{relationship.Type}')" +
                            $".to(g.V('{relationship.TargetId}'))";

            // Add guard properties if present
            if (!string.IsNullOrEmpty(relationship.GuardExpressionId))
                edgeQuery += $".property('guardExpressionId', '{relationship.GuardExpressionId}')";

            if (relationship.GuardExpression != null)
                edgeQuery += $".property('guardExpressionValue', '{relationship.GuardExpression.Value}')";

            await ExecuteQueryAsync(edgeQuery, _client);
        }
    }


    static public async Task CleanDatabaseAsync(GremlinClient _client)
    {
        try
        {
            // Query to drop all vertices (and their connected edges)
            string query = "g.V().drop()";

            await ExecuteQueryAsync(query, _client);

            Console.WriteLine("Database cleaned successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while cleaning the database: {ex.Message}");
        }
    }
    static private async Task ExecuteQueryAsync(string query, GremlinClient _client)
    {
        try
        {
            await _client.SubmitAsync<dynamic>(query);
        }
        catch (ResponseException e)
        {
            Console.WriteLine($"Error executing query: {query}");
            Console.WriteLine($"Response status code: {e.StatusAttributes["x-ms-status-code"]}");
            Console.WriteLine($"Response error message: {e.Message}");
        }
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