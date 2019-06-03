﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Tailviewer.BusinessLogic.DataSources;
using Tailviewer.BusinessLogic.LogFiles;
using Tailviewer.Settings;

namespace Tailviewer.Test.BusinessLogic.DataSources
{
	[TestFixture]
	[Issue("https://github.com/Kittyfisto/Tailviewer/issues/125")]
	public sealed class FolderDataSourceTest
	{
		[SetUp]
		public void SetUp()
		{
			_taskScheduler = new ManualTaskScheduler();
			_logFileFactory = new PluginLogFileFactory(_taskScheduler);
			_filesystem = new InMemoryFilesystem();
			_settings = new DataSource
			{
				Id = DataSourceId.CreateNew(),
				MergedDataSourceDisplayMode = DataSourceDisplayMode.CharacterCode
			};
		}

		private ManualTaskScheduler _taskScheduler;
		private DataSource _settings;
		private ILogFileFactory _logFileFactory;
		private InMemoryFilesystem _filesystem;

		[Test]
		public void TearDown()
		{
		}

		[Test]
		public void TestChange([Values(null, "", @"C:\temp")] string folderPath,
		                       [Values(null, "", "*.log")] string logFileRegex,
		                       [Values(true, false)] bool recursive)
		{
			var dataSource = new FolderDataSource(_taskScheduler,
			                                      _logFileFactory,
			                                      _filesystem,
			                                      _settings,
			                                      TimeSpan.Zero);
			dataSource.Change(folderPath, logFileRegex, recursive);
			dataSource.LogFileFolderPath.Should().Be(folderPath);
			dataSource.LogFileRegex.Should().Be(logFileRegex);
			dataSource.Recursive.Should().Be(recursive);

			_settings.LogFileFolderPath.Should().Be(folderPath);
			_settings.LogFileRegex.Should().Be(logFileRegex);
			_settings.Recursive.Should().Be(recursive);
		}

		[Test]
		public void TestNoSuchFolder()
		{
			var dataSource = new FolderDataSource(_taskScheduler,
			                                      _logFileFactory,
			                                      _filesystem,
			                                      _settings,
			                                      TimeSpan.Zero);
			var path = Path.Combine(_filesystem.Roots.Result.First().FullName, "logs");
			dataSource.Change(path, "*", false);

			_taskScheduler.RunOnce();
			dataSource.OriginalSources.Should().BeEmpty();
		}

		[Test]
		public void TestNoSuchDrive()
		{
			var dataSource = new FolderDataSource(_taskScheduler,
			                                      _logFileFactory,
			                                      _filesystem,
			                                      _settings,
			                                      TimeSpan.Zero);

			var path = Path.Combine("Z:", "logs");
			dataSource.Change(path, "*", false);

			dataSource.OriginalSources.Should().BeEmpty();
		}

		[Test]
		public void TestEmptyFolder()
		{
			var dataSource = new FolderDataSource(_taskScheduler,
			                                      _logFileFactory,
			                                      _filesystem,
			                                      _settings,
			                                      TimeSpan.Zero);

			var path = Path.Combine(_filesystem.Roots.Result.First().FullName, "logs");
			_filesystem.CreateDirectory(path).Wait();

			dataSource.Change(path, "*", false);

			dataSource.OriginalSources.Should().BeEmpty();
		}

		[Test]
		[Ignore("Not yet implemented")]
		public void TestOneMatchingLogFile()
		{
			var dataSource = new FolderDataSource(_taskScheduler,
			                                      _logFileFactory,
			                                      _filesystem,
			                                      _settings,
			                                      TimeSpan.Zero);

			var path = Path.Combine(_filesystem.Roots.Result.First().FullName, "logs");
			_filesystem.CreateDirectory(path);
			_filesystem.WriteAllBytes(Path.Combine(path, "foo.log"), new byte[0]);
			_filesystem.WriteAllBytes(Path.Combine(path, "foo.txt"), new byte[0]).Wait();

			dataSource.Change(path, "*.log", false);

			dataSource.Property(x => (IEnumerable<IDataSource>)x.OriginalSources).ShouldEventually().HaveCount(1);
			var child = dataSource.OriginalSources[0];
			child.FullFileName.Should().Be(Path.Combine(path, "foo.log"));
		}
	}
}