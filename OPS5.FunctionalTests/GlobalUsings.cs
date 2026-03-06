global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
global using Xunit;
global using Xunit.Abstractions;
global using FluentAssertions;

// Disable parallel test execution — functional tests redirect Console.Out
// during Engine.Run() which is a global resource.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
