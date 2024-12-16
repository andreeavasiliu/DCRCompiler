using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;
using System.Text;

public class JITExecutor
{
    public DCRGraph Graph;
    public JITCodeGenerator JitGenerator;

    public JITExecutor(DCRGraph graph)
    {
        Graph = graph;
        JitGenerator = new JITCodeGenerator(graph);
    }

    public async Task ExecuteEventAsync(string eventId)
    {
        if (!Graph.Events.ContainsKey(eventId))
            throw new ArgumentException($"Event {eventId} not found.");
        // Generate the JIT code for this event
        var jitCodeBuilder = new StringBuilder(JitGenerator.GenerateCodeForEvent(eventId));
        jitCodeBuilder.AppendLine($"Graph.Events[\"{eventId}\"].Executed = true;");
        string jitCode = jitCodeBuilder.ToString();
        // Compile and execute the generated code
        await ExecuteGeneratedCodeAsync(jitCode);
    }

    private async Task ExecuteGeneratedCodeAsync(string jitCode)
    {
        // Create a Globals object with the graph
        var globals = new Globals(Graph);

        // Add necessary references and imports for the script
        var scriptOptions = ScriptOptions.Default
            .AddReferences(typeof(DCRGraph).Assembly)
            .AddImports("System", "System.Collections.Generic");

        // Execute the generated code with Globals as context
        await CSharpScript.RunAsync(jitCode, scriptOptions, globals: globals);
    }

}
