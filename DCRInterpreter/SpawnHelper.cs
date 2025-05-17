using Newtonsoft.Json;

public static class SpawnHelper
{
    public static async Task SpawnEachAsync(DCRGraph graph, string templateId, string spawnData, int maxConcurrency = 6)
    {
        if (spawnData.StartsWith("$"))
        {
            spawnData = graph.Values[spawnData]?.ToString() ?? throw new ArgumentException($"Value for {spawnData} not found");
        }
        var entries = ParseSpawnData(spawnData);

        // Generate all instance IDs upfront
        var instanceIds = Enumerable.Range(0, entries.Count)
            .Select(_ => graph.GetNextInstanceId())
            .ToList();

        var tasks = entries.Zip(instanceIds).Select(pair =>
            ProcessSpawnEntry(graph, templateId, pair.First, pair.Second)
        );

        await Task.WhenAll(tasks);
    }
    private static async Task ProcessSpawnEntry(DCRGraph graph, string templateId,
    Dictionary<string, object?> data, int instanceId)
    {
        await Task.Run(() =>
        {
            lock (graph.SpawnLock) // Add lock object to DCRGraph
            {
                graph.AddSpawnedInstance(templateId, instanceId);
                graph.ApplySpawnData(templateId, instanceId, data);
                graph.CompileSpawnedInstance(templateId, instanceId);
            }
        });
    }
    public static void SpawnEach(DCRGraph graph, string templateId, string? spawnData, int maxConcurrency = 6)
    {
        if (spawnData == null)
        {
            graph.AddSpawnWithData(templateId);
        }
        else
        {
            SpawnEachAsync(graph, templateId, spawnData, maxConcurrency).GetAwaiter().GetResult();
        }
    }




    public static List<Dictionary<string, object?>> ParseSpawnData(string spawnData)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(spawnData)
                ?? new List<Dictionary<string, object?>>();
        }
        catch
        {
            return new List<Dictionary<string, object?>>();
        }
    }
}
