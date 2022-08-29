using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FluentAssertions;
using GitTreeVersion.Deployables;
using GitTreeVersion.Paths;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests;

[TestFixture]
public class DotNetClassicProjectScenarios : GitTestBase
{
    [Test]
    public void ProjectOnlyAssemblyInfo()
    {
        var repositoryPath = CreateGitRepository();
        var projectDirectoryPath = repositoryPath.CombineToDirectory("project1");
        var projectFilePath = projectDirectoryPath.CombineToFile("project1.csproj");
        Directory.CreateDirectory(projectFilePath.Parent.FullName);

        SaveDocument(projectFilePath, LoadCatapultCsproj());

        var assemblyInfoFile = projectDirectoryPath.CombineToFile("Properties", "AssemblyInfo.cs");
        Directory.CreateDirectory(assemblyInfoFile.Parent.FullName);
        File.WriteAllText(assemblyInfoFile.FullName, "");

        var deployable = new DeployableResolver().Resolve(projectFilePath);
        deployable.Should().NotBeNull();
        deployable!.ApplyVersion(new SemVersion(1, 2, 3), new ApplyOptions(false, false));

        File.ReadAllText(assemblyInfoFile.FullName).Should().Be(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.2.3"")]
");
    }

    [Test]
    public void ProjectOnlySolutionInfo()
    {
        var repositoryPath = CreateGitRepository();
        var projectFilePath = repositoryPath.CombineToDirectory("project1").CombineToFile("project1.csproj");
        Directory.CreateDirectory(projectFilePath.Parent.FullName);

        var document = LoadCatapultCsproj();
        AddCompile(document, "../SolutionInfo.cs");
        RemovePropertiesAssemblyInfo(document);
        SaveDocument(projectFilePath, document);

        var solutionInfoFile = repositoryPath.CombineToFile("SolutionInfo.cs");
        File.WriteAllText(solutionInfoFile.FullName, "");

        var deployable = new DeployableResolver().Resolve(projectFilePath);
        deployable.Should().NotBeNull();
        deployable!.ApplyVersion(new SemVersion(1, 2, 3), new ApplyOptions(false, false));

        File.ReadAllText(solutionInfoFile.FullName).Should().Be(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.2.3"")]
");
    }

    [Test]
    public void ProjectBothSolutionInfoAndAssemblyInfo_PrioritizesSolutionInfo()
    {
        var repositoryPath = CreateGitRepository();
        var projectDirectoryPath = repositoryPath.CombineToDirectory("project1");
        var projectFilePath = projectDirectoryPath.CombineToFile("project1.csproj");
        Directory.CreateDirectory(projectFilePath.Parent.FullName);

        var document = LoadCatapultCsproj();
        AddCompile(document, "../SolutionInfo.cs");
        SaveDocument(projectFilePath, document);

        var solutionInfoFile = repositoryPath.CombineToFile("SolutionInfo.cs");
        File.WriteAllText(solutionInfoFile.FullName, "");

        var assemblyInfoFile = projectDirectoryPath.CombineToFile("Properties", "AssemblyInfo.cs");
        Directory.CreateDirectory(assemblyInfoFile.Parent.FullName);
        File.WriteAllText(assemblyInfoFile.FullName, "");

        var deployable = new DeployableResolver().Resolve(projectFilePath);
        deployable.Should().NotBeNull();
        deployable!.ApplyVersion(new SemVersion(1, 2, 3), new ApplyOptions(false, false));

        File.ReadAllText(solutionInfoFile.FullName).Should().Be(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.2.3"")]
");
    }

    [Test]
    public void ProjectBothSolutionInfoAndAssemblyInfo_AssemblyInfoContainsAttributes_PrioritizesSolutionInfoButKeepsUpdatesExistingAttributes()
    {
        var repositoryPath = CreateGitRepository();
        var projectDirectoryPath = repositoryPath.CombineToDirectory("project1");
        var projectFilePath = projectDirectoryPath.CombineToFile("project1.csproj");
        Directory.CreateDirectory(projectFilePath.Parent.FullName);

        var document = LoadCatapultCsproj();
        AddCompile(document, "../SolutionInfo.cs");
        SaveDocument(projectFilePath, document);

        var solutionInfoFile = repositoryPath.CombineToFile("SolutionInfo.cs");
        File.WriteAllText(solutionInfoFile.FullName, "");

        var assemblyInfoFile = projectDirectoryPath.CombineToFile("Properties", "AssemblyInfo.cs");
        Directory.CreateDirectory(assemblyInfoFile.Parent.FullName);
        File.WriteAllText(assemblyInfoFile.FullName, @"[assembly: AssemblyInformationalVersionAttribute(""1.0.0"")]");

        var deployable = new DeployableResolver().Resolve(projectFilePath);
        deployable.Should().NotBeNull();
        deployable!.ApplyVersion(new SemVersion(1, 2, 3), new ApplyOptions(false, false));

        File.ReadAllText(assemblyInfoFile.FullName).Trim().Should().Be(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.2.3"")]");
        File.ReadAllText(solutionInfoFile.FullName).Trim().Should().Be(@"[assembly: System.Reflection.AssemblyVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.2.3"")]");
    }

    [Test]
    public void ProjectBothSolutionInfoAndAssemblyInfo_SkipSolutionInfo_WritesToAssemblyInfo()
    {
        var repositoryPath = CreateGitRepository();
        var projectDirectoryPath = repositoryPath.CombineToDirectory("project1");
        var projectFilePath = projectDirectoryPath.CombineToFile("project1.csproj");
        Directory.CreateDirectory(projectFilePath.Parent.FullName);

        var document = LoadCatapultCsproj();
        AddCompile(document, "../SolutionInfo.cs");
        SaveDocument(projectFilePath, document);

        var solutionInfoFile = repositoryPath.CombineToFile("SolutionInfo.cs");
        File.WriteAllText(solutionInfoFile.FullName, "");

        var assemblyInfoFile = projectDirectoryPath.CombineToFile("Properties", "AssemblyInfo.cs");
        Directory.CreateDirectory(assemblyInfoFile.Parent.FullName);
        File.WriteAllText(assemblyInfoFile.FullName, "");

        var deployable = new DeployableResolver().Resolve(projectFilePath);
        deployable.Should().NotBeNull();
        deployable!.ApplyVersion(new SemVersion(1, 2, 3), new ApplyOptions(false, true));

        File.ReadAllText(assemblyInfoFile.FullName).Trim().Should().Be(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyVersionAttribute(""1.2.3"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.2.3"")]");
        File.ReadAllText(solutionInfoFile.FullName).Trim().Should().Be(@"");
    }

    private static void RemovePropertiesAssemblyInfo(XDocument document)
    {
        var assemblyInfoCompileElement = document.XPathSelectElement(@"//*[local-name() = 'Compile'][@Include='Properties\AssemblyInfo.cs']");
        assemblyInfoCompileElement.Should().NotBeNull();
        assemblyInfoCompileElement!.Remove();
    }

    private static void AddCompile(XDocument document, string fileName)
    {
        var itemGroupElement = document.XPathSelectElement("//*[local-name() = 'ItemGroup']");
        itemGroupElement.Should().NotBeNull();
        itemGroupElement!.Add(new XElement("Compile", new XAttribute("Include", fileName)));
    }

    private static XDocument LoadCatapultCsproj()
    {
        return XDocument.Parse(ResourceReader.CatapultCsproj, LoadOptions.PreserveWhitespace);
    }

    private static void SaveDocument(AbsoluteFilePath projectFilePath, XDocument document)
    {
        var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true };
        using var xmlWriter = XmlWriter.Create(projectFilePath.FullName, writerSettings);
        document.Save(xmlWriter);
    }
}