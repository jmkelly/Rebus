﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Timers.Factories;

#pragma warning disable 1998

namespace Rebus.Tests.Timers
{
    [TestFixture(typeof(TplTaskFactory))]
    [TestFixture(typeof(TimerTaskFactory))]
    [TestFixture(typeof(ThreadingTimerTaskFactory))]
    public class TestAsyncTask<TFactory> : FixtureBase where TFactory : IAsyncTaskFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public async Task CanActuallyStopTaskWithLongInterval(int secondsToLetTheTaskRun)
        {
            var task = _factory.CreateTask(TimeSpan.FromMinutes(4.5), async () => { Console.WriteLine("INVOKED!!!"); });

            using (task)
            {
                task.Start();

                Console.WriteLine($"Letting the task run for {secondsToLetTheTaskRun} seconds...");

                await Task.Delay(TimeSpan.FromSeconds(secondsToLetTheTaskRun));

                Console.WriteLine("Quitting....");
            }

            Console.WriteLine("Done!");
        }

        [Test]
        public async Task DoesNotDieOnTransientErrors()
        {
            var throwException = true;
            var taskWasCompleted = false;

            var task = _factory.CreateTask(TimeSpan.FromMilliseconds(400), async () =>
            {
                if (throwException)
                {
                    throw new Exception("but you told me to do it!");
                }

                taskWasCompleted = true;
            });

            using (task)
            {
                Console.WriteLine("Starting the task...");
                task.Start();

                Console.WriteLine("Waiting for task to run a little...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Console.WriteLine("Suddenly, the transient error disappears...");
                throwException = false;

                Console.WriteLine("and life goes on...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.That(taskWasCompleted, Is.True, "The task did NOT resume properly after experiencing exceptions!");
            }
        }

        [Test]
        public async Task ItWorks()
        {
            var stopwatch = Stopwatch.StartNew();
            var events = new ConcurrentQueue<TimeSpan>();
            var task = _factory.CreateTask(TimeSpan.FromSeconds(0.2),
                async () =>
                {
                    events.Enqueue(stopwatch.Elapsed);
                });

            using (task)
            {
                task.Start();

                await Task.Delay(1199);
            }

            Console.WriteLine(string.Join(Environment.NewLine, events));

            Assert.That(events.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(events.Count, Is.LessThanOrEqualTo(7));
        }
    }
}