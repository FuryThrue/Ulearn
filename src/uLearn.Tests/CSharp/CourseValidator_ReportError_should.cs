﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using test;
using uLearn.Extensions;
using uLearn.Model;
using uLearn.Model.Blocks;

namespace uLearn.CSharp
{
	[TestFixture]
	public class CourseValidator_ReportError_should
	{
		private static string tempSlideFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "ReportErrorTests_Temp_SlideFolder");
		private static DirectoryInfo tempSlideFolder = new DirectoryInfo(tempSlideFolderPath);

		private static ProjectExerciseBlock exBlock;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			TestsHelper.RecreateDirectory(tempSlideFolderPath);
		}

		[SetUp]
		public void SetUp()
		{
			exBlock = new ProjectExerciseBlock
			{
				StartupObject = "test.Program",
				UserCodeFilePath = TestsHelper.UserCodeFileName,
				SlideFolderPath = tempSlideFolder,
				CsProjFilePath = TestsHelper.CsProjFilePath,
			};
			FileSystem.CopyDirectory(TestsHelper.ProjSlideFolderPath, tempSlideFolderPath, true);

			string studentZipFilepath = Path.Combine(tempSlideFolderPath, "ProjDir.exercise.zip");
			if (File.Exists(studentZipFilepath))
				File.Delete(studentZipFilepath);

			var ctx = new BuildUpContext(new Unit(null, exBlock.SlideFolderPath), CourseSettings.DefaultSettings, null, String.Empty);
			exBlock.BuildUp(ctx, ImmutableHashSet<string>.Empty).ToList();
		}

		[Test]
		public void ReportError_If_StudentZip_HasErrors()
		{
			FileSystem.CopyDirectory(tempSlideFolder.GetSubdir("projDir").FullName, tempSlideFolder.GetSubdir("FullProjDir").FullName);
			exBlock.CsProjFilePath = Path.Combine("FullProjDir", TestsHelper.CsProjFilename);
			SaveTempZipFileWithFullProject();

			var validatorOutput = TestsHelper.ValidateBlock(exBlock);

			validatorOutput
				.Should().Contain($"Student zip exercise directory has 'wrong answer' and/or solution files ({TestsHelper.OrderedWrongAnswersAndSolutionNames})");
			validatorOutput
				.Should().Contain($"Student's csproj has user code item ({exBlock.UserCodeFilePath}) of not compile type");
			validatorOutput
				.Should().Contain($"Student's csproj has 'wrong answer' and/or solution items ({TestsHelper.OrderedWrongAnswersAndSolutionNames})");
		}

		private void SaveTempZipFileWithFullProject()
		{
			var zipWithFullProj = new FileInfo(Path.Combine(tempSlideFolderPath, "FullProjDir.exercise.zip"));
			var noExcludedFiles = new Func<string, bool>(_ => false);
			var noExcludedDirs = new string[0];

			var csProjFile = TestsHelper.ProjExerciseFolder.GetFile(TestsHelper.CsProjFilename);
			ProjModifier.ModifyCsproj(csProjFile, ProjModifier.ResolveLinks);

			new LazilyUpdatingZip(
					TestsHelper.ProjExerciseFolder,
					noExcludedDirs,
					noExcludedFiles,
					ResolveCsprojLink,
					zipWithFullProj)
				.UpdateZip();

			byte[] ResolveCsprojLink(FileInfo file)
			{
				return file.Name.Equals(exBlock.CsprojFileName) ? ProjModifier.ModifyCsproj(file, ProjModifier.ResolveLinks) : null;
			}
		}

		[Test]
		public void ReportError_If_ExerciseFolder_HasErrors()
		{
			File.Delete(exBlock.UserCodeFile.FullName);
			File.Delete(Path.Combine(tempSlideFolderPath, exBlock.CsProjFilePath));

			var validatorOutput = TestsHelper.ValidateBlock(exBlock);

			validatorOutput
				.Should().Contain($"Exercise folder ({exBlock.ExerciseFolder.Name}) doesn't contain ({exBlock.CsprojFileName})");
			validatorOutput
				.Should().Contain($"Exercise folder ({exBlock.ExerciseFolder.Name}) doesn't contain ({exBlock.UserCodeFilePath})");
		}

		[Test]
		public void ReportError_If_CorrectSolution_Not_Building()
		{
			File.WriteAllText(exBlock.CorrectSolutionFile.FullName, "");

			var validatorOutput = TestsHelper.ValidateBlock(exBlock);

			validatorOutput
				.Should().Contain($"Correct solution file {exBlock.CorrectSolutionFileName} verdict is not OK. RunResult = Id: test.csproj, Verdict: CompilationError");
		}

		[Test]
		public void ReportError_If_NUnitTestRunner_Tries_To_Run_NonExisting_Test_Class()
		{
				exBlock.NUnitTestClasses = new[] { "non_existing.test_class", };

			var validatorOutput = TestsHelper.ValidateBlock(exBlock);

				validatorOutput
					.Should()
				.Contain($"Correct solution file {exBlock.CorrectSolutionFileName} verdict is not OK. RunResult = Id: test.csproj, Verdict: RuntimeError: System.ArgumentException: Error in checking system: test class non_existing.test_class does not exist");
			}

		[Test]
		public void ReportError_If_Solution_For_ProjectExerciseBlock_Is_Not_Solution()
		{
				exBlock.NUnitTestClasses = new[] { $"test.{nameof(OneFailingTest)}" };

			var validatorOutput = TestsHelper.ValidateBlock(exBlock);

				validatorOutput
					.Should()
				.Contain($"Correct solution file {exBlock.CorrectSolutionFileName} is not solution. RunResult = Id: test.csproj, Verdict: Ok")
					.And
					.Contain("Error on NUnit test: I_am_a_failure");
			}
			}
}