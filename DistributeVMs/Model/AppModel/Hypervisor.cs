using System;
using System.Collections.Generic;
using DistributeVMs.Model.JsonModel;

namespace DistributeVMs.Model.AppModel
{
	/// <summary>
	/// Hypervisor Class
	/// </summary>
	public class Hypervisor : IComparable<Hypervisor>
	{
		private List<Vm> _vms = null;

		/// <summary>
		/// Constructor for creating an AppModel Hypervisor from a deserialized JsonModel Hypervisor 
		/// </summary>
		/// <param name="hvj">The deserialized JsonModel Hypervisor object.</param>
		public Hypervisor(HypervisorJson hvj) : this(hvj.Id, hvj.Maxram)
		{
		}

		/// <summary>
		/// Parametrized default Constructor (there is no parameterless default constructur)
		/// </summary>
		/// <param name="id">Id of the Hypervisor.</param>
		/// <param name="maxram">Capacity (maximum RAM) of the Hypervisor,</param>
		public Hypervisor(string id, int maxram)
		{
			_vms = new List<Vm>();
			Id = id;
			Maxram = maxram;
		}

		/// <summary>
		/// Id of the Hypervisor
		/// </summary>
		public string Id { get; set; }
		/// <summary>
		/// The Capacity (maximum Ram) of the Hypervisor
		/// </summary>
		public int Maxram { get; set; }

		/// <summary>
		/// The current Load (relative, in percent) caused by all Vms assigned to this Hypervisor.
		/// </summary>
		public double CurrentLoadPercent { get; private set; } = 0;

		/// <summary>
		/// The current Load (absolute, in GB) caused by all Vms assigned to this Hypervisor.
		/// </summary>
		public int CurrentLoadAbsolute { get; private set; } = 0;

		/// <summary>
		/// The List of Vms that are currently assigned to this Hypervisor.
		/// The List of current Vms is managed internally and should not be manipulated from outside, 
		/// thus only a copy of the current List is returned.
		/// </summary>
		public List<Vm> Vms
		{
			get
			{
				return new List<Vm>(_vms);
			}
		}

		/// <summary>
		/// Resets the Hypervisor by setting its load to 0 and removing all Vms.
		/// </summary>
		public void Reset()
		{
			CurrentLoadPercent = 0;
			CurrentLoadAbsolute = 0;
			_vms.Clear();
		}

		/// <summary>
		/// Checks, if the Vm <paramref name="vm"/> can be added to the Hypervisor without exceeding its capacity.
		/// </summary>
		/// <param name="vm">The Vm that should be checked.</param>
		/// <returns>true, if the Vm can be added without exceeding the Hypervisors capacity.</returns>
		public bool CheckIfVmFits(Vm vm)
		{
			return (CurrentLoadAbsolute + vm.Ram) <= Maxram;
		}

		/// <summary>
		/// Adds a new Vm to the Hypervisors List of managed Vms.
		/// </summary>
		/// <exception cref="OverflowException.OverflowException">Will be thrown, if adding the Vm would exceed the Hypervisors capacity.</exception>
		/// <param name="vm">The vm that should be added to the Hypervisor.</param>
		public void AddVm(Vm vm)
		{
			var newLoad = CurrentLoadAbsolute + vm.Ram;

			if (newLoad > Maxram)
			{
				throw new OverflowException($"Hypervisor {Id} has not enough free space to receive Vm {vm.Id}");
			}

			CurrentLoadPercent = CalcLoadAfterAddingVm(vm);
			CurrentLoadAbsolute = newLoad;
			_vms.Add(vm);
		}

		/// <summary>
		/// Calculates the (procentual) Load the Hypervisor will have after addig the Vm <paramref name="vm"/>
		/// </summary>
		/// <param name="vm">The Vm ne the Load should be calculated for.</param>
		/// <returns></returns>
		public double CalcLoadAfterAddingVm(Vm vm)
		{
			var newCurrentLoad = CurrentLoadAbsolute + vm.Ram;
			return (double)newCurrentLoad * 100 / Maxram;
		}

		public int CompareTo(Hypervisor other)
		{
			return Maxram - other.Maxram;
		}
	}
}
