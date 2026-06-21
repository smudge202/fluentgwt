namespace FluentGwt;

[Flags]
public enum IntegrationJustification
{
	None = 0,
	NetworkIo = 1,
	DiskIo = 2,
	UnsafeCode = 4,
	MultipleThreads = 8,
	ThreadSynchronisation = 16,
}
