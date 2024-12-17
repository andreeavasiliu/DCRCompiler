using System;
using System.Reflection.Emit;

public class JITCodeGenerator
{
    private DCRGraph Graph;

    public JITCodeGenerator(DCRGraph graph)
    {
        Graph = graph;
    }

    // Generate and compile a DynamicMethod for an event
    public Action<DCRGraph> GenerateLogicForEvent(string eventId)
    {
        var method = new DynamicMethod(
            $"Execute_{eventId}",
            typeof(void),                      // Return type
            new[] { typeof(DCRGraph) },        // Parameter types
            typeof(DCRGraph).Module            // Owner module
        );

        var il = method.GetILGenerator();

        // Generate IL for response rules (mark target events as pending)
        foreach (var response in Graph.Responses)
        {
            if (response.SourceId == eventId)
            {
                il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                il.Emit(OpCodes.Ldstr, response.TargetId); // Load target ID
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
            }
        }

        // Generate IL for inclusion rules (include target events)
        foreach (var inclusion in Graph.Inclusions)
        {
            if (inclusion.SourceId == eventId)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                il.Emit(OpCodes.Ldstr, inclusion.TargetId);
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
            }
        }

        // Generate IL for exclusion rules (exclude target events)
        foreach (var exclusion in Graph.Exclusions)
        {
            if (exclusion.SourceId == eventId)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                il.Emit(OpCodes.Ldstr, exclusion.TargetId);
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Included = false)
                il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
            }
        }

        // Clear pending state for the executed event
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
        il.Emit(OpCodes.Ldstr, eventId);
        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
        il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Pending = false)
        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);

        // Return from the method
        il.Emit(OpCodes.Ret);

        return (Action<DCRGraph>)method.CreateDelegate(typeof(Action<DCRGraph>));
    }
}
