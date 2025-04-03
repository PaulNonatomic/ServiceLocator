using UnityEngine;

namespace Tests.PlayMode
{
	public class MonoBehaviourTestService : MonoBehaviour, IMonoBehaviourTestService
	{
		public string GetStatus()
		{
			return "Active";
		}
	}
}