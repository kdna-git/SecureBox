using Xunit.Sdk;
using Xunit.v3;

// StoragePaths and VaultConfig.Instance hold process wide state, so tests that
// read or write them must not run concurrently
[assembly: Parallelization(Mode = ParallelMode.None)]
