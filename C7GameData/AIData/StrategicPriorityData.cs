using System.Collections.Generic;

namespace C7GameData.AIData {
	public class StrategicPriorityData {
		public string priorityKey = "";					//used to look up behavior object
		public Dictionary<string, string> properties;	//may be set by the behavior object to alter the behavior
	}
}
