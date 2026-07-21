using System.Text.RegularExpressions;
using Xunit;

namespace SkyRoc.Tests.Architecture;

public partial class SourceFileOrganizationTests
{
    private static readonly string[] ProjectDirectories =
    [
        "Application",
        "Domain",
        "Infrastructure",
        "Shared",
        "SkyRoc",
        "SkyRoc.Tests"
    ];

    [Fact]
    public void SourceFiles_ContainOneMatchingTopLevelType()
    {
        // 从测试输出目录向上定位仓库根，兼容 artifacts/* 定向输出（相对 .. 层数不固定）。
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyRoc.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var repositoryRoot = directory.FullName;
        var violations = new List<string>();

        foreach (var projectDirectory in ProjectDirectories)
        {
            var projectPath = Path.Combine(repositoryRoot, projectDirectory);
            foreach (var file in Directory.EnumerateFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (IsGeneratedOrMigrationFile(file))
                    continue;

                var declarations = TopLevelTypeRegex().Matches(File.ReadAllText(file));
                if (declarations.Count > 1)
                {
                    violations.Add($"{Path.GetRelativePath(repositoryRoot, file)} contains {declarations.Count} top-level types.");
                    continue;
                }

                if (declarations.Count == 1)
                {
                    var typeName = declarations[0].Groups["name"].Value;
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (!string.Equals(typeName, fileName, StringComparison.Ordinal))
                        violations.Add($"{Path.GetRelativePath(repositoryRoot, file)} declares {typeName}.");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static bool IsGeneratedOrMigrationFile(string file)
    {
        var segments = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
               || segments.Contains("obj", StringComparer.OrdinalIgnoreCase)
               || segments.Contains("Migrations", StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(
        "^(?:public|internal|file)\\s+(?:(?:abstract|sealed|static|partial|readonly)\\s+)*(?:class|record(?:\\s+(?:class|struct))?|interface|enum|struct)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex TopLevelTypeRegex();
}
