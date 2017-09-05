# TaskTimer: Tasks that complete on timer

`TaskTimer` is `Task.Delay()` on steroids: it returns a series of Tasks that become completed on timer. This is similar in principle to [Observable.Interval](https://msdn.microsoft.com/en-us/library/system.reactive.linq.observable.interval(v=vs.103).aspx), but uses `Task` instead of `Observable`.

    async Task DoStuffOnTimer()
    {
        using (var timer = new TaskTimer(1000).Start())
        {
        	// Call DoStuff() every second
            foreach (var task in timer)
            {
                await task;
                DoStuff();
            }
        }
    }

## Using TaskTimer

Manually add `TaskTimer.cs` to your project, or use  TaskTimer Nuget package: https://www.nuget.org/packages/TaskTimer/1.0.0.

First step is to create a timer. You must specify the period and the initial delay or start time:

    // period=1000ms, starts immediately
    var timer1 = new TaskTimer(1000).Start();

    // period=1 second, starts immediately
    var timer2 = new TaskTimer(TimeSpan.FromSeconds(1)).Start();

    // period=1000ms, starts 300ms from now
    var timer3 = new TaskTimer(1000).Start(300);

    // period=1000ms, starts 5 minutes from now
    var timer4 = new TaskTimer(1000).Start(TimeSpan.FromMinutes(5));

    // period=1000ms, starts at next UTC midnight
    var timer5 = new TaskTimer(1000).StartAt(DateTime.UtcNow.Date.AddDays(1));
    
Once the timer is created, it returns an infinite series of tasks, which become completed on the "tick" of the timer, one by one. The timer is disposable, it must be put in the `using` block:

    using (var timer = new TaskTimer(1000).Start())
    {
        // infinite loop, calls DoSomethingUseful() once per second
        foreach (var task in timer)
        {
            await task;
            DoSomethingUseful();
        }
    }

## Avoiding infinite loop

The series of task that the timer returns it infinite. The following will not work, as it wwould require an array of infinite length:

	var allTasksTillTheEndOfTime = new TaskTimer(1000).ToArray(); // will fail!

If you know the number of iterations in advance, use Take() method:

	using (var timer = new TaskTimer(1000).Start())
    {
        // loop executes 10 times
        foreach (var task in timer.Take(10))
        {
            await task;
            DoSomethingUseful();
        }
	}

If you don’t, you can use a cancellation token. You can pass the cancellation token to the timer using the `CancelWith()` method:

    async Task Tick(CancellationToken token)
    {
        using (var timer = new TaskTimer(1000).CancelWith(token).Start())
        {
            try
            {
                foreach (var task in timer)
                {
                    await task;
                    DoSomethingUseful();
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Timer Canceled");
            }
        }
    }
   
## Why not just use Task.Delay()

`Task.Delay()` is good enough for many cases, but when run in a loop, it has a problem. Consider the following code:

    async Task UseTaskDelay()
    {
        while (true)
        {
            DoSomething(); // takes 300ms on average
            await Task.Delay(1000);
        }
    }

One execution of the loop takes 1300ms on average, instead of  1000ms we wanted. So, on every execution we will accumulate an additional delay of 300ms. If DoSomething() takes different time from iteration to iteration, our activity will not only be late, but it will also be irregular. One could, of course, use real-time clock to adjust the delay after every execution, but this is quite laborious and hard to encapsulate.

## Timer Drift

`TaskTimer` uses `System.Threading.Timer` internally. It will definitely give you more accuracy that repeated invocation of `Task.Delay()`, but it is not the most accurate timer in the world. From my experience, it will drift by a fraction of a millisecond per loop, and it may lead to sizable delays over hundreds or thousands of loops. The bad news is that you would hardly do better with any other timer mechanism that relies on `System.Threading.Timer` or `System.Timers.Timer`. Allegedly "multimedia timers" work better, but I've never used them. More details here: https://stackoverflow.com/questions/6259120/system-threading-timer-call-drifts-a-couple-seconds-every-day.

## About Disposable Enumerable

`Start()` method creates the underlying `System.Threading.Timer` object. If requested initial delay is zero, the timer starts ticking immediately. This timer must be eventually disposed of, so the `Start()` method returns an object of type  `ITaskTimer`, which is both `IDisposable` and `IEnumerable`.

The timer can be iterated over only once. Attempt to iterate over the timer again will throw an `InvalidOperationException`:

    using (var timer = new TaskTimer(1000).Start())
    {
        // loop executes 10 times
        foreach (var task in timer.Take(10))
        {
            await task;
            DoSomethingUseful();
        }

        // Attempt to start from the beginning and iterate over the timer again
        // This will not work, it throws an InvalidOperationException
        foreach (var task in timer.Take(20)) ...
    }

Traditionally in .NET it's `IEnumerator` that's disposable, and not `IEnumerable`. Compiler generated code for `foreach` loop creates an `IEnumerator` in the beginning and disposes it in the end. 

We could follow this paradigm, but then creation of the underlying system timer and 'ticking' would not happen until one actually start iterating over the tasks, and each iteration would create its own timer. This is somewhat similar to how "hot observables" behave, and I personally find it very confusing. Therefore, this is NOT how `TaskTimer` behaves. `TaskTimer` creates the timer immediately, and it will be ticking whether it is being iterated over or not. The downside of it is that you have to manually dispose the enumerable.