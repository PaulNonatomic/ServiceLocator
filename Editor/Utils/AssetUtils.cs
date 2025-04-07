using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.ServiceLocator.Utils
{
	public static class AssetUtils
	{
		public static T FindAssetByType<T>() where T : Object
		{
			var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
			if (guids.Length == 0)
			{
				return null;
			}

			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		public static List<T> FindAssetsByType<T>() where T : Object
		{
			var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
			var results = new List<T>();

			if (guids.Length == 0)
			{
				return results;
			}

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (asset != null)
				{
					results.Add(asset);
				}
			}

			return results;
		}
	}
}