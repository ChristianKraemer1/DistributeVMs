
using DistributeVMs.Model.JsonModel;

namespace DistributeVMs.Model.AppModel
{
	/// <summary>
	/// Vm class
	/// </summary>
	public class Vm
	{
		/// <summary>
		/// Constructor for creating an AppModel Vm from a deserialized JsonModel Vm 
		/// </summary>
		/// <param name="vmj">The deserialized JsonModel Vm object.</param>
		public Vm(VmJson vmj) : this(vmj.Id, vmj.Ram)
		{
		}

		/// <summary>
		/// Parametrized default Constructor (there is no parameterless default constructur)
		/// </summary>
		/// <param name="id">The Id of the Vm.</param>
		/// <param name="ram">The memory consumption of the Vm.</param>
		public Vm(string id, int ram)
		{
			Id = id;
			Ram = ram;
		}

		/// <summary>
		/// The Id of the vm.
		/// </summary>
		public string Id { get; set; }
		/// <summary>
		/// The memory consumption of the Vm.
		/// </summary>
		public int Ram { get; set; }
	}
}
