using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Driver;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;
using static Gremlin.Net.Process.Traversal.__;
using static Gremlin.Net.Process.Traversal.P;
using static Gremlin.Net.Process.Traversal.Order;
using static Gremlin.Net.Process.Traversal.Operator;
using static Gremlin.Net.Process.Traversal.Pop;
using static Gremlin.Net.Process.Traversal.Scope;
using static Gremlin.Net.Process.Traversal.TextP;
using static Gremlin.Net.Process.Traversal.Column;
using static Gremlin.Net.Process.Traversal.Direction;
using static Gremlin.Net.Process.Traversal.T;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Xml.Linq;
using static DCR.Core.Generator.API.Parameters;
using DCR.IO.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RestAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DCRGraphController : ControllerBase
    {
        private readonly ILogger<DCRGraphController> _logger;
        private readonly GremlinClient _gremlinClient;
        public DCRGraphController(ILogger<DCRGraphController> logger, GremlinClient client)
        {
            _logger = logger;
            _gremlinClient = client;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGraphById(string id)
        {
            try
            {
                // Query to get the DCRGraph vertex
                var graphproperties = $"g.V().has('partitionKey', '{id}').hasLabel('DCRGraph')";
                var metadata = await _gremlinClient.SubmitAsync<dynamic>(graphproperties);

                if (metadata is null)
                    return NotFound($"DCR Graph with ID {id} not found.");
                var metadataJson = JToken.FromObject(metadata);
                // Now, inspect or traverse metadataJson as needed:
                string titleToken = metadataJson.First["properties"]["title"][0].Value<string>("value");
                var dcrGraph = new DCRGraph(titleToken)
                {
                   // Id = (string)metadataV["id"],
                };

                // Query to get all events connected to this DCRGraph
                var eventsQuery = $"g.V().has('partitionKey', '{id}').hasLabel('Event')";
                var eventsResult = await _gremlinClient.SubmitAsync<dynamic>(eventsQuery);

                foreach (var eventVertex in eventsResult)
                {
                    var eventId = (string)eventVertex["id"];
                   // var eventData = JsonSerializer.Deserialize<Event>((string)eventVertex["properties"]["data"][0]["value"]);

                  //  dcrGraph.Events[eventId] = eventData;
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
                }

                return Ok(dcrGraph);
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

            try
            {
                var graphkey = graph.Id;
                // Add graph vertex
                var gremlinQuery = $"g.addV('DCRGraph').property('graph', 'pk').property('partitionKey', '{graphkey}').property('id', '{graph.Id}').property('title', '{graph.Title}')";
                await _gremlinClient.SubmitAsync<dynamic>(gremlinQuery);

                // Insert vertices for each event
                foreach (var eventEntry in graph.Events)
                {
                    var eventId = eventEntry.Key;
                    var eventObj = eventEntry.Value;

                    var vertexQuery = $"g.addV('Event')" +
                                      ".property('graph', 'pk')" +
                                      $".property('partitionKey', '{graphkey}')" +
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

                    if (!string.IsNullOrEmpty(eventObj.Label))
                        vertexQuery += $".property('label', '{eventObj.Label}')";

                    await _gremlinClient.SubmitAsync<dynamic>(vertexQuery);

                    if (eventObj.Parent != null)
                    {
                        var parentEdgeQuery = $"g.V('{eventObj.Parent.Id}').addE('parentOf').to(g.V('{eventId}'))";
                        await _gremlinClient.SubmitAsync<dynamic>(parentEdgeQuery);
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

                    await _gremlinClient.SubmitAsync<dynamic>(edgeQuery);
                }
                return CreatedAtAction(nameof(GetGraphById), new { id = graph.Id }, graph.Title);
            }
            catch (ResponseException e)
            {
                Console.WriteLine($"Error executing query: {e.Data}");
                Console.WriteLine($"Response error message: {e.Message}");
                return StatusCode((int)e.StatusCode, $"Error creating graph");
            }
            catch (Exception ex)
            {
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
