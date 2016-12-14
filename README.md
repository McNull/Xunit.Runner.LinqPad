# Xunit Runner LinqPad

Run Xunit test within LinqPad

## Example

```
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
