using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DistributeVMs.Model.JsonModel
{
	/// <summary>
	/// Hypervisor class that is only used for deserializing it from a Json file.
	/// The Application will use its own "AppModel" Hypervisor class for processing, to keep this object clear of logic 
	/// and maintain compatibility in case of file format changes.
	/// </summary>

	[DataContract(Name = "hypervisor")]
	public class HypervisorJson
	{
		/// <summary>
		/// The Id of the Hypervisor.
		/// </summary>
		[DataMember(Name = "id")]
		public string Id { get; set; }
		/// <summary>
		/// The Capacity (maximum Ram) of the Hypervisor.
		/// </summary>
		[DataMember(Name = "maxram")]
		public int Maxram { get; set; }
	}

	/// <summary>
	/// The root object that contains the Hypervisor objects.
	/// </summary>
	[DataContract]
	public class HypervisorJsonRoot
	{
		[DataMember(Name = "hypervisors")]
		public List<HypervisorJson> Hypervisors { get; set; }
	}
}
