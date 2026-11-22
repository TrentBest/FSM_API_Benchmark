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
                    Disposition = "UNCLASSIFIED"
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
                        Disposition = "UNCLASSIFIED"
                    });
                var properties = t.GetProperties(flags).Select(p => new
                {
                    SurfaceType = t.FullName,
                    DeclaringType = p.DeclaringType?.FullName,
                    Kind = "Property",
                    Name = p.Name,
                    Signature = p.ToString(),
                    Disposition = "UNCLASSIFIED"
                });
                var events = t.GetEvents(flags).Select(e => new
                {
                    SurfaceType = t.FullName,
                    DeclaringType = e.DeclaringType?.FullName,
                    Kind = "Event",
                    Name = e.Name,
                    Signature = e.ToString(),
                    Disposition = "UNCLASSIFIED"
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
            Console.WriteLine("Wrote " + members.Length + " public API inventory entries to " + fullPath);
        }
    }
}