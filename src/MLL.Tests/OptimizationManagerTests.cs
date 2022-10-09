using MLL.Common.Optimization;
using MLL.Common.Threading;
using MLL.Layer.Threading;
using MLL.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLL.Tests;

[TestFixture]
public class OptimizationManagerTests
{
    [Test]
    public void Test_OptimizationManager()
    {
        var threadsToTimings = new Dictionary<int, float>
        {
            [1] = 1000,
            [2] = 900,
            [3] = 800,
            [4] = 1009,
            [5] = 1100
        };

        var timeTrackerMock = new TimeTrackerMock();
        var computerMock = new ThreadedComputerMock();

        var controller = new ThreadedProcessorController(computerMock, timeTrackerMock);

        var collector = new ThreadedProcessorStatCollector(
            controller, 100, 0.05f, 5, ListSelection.Range(5), () => { });

        var manager = new OptimizationManager(collector);

        foreach (var (thread, timingsMs) in threadsToTimings)
        {
            var timings = TimeSpan.FromMilliseconds(timingsMs);
            timeTrackerMock.Timings.AddRange(Enumerable.Repeat(timings, 100));
            manager.Optimize();
        }

        Assert.AreEqual(threadsToTimings.MinBy(x => x.Value).Key, 
            controller.Computer.ThreadInfo.Threads);
    }

    public class TimeTrackerMock : ITimeTracker
    {
        public List<TimeSpan> Timings { get; } = new();
    }

    public class ThreadedComputerMock : IThreadedComputer
    {
        public LayerThreadInfo ThreadInfo { get; set; }
    }
}
