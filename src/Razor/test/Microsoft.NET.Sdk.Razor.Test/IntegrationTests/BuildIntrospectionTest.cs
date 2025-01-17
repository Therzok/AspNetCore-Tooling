// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntrospectionTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIntrospectionTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsProjectCapabilities()
        {
            var result = await DotnetMSBuild("_IntrospectProjectCapabilityItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazor");
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazorConfiguration");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsCshtmlFilesToUpToDateCheckInput()
        {
            var result = await DotnetMSBuild("_IntrospectUpToDateCheck");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckInput: {Path.Combine("Views", "Home", "Index.cshtml")}");
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckInput: {Path.Combine("Views", "_ViewStart.cshtml")}");
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckBuilt: {Path.Combine(IntermediateOutputPath, "SimpleMvc.Views.dll")}");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsGeneratedRazorFilesAndAssemblyInfoToRazorCompile()
        {
            var result = await DotnetMSBuild("Build", "/t:_IntrospectRazorCompileItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, $"RazorCompile: {Path.Combine(IntermediateOutputPath, "Razor", "Views", "Home", "Index.cshtml.g.cs")}");
            Assert.BuildOutputContainsLine(result, $"RazorCompile: {Path.Combine(IntermediateOutputPath, "SimpleMvc.RazorTargetAssemblyInfo.cs")}");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_UsesUseSharedCompilationToSetDefaultValueOfUseRazorBuildServer()
        {
            var result = await DotnetMSBuild("Build", "/t:_IntrospectUseRazorBuildServer /p:UseSharedCompilation=false");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UseRazorBuildServer: false");
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task GetCopyToOutputDirectoryItems_WhenNoFileIsPresent_ReturnsEmptySequence()
        {
            var result = await DotnetMSBuild(target: default);

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            result = await DotnetMSBuild(target: "GetCopyToOutputDirectoryItems", "/t:_IntrospectGetCopyToOutputDirectoryItems /p:BuildProjectReferences=false");
            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.dll");
            Assert.BuildOutputContainsLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.pdb");

            // Remove all views from the class library
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            // dotnet msbuild /p:BuildProjectReferences=false
            result = await DotnetMSBuild(target: "GetCopyToOutputDirectoryItems", "/t:_IntrospectGetCopyToOutputDirectoryItems /p:BuildProjectReferences=false");

            Assert.BuildOutputDoesNotContainLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.dll");
            Assert.BuildOutputDoesNotContainLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_ResolvesRazorLangVersionTo30ForNetCoreApp30Projects()
        {
            var result = await DotnetMSBuild("ResolveRazorConfiguration", "/t:_IntrospectResolvedConfiguration");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "RazorLangVersion: 3.0");
            Assert.BuildOutputContainsLine(result, "ResolvedRazorConfiguration: MVC-3.0");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task RazorSdk_ResolvesRazorLangVersionFromValueSpecified()
        {
            var result = await DotnetMSBuild("ResolveRazorConfiguration", "/t:_IntrospectResolvedConfiguration");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "RazorLangVersion: 3.0");
            Assert.BuildOutputContainsLine(result, "ResolvedRazorConfiguration: Default");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task RazorSdk_ResolvesRazorLangVersionTo21WhenUnspecified()
        {
            // This is equivalent to not specifying a value for RazorLangVersion
            AddProjectFileContent("<PropertyGroup><RazorLangVersion /></PropertyGroup>");

            var result = await DotnetMSBuild("ResolveRazorConfiguration", "/t:_IntrospectResolvedConfiguration");

            Assert.BuildPassed(result, allowWarnings: true);
            Assert.BuildOutputContainsLine(result, "RazorLangVersion: 2.1");
            // BuildOutputContainsLine matches entire lines, so it's fine to Assert the following.
            Assert.BuildOutputContainsLine(result, "ResolvedRazorConfiguration:");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task RazorSdk_WithRazorLangVersionLatest()
        {
            var result = await DotnetMSBuild("ResolveRazorConfiguration", "/t:_IntrospectResolvedConfiguration /p:RazorLangVersion=Latest");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "RazorLangVersion: Latest");
            Assert.BuildOutputContainsLine(result, "ResolvedRazorConfiguration: Default");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_ResolvesRazorConfigurationToMvc30()
        {
            var result = await DotnetMSBuild("ResolveRazorConfiguration", "/t:_IntrospectResolvedConfiguration");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "RazorLangVersion: 3.0");
            Assert.BuildOutputContainsLine(result, "ResolvedRazorConfiguration: MVC-3.0");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task UpToDateReloadFileTypes_Default()
        {
            var result = await DotnetMSBuild("_IntrospectUpToDateReloadFileTypes");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UpToDateReloadFileTypes: ;.cs;.razor;.resx;.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task UpToDateReloadFileTypes_WithRuntimeCompilation()
        {
            AddProjectFileContent(
@"
<PropertyGroup>
    <RazorUpToDateReloadFileTypes>$(RazorUpToDateReloadFileTypes.Replace('.cshtml', ''))</RazorUpToDateReloadFileTypes>
</PropertyGroup>");

            var result = await DotnetMSBuild("_IntrospectUpToDateReloadFileTypes");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UpToDateReloadFileTypes: ;.cs;.razor;.resx;");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task UpToDateReloadFileTypes_WithwWorkAroundRemoved()
        {
            var result = await DotnetMSBuild("_IntrospectUpToDateReloadFileTypes", "/p:_RazorUpToDateReloadFileTypesAllowWorkaround=false");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UpToDateReloadFileTypes: ;.cs;.razor;.resx;.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task UpToDateReloadFileTypes_WithRuntimeCompilationAndWorkaroundRemoved()
        {
            AddProjectFileContent(
@"
<PropertyGroup>
    <RazorUpToDateReloadFileTypes>$(RazorUpToDateReloadFileTypes.Replace('.cshtml', ''))</RazorUpToDateReloadFileTypes>
</PropertyGroup>");

            var result = await DotnetMSBuild("_IntrospectUpToDateReloadFileTypes", "/p:_RazorUpToDateReloadFileTypesAllowWorkaround=false");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UpToDateReloadFileTypes: ;.cs;.razor;.resx;");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task IntrospectJsonContentFiles()
        {
            var result = await DotnetMSBuild("_IntrospectContentItems");

            Assert.BuildPassed(result);
            var launchSettingsPath = Path.Combine("Properties", "launchSettings.json");
            Assert.BuildOutputContainsLine(result, $"Content: {launchSettingsPath} CopyToOutputDirectory=PreserveNewest CopyToPublishDirectory=Never ExcludeFromSingleFile=true");
            Assert.BuildOutputContainsLine(result, "Content: appsettings.json CopyToOutputDirectory=PreserveNewest CopyToPublishDirectory=PreserveNewest ExcludeFromSingleFile=true");
            Assert.BuildOutputContainsLine(result, "Content: appsettings.Development.json CopyToOutputDirectory=PreserveNewest CopyToPublishDirectory=PreserveNewest ExcludeFromSingleFile=true");
        }
    }
}
