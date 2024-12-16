using System;
using System.Collections.Generic;
using System.Text;

public class JITCodeGenerator
{
    public DCRGraph Graph;

    public JITCodeGenerator(DCRGraph graph)
    {
        Graph = graph;
    }

    public string GenerateCodeForEvent(string eventId)
    {
        StringBuilder codeBuilder = new StringBuilder();

        // Generate code for response rules
        foreach (var response in Graph.Responses)
        {
            if (response.SourceId == eventId)
            {
                codeBuilder.AppendLine($"Graph.Events[\"{response.TargetId}\"].Pending = true;");
            }
        }

        // Generate code for inclusion rules
        foreach (var inclusion in Graph.Inclusions)
        {
            if (inclusion.SourceId == eventId)
            {
                codeBuilder.AppendLine($"Graph.Events[\"{inclusion.TargetId}\"].Included = true;");
            }
        }

        // Generate code for exclusion rules
        foreach (var exclusion in Graph.Exclusions)
        {
            if (exclusion.SourceId == eventId)
            {
                codeBuilder.AppendLine($"Graph.Events[\"{exclusion.TargetId}\"].Included = false;");
            }
        }

        // Clear pending state for the executed event
        codeBuilder.AppendLine($"Graph.Events[\"{eventId}\"].Pending = false;");

        return codeBuilder.ToString();
    }
}
