using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	public class SceneGroupData
	{
		public string SceneName { get; set; } = "No Scene";
		public Scene Scene { get; set; } = default;
		public bool IsUnloaded { get; set; } = false;
		public List<(Type Type, object Instance)> Services { get; } = new List<(Type, object)>();
	}
}