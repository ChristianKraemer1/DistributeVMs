﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using DistributeVMs.Model.AppModel;
using DistributeVMs.Model.JsonModel;
using Newtonsoft.Json;

namespace DistributeVMs
{
	/// <summary>
	/// DistributeVMs
	/// 
	/// Commandline tool for equally distributing VMs to a set of given Hypervisors (both defined in seperate json files).
	/// 
	/// Usage: DistributeVMs [path to json files]
	/// 
	/// </summary>
	class Program
	{
		private const string CONF_KEY_FILENAME_HYPERVISOR = "FilenameHypervisor";
		private const string CONF_KEY_FILENAME_VMS = "FilenameVms";
		private const string CONF_KEY_DEFAULTPATH = "DefaultPath";

		private const string CONF_KEY_PROFILINGACTIVE = "ExecuteInProfilingMode";
		private const string CONF_KEY_PROFILING_SAVERESULTS = "SaveLastProfilingResultToFile";
		private const string CONF_KEY_NUMPROFILING_RUNS = "NumProfilingRuns";
		private const string CONF_KEY_NUMPROFILING_VMS = "NumProfilingVms";
		private const string CONF_KEY_NUMPROFILING_HYPERVISORS = "NumProfilingHypervisors";
		



		private static string _path;

		private static bool _isProfilingActive = false;
		private static bool _saveProfilingResults = false;
		private static int _numProfilingRuns = 50;
		private static int _numProfilingVms = 250;
		private static int _numProfilingHypervisors = 1000;

		static void Main(string[] args)
		{
			// Get default path and filenames from the .config file
			_path = ConfigurationManager.AppSettings[CONF_KEY_DEFAULTPATH] ?? "./";
			string filenameHypervisor = ConfigurationManager.AppSettings[CONF_KEY_FILENAME_HYPERVISOR] ?? "hypervisor.json";
			string filenameVms = ConfigurationManager.AppSettings[CONF_KEY_FILENAME_VMS] ?? "vms.json";

			// Read the configuration settings for the profiling mode.
			ReadProfilerConfiguration();

			if (_isProfilingActive)
			{
				Profiler.Profile(_numProfilingRuns, _numProfilingHypervisors, _numProfilingVms, _saveProfilingResults);
				return;
			}

			// Process Command-Line arguments
			if (!ProcessArgs(args))
			{
				return;
			}

			// Import the VMs and Hypervisors from the json files
			List<Hypervisor> hypervisors = ReadHypervisors(Path.Combine(_path, filenameHypervisor));
			if (hypervisors == null)
			{
				return;
			}

			List<Vm> vms = ReadVms(Path.Combine(_path, filenameVms));
			if (vms == null)
			{
				return;
			}

			var hvManager = new HypervisorManager(hypervisors);

			// Distribute the Vms one after another to the Hypervisors.
			// If intermediate Results should be logged to the Console, move the 'Console.WriteLine' statement into the foreach-block.
			foreach (var vm in vms)
			{
				hvManager.AddVm(vm);
			}

			Console.WriteLine("\r\nResult:");
			// Log the result to the Console.
			Console.WriteLine(hvManager.GetJsonResultString());
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
			Console.WriteLine($"Reading Hypervisors from File {filenameJson}");
			var hypervisorRoot = DeserializeJsonFromFile<HypervisorJsonRoot>(filenameJson);

			if (hypervisorRoot == null)
			{
				return null;
			}
			if (hypervisorRoot.Hypervisors == null || !hypervisorRoot.Hypervisors.Any())
			{
				Console.WriteLine($"No hypervisors found in file {filenameJson}");
				return null;
			}
			Console.WriteLine($"{hypervisorRoot.Hypervisors.Count} Hypervisors read.");

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
			Console.WriteLine($"Reading VMs from File {filenameJson}");
			VmJsonRoot vmRoot = DeserializeJsonFromFile<VmJsonRoot>(filenameJson);

			if (vmRoot == null)
			{
				return null;
			}

			if(vmRoot.Vms == null || !vmRoot.Vms.Any())
			{
				Console.WriteLine($"No VMs found in file {filenameJson}");
				return null;
			}
			Console.WriteLine($"{vmRoot.Vms.Count} VMs read.");

			// convert deserialized json objects to AppModel objects.
			return vmRoot.Vms.Select(vmj => new Vm(vmj)).ToList();
		}

		/// <summary>
		/// Helper method: Deserialiaze a json file to an (root-) object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Type of the root object, the file should be deserialized to.</typeparam>
		/// <param name="filename">Path and filename of the Json file.</param>
		/// <returns>A deserialized root object of Typr <typeparamref name="T"/></returns>
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
			catch (FileNotFoundException)
			{
				Console.WriteLine($"Error reading file {filename}: File not found.");
			}
			catch (DirectoryNotFoundException)
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

		/// <summary>
		/// Read the settings for the profiling mode from the Configuration
		/// </summary>
		private static void ReadProfilerConfiguration()
		{
			// Get parameters for Profilig mode, if active
			var profileEntry = ConfigurationManager.AppSettings[CONF_KEY_PROFILINGACTIVE] ?? "false";
			Boolean.TryParse(profileEntry, out _isProfilingActive);

			if (_isProfilingActive)
			{
				var saveResultsEntry = ConfigurationManager.AppSettings[CONF_KEY_PROFILING_SAVERESULTS] ?? "false";
				bool.TryParse(saveResultsEntry, out _saveProfilingResults);

				var numRunsEntry = ConfigurationManager.AppSettings[CONF_KEY_NUMPROFILING_RUNS] ?? "50";
				int.TryParse(numRunsEntry, out _numProfilingRuns);

				var numVmsEntry = ConfigurationManager.AppSettings[CONF_KEY_NUMPROFILING_VMS] ?? "250";
				int.TryParse(numVmsEntry, out _numProfilingVms);

				var numHypervisorEntry = ConfigurationManager.AppSettings[CONF_KEY_NUMPROFILING_HYPERVISORS] ?? "1000";
				int.TryParse(numHypervisorEntry, out _numProfilingHypervisors);
			}
		}
	}
}
