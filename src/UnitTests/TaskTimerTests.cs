using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IKriv.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskTimerUnitTests
{
    [TestClass]
    public class TaskTimerTests
    {
        private CustomTimer _customTimer;

        class CustomTimer : IDisposable
        {
            private Action _callback;

            public CustomTimer SetCallback(Action callback)
            {
                if (_callback != null) throw new InvalidOperationException("Unexpected behavior: timer created more than once");
                _callback = callback;
                return this;
            }


            public void Tick()
            {
                _callback();
            }

            public void Dispose()
            {
            }
        }

        private IEnumerable<Task> GetTasks()
        {
            return new TaskTimer(0).StartOnTimer(callback=>_customTimer.SetCallback(callback));
        }

        private static bool IsFinished(Task task)
        {
            return task.IsCompleted || task.IsCanceled || task.IsFaulted;
        }

        [TestInitialize]
        public void Setup()
        {
            _customTimer = new CustomTimer();
        }

        [TestMethod]
        public void NoTasksAreFinished_UntilTimerTicks()
        {
            var tasks = GetTasks().Take(10).ToArray();
            Assert.IsFalse(tasks.Any(IsFinished));
        }

        [TestMethod]
        public void NumberOfTasksFinished_EqualsNumberOfTimerTicks()
        {
            var tasks = GetTasks().Take(10).ToArray();

            for (int i = 1; i < 10; ++i)
            {
                _customTimer.Tick();
                Assert.IsTrue(tasks.Take(i).All(IsFinished), $"Expected tasks {i} tasks to be finished");
                Assert.IsFalse(tasks.Skip(i).Any(IsFinished), $"Expected tasks after {i} to be not finished");
            }
        }

        /*
        [TestMethod]
        public void LateTask_ComesAsComplete()
        {
            var tasks = GetTasks();
            _customTimer.Tick();
            var firstTask = tasks.First();
            Assert.IsTrue(firstTask.IsCompleted);
        }
        */

    }
}
