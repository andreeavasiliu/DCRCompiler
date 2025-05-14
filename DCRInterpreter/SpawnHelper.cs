
using System.Collections.Generic;
using Newtonsoft.Json;

public static class SpawnHelper
{
    public static async Task SpawnEachAsync(DCRGraph graph, string templateId, string spawnData, int maxConcurrency = 6)
    {
        if (spawnData.StartsWith("$"))
        {
            spawnData = graph.Values[spawnData]?.ToString() ?? throw new ArgumentException($"Value for {spawnData} not found");
        }
        var entries = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(spawnData)
            ?? throw new ArgumentException("Invalid spawn data JSON");

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task>();

        foreach (var entry in entries)
        {
            await semaphore.WaitAsync();

            var task = Task.Run(() =>
            {
                try
                {
                    graph.AddSpawnWithData(templateId, entry);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
    public static void SpawnEach(DCRGraph graph, string templateId, string? spawnData, int maxConcurrency = 6)
    {
        if (spawnData == null)
        {
            graph.AddSpawnWithData(templateId, new Dictionary<string, object?>());
        }
        else
            SpawnEachAsync(graph, templateId, spawnData, maxConcurrency).GetAwaiter().GetResult();
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
