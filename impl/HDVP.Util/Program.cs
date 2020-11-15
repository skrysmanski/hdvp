﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Text;
using System.Threading.Tasks;

using AppMotor.Core.System;

using HDVP.Internals;
using HDVP.Util.CliApp;
using HDVP.Util.Properties;

namespace HDVP.Util
{
    internal static class Program
    {
        private sealed class BenchmarkCommand : CliCommand
        {
            /// <inheritdoc />
            public override string Name => "benchmark";

            private CliParam<int> Seconds { get; } =
                new CliParam<int>("--seconds", "-s")
                {
                    HelpText = LocalizableResources.HelpText_Benchmark_Seconds,
                    DefaultValue = 10,
                };

            private CliParam<int> HashLength { get; } =
                new CliParam<int>("--hash-length", "-l")
                {
                    HelpText = LocalizableResources.HelpText_Benchmark_HashLength,
                    DefaultValue = 8,
                };

            /// <inheritdoc />
            public override int Execute()
            {
                if (this.Seconds.Value < 1)
                {
                    throw new ArgumentException("Seconds must be greater than 1");
                }

                Terminal.WriteLine(LocalizableResources.Benchmark_CalculateFirstHash);
                Terminal.WriteLine();

                var salt = HdvpSalt.CreateNewSalt();
                var verifiableData = HdvpVerifiableData.ReadFromMemory(Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr"));

                var firstSlowHash = HdvpSlowHashAlgorithm.CreateHash(verifiableData, salt, byteCount: this.HashLength.Value);

                Terminal.WriteLine(LocalizableResources.Benchmark_FirstHashResult + " " + BitConverter.ToString(firstSlowHash));
                Terminal.WriteLine();
                Terminal.WriteLine();

                Terminal.WriteLine(LocalizableResources.Benchmark_RunIntro, this.Seconds.Value);

                var testTime = TimeSpan.FromSeconds(this.Seconds.Value);
                var startTime = DateTime.UtcNow;

                int hashCount = 0;
                while (DateTime.UtcNow - startTime < testTime)
                {
                    // ReSharper disable once MustUseReturnValue
                    HdvpSlowHashAlgorithm.CreateHash(verifiableData, salt, byteCount: this.HashLength.Value);
                    hashCount++;
                }

                var timeSpent = DateTime.UtcNow - startTime;
                Terminal.WriteLine(LocalizableResources.Benchmark_HashesPerSecondResult, hashCount / timeSpent.TotalSeconds);

                return 0;
            }
        }

        private static int Main(string[] args)
        {
            /*var benchmarkCommand = new Command("benchmark", LocalizableResources.HelpText_Benchmark)
            {
                new Option<int>(
                    new[] { "--seconds", "-s" },
                    getDefaultValue: () => 10,
                    description: LocalizableResources.HelpText_Benchmark_Seconds
                ),
                new Option<int>(
                    new[] { "--hash-length", "-l" },
                    getDefaultValue: () => 8,
                    description: LocalizableResources.HelpText_Benchmark_HashLength
                ),
            };
            //benchmarkCommand.Handler = CommandHandler.Create<int, int>(RunBenchmark);

            var rootCommand = new RootCommand(LocalizableResources.AppDescription)
            {
                benchmarkCommand,
            };*/

            var rootCommand = new RootCommand(LocalizableResources.AppDescription);

            var benchmarkCommand = new BenchmarkCommand();

            rootCommand.AddCommand(benchmarkCommand.UnderlyingImplementation);

            /*
            var parser = new CommandLineBuilder(rootCommand)
                          .UseDefaults()
                          .Build();

            var parseResult = parser.Parse(args);

            benchmarkCommand.UnderlyingImplementation.Handler = CommandHandler.Create(() => benchmarkCommand.Execute(new CliValues(parseResult)));
            */

            //benchmarkCommand.UnderlyingImplementation.Handler = new MyInvocationHandler(benchmarkCommand);

            return rootCommand.Invoke(args);
        }

        /*private sealed class MyInvocationHandler : ICommandHandler
        {
            private readonly BenchmarkCommand m_benchmarkCommand;

            public MyInvocationHandler(BenchmarkCommand benchmarkCommand)
            {
                this.m_benchmarkCommand = benchmarkCommand;
            }

            /// <inheritdoc />
            public Task<int> InvokeAsync(InvocationContext context)
            {
                var retVal = this.m_benchmarkCommand.Execute(new CliValues(context.ParseResult));
                return Task.FromResult(retVal);
            }
        }*/
    }
}
