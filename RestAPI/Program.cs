using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddLogging();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<GremlinClient>(
        (serviceProvider) =>
        {
            
            var gremlinServer = new GremlinServer(
                hostname: $"{accountName}.gremlin.cosmos.azure.com",
                port: 443,
                username: "/dbs/DCRgraph/colls/events",
                password: $"{accountKey}",
                enableSsl: true
            );

            var connectionPoolSettings = new ConnectionPoolSettings
            {
                MaxInProcessPerConnection = 32,
                PoolSize = 4,
                ReconnectionAttempts = 4,
                ReconnectionBaseDelay = TimeSpan.FromSeconds(1)
            };

            return new GremlinClient(
                gremlinServer: gremlinServer,
                connectionPoolSettings: connectionPoolSettings,
                messageSerializer: new Gremlin.Net.Structure.IO.GraphSON.GraphSON2MessageSerializer()
            );
        }
    );

builder.Services.AddSingleton<GraphTraversalSource>(
    (serviceProvider) =>
    {
        GremlinClient gremlinClient = serviceProvider.GetService<GremlinClient>();
        var driverRemoteConnection = new DriverRemoteConnection(gremlinClient, "g");
        return AnonymousTraversalSource.Traversal().WithRemote(driverRemoteConnection);
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
