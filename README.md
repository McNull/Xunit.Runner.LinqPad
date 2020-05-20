# Xunit.Runner.LinqPad

Run [Xunit](https://xunit.github.io/) tests within LinqPad.

## Example

```csharp
void Main()
{
	XunitRunner.Run(Assembly.GetExecutingAssembly());
}

// Define other methods and classes here

public class Class1
{
	[Fact]
	public void PassingTest()
	{
		Assert.Equal(4, Add(2, 2));
	}

	[Fact]
	public void FailingTest()
	{
		Assert.Equal(5, Add(2, 2));
	}

	int Add(int x, int y)
	{
		return x + y;
	}
}
```

## Configuration

`XunitRunner` includes default actions that write information to the console for `OnDiscoveryComplete()`, `OnExecutionComplete()`, `OnTestFailed()`, and `OnTestSkipped()`. If you want more control over what happens for these or other events, you can pass an `Action<AssemblyRunner>` into `Run()`.

Each `Action` you pass in that takes some action on the UI thread (like `Console.WriteLine()` should start with `lock (XunitRunner.Sync)` .

```csharp
void Main()
{
    Action<AssemblyRunner> configure = r =>
    {
        r.OnTestFailed = i => { lock (XunitRunner.Sync) Console.WriteLine("BIG FAIL"); };
    };
    
    XunitRunner.Run(Assembly.GetExecutingAssembly(), configure);
}
```

