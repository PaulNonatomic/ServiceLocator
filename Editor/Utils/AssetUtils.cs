using System.Collections.Generic;

namespace Nonatomic.ServiceLocator.Utils
{
	public static class AssetUtils
	{
		public static T FindAssetByType<T>() where T : UnityEngine.Object
		{
			var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name);
			if (guids.Length == 0) return null;
			
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
			return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
		}
		
		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name);
			var results = new List<T>();
			
			if (guids.Length == 0) return results;
			
			foreach(var guid in guids)
			{
				var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
				if (asset != null) results.Add(asset);
			}
			
			return results;
		}
	}
}