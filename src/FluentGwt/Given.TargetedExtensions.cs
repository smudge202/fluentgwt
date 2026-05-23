using System.Runtime.CompilerServices;

namespace FluentGwt;

public static class GivenExtensions
{
	public static Given<Target> Given<Target>(this Target target) =>
		new(target);

	[OverloadResolutionPriority(-1)]
	public static Given<Target> Given<Target, Value>(this Target target, Value state)
	{
		if (typeof(Target) == typeof(Value))
			throw new InvalidOperationException(
				$"The target of the Given cannot be replaced by adding another default {typeof(Target)} - consider using a named or keyed instance");
		return target.Given(StateHolder.DefaultKey, state);
	}

	[OverloadResolutionPriority(-1)]
	public static Given<Target> Given<Target, Value>(this Target target, string name, Value state) =>
		target.Given((object)name, state);

	[OverloadResolutionPriority(-1)]
	public static Given<Target> Given<Target, Value>(this Target target, object key, Value state)
	{
		var given = target.Given();
		given.AddState(key, () => state);
		return given;
	}

	[OverloadResolutionPriority(-2)]
	public static Given<Target> Given<Target>(this Target target, Action<Target> transition) =>
		new(target, transition.AsCompletedTask());

	[OverloadResolutionPriority(-1)]
	public static Given<Target> Given<Target>(this Target target, Func<Target, Task> transition) =>
		new(target, transition);

	[OverloadResolutionPriority(-2)]
	public static Given<Target> Given<Target>(this Target target, Func<Target, ValueTask> transition)
	{
		ArgumentNullException.ThrowIfNull(transition);
		return new(target, x => transition(x).AsTask());
	}

	public static GivenBase<Target> Given<Target, Value>(this GivenBase<Target> given, Value state) =>
		given.Given(StateHolder.DefaultKey, state);

	public static GivenBase<Target> Given<Target, Value>(this GivenBase<Target> given, string name, Value state) =>
		given.Given((object)name, state);

	public static GivenBase<Target> Given<Target, Value>(this GivenBase<Target> given, object key, Value state)
	{
		ArgumentNullException.ThrowIfNull(given);
		given.AddState(key, () => state);
		return given;
	}

	public static Given<Target> Given<Target, Value>(this Given<Target> given, Value state)
	{
		if (typeof(Target) == typeof(Value))
			throw new InvalidOperationException(
				$"The target of the Given cannot be replaced by adding another default {typeof(Target)} - consider using a named or keyed instance");
		return given.Given(StateHolder.DefaultKey, state);
	}

	public static Given<Target> Given<Target, Value>(this Given<Target> given, string name, Value state) =>
		given.Given((object)name, state);

	public static Given<Target> Given<Target, Value>(this Given<Target> given, object key, Value state)
	{
		ArgumentNullException.ThrowIfNull(given);
		given.AddState(key, () => state);
		return given;
	}

	public static Given<Target> Given<Target>(this Given<Target> given, Action<Target> transition) =>
		given.Given(transition.AsCompletedTask());

	[OverloadResolutionPriority(1)]
	public static Given<Target> Given<Target>(this Given<Target> given, Func<Target, Task> transition)
	{
		ArgumentNullException.ThrowIfNull(given);
		given.AddTransition(transition);
		return given;
	}

	public static Given<Target> Given<Target>(this Given<Target> given, Func<Target, ValueTask> transition)
	{
		ArgumentNullException.ThrowIfNull(transition);
		return given.Given(x => transition(x).AsTask());
	}

	public static Given<Target> And<Target>(this Given<Target> given, Action<Target> transition) => given.Given(transition);

	[OverloadResolutionPriority(1)]
	public static Given<Target> And<Target>(this Given<Target> given, Func<Target, Task> transition) => given.Given(transition);

	public static Given<Target> And<Target>(this Given<Target> given, Func<Target, ValueTask> transition) => given.Given(transition);
}
