using Microsoft.Extensions.Time.Testing;

namespace FluentGwt;

internal sealed class FixtureTime(DateTimeOffset start) : FakeTimeProvider(start)
{
	private readonly Lock _lock = new();
	private readonly List<WatchedTimer> _stepTimers = [];
	private long _fired;
	private long _step;

	public long Fired => Interlocked.Read(ref _fired);

	public long Step => Interlocked.Read(ref _step);

	public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		ArgumentNullException.ThrowIfNull(callback);
		var timer = new WatchedTimer(dueTime, period);
		lock (_lock)
			_stepTimers.Add(timer);
		timer.Inner = base.CreateTimer(
			x =>
			{
				timer.Fire();
				Interlocked.Increment(ref _fired);
				callback(x);
			},
			state,
			dueTime,
			period);
		return timer;
	}

	public void BeginStep()
	{
		lock (_lock)
			_stepTimers.Clear();
		Interlocked.Increment(ref _step);
	}

	public TimeSpan? PendingInStep()
	{
		lock (_lock)
			return _stepTimers.FirstOrDefault(x => x.Pending)?.DueTime;
	}

	private sealed class WatchedTimer(TimeSpan dueTime, TimeSpan period) : ITimer
	{
		private readonly Lock _lock = new();
		private TimeSpan _dueTime = dueTime;
		private TimeSpan _period = period;
		private bool _fired;
		private bool _disposed;

		public ITimer? Inner { get; set; }

		public TimeSpan DueTime
		{
			get
			{
				lock (_lock)
					return _dueTime;
			}
		}

		public bool Pending
		{
			get
			{
				lock (_lock)
					return !_disposed && _dueTime != Timeout.InfiniteTimeSpan && (!_fired || _period != Timeout.InfiniteTimeSpan);
			}
		}

		public void Fire()
		{
			lock (_lock)
				_fired = true;
		}

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			lock (_lock)
			{
				_dueTime = dueTime;
				_period = period;
				_fired = false;
			}
			return Inner?.Change(dueTime, period) ?? false;
		}

		public void Dispose()
		{
			lock (_lock)
				_disposed = true;
			Inner?.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			lock (_lock)
				_disposed = true;
			if (Inner is not null)
				await Inner.DisposeAsync();
		}
	}
}
