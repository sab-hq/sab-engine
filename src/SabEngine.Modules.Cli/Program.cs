// SabEngine.Modules.Cli — validates every manifest.yaml under a given
// directory against SabEngine.Modules (PD-12). Usage:
//   dotnet run --project src/SabEngine.Modules.Cli -- <path-to-modules-directory>
//
// Exit code 0 if every manifest is valid, 1 otherwise — this is what
// makes it usable as a CI gate (pre-development-checklist.md, PD-13),
// not just a local convenience.

using SabEngine.Modules;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: SabEngine.Modules.Cli <path-to-modules-directory>");
    return 1;
}

var modulesRoot = args[0];
if (!Directory.Exists(modulesRoot))
{
    Console.Error.WriteLine($"Directory not found: {modulesRoot}");
    return 1;
}

var manifestPaths = Directory.GetFiles(modulesRoot, "manifest.yaml", SearchOption.AllDirectories);

if (manifestPaths.Length == 0)
{
    Console.WriteLine($"No manifest.yaml files found under {modulesRoot}.");
    return 0;
}

var parser = new ModuleManifestParser();
var failureCount = 0;

foreach (var path in manifestPaths.OrderBy(p => p))
{
    var relativePath = Path.GetRelativePath(modulesRoot, path);
    try
    {
        var yaml = File.ReadAllText(path);
        var manifest = parser.Parse(yaml);
        Console.WriteLine($"OK    {relativePath}  (id: {manifest.Id}, version: {manifest.Version})");
    }
    catch (ModuleManifestParseException ex)
    {
        failureCount++;
        Console.WriteLine($"FAIL  {relativePath}");
        Console.WriteLine($"      {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"{manifestPaths.Length - failureCount}/{manifestPaths.Length} manifest(s) valid.");

return failureCount == 0 ? 0 : 1;
