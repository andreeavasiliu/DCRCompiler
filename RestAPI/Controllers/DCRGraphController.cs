using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Driver;
using Microsoft.AspNetCore.Mvc;
using RestAPI.Service;
using System.Text.Json;
using System.Xml.Linq;
using static DCR.Core.Generator.API.Parameters;
using DCR.IO.Xml;
using Gremlin.Net.Process.Traversal;

namespace RestAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DCRGraphController : ControllerBase
    {
        private readonly ILogger<DCRGraphController> _logger;
        private GraphTraversalSource g { get; set; }
        public DCRGraphController(ILogger<DCRGraphController> logger, GraphTraversalSource gremlin)
        {
            _logger = logger;
            g = gremlin;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGraphById(string id)
        {
            try
            {  /*
                // Query to get the DCRGraph vertex
                var graphproperties = $"g.V().has('partitionKey', '{id}').hasLabel('DCRGraph')";
                var metadata = await _gremlinClient.SubmitAsync<dynamic>(graphproperties);

                if (metadata is null)
                    return NotFound($"DCR Graph with ID {id} not found.");

                // Extract DCRGraph properties
                var metadataV = metadata.First();
                var test = metadataV["properties"]["title"];
                var test2 = metadataV["id"];
                var dcrGraph = new DCRGraph((string)metadataV["properties"]["title"]["value"])
                {
                    Id = (string)metadataV["properties"]["id"],
                };

                // Query to get all events connected to this DCRGraph
                var eventsQuery = $"g.V().has('partitionKey', '{id}').hasLabel('Event')";
                var eventsResult = await _gremlinClient.SubmitAsync<dynamic>(eventsQuery);

                foreach (var eventVertex in eventsResult)
                {
                    var eventId = (string)eventVertex["id"];
                    var eventData = JsonSerializer.Deserialize<Event>((string)eventVertex["properties"]["data"][0]["value"]);

                    dcrGraph.Events[eventId] = eventData;
                }

                // Query to get all relationships between events
                var relationshipsQuery = $"g.V().has('partitionKey', '{id}').hasLabel('Event').outE().as('e').inV().as('v').select('e', 'v')";
                var relationshipsResult = await _gremlinClient.SubmitAsync<dynamic>(relationshipsQuery);

                foreach (var result in relationshipsResult)
                {
                    var edge = result["e"];
                    var targetEventVertex = result["v"];

                    var relationship = new Relationship(
                        source: (string)edge["outV"], // Source event ID
                        target: (string)targetEventVertex["id"], // Target event ID
                        relationshipType: Enum.Parse<RelationshipType>((string)edge["label"], true) // Relationship type (Condition, Response, etc.)
                    );

                    dcrGraph.Relationships.Add(relationship);
                }*/

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving graph: {ex.Message}");
            }
        }

        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<IActionResult> CreateGraph([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var document = XDocument.Load(stream);
            DCRGraph graph = DCRInterpreter.ParseDCRGraphFromXml(document);
            if (graph == null)
                return BadRequest("Invalid graph data.");
            var tx = g.Tx();    // create a transaction
                                // spawn a new GraphTraversalSource binding all traversals established from it to tx
            var gtx = tx.Begin();
            try
            {
                var graphkey = graph.Id;

                // Add graph vertex
                gtx.AddV("DCRGraph")
                    .Property("graph", "pk")
                    .Property("partitionKey", graphkey)
                    .Property("id", graph.Id)
                    .Property("title", graph.Title)
                    .Next();

                // Insert vertices for each event
                foreach (var eventEntry in graph.Events)
                {
                    var eventId = eventEntry.Key;
                    var eventObj = eventEntry.Value;
                    var eve = gtx.AddV("Event")
                        .Property("graph", "pk")
                        .Property("partitionKey", graphkey)
                        .Property("id", eventId.EscapeGremlinString())
                        .Property("type", eventObj.Type)
                        .Property("executed", eventObj.Executed.ToString().ToLower())
                        .Property("included", eventObj.Included.ToString().ToLower())
                        .Property("pending", eventObj.Pending.ToString().ToLower());

                    if (eventObj.Data != null)
                        eve.Property("data", eventObj.Data);

                    if (eventObj.Roles.Count > 0)
                        eve.Property("roles", string.Join(",", eventObj.Roles));

                    if (eventObj.ReadRoles.Count > 0)
                        eve.Property("readRoles", string.Join(",", eventObj.ReadRoles));

                    if (!string.IsNullOrEmpty(eventObj.Description))
                        eve.Property("description", eventObj.Description);

                    if (!string.IsNullOrEmpty(eventObj.Label))
                        eve.Property("label", eventObj.Label);

                    eve.Next();

                    if (eventObj.Parent != null)
                    {
                        gtx.V(eventObj.Parent.Id)
                           .AddE("parentOf")
                           .To(gtx.V(eventId.EscapeGremlinString()))
                           .Next();
                    }
                    

                }

                // Insert edges for each relationship
                foreach (var relationship in graph.Relationships)
                {

                    var rel = gtx.V(relationship.SourceId).AddE(relationship.Type.ToString()).To(relationship.TargetId);


                    // Add guard properties if present
                    if (!string.IsNullOrEmpty(relationship.GuardExpressionId))
                        rel.Property("guardExpressionId", relationship.GuardExpressionId);

                    if (relationship.GuardExpression != null)
                        rel.Property("guardExpressionValue", relationship.GuardExpression.Value);
                    rel.Next();
                }
                await tx.CommitAsync();
                return CreatedAtAction(nameof(GetGraphById), new { id = graph.Id }, graph.Title);
            }
            catch (ResponseException e)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"Error executing query: {e.Data}");
                Console.WriteLine($"Response error message: {e.Message}");
                return StatusCode((int)e.StatusCode, $"Error creating graph");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error creating graph: {ex.Message}");
            }

        }

        public async Task ExecuteQueryAsync(string query, GremlinClient _client)
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
    }
}
