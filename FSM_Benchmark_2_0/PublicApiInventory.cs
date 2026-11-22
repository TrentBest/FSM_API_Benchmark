using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace FSM_Benchmark_2_0
{
    internal static class PublicApiInventory
    {
        public static void Write(string outputPath)
        {
            var assembly = typeof(TheSingularityWorkshop.FSM_API.FsmApi).Assembly;
            var types = assembly.GetExportedTypes()
                .Where(t => t.Namespace == "FSM_API" ||
                    t.Namespace == "TheSingularityWorkshop.FSM_API" ||
                    (t.Namespace != null && t.Namespace.StartsWith("TheSingularityWorkshop.FSM_API.IntegerBacked", StringComparison.Ordinal)))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

            var members = types.SelectMany(t =>
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                var constructors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Select(c => new
                {
                    SurfaceType = t.FullName,
                    DeclaringType = c.DeclaringType?.FullName,
                    Kind = "Constructor",
                    Name = c.Name,
                    Signature = c.ToString(),
                    Disposition = Classify(t, "Constructor", c.Name),
                    CoverageStatus = Coverage(t, "Constructor", c.Name)
                });
                var methods = t.GetMethods(flags)
                    .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
                    .Select(m => new
                    {
                        SurfaceType = t.FullName,
                        DeclaringType = m.DeclaringType?.FullName,
                        Kind = "Method",
                        Name = m.Name,
                        Signature = m.ToString(),
                        Disposition = Classify(t, "Method", m.Name),
                        CoverageStatus = Coverage(t, "Method", m.Name)
                    });
                var properties = t.GetProperties(flags).Select(p => new
                {
                    SurfaceType = t.FullName,
                    DeclaringType = p.DeclaringType?.FullName,
                    Kind = "Property",
                    Name = p.Name,
                    Signature = p.ToString(),
                    Disposition = Classify(t, "Property", p.Name),
                    CoverageStatus = Coverage(t, "Property", p.Name)
                });
                var events = t.GetEvents(flags).Select(e => new
                {
                    SurfaceType = t.FullName,
                    DeclaringType = e.DeclaringType?.FullName,
                    Kind = "Event",
                    Name = e.Name,
                    Signature = e.ToString(),
                    Disposition = Classify(t, "Event", e.Name),
                    CoverageStatus = Coverage(t, "Event", e.Name)
                });
                return constructors.Cast<object>().Concat(methods).Concat(properties).Concat(events);
            }).ToArray();

            var document = new
            {
                Assembly = assembly.GetName().Name,
                AssemblyVersion = assembly.GetName().Version?.ToString(),
                SourceCommit = Environment.GetEnvironmentVariable("FSM_API_SOURCE_SHA") ?? "not-provided",
                GeneratedUtc = DateTime.UtcNow.ToString("O"),
                MemberCount = members.Length,
                Members = members
            };

            var fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllText(fullPath, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("Wrote " + members.Length + " classified public API inventory entries to " + fullPath);
        }

        private static string Classify(Type type, string kind, string name)
        {
            if (kind == "Property" || kind == "Event") return "Diagnostic";
            if (name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Validate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Throw", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Contract";
            if (name == "Update" || name == "UpdateAll" || name == "TickAll" ||
                name == "EvaluateConditions" || name == "Step" || name == "TransitionTo" ||
                name == "ResetFSMInstance" || name == "ToInteger" || name == "ToString" ||
                name == "BuildDefinition" || name == "State" || name == "Transition" ||
                name == "AnyTransition" || name == "CreateFiniteStateMachine" ||
                name == "CreateInstance" || name == "Register" || name == "RegisterDefinition")
                return "TimedCandidate";
            if (name.StartsWith("Get", StringComparison.Ordinal) ||
                name == "Contains" || name == "Exists" || name.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Diagnostic";
            if (name.StartsWith("Destroy", StringComparison.Ordinal) ||
                name.StartsWith("Remove", StringComparison.Ordinal) ||
                name.StartsWith("Unregister", StringComparison.Ordinal) ||
                name.StartsWith("Move", StringComparison.Ordinal) ||
                name.StartsWith("Add", StringComparison.Ordinal) ||
                name.StartsWith("Reset", StringComparison.Ordinal) ||
                name == "Initialize")
                return "Scenario";
            return "Contract";
        }

        private static string Coverage(Type type, string kind, string name)
        {
            if (kind != "Method") return "Not yet measured; inventory accounted";
            var surface = type.FullName ?? type.Name;
            if ((surface == "TheSingularityWorkshop.FSM_API.FsmApi+Interaction" ||
                 surface == "TheSingularityWorkshop.FSM_API.IntegerBacked.FsmApi+Interaction") &&
                (name == "Update" || name == "Contains" || name == "Exists"))
                return "Current public-facade benchmark";
            if (surface == "TheSingularityWorkshop.FSM_API.IntegerBacked.FSMRuntime" &&
                (name == "Update" || name == "Contains"))
                return "Exercised transitively by current public-facade benchmark";
            return "Not yet measured; inventory accounted";
        }
    }
}