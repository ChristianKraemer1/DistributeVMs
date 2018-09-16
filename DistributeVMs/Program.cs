using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DistributeVMs.Model.AppModel;
using DistributeVMs.Model.JsonModel;
using Newtonsoft.Json;

namespace DistributeVMs
{
	class Program
	{
		private const string CONF_KEY_FILENAME_HYPERVISOR = "FilenameHypervisor";
		private const string CONF_KEY_FILENAME_VMS = "FilenameVms";
		private const string CONF_KEY_DEFAULTPATH = "DefaultPath";

		private static string _path;

		static void Main(string[] args)
		{
			bool profile = false;

			_path = ConfigurationManager.AppSettings[CONF_KEY_DEFAULTPATH] ?? "./";
			string filenameHypervisor = ConfigurationManager.AppSettings[CONF_KEY_FILENAME_HYPERVISOR] ?? "hypervisor.json";
			string filenameVms = ConfigurationManager.AppSettings[CONF_KEY_FILENAME_VMS] ?? "vms.json";

			if (!ProcessArgs(args))
			{
				return;
			}

			if (profile)
			{
				Profile(200, 3, 4);
			}
			else
			{
				var hypervisors = ReadHypervisors(Path.Combine(_path, filenameHypervisor));
				var vms = ReadVms(Path.Combine(_path, filenameVms));

				if (hypervisors == null || vms == null)
				{
					return;
				}

				var hvManager = new HypervisorManager(hypervisors);

				foreach (var vm in vms)
				{
					hvManager.AddVm(vm);
				}
				Console.WriteLine(hvManager.GetJsonResultString());
			}
		}

		private static void Profile(int numTestRuns, int numHypervisors = 250, int numVms = 1000)
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
			return Enumerable.Range(1, num).Select(i => new Hypervisor($"hypervisor{i}", sizes[rnd.Next(sizes.Length)]) ).ToList();
		}

		private static List<Vm> GenerateVms(int num)
		{
			Random rnd = new Random();
			int[] sizes = { 64, 128, 256, 512 };
			return Enumerable.Range(1, num).Select(i => new Vm($"", sizes[rnd.Next(sizes.Length)])).ToList();
		}

		
		/// <summary>
		/// Helper-method: Deserialize the Hypervisors from a json file, and convert the deserialized JsonModel Hypervisors 
		/// To AppModel Hypervisors.
		/// Exceptions are propagated up to the caller.
		/// </summary>
		/// <param name="filenameJson">Path and filename of the json file containing the Hypervisors.</param>
		/// <returns>A List of AppModel Hypervisor objects.</returns>
		private static List<Hypervisor> ReadHypervisors(string filenameJson)
		{
			var hypervisorRoot = DeserializeJsonFromFile<HypervisorJsonRoot>(filenameJson);

			if (hypervisorRoot == null)
			{
				return null;
			}
			if (hypervisorRoot.Hypervisors == null)
			{
				Console.WriteLine($"No hypervisors found in file {filenameJson}");
				return null;
			}

			// convert deserialized json objects to AppModel objects
			return hypervisorRoot.Hypervisors.Select(hvj => new Hypervisor(hvj)).ToList();
		}

		/// <summary>
		/// Helper-method: Deserialize the Vms from a json file, and convert the deserialized JsonModel Vms 
		/// To AppModel Vms.
		/// Exceptions are propagated up to the caller.
		/// </summary>
		/// <param name="filenameJson">Path and filename of the json file containing the Vms.</param>
		/// <returns>A List of AppModel Vm objects.</returns>
		private static List<Vm> ReadVms(string filenameJson)
		{
			VmJsonRoot vmRoot = DeserializeJsonFromFile<VmJsonRoot>(filenameJson);

			if (vmRoot == null)
			{
				return null;
			}

			if(vmRoot.Vms == null)
			{
				Console.WriteLine($"No hypervisors found in file {filenameJson}");
				return null;
			}
			// convert deserialized json objects to AppModel objects.
			return vmRoot.Vms.Select(vmj => new Vm(vmj)).ToList();
		}

		private static T DeserializeJsonFromFile<T>(string filename) where T : class
		{
			T rootObject = null;

			try
			{
				using (StreamReader file = File.OpenText(filename))
				{
					JsonSerializer serializer = new JsonSerializer();
					rootObject = (T)serializer.Deserialize(file, typeof(T));
				}
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine($"Error reading file {filename}: File not found.");
			}
			catch (DirectoryNotFoundException e)
			{
				Console.WriteLine($"Error reading file {filename}: Directory not found.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Unexpected error reading file {filename}: ", e);
			}

			return rootObject;
		}


		/// <summary>
		/// Helper method for interpreting the command line arguments.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private static bool ProcessArgs(string[] args)
		{
			if (args.Length > 0)
			{
				if (args.Length != 1)
				{
					Console.WriteLine("Usage: DistributeVMs [path to .json files]");
					return false;
				}
				_path = args[0];
			}

			return true;
		}
	}
}
