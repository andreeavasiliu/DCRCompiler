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
    public Func<DCRGraph,string,List<string>> GenerateLogicForEvent(string eventId)
    {
        var method = new DynamicMethod(
            $"Execute_{eventId}",
            typeof(List<string>),                      // Return type
            new[] { typeof(DCRGraph), typeof(string) },        // Parameter types
            typeof(DCRGraph).Module            // Owner module
        );

        var il = method.GetILGenerator();

        // Generate IL for relationship rules 

        List<string> targets = new List<string>();
        
        foreach (var relation in Graph.Relationships)
        {
            if (relation.SourceId == eventId)
            {
                //I know it looks silly. but i'm guessing the relationships that are not covered here
                //have incomplete pieces of code that super-break everything
                switch (relation.Type)
                {
                    case RelationshipType.Response:
                        targets.Add(relation.TargetId);

                        il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                        il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                        il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
                        break;
                    case RelationshipType.Include:
                        targets.Add(relation.TargetId);

                        il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                        il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                        il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                        break;
                    case RelationshipType.Exclude:
                        targets.Add(relation.TargetId);

                        il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                        il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
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

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
        il.Emit(OpCodes.Ldstr, eventId);
        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
        il.Emit(OpCodes.Ldc_I4_1); // Load constant false (Executed = true)
        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Executed").SetMethod);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
        il.Emit(OpCodes.Ldstr, eventId);
        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
        il.Emit(OpCodes.Ldarg_1); // Load constant false (Executed = true)
        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Data").SetMethod);

        foreach (var eve in Graph.Robots)
        {
            foreach (var relation in Graph.Relationships)
            {
                if (relation.SourceId == eve.Id)
                {
                    switch (relation.Type)
                    {
                        case RelationshipType.Response:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
                            break;
                        case RelationshipType.Include:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                            break;
                        case RelationshipType.Exclude:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
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

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, eve.Id);
            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Event>).GetMethod("get_Item"));
            il.Emit(OpCodes.Ldc_I4_1); // Load constant false (Executed = true)
            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Executed").SetMethod);
        }

        ConstructorInfo listCtor = typeof(List<string>).GetConstructor(Type.EmptyTypes);
        il.Emit(OpCodes.Newobj, listCtor); // new List<string>()

        // (Optional) Store the new list in a local variable.
        LocalBuilder listLocal = il.DeclareLocal(typeof(List<string>));
        il.Emit(OpCodes.Stloc, listLocal);

        il.Emit(OpCodes.Ldloc, listLocal);    // Load listLocal
        il.Emit(OpCodes.Ldstr, eventId);          // Push "A"
        il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("Add")); // listLocal.Add("A")


        foreach (var tgt in targets)
        {
            il.Emit(OpCodes.Ldloc, listLocal);    // Load listLocal
            il.Emit(OpCodes.Ldstr, tgt);          // Push "A"
            il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("Add")); // listLocal.Add("A")

            // --- 0. Create an instance of StateUpdateCompiler //maybe store in a local?
            ConstructorInfo stateUpdateCompilerCtor = typeof(StateUpdateCompiler).GetConstructor(new[] { typeof(DCRGraph) });
            il.Emit(OpCodes.Ldarg_0);  // Load DCRGraph instance (the first argument)
            il.Emit(OpCodes.Newobj, stateUpdateCompilerCtor); // Create new StateUpdateCompiler instance with DCRGraph

            // --- 1. Call GenerateLogicForEvent(tgt) and store the returned delegate ---
            il.Emit(OpCodes.Ldstr, tgt);  // push the target string as parameter
            MethodInfo generateLogicMethod = typeof(StateUpdateCompiler).GetMethod("GenerateLogicForEvent", BindingFlags.Public | BindingFlags.Instance);
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
        il.Emit(OpCodes.Ldloc, listLocal);
        il.Emit(OpCodes.Ret);

        return (Func<DCRGraph,string, List<string>>)method.CreateDelegate(typeof(Func<DCRGraph, string, List<string>>));
    }
}
