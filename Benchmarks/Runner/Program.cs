using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Runner;

var debug = false;
#if DEBUG
debug = true;
#endif
if (debug)
{
	BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
}
else
{
	//BenchmarkRunner.Run<CommandHandlerBenchmarks>();
	BenchmarkRunner.Run<Benchmarks>();
	//BenchmarkRunner.Run<EmptyRequestBenchmarks>();
}