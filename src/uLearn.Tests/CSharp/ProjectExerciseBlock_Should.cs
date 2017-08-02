﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Build.Evaluation;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using RunCsJob;
using RunCsJob.Api;
using test;
using uLearn.Extensions;
using uLearn.Model;
using uLearn.Model.Blocks;

namespace uLearn.CSharp
{
	[TestFixture]
	public class ProjectExerciseBlock_Should
	{
		private ProjectExerciseBlock ex;
		private List<SlideBlock> exBlocks;

		private string tempSlideFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(ProjectExerciseBlock_Should));
		private DirectoryInfo tempSlideFolder => new DirectoryInfo(tempSlideFolderPath);

		private string studentExerciseFolderPath => Path.Combine(tempSlideFolderPath, "ProjectExerciseBlockTests_Student_ExerciseFolder");
		private DirectoryInfo studentExerciseFolder => new DirectoryInfo(studentExerciseFolderPath);

		private string checkerExerciseFolderPath => Path.Combine(tempSlideFolderPath, "ProjectExerciseBlockTests_Checker_ExerciseFolder");

		private string studentCsProjFilePath => Path.Combine(studentExerciseFolderPath, TestsHelper.CsProjFilename);
		private string checkerCsprojFilePath => Path.Combine(checkerExerciseFolderPath, TestsHelper.CsProjFilename);

		private Project studentZipCsproj;
		private Project checkerZipCsproj;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			TestsHelper.RecreateDirectory(tempSlideFolderPath);
			FileSystem.CopyDirectory(TestsHelper.ProjSlideFolderPath, tempSlideFolderPath);

			TestsHelper.RecreateDirectory(checkerExerciseFolderPath);
			TestsHelper.RecreateDirectory(studentExerciseFolderPath);

			ex = new ProjectExerciseBlock
			{
				StartupObject = "test.Program",
				UserCodeFilePath = TestsHelper.UserCodeFileName,
				SlideFolderPath = tempSlideFolder,
				CsProjFilePath = TestsHelper.CsProjFilePath
			};

			var ctx = new BuildUpContext(new Unit(null, ex.SlideFolderPath), CourseSettings.DefaultSettings, null, String.Empty);
			exBlocks = ex.BuildUp(ctx, ImmutableHashSet<string>.Empty).ToList();
			Utils.UnpackZip(ex.StudentsZip.Content(), studentExerciseFolderPath);

			var zipBytes = ex.GetZipBytesForChecker("i_am_user_code");
			Utils.UnpackZip(zipBytes, checkerExerciseFolderPath);

			studentZipCsproj = new Project(studentCsProjFilePath, null, null, new ProjectCollection());
			checkerZipCsproj = new Project(checkerCsprojFilePath, null, null, new ProjectCollection());
		}

		[Test]
		public void FindSolutionFile_OnBuildUp()
		{
			var correctSolutionCode = ex.CorrectSolutionFile.ContentAsUtf8();

			exBlocks.OfType<CodeBlock>()
				.Should().Contain(block => block.Code.Equals(correctSolutionCode) && block.Hide);
		}

		[Test]
		public void When_CreateStudentZip_Contain_UserCodeFile_OfCompileType_Inside_Csproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsNamesForCompile(studentZipCsproj);

			itemNamesForCompile.Should().Contain(TestsHelper.UserCodeFileName);
		}

		[Test]
		public void When_CreateStudentZip_Contain_Resolved_Links_Inside_Csproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsForCompile(checkerZipCsproj);

			itemNamesForCompile.Should().Contain(i => i.UnevaluatedInclude.Equals("~$Link.cs"));
		}

		[Test]
		public void When_CreateStudentZip_Contain_Resolved_Link_Files_Inside_Csproj()
		{
			var projFiles = studentExerciseFolder.GetFiles().Select(f => f.Name);

			projFiles.Should().Contain("~$Link.cs");
		}

		[Test]
		public void When_CreateStudentZip_NotContain_AnyWrongAnswersOrSolution_OfCompileType_InsideCsproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsNamesForCompile(studentZipCsproj);

			itemNamesForCompile.Should().NotContain(TestsHelper.WrongAnswersAndSolutionNames);
		}

		[Test]
		public void When_CreateStudentZip_NotContain_AnyWrongAnswersOrSolution_Inside_ExerciseDirectory()
		{
			var projFiles = studentExerciseFolder.GetFiles().Select(f => f.Name);

			projFiles.Should().NotContain(TestsHelper.WrongAnswersAndSolutionNames);
		}

		[Test]
		public void When_CreateStudentZip_Make_Project_Able_To_Compile_If_Project_Depends_On_Many_Tasks()
		{
			var submission = new ProjRunnerSubmission
			{
				Id = "my_id",
				Input = "",
				NeedRun = true,
				ProjectFileName = "test.csproj",
				ZipFileData = ex.StudentsZip.Content()
			};
			var result = SandboxRunner.Run(submission);

			result.CompilationOutput.Should().Be("");
			result.Error.Should().Be("");
			result.Verdict.Should().Be(Verdict.Ok);
		}

		[Test]
		public void When_CreateCheckerZip_Contain_UserCodeFile_OfCompileType_InsideCsproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsNamesForCompile(checkerZipCsproj);

			itemNamesForCompile.Should().Contain(TestsHelper.UserCodeFileName);
		}

		[Test]
		public void When_CreateCheckerZip_NotContain_CorrectSolution_OfCompileType_InsideCsproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsNamesForCompile(checkerZipCsproj);

			itemNamesForCompile.Should().NotContain(ex.CorrectSolutionFileName);
		}

		[Test]
		public void When_CreateCheckerZip_NotRemove_OtherSolutions_OfCompileType_FromCsproj()
		{
			var itemNamesForCompile = GetFromCsProjItemsNamesForCompile(checkerZipCsproj);
			var anotherSolutionReferencedByCurrentSolution = $"{nameof(AnotherTask)}.Solution.cs";

			itemNamesForCompile.Should().Contain(anotherSolutionReferencedByCurrentSolution);
		}

		private List<string> GetFromCsProjItemsNamesForCompile(Project csproj)
		{
			return GetFromCsProjItemsForCompile(csproj).Select(i => i.UnevaluatedInclude).ToList();
		}

		private List<ProjectItem> GetFromCsProjItemsForCompile(Project csproj)
		{
			return csproj.Items
				.Where(i => i.ItemType.Equals("Compile", StringComparison.InvariantCultureIgnoreCase))
				.ToList();
		}
	}
}