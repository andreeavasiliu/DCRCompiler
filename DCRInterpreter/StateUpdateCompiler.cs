using System.Collections.Concurrent;
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
    public Func<DCRGraph, string?, List<string>> GenerateLogicForEvent(string eventId)
    {
        var method = new DynamicMethod(
            $"Execute_{eventId}",
            typeof(List<string>),                      // Return type
            new[] { typeof(DCRGraph), typeof(string) },        // Parameter types
            typeof(DCRGraph).Module            // Owner module
        );

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
        il.Emit(OpCodes.Ldstr, eventId);
        il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Data").SetMethod);

        // Generate IL for relationship rules 

        List<string> targets = new List<string>();

        void ExecuteEvent(string eventId)
        {
            // Clear pending state for the executed event
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, eventId);
            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
            il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Pending = false)
            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, eventId);
            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
            il.Emit(OpCodes.Ldc_I4_1); // Load constant false (Executed = true)
            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Executed").SetMethod);
            foreach (var relation in Graph.Relationships)
            {
                if (relation.SourceId == eventId && relation.Type != RelationshipType.Update)
                {
                    Label continueLabel = il.DefineLabel();
                    if (relation.GuardExpressionId != null)
                    {
                        // Load DCRGraph parameter
                        il.Emit(OpCodes.Ldarg_0);

                        // Load Expressions property
                        il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Expressions")?.GetGetMethod()
                            ?? throw new Exception("Expressions property not found"));

                        // Load guard expression ID
                        il.Emit(OpCodes.Ldstr, relation.GuardExpressionId);

                        // Call get_Item on Dictionary<string, DcrExpression>
                        var getItemMethod = typeof(Dictionary<string, DcrExpression>)
                            .GetMethod("get_Item", new[] { typeof(string) })
                            ?? throw new Exception("Dictionary get_Item(string) method not found");

                        il.Emit(OpCodes.Callvirt, getItemMethod);

                        // Load DCRGraph instance again as an argument for Evaluate()
                        il.Emit(OpCodes.Ldarg_0);

                        // Call Evaluate(DCRGraph)
                        var evalMethod = typeof(DcrExpression).GetMethod("Evaluate", new[] { typeof(DCRGraph) })
                            ?? throw new Exception("Evaluate(DCRGraph) method not found");

                        il.Emit(OpCodes.Callvirt, evalMethod);

                        // Branch if Evaluate() returns false
                        il.Emit(OpCodes.Brfalse, continueLabel);
                    }
                    //I know it looks silly. but i'm guessing the relationships that are not covered here
                    //have incomplete pieces of code that super-break everything
                    switch (relation.Type)
                    {
                        case RelationshipType.Response:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Pending = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);
                            break;

                        case RelationshipType.Include:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant true (Included = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                            break;

                        case RelationshipType.Exclude:
                            targets.Add(relation.TargetId);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Included = false)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Included").SetMethod);
                            break;

                        case RelationshipType.Update:
                            targets.Add(relation.TargetId);

                            //Copy Data from guard source to relation target
                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.GuardExpression.Value); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Data").GetGetMethod());

                            LocalBuilder sourceData = il.DeclareLocal(typeof(object)); //works with null
                            il.Emit(OpCodes.Stloc, sourceData);

                            il.Emit(OpCodes.Ldarg_0); // Load DCRGraph parameter
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // Load target ID
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldloc, sourceData);
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Data").GetSetMethod());


                            // Clear pending state for the executed target
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId);
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_0); // Load constant false (Pending = false)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Pending").SetMethod);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
                            il.Emit(OpCodes.Ldstr, relation.TargetId);
                            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));
                            il.Emit(OpCodes.Ldc_I4_1); // Load constant false (Executed = true)
                            il.Emit(OpCodes.Callvirt, typeof(Event).GetProperty("Executed").SetMethod);

                            break;
                        case RelationshipType.Spawn:
                            targets.Add(relation.TargetId);
                            il.Emit(OpCodes.Ldarg_0); // DCRGraph
                            il.Emit(OpCodes.Ldstr, relation.TargetId); // template ID
                            if (relation.SpawnData == null)
                            {
                                il.Emit(OpCodes.Ldnull); // null
                            }
                            else
                                il.Emit(OpCodes.Ldstr, relation.SpawnData ); // JSON
                            il.Emit(OpCodes.Call, typeof(SpawnHelper).GetMethod("SpawnEach"));

                            break;

                    }
                    il.MarkLabel(continueLabel);
                }
            }
        }

        ExecuteEvent(eventId);

        foreach (var eve in Graph.Robots.IntersectBy(targets, t => t.Id))
        {
            ExecuteEvent(eve.Id);
        }

        ConstructorInfo listCtor = typeof(List<string>).GetConstructor(Type.EmptyTypes);
        il.Emit(OpCodes.Newobj, listCtor); // new List<string>()

        // (Optional) Store the new list in a local variable.
        LocalBuilder listLocal = il.DeclareLocal(typeof(List<string>));
        il.Emit(OpCodes.Stloc, listLocal);

        il.Emit(OpCodes.Ldloc, listLocal);    // Load listLocal
        il.Emit(OpCodes.Ldstr, eventId);          // Push "A"
        il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("Add")); // listLocal.Add("A")


        foreach (var tgt in targets.Distinct())
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
            LocalBuilder funcLocal = il.DeclareLocal(typeof(Func<DCRGraph, string, List<string>>));
            il.Emit(OpCodes.Stloc, funcLocal);

            // --- 2. Retrieve the Event from the graph's Events dictionary ---
            // Load the graph instance (the first argument of the dynamic method)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(DCRGraph).GetProperty("Events").GetGetMethod());
            il.Emit(OpCodes.Ldstr, tgt); // Load target ID
            il.Emit(OpCodes.Callvirt, typeof(ConcurrentDictionary<string, Event>).GetMethod("get_Item"));

            // --- 3. Assign the delegate to the Event's CompiledLogic property ---
            // Load the delegate from the local variable.
            il.Emit(OpCodes.Ldloc, funcLocal);
            // Get the property setter for CompiledLogic.
            MethodInfo setCompiledLogic = typeof(Event).GetProperty("CompiledLogic").GetSetMethod();
            il.EmitCall(OpCodes.Callvirt, setCompiledLogic, null);

        }
        // Return from the method
        il.Emit(OpCodes.Ldloc, listLocal);
        il.Emit(OpCodes.Ret);

        return (Func<DCRGraph, string?, List<string>>)method.CreateDelegate(typeof(Func<DCRGraph, string?, List<string>>));
    }
}
