using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributeVMs.Model.AppModel;

namespace DistributeVMs
{
	public static class Profiler
	{
		public static void Profile(int numTestRuns, int numHypervisors = 250, int numVms = 1000)
		{
			double avgTimeAll = 0;
			double avgDevAll = 0;

			for (int tr = 0; tr < numTestRuns; tr++)
			{
				var hypervisors = GenerateHypervisors(numHypervisors);
				var vms = GenerateVms(numVms);

				var hvManager = new HypervisorManager(hypervisors);

				double avgTime = 0;
				int i = 0;

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

			Console.WriteLine($"Average time: {avgTimeAll / numTestRuns}ms Average deviation: {avgDevAll / numTestRuns}%");
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
			return Enumerable.Range(1, num).Select(i => new Vm($"", sizes[rnd.Next(sizes.Length)])).ToList();
		}
	}
}
