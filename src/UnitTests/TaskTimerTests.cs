using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IKriv.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskTimerUnitTests
{
    [TestClass]
    public class TaskTimerTests
    {
        private CustomTimer _customTimer;
        private Func<Action, IDisposable> _getCustomTimer;

        private class CustomTimer : IDisposable
        {
            private Action _callback;
            

            public CustomTimer SetCallback(Action callback)
            {
                if (_callback != null) throw new InvalidOperationException("Unexpected behavior: timer created more than once");
                _callback = callback;
                return this;
            }

            public bool IsDisposed { get; private set; }


            public void Tick()
            {
                _callback();
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private ITaskTimer GetTimer()
        {
            return new TaskTimer(0).StartOnTimer(_getCustomTimer);
        }

        private static bool IsFinished(Task task)
        {
            return task.IsCompleted || task.IsCanceled || task.IsFaulted;
        }

        [TestInitialize]
        public void Setup()
        {
            _customTimer = new CustomTimer();
            _getCustomTimer = callback => _customTimer.SetCallback(callback);
        }

        [TestMethod]
        public void NoTasksAreFinished_UntilTimerTicks()
        {
            using (var timer = GetTimer())
            {
                var tasks = timer.Take(10).ToArray();
                Assert.IsFalse(tasks.Any(IsFinished));
            }
        }

        [TestMethod]
        public void NumberOfTasksFinished_EqualsNumberOfTimerTicks()
        {
            using (var timer = GetTimer())
            {
                var tasks = timer.Take(10).ToArray();

                for (int i = 1; i < 10; ++i)
                {
                    _customTimer.Tick();
                    Assert.IsTrue(tasks.Take(i).All(IsFinished), $"Expected tasks {i} tasks to be finished");
                    Assert.IsFalse(tasks.Skip(i).Any(IsFinished), $"Expected tasks after {i} to be not finished");
                }
            }
        }

        [TestMethod]
        public void LateTask_ComesAsComplete()
        {
            using (var timer = GetTimer())
            {
                _customTimer.Tick();
                var firstTask = timer.First();
                Assert.IsTrue(firstTask.IsCompleted);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CannotEnumerateTimerTwice()
        {
            using (var timer = GetTimer())
            {
                var unused = timer.Take(3).ToArray(); // iterate over the timer

                try
                {
                    var unused2 = timer.Take(3).ToArray(); // iterate over the timer again
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual("Timer cannot be enumerated twice", e.Message);
                    throw;
                }
            }
        }

        [TestMethod]
        public void Dispose_DisposesTimer()
        {
            var timer = GetTimer();
            Assert.IsFalse(_customTimer.IsDisposed);
            timer.Dispose();
            Assert.IsTrue(_customTimer.IsDisposed);
        }

        [TestMethod]
        public void Cancel_CancelsAllPendingTasks()
        {
            var cancelSource = new CancellationTokenSource();

            using (var timer = new TaskTimer(0).CancelWith(cancelSource.Token).StartOnTimer(_getCustomTimer))
            {
                var tasks = timer.Take(10).ToArray();

                // tick the timer 3 times
                for (int i = 0; i < 3; ++i) _customTimer.Tick();

                cancelSource.Cancel();

                Assert.IsTrue(tasks.Take(3).All(t=>t.IsCompleted));
                Assert.IsTrue(tasks.Skip(3).All(t => t.IsCanceled));
            }
        }

        [TestMethod]
        public void TaskCreatedAfterCancel_IsCanceledImmediately()
        {
            var cancelSource = new CancellationTokenSource();

            using (var timer = new TaskTimer(0).CancelWith(cancelSource.Token).StartOnTimer(_getCustomTimer))
            {
                using (var enumerator = timer.GetEnumerator())
                {
                    // skip 3 tasks
                    for (int i = 0; i < 3; ++i) enumerator.MoveNext();

                    cancelSource.Cancel();

                    enumerator.MoveNext();
                    var task = enumerator.Current;
                    Assert.IsNotNull(task);
                    Assert.IsTrue(task.IsCanceled);
                }
            }
        }
    }
}
