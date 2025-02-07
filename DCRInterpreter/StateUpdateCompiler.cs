using System;
using System.Reflection;
using System.Reflection.Emit;

public class StateUpdateCompiler
{
    private DCRGraph Graph;

    public StateUpdateCompiler(DCRGraph graph)
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
        List<string> targets = new List<string>();
        // Generate IL for relationship rules 
        foreach (var relation in Graph.Relationships)
        {
            if (relation.SourceId == eventId)
            {
                targets.Add(relation.TargetId);
                il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                switch (relation.Type)
                {
                    case RelationshipType.Response:
                        il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
                        break;
                    case RelationshipType.Include:
                        il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                        break;
                    case RelationshipType.Exclude:
                        il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Included = false)
                        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                        break;
                }
            }
        }

        // Clear pending state for the executed event
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
        il.Emit(OpCodes.Ldstr, eventId);
        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
        il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Pending = false)
        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);

        foreach(var eve in Graph.Robots)
        {
            foreach (var relation in Graph.Relationships)
            {
                if (relation.SourceId == eve.Id)
                {
                    targets.Add(relation.TargetId);
                    il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                    il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                    il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                    il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                    switch (relation.Type)
                    {
                        case RelationshipType.Response:
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
                            break;
                        case RelationshipType.Include:
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                            break;
                        case RelationshipType.Exclude:
                            il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Included = false)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                            break;
                    }
                }
            }

            // Clear pending state for the executed event
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, eve.Id);
            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
            il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Pending = false)
            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
        }


        foreach(var tgt in targets)
        {
            // --- 1. Call GenerateLogicForEvent(tgt) and store the returned delegate ---
            il.Emit(OpCodes.Ldstr, tgt);  // push the target string as parameter
            MethodInfo generateLogicMethod = typeof(DCRGraph).GetMethod("GenerateLogicForEvent", BindingFlags.Public | BindingFlags.Static);
            il.EmitCall(OpCodes.Call, generateLogicMethod, null);
            // The call returns an Action<DCRGraph> on the evaluation stack.
            // Store it into a local variable for later use.
            LocalBuilder actionLocal = il.DeclareLocal(typeof(Action<DCRGraph>));
            il.Emit(OpCodes.Stloc, actionLocal);

            // --- 2. Retrieve the Event from the graph's Events dictionary ---
            // Load the graph instance (the first argument of the dynamic method)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, tgt); // Load target ID
            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));

            // --- 3. Assign the delegate to the Event's CompiledLogic property ---
            // Load the delegate from the local variable.
            il.Emit(OpCodes.Ldloc, actionLocal);
            // Get the property setter for CompiledLogic.
            MethodInfo setCompiledLogic = typeof(Event)
                .GetProperty("CompiledLogic")
                .GetSetMethod();
            il.EmitCall(OpCodes.Callvirt, setCompiledLogic, null);
        }

        // Return from the method
        il.Emit(OpCodes.Ret);

        return (Action<DCRGraph>)method.CreateDelegate(typeof(Action<DCRGraph>));
    }
}
