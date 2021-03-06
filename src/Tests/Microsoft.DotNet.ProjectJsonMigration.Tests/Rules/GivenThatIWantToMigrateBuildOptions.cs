﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.DotNet.Internal.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration.Rules;
using Microsoft.DotNet.Internal.ProjectModel.Files;
using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigrateBuildOptions : TestBase
    {
        [Fact]
        public void MigratingDeprecatedCompilationOptionsWithEmitEntryPointPopulatesOutputTypeField()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""compilationOptions"": {
                        ""emitEntryPoint"": ""true""
                    },
                    ""exclude"": [
                        ""node_modules""
                    ]
                }");

            mockProj.Properties.Count(p => p.Name == "OutputType").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "OutputType").Value.Should().Be("Exe");

            mockProj.Items.Count(i => i.ItemType.Equals("Compile", StringComparison.Ordinal))
                .Should().Be(1);
            mockProj.Items.Count(i =>
                i.ItemType.Equals("Compile", StringComparison.Ordinal) &&
                i.Remove.Equals("node_modules"))
                .Should().Be(1);

            mockProj.Items.Count(i => i.ItemType.Equals("EmbeddedResource", StringComparison.Ordinal))
                .Should().Be(1);
            mockProj.Items.Count(i =>
                i.ItemType.Equals("EmbeddedResource", StringComparison.Ordinal) &&
                i.Remove.Equals("node_modules"))
                .Should().Be(1);
        }

        [Fact]
        public void SpecifiedDefaultPropertiesAreRemovedWhenTheyExistInTheCsprojTemplate()
        {
            // Setup project with default properties
            var defaultPropertiesExpectedToBeRemoved = new string[]
            {
                "OutputType",
                "TargetExt"
            };

            var defaultValue = "defaultValue";

            var templateProj = ProjectRootElement.Create();
            var defaultPropertyGroup = templateProj.AddPropertyGroup();

            foreach (var defaultPropertyName in defaultPropertiesExpectedToBeRemoved)
            {
                defaultPropertyGroup.AddProperty(defaultPropertyName, defaultValue);
            }

            // Setup projectcontext
            var testProjectDirectory = TestAssets.Get("TestAppWithRuntimeOptions")
                            .CreateInstance()
                            .WithSourceFiles()
                            .Root.FullName;
            var projectContext = ProjectContext.Create(testProjectDirectory, FrameworkConstants.CommonFrameworks.NetCoreApp10);

            var testSettings = MigrationSettings.CreateMigrationSettingsTestHook(testProjectDirectory, testProjectDirectory, templateProj);
            var testInputs = new MigrationRuleInputs(new[] {projectContext}, templateProj, templateProj.AddItemGroup(),
                templateProj.AddPropertyGroup());
            new MigrateBuildOptionsRule().Apply(testSettings, testInputs);

            defaultPropertyGroup.Properties.Count.Should().Be(0);
        }

        [Fact]
        public void MigratingEmptyBuildOptionsGeneratesAnEmptyCSProj()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": { }
                }");

            mockProj.Items.Count().Should().Be(0);
        }

        [Fact]
        public void MigratingWebProjectWithoutCustomSourcesOrResourcesDoesNotEmitCompileAndEmbeddedResource()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }");

            mockProj.Items.Count().Should().Be(0);
        }

        [Fact]
        public void MigratingConsoleProjectWithoutCustomSourcesOrResourcesDoesNotEmitCompileAndEmbeddedResource()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }");

            mockProj.Items.Count().Should().Be(0);
        }

        [Fact]
        public void MigratingOutputNamePopulatesAssemblyName()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""outputName"": ""some name""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "AssemblyName").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "AssemblyName").Value.Should().Be("some name");
        }

        [Fact]
        public void MigratingOutputNamePopulatesPackageIdWithTheProjectContainingFolderName()
        {
            var testDirectoryPath = Temp.CreateDirectory().Path;
            var testDirectoryName = new DirectoryInfo(testDirectoryPath).Name;
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""outputName"": ""some name""
                    }
                }",
                testDirectoryPath);

            mockProj.Properties.Count(p => p.Name == "PackageId").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PackageId").Value.Should().Be(testDirectoryName);
        }

        [Fact]
        public void MigratingEmitEntryPointTruePopulatesOutputTypeField()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "OutputType").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "OutputType").Value.Should().Be("Exe");
        }

        [Fact]
        public void MigratingEmitEntryPointFalsePopulatesOutputTypeFields()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "OutputType").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "OutputType").Value.Should().Be("Library");
        }

        [Fact]
        public void MigratingDefinePopulatesDefineConstants()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""define"": [ ""DEBUG"", ""TRACE"" ]
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "DefineConstants").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "DefineConstants")
                .Value.Should().Be("$(DefineConstants);DEBUG;TRACE");
        }

        [Fact]
        public void MigratingNowarnPopulatesNoWarn()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""nowarn"": [ ""CS0168"", ""CS0219"" ]
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "NoWarn").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "NoWarn").Value.Should().Be("$(NoWarn);CS0168;CS0219");
        }

        [Fact]
        public void MigratingWarningsAsErrorsPopulatesWarningsAsErrors()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""warningsAsErrors"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "TreatWarningsAsErrors").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "TreatWarningsAsErrors").Value.Should().Be("true");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""warningsAsErrors"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "TreatWarningsAsErrors").Should().Be(0);
        }

        [Fact]
        public void MigratingAllowUnsafePopulatesAllowUnsafeBlocks()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""allowUnsafe"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "AllowUnsafeBlocks").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "AllowUnsafeBlocks").Value.Should().Be("true");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""allowUnsafe"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "AllowUnsafeBlocks").Should().Be(0);
        }

        [Fact]
        public void MigratingOptimizePopulatesOptimize()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""optimize"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "Optimize").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "Optimize").Value.Should().Be("true");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""optimize"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "Optimize").Should().Be(0);
        }

        [Fact]
        public void MigratingPlatformPopulatesPlatformTarget()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""platform"": ""x64""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "PlatformTarget").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PlatformTarget").Value.Should().Be("x64");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""platform"": ""x86""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "PlatformTarget").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PlatformTarget").Value.Should().Be("x86");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""platform"": ""foo""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "PlatformTarget").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PlatformTarget").Value.Should().Be("foo");
        }

        [Fact]
        public void MigratingLanguageVersionPopulatesLangVersion()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""languageVersion"": ""5""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "LangVersion").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "LangVersion").Value.Should().Be("5");
        }

        [Fact]
        public void MigratingLanguageVersionRemovesCsharpInLangVersion()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""languageVersion"": ""csharp5""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "LangVersion").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "LangVersion").Value.Should().Be("5");
        }

        [Fact]
        public void MigratingKeyFilePopulatesAssemblyOriginatorKeyFileAndSignAssembly()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""keyFile"": ""../keyfile.snk""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "AssemblyOriginatorKeyFile").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "AssemblyOriginatorKeyFile").Value.Should().Be("../keyfile.snk");

            mockProj.Properties.Count(p => p.Name == "SignAssembly").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "SignAssembly").Value.Should().Be("true");

            mockProj.Properties.Count(p => p.Name == "PublicSign").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PublicSign").Value.Should().Be("true");
            mockProj.Properties.First(p => p.Name == "PublicSign").Condition.Should().Be(" '$(OS)' != 'Windows_NT' ");
        }

        [Fact]
        public void MigratingDelaySignPopulatesDelaySign()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""delaySign"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "DelaySign").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "DelaySign").Value.Should().Be("true");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""delaySign"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "DelaySign").Should().Be(0);
        }

        [Fact]
        public void MigratingPublicSignPopulatesPublicSign()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""publicSign"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "PublicSign").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "PublicSign").Value.Should().Be("true");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""publicSign"": ""false""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "PublicSign").Should().Be(0);
        }

        [Fact]
        public void MigratingDebugTypePopulatesDebugType()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""debugType"": ""full""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "DebugType").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "DebugType").Value.Should().Be("full");

            mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""debugType"": ""foo""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "DebugType").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "DebugType").Value.Should().Be("foo");
        }

        [Fact]
        public void MigratingXmlDocPopulatesGenerateDocumentationFile()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""xmlDoc"": ""true""
                    }
                }");

            mockProj.Properties.Count(p => p.Name == "GenerateDocumentationFile").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "GenerateDocumentationFile").Value.Should().Be("true");
        }

        [Fact]
        public void ExcludedPatternsAreNotEmittedOnNoneWhenBuildingAWebProject()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true,
                        ""copyToOutput"": {
                            ""include"": [""wwwroot"", ""**/*.cshtml"", ""appsettings.json"", ""web.config""],
                        }
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }");

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(0);
        }

        [Theory]
        [InlineData("compile", "Compile", 3, "")]
        [InlineData("embed", "EmbeddedResource", 3, ";rootfile.cs")]
        public void MigratingGroupIncludeExcludePopulatesAppropriateProjectItemElement(
            string group,
            string itemName,
            int expectedNumberOfCompileItems,
            string expectedRootFiles)
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteExtraFiles(testDirectory);

            var pj = @"
                {
                    ""buildOptions"": {
                        ""<group>"": {
                            ""include"": [""root"", ""src"", ""rootfile.cs""],
                            ""exclude"": [""src"", ""rootfile.cs""],
                            ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                            ""excludeFiles"": [""src/file2.cs""]
                        }
                    }
                }".Replace("<group>", group);

            var mockProj = RunBuildOptionsRuleOnPj(pj,
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals(itemName, StringComparison.Ordinal))
                .Should().Be(expectedNumberOfCompileItems);

            var defaultIncludePatterns = GetDefaultIncludePatterns(group);
            var defaultExcludePatterns = GetDefaultExcludePatterns(group);

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals(itemName, StringComparison.Ordinal)))
            {
                VerifyContentMetadata(item);

                if (string.IsNullOrEmpty(item.Include))
                {
                        item.Remove.Should()
                            .Be(@"src\**\*;rootfile.cs;src\file2.cs");
                }
                else if (item.Include.Contains(@"src\file1.cs"))
                {
                    item.Include.Should().Be(@"src\file1.cs;src\file2.cs");
                    item.Exclude.Should().Be(@"src\file2.cs");
                }
                else
                {
                    if (defaultIncludePatterns.Any())
                    {
                        item.Include.Should()
                            .Be($@"root\**\*;src\**\*{expectedRootFiles};" + string.Join(";", defaultIncludePatterns).Replace("/", "\\"));
                    }
                    else
                    {
                        item.Include.Should()
                            .Be($@"root\**\*;src\**\*{expectedRootFiles}");
                    }

                    if (defaultExcludePatterns.Any())
                    {
                        item.Exclude.Should()
                            .Be(@"src\**\*;rootfile.cs;" + string.Join(";", defaultExcludePatterns).Replace("/", "\\") +
                                @";src\file2.cs");
                    }
                    else
                    {
                        item.Exclude.Should()
                            .Be(@"src\**\*;rootfile.cs;src\file2.cs");
                    }
                }
            }
        }

        [Fact]
        public void MigratingCopyToOutputIncludeExcludePopulatesAppropriateProjectItemElement()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteExtraFiles(testDirectory);

            var pj = @"
                {
                    ""buildOptions"": {
                        ""copyToOutput"": {
                            ""include"": [""root"", ""src"", ""rootfile.cs""],
                            ""exclude"": [""anothersource"", ""rootfile1.cs""],
                            ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                            ""excludeFiles"": [""src/file3.cs""]
                        }
                    }
                }";

            var mockProj = RunBuildOptionsRuleOnPj(pj,
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal))
                .Should().Be(4);

            var copyItems = mockProj.Items.Where(i =>
                i.ItemType.Equals("None", StringComparison.Ordinal) &&
                i.Metadata.Any(m => m.Name == "CopyToOutputDirectory" && m.Value == "PreserveNewest"));

            copyItems.Count().Should().Be(2);

            var excludeItems = mockProj.Items.Where(i =>
                i.ItemType.Equals("None", StringComparison.Ordinal) &&
                i.Metadata.Any(m => m.Name == "CopyToOutputDirectory" && m.Value == "Never"));

            excludeItems.Count().Should().Be(2);

            foreach (var item in copyItems)
            {
                VerifyContentMetadata(item);

                if (item.Update.Contains(@"src\file1.cs"))
                {
                    item.Update.Should().Be(@"src\file1.cs;src\file2.cs");
                }
                else
                {
                    item.Update.Should().Be(@"root\**\*;src\**\*;rootfile.cs");
                }
            }

            foreach (var item in excludeItems)
            {
                VerifyContentMetadata(item);

                if (item.Update.Contains(@"src\file3.cs"))
                {
                    item.Update.Should().Be(@"src\file3.cs");
                }
                else
                {
                    item.Update.Should().Be(@"anothersource\**\*;rootfile1.cs");
                }
            }
        }

        [Theory]
        [InlineData("compile", "Compile", "")]
        [InlineData("embed", "EmbeddedResource", ";rootfile.cs")]
        public void MigratingGroupIncludeOnlyPopulatesAppropriateProjectItemElement(
            string group,
            string itemName,
            string expectedRootFiles)
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteExtraFiles(testDirectory);

            var pj = @"
                {
                    ""buildOptions"": {
                        ""<group>"": [""root"", ""src"", ""rootfile.cs""]
                    }
                }".Replace("<group>", group);

            var mockProj = RunBuildOptionsRuleOnPj(pj,
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals(itemName, StringComparison.Ordinal)).Should().Be(1);

            var defaultIncludePatterns = GetDefaultIncludePatterns(group);
            var defaultExcludePatterns = GetDefaultExcludePatterns(group);

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals(itemName, StringComparison.Ordinal)))
            {
                VerifyContentMetadata(item);

                if (defaultIncludePatterns.Any())
                {
                    item.Include.Should()
                        .Be($@"root\**\*;src\**\*{expectedRootFiles};" + string.Join(";", defaultIncludePatterns).Replace("/", "\\"));
                }
                else
                {
                    item.Include.Should()
                        .Be($@"root\**\*;src\**\*{expectedRootFiles}");
                }

                if (defaultExcludePatterns.Any())
                {
                    item.Exclude.Should()
                        .Be(string.Join(";", defaultExcludePatterns).Replace("/", "\\"));
                }
                else
                {
                    item.Exclude.Should()
                        .Be(string.Empty);
                }
            }
        }

        [Theory]
        [InlineData("copyToOutput", "None", ";rootfile.cs")]
        public void MigratingCopyToOutputIncludeOnlyPopulatesAppropriateProjectItemElement(
            string group,
            string itemName,
            string expectedRootFiles)
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteExtraFiles(testDirectory);

            var pj = @"
                {
                    ""buildOptions"": {
                        ""<group>"": [""root"", ""src"", ""rootfile.cs""]
                    }
                }".Replace("<group>", group);

            var mockProj = RunBuildOptionsRuleOnPj(pj,
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals(itemName, StringComparison.Ordinal)).Should().Be(1);

            mockProj.Items.Single().Update.Should().Be($@"root\**\*;src\**\*{expectedRootFiles}");
        }

        [Fact]
        public void MigratingTestProjectAddsGenerateRuntimeConfigurationFiles()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""testRunner"": ""xunit""
                }");

            mockProj.Properties.Count(p => p.Name == "GenerateRuntimeConfigurationFiles").Should().Be(1);
            mockProj.Properties.First(p => p.Name == "GenerateRuntimeConfigurationFiles").Value.Should().Be("true");
        }

        [Fact]
        public void MigratingANonTestProjectDoesNotAddGenerateRuntimeConfigurationFiles()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                }");

            mockProj.Properties.Count(p => p.Name == "GenerateRuntimeConfigurationFiles").Should().Be(0);
        }

        [Fact]
        public void MigratingAnAppWithAppConfigAddsItAsNoneToTheCsProj()
        {
            var tempDirectory = Temp.CreateDirectory().Path;
            File.Create(Path.Combine(tempDirectory, "App.config")).Dispose();
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                }",
                tempDirectory);

            mockProj.Items.Count(i => i.ItemType == "None").Should().Be(1);
            mockProj.Items.First(i => i.ItemType == "None").Include.Should().Be("App.config");
        }

        [Fact]
        public void MigratingCompileIncludeWithPlainFileNamesRemovesThem()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""compile"": {
                            ""include"": [""filename1.cs"", ""filename2.cs""],
                        }
                    }
                }");

            mockProj.Items.Count(i => i.ItemType.Equals("Compile", StringComparison.Ordinal)).Should().Be(0);
        }

        [Fact]
        public void MigratingCompileIncludeFilesWithPlainFileNamesRemovesThem()
        {
            var mockProj = RunBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""compile"": {
                            ""includeFiles"": [""filename1.cs"", ""filename2.cs""],
                        }
                    }
                }");

            mockProj.Items.Count(i => i.ItemType.Equals("Compile", StringComparison.Ordinal)).Should().Be(0);
        }

        [Fact]
        public void MigratingProjectWithCompileResourcesPopulatesAppropriateProjectItemElements()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteCompileResourceFiles(testDirectory);

            var pj = @"
                {
                  ""version"": ""1.0.0-*"",
                  ""dependencies"": {
                    ""NETStandard.Library"": ""1.6.0""
                  },
                  ""frameworks"": {
                    ""netstandard1.5"": {}
                  }
                }";

            var mockProj = RunBuildOptionsRuleOnPj(pj, testDirectory);

            var compileItems = mockProj.Items.Where(i => i.ItemType.Equals("Compile", StringComparison.Ordinal));
            compileItems.Count().Should().Be(1);
            var compileItem = compileItems.Single();
            compileItem.Include.Should().BeEmpty();
            compileItem.Exclude.Should().BeEmpty();
            compileItem.Remove.Should().Be(@"compiler\resources\*");

            var embeddedResourceItems = mockProj.Items.Where(
                i => i.ItemType.Equals("EmbeddedResource", StringComparison.Ordinal));
            embeddedResourceItems.Count().Should().Be(1);
            var embeddedResourceItem = embeddedResourceItems.Single();
            embeddedResourceItem.Include.Should().Be(@"compiler\resources\*");
            embeddedResourceItem.Exclude.Should().BeEmpty();
            embeddedResourceItem.Remove.Should().BeEmpty();
        }

        [Fact]
        public void MigratingProjectWithoutCompileResourcesDoesNotAddProjectItemElements()
        {
            var testDirectory = Temp.CreateDirectory().Path;

            var pj = @"
                {
                  ""version"": ""1.0.0-*"",
                  ""dependencies"": {
                    ""NETStandard.Library"": ""1.6.0""
                  },
                  ""frameworks"": {
                    ""netstandard1.5"": {}
                  }
                }";

            var mockProj = RunBuildOptionsRuleOnPj(pj, testDirectory);
            mockProj.Items.Count.Should().Be(0);
        }

        private static IEnumerable<string> GetDefaultExcludePatterns(string group)
        {
            var defaultExcludePatterns = new List<string>(group == "copyToOutput" ?
                                    Enumerable.Empty<string>() :
                                    ProjectFilesCollection.DefaultBuiltInExcludePatterns);

            if (group == "embed")
            {
                defaultExcludePatterns.Add("@(EmbeddedResource)");
            }

            return defaultExcludePatterns;
        }

        private static IEnumerable<string> GetDefaultIncludePatterns(string group)
        {
            return Enumerable.Empty<string>();
        }

        private static void VerifyContentMetadata(ProjectItemElement item)
        {
            if (item.ItemType == "None")
            {
                item.Metadata.Count(m => m.Name == "CopyToOutputDirectory").Should().Be(1);
            }
        }

        private void WriteExtraFiles(string directory)
        {
            Directory.CreateDirectory(Path.Combine(directory, "root"));
            Directory.CreateDirectory(Path.Combine(directory, "src"));
            Directory.CreateDirectory(Path.Combine(directory, "anothersource"));
            File.WriteAllText(Path.Combine(directory, "root", "file1.txt"), "content");
            File.WriteAllText(Path.Combine(directory, "root", "file2.txt"), "content");
            File.WriteAllText(Path.Combine(directory, "root", "file3.txt"), "content");
            File.WriteAllText(Path.Combine(directory, "src", "file1.cs"), "content");
            File.WriteAllText(Path.Combine(directory, "src", "file2.cs"), "content");
            File.WriteAllText(Path.Combine(directory, "src", "file3.cs"), "content");
            File.WriteAllText(Path.Combine(directory, "anothersource", "file4.cs"), "content");
            File.WriteAllText(Path.Combine(directory, "rootfile.cs"), "content");
        }

        private void WriteCompileResourceFiles(string directory)
        {
            Directory.CreateDirectory(Path.Combine(directory, "compiler"));
            Directory.CreateDirectory(Path.Combine(directory, "compiler", "resources"));
            File.WriteAllText(Path.Combine(directory, "compiler", "resources", "file.cs"), "content");
        }

        private ProjectRootElement RunBuildOptionsRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            return TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigrateBuildOptionsRule()
            }, s, testDirectory);
        }
    }
}
