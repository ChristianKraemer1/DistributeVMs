using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributeVMs.Model.AppModel;
using DistributeVMs.Model.JsonModel;
using Newtonsoft.Json;

namespace DistributeVMs
{
	public static class Profiler
	{
		const string RESULT_FILENAME = "profiling_result.json";

		public static void Profile(int numTestRuns, int numHypervisors = 250, int numVms = 1000, bool writeResultToFile = false)
		{
			double avgTimeAll = 0;
			double avgDevAll = 0;

			Console.WriteLine($"Starting {numTestRuns} test runs with {numVms} VMs and {numHypervisors} Hypervisors.");

			HypervisorManager hvManager = null;

			for (int tr = 0; tr < numTestRuns; tr++)
			{
				var hypervisors = GenerateHypervisors(numHypervisors);
				var vms = GenerateVms(numVms);

				hvManager = new HypervisorManager(hypervisors);

				double avgTime = 0;
				int i = 0;

				Console.Write($"\rRun {tr+1} ({((tr+1) * 100) / numTestRuns}%)    ");

				Stopwatch stopwatch = new Stopwatch();

				foreach (var vm in vms)
				{

					stopwatch.Reset();
					stopwatch.Start();
					hvManager.AddVm(vm);
					stopwatch.Stop();

					var ts = stopwatch.Elapsed;
					avgTime += ts.TotalMilliseconds;
					i++;
				}

				avgTimeAll += (avgTime / i);
				avgDevAll += hvManager.GetAverageDeviation();
			}

			Console.WriteLine("");
			Console.WriteLine($"Average time per Vm: {avgTimeAll / numTestRuns}ms Average deviation: {avgDevAll / numTestRuns}%");

			if (writeResultToFile)
			{
				var filename = Path.Combine("./", RESULT_FILENAME);
				Console.WriteLine($"Writing result of last test run to file {filename}");

				if (hvManager != null)
				{
					WriteResultToFile(hvManager, filename);
				}
			}
		}

		private static List<Hypervisor> GenerateHypervisors(int num)
		{
			Random rnd = new Random();
			int[] sizes = { 512, 1024, 2048, 4096, 8192 };
			return Enumerable.Range(1, num).Select(i => new Hypervisor($"hypervisor{i}", sizes[rnd.Next(sizes.Length)])).ToList();
		}

		private static List<Vm> GenerateVms(int num)
		{
			Random rnd = new Random();
			int[] sizes = { 64, 128, 256, 512 };
			return Enumerable.Range(1, num).Select(i => new Vm($"vm{i}", sizes[rnd.Next(sizes.Length)])).ToList();
		}

		private static void WriteResultToFile(HypervisorManager manager, string filename)
		{
			try
			{
				File.WriteAllText(filename, manager.GetJsonResultString());
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error writing profiling result to file {filename}: ", e);
			}
		}
	}
}
