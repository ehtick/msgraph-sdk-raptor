using System;
using System.Threading;
using NUnit.Framework;
[assembly: RequiresThread(ApartmentState.MTA)]
[assembly: Parallelizable(ParallelScope.All)]
[assembly: CLSCompliant(false)]
