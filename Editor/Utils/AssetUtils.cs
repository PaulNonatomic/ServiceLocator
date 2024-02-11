using System;
using System.Collections.Generic;

namespace Nonatomic.ServiceLocator.Editor.Utils
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
	}
}