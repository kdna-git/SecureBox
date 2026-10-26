using Xunit.Sdk;
using Xunit.v3;

// The headless session runs every test on its own dispatcher thread, and
// StoragePaths and VaultConfig.Instance hold process wide state
[assembly: Parallelization(Mode = ParallelMode.None)]
