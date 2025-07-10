
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using UnityEditor;

namespace MivorTools.Extras.FileTools {
	
	public class AssetBackups {
		// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/MenuItem.html
		// Ctrl %, Shift #, Alt &
		public const string backupQuickHotkey = "#&b";
		
		public const int menuTopCategoryPriority = 1200;
		public const int menuSubCategoryPriority = 1200;
		public const string moreMenuName = "MivorTools";
		public const string topMenuItemNamePrefix = "MivorTools - ";

		static AssetBackups(){}

		// Quick Backup
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Quick Backup "+backupQuickHotkey, false, menuTopCategoryPriority+4)]
		public static void menuBackupQuick(){
			if(Selection.objects.Length==0) return;
			foreach(UnityObject obj in Selection.objects){
			 	var path = AssetDatabase.GetAssetPath(obj);
				backupFile(obj,path);
			}
			// var assets = new List<(UnityObject,string)>();
			// foreach(UnityObject obj in Selection.objects){
			// 	var path = AssetDatabase.GetAssetPath(obj);
			// 	var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
			// 	var meta = getObjForPath(metaPath);
			// 	// Add meta first before obj
			// 	//assets.Add((meta,metaPath)); // Failed to backup file: Assets/Test Assets.meta, to: Assets/_backups/2023-11-30.00-03-53.3556461.Test Assets.meta.backup
			// 	assets.Add((obj,path));
			// }
			// backupFiles(assets);
		}
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Quick Backup "+backupQuickHotkey, true, menuTopCategoryPriority+4)]
		public static bool menuBackupQuick_validate(){
			if(Selection.objects.Length<=0) return false;
			//if(Selection.objects.Length>1) return false;
			if(AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.objects[0]))) return false;
			return true;
		}
		
		// // Configure Backups
		// [MenuItem("Assets/"+moreMenuName+"/Backups Configure\t(for this asset)", false, menuSubCategoryPriority+5)]
		// public static void menuBackupConfigure(){
		// 	// TODO
		// }
		// [MenuItem("Assets/"+moreMenuName+"/Backups Configure\t(for this asset)", true, menuSubCategoryPriority+5)]
		// public static bool menuBackupConfigure_validate(){
		// 	return false;
		// }

		// // Configure Backups
		// [MenuItem("Assets/"+moreMenuName+"/Backups Find\t(for this asset)", false, menuSubCategoryPriority+5)]
		// public static void menuBackupFind(){
		// 	// TODO
		// }
		// [MenuItem("Assets/"+moreMenuName+"/Backups Find\t(for this asset)", true, menuSubCategoryPriority+5)]
		// public static bool menuBackupFind_validate(){
		// 	return false;
		// }

		public static void backupFile(string path,string suffix=""){
			backupFile(null,path,suffix);
		}

		public static void backupFile(UnityObject obj,string path,string suffix=""){
			var assets = new List<(UnityObject,string)>();
			assets.Add((obj,path));
			backupFiles(assets,suffix);
		}

		public static void backupFiles(List<(UnityObject,string)> assets,string suffix=""){
			try {
				AssetDatabase.StartAssetEditing();
				foreach((UnityObject obj,string path) in assets){
					var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"); // yyyy-MM-dd.HH-mm-ss.fffffff
					var folderName = "_backups";
					var dirParent = getDirPathForFile(path);
					var dirPath = dirParent+"/"+folderName;
					if(!AssetDatabase.IsValidFolder(dirPath)) AssetDatabase.CreateFolder(dirParent,folderName);
					string baseName = getBaseNameForFile(path);
					string newPath = dirPath+"/"+timestamp+" - "+baseName; //+".backup";
					newPath = appendBeforeExt(newPath,suffix);
					string baseNameNew = getBaseNameForFile(newPath);
					if(fileExists(newPath)){
						bool renamed = false;
						for(int i=2;i<1000;i++){
							string newPath2; string extra = " - "+i+"";
							newPath2 = appendBeforeExt(dirPath+"/"+baseNameNew,extra);
							if(!fileExists(newPath2)){
								newPath = newPath2;
								renamed = true;
								break;
							}
						}
						if(!renamed){
							Debug.LogWarning("Failed to find new filename for new file: "+newPath);
							continue;
						}
					}
					// Copy Original Asset
					var result = AssetDatabase.CopyAsset(path,newPath);
					if(!result) Debug.LogWarning("Failed to backup file: "+path+", to: "+newPath);
				}
			}
			finally {
				AssetDatabase.StopAssetEditing();
			}
		}

		private static string appendBeforeExt(string path,string str){
			string baseName = getBaseNameForFile(path);
			if(!baseName.Contains(".")) path = path+str;
			else path = path.Substring(0,path.LastIndexOf("."))+str+path.Substring(path.LastIndexOf("."));
			return path;
		}

		private static string getDirPathForFile(Object obj){
			var path = AssetDatabase.GetAssetPath(obj);
			return getDirPathForFile(path);
		}

		private static string getDirPathForFile(string path){
			if(path.Length>0) path = path.Substring(0,path.LastIndexOf("/"));
			if(path.Length<=0) return null;
			return path;
		}

		private static string getBaseNameForFile(string path){
			return path.Substring(path.LastIndexOf("/")+1);
		}

		private static UnityEngine.Object getObjForPath(string path){
			UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
			if(objs.Length>0) return objs[0];
			UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
			if(obj) return obj;
			return null;
		}

		private static bool fileExists(string path){
			var obj = getObjForPath(path);
			if(obj) return true;
			return false;
		}

	}
}

#endif
