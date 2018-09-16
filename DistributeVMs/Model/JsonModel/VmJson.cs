using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DistributeVMs.Model.JsonModel
{
	/// <summary>
	/// Vm class that is only used for deserializing objects from Json. <seealso cref="HypervisorJson"/> 
	/// </summary>
	[DataContract(Name ="vm")]
	public class VmJson
	{
		/// <summary>
		/// The Id of the Vm.
		/// </summary>
		[DataMember(Name = "id")]
		public string Id { get; set; }
		/// <summary>
		/// The memory consumption of the Vm.
		/// </summary>
		[DataMember(Name = "ram")]
		public int Ram { get; set; }
	}

	/// <summary>
	/// The root object that contains the Vms.
	/// </summary>
	[DataContract]
	public class VmJsonRoot
	{
		[DataMember(Name = "vms")]
		public List<VmJson> Vms { get; set; }
	}
}
