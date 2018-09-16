using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributeVMs.Model.AppModel;
using Newtonsoft.Json.Linq;

namespace DistributeVMs
{
	/// <summary>
	/// Manages a list of Hypervisors.
	/// </summary>
	class HypervisorManager
	{
		int _numFreeHypervisors;
		private List<Hypervisor> _managedHypervisors;

		/// <summary>
		/// Initialize the HypervisorManager with the hypervisors that should be managed.
		/// </summary>
		/// <exception cref="ArgumentNullException.ArgumentNullException">
		/// Will be thrown if <paramref name="hypervisors"/> is null.
		/// </exception>
		/// <param name="hypervisors">The list of Hypervisors that should be managed by this instance of HypervisroManager</param>
		public HypervisorManager(List<Hypervisor> hypervisors)
		{
			_managedHypervisors = hypervisors ?? throw new ArgumentNullException("hypervisors");
			_numFreeHypervisors = hypervisors.Where(hv => hv.CurrentLoadAbsolute == 0).Count();
		}

		/// <summary>
		/// The average load (memory allocation) of all managed Hypervisors in percent.
		/// </summary>
		/// <returns>The average load in percent.</returns>
		public double GetAverageLoad()
		{
			return _managedHypervisors.Sum(hv => hv.CurrentLoadPercent) / _managedHypervisors.Count();
		}

		/// <summary>
		/// The average deviation from the average load of all managed Hypervisors in percent.
		/// </summary>
		/// <returns>The average deviation in percent.</returns>
		public double GetAverageDeviation()
		{
			var avgLoad = GetAverageLoad();
			return _managedHypervisors.Select(hv => Math.Abs(avgLoad - hv.CurrentLoadPercent)).Sum() / _managedHypervisors.Count();
		}

		/// <summary>
		/// Determines the best Hypervisor from the List of managed Hypervisors for the Vm <paramref name="vm"/> and adds the Vm to it. 
		/// </summary>
		/// <param name="vm">The Vm that should be added to one of the managed Hypervisors</param>
		public void AddVm(Vm vm)
		{
			// If there are still Hypervisors without any assigned Vms, check first if the new Vm should be added to one of them.
			// Else find the Hypervisor were adding the Vm leads to the best result (equally distributed load).
			Hypervisor bestCandidate = (_numFreeHypervisors > 0 ? FindBestEmptyHypervisor(vm) : null)?? FindBestHypervisor(vm);

			if (bestCandidate != null)
			{
				// update number of free Hypervisors (Hypervisors without any Vm)
				if (bestCandidate.CurrentLoadAbsolute == 0)
					_numFreeHypervisors--;

				// Add the Vm to the determined Hypervisor.
				bestCandidate.AddVm(vm);
			}
			else
			{
				// No Hypervisor with enough capacity for the Vm found.
				Console.WriteLine($"WARNING - not enough free capacity for adding Vm {vm.Id}");
			}
		}

		/// <summary>
		/// This method finds the best matching Hypervisor for the Vm <paramref name="vm"/> 
		/// from the Hypervisors that currently have no Vm assigned.
		/// </summary>
		/// <param name="vm"></param>
		/// <returns></returns>
		private Hypervisor FindBestEmptyHypervisor(Vm vm)
		{
			Hypervisor result = null;
			// The List of managed Hypervisors is filtered by Hypervisors without Vms (Load==0), that have enough capacity 
			// to take the Vm (maxram <= vm.ram) and where adding the Vm would cause a maximum load of 25%.
			// Then the Hypervisor with the lowest overall-capacity is picked.
			// This ensures, that relatively small Vms will be assigned to relatively small empty Hypervisors,
			// which turned out to be a good strategy to populate empty Hypervisors.

			var matchingHvs = _managedHypervisors.Where(mh => mh.CurrentLoadAbsolute == 0
													&& mh.Maxram >= vm.Ram
													&& mh.CalcLoadAfterAddingVm(vm) <= 25);
			if (matchingHvs.Any())
			{
				result = matchingHvs.Min();
			}
			return result;
		}

		/// <summary>
		/// Finds the best matching Hypervisor for a Vm out of the List of managed Hypervisors.
		/// This is done by assigning the Vm to each Hypervisor consecutively and calculating the average deviation from the average load
		/// this would cause. Then the Hypervisor that causes the best result (lowest overall deviation) will be picked.
		/// </summary>
		/// <param name="vm">The Vm for which the best matching Hypervisor should be found.</param>
		/// <returns></returns>
		private Hypervisor FindBestHypervisor(Vm vm)
		{
			Hypervisor result = null;

			// temporary array to calculate the overall-deviation for each candidate without changing the Hypervisors current load.
			double[] tmpLoad = _managedHypervisors.Select(hv => hv.CurrentLoadPercent).ToArray();
			double lowestDeviation = 100;

			for (var i = 0; i < _managedHypervisors.Count; i++)
			{
				var hv = _managedHypervisors[i];

				if (hv.CheckIfVmFits(vm))
				{
					// calculate the new load, the Hypervisor would have after adding this Vm.
					tmpLoad[i] = hv.CalcLoadAfterAddingVm(vm);
					// Calculate the average load, and the averate deviation from the average load for all Hypervisors.
					var tmpAverage = tmpLoad.Sum() / tmpLoad.Where(l => l > 0).Count();
					var tmpAverageDeviation = tmpLoad.Select(l => Math.Abs(tmpAverage - l)).Sum() / tmpLoad.Length;
					// restore the load to its original value for the next iteration.
					tmpLoad[i] = hv.CurrentLoadPercent;

					// Find the lowest overall deviation. If the deviation caused by this Hypervisor is equal to the
					// lowest deviation so far, the Hypervisor with the lower load after adding the Vm is chosen.
					if ((tmpAverageDeviation < lowestDeviation) ||
						(tmpAverageDeviation == lowestDeviation && result.CalcLoadAfterAddingVm(vm) > hv.CalcLoadAfterAddingVm(vm)))
					{
						lowestDeviation = tmpAverageDeviation;
						result = hv;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Creates a json string containing all Hypervisors with their currently assigned Vms.
		/// </summary>
		/// <returns>A string containing all managed Hypervisors in Json format.</returns>
		public string GetJsonResultString()
		{
			JObject rootObject = new JObject();

			foreach (var hv in _managedHypervisors)
			{
				rootObject[hv.Id] = new JObject();
				var vmArray = new JArray();
				hv.Vms.ForEach(vm => vmArray.Add(JToken.FromObject(vm)));
				rootObject[hv.Id]["vms"] = vmArray;
			}
			return rootObject.ToString(Newtonsoft.Json.Formatting.Indented);
		}
	}
}
