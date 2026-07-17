using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Architecture;

public class XmlDocumentationCoverageTests
{
    private static readonly (Assembly Assembly, string[] NamespacePrefixes)[] Targets =
    [
        (typeof(BaseEntity).Assembly, ["Domain.Entities", "Domain.Interfaces"]),
        (typeof(IAuthService).Assembly, ["Application.Interfaces", "Application.Services", "Application.Events"]),
        (typeof(UnitOfWork).Assembly, ["Infrastructure.Repositories"]),
        (typeof(AuthController).Assembly, ["SkyRoc.Controllers"])
    ];

    [Fact]
    public void PublicContracts_HaveXmlDocumentation()
    {
        var violations = new List<string>();

        foreach (var target in Targets)
        {
            var documentationPath = Path.Combine(
                AppContext.BaseDirectory,
                $"{target.Assembly.GetName().Name}.xml");
            Assert.True(File.Exists(documentationPath), $"缺少 XML 文档文件：{documentationPath}");

            var documentedMembers = XDocument.Load(documentationPath)
                .Descendants("member")
                .Select(element => element.Attribute("name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToHashSet(StringComparer.Ordinal);

            foreach (var type in target.Assembly.GetTypes().Where(type => IsTargetType(type, target.NamespacePrefixes)))
            {
                AssertDocumented(documentedMembers, $"T:{type.FullName}", type.FullName!, violations);

                foreach (var member in GetDocumentedMembers(type))
                {
                    var prefix = GetDocumentationPrefix(type, member);
                    if (!documentedMembers.Any(name => name.StartsWith(prefix, StringComparison.Ordinal)))
                        violations.Add($"{type.FullName}.{member.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static bool IsTargetType(Type type, IEnumerable<string> namespacePrefixes)
    {
        return type.IsPublic
               && type.Namespace is not null
               && namespacePrefixes.Any(prefix => type.Namespace.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IEnumerable<MemberInfo> GetDocumentedMembers(Type type)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance
                                          | BindingFlags.Static
                                          | BindingFlags.Public
                                          | BindingFlags.NonPublic
                                          | BindingFlags.DeclaredOnly;

        return type.GetMembers(bindingFlags)
            .Where(member => member is MethodInfo or PropertyInfo or FieldInfo or EventInfo)
            .Where(member => !IsCompilerGenerated(member))
            .Where(IsPublicOrProtectedContract);
    }

    private static bool IsCompilerGenerated(MemberInfo member)
    {
        return member.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
               || member.Name.StartsWith("<", StringComparison.Ordinal)
               || member is MethodBase { IsSpecialName: true }
               || member is FieldInfo { IsSpecialName: true };
    }

    private static bool IsPublicOrProtectedContract(MemberInfo member)
    {
        return member switch
        {
            MethodBase method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly,
            PropertyInfo property => IsVisible(property.GetMethod) || IsVisible(property.SetMethod),
            FieldInfo field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
            EventInfo eventInfo => IsVisible(eventInfo.AddMethod) || IsVisible(eventInfo.RemoveMethod),
            _ => false
        };
    }

    private static bool IsVisible(MethodBase? method)
    {
        return method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
    }

    private static string GetDocumentationPrefix(Type type, MemberInfo member)
    {
        var memberKind = member switch
        {
            MethodInfo => "M",
            PropertyInfo => "P",
            FieldInfo => "F",
            EventInfo => "E",
            _ => throw new ArgumentOutOfRangeException(nameof(member))
        };

        return $"{memberKind}:{type.FullName}.{member.Name}";
    }

    private static void AssertDocumented(
        IReadOnlySet<string> documentedMembers,
        string documentationId,
        string displayName,
        ICollection<string> violations)
    {
        if (!documentedMembers.Contains(documentationId))
            violations.Add(displayName);
    }
}
