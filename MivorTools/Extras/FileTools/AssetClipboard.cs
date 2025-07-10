
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using UnityEditor;

namespace MivorTools.Extras.AssetClipboard {
	public class AssetClipboard {
		// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/MenuItem.html
		// Ctrl %, Shift #, Alt &
		private const string cutHotkey = "#&x";
		private const string copyHotkey = "#&c";
		private const string pasteHotkey = "#&v";
		private const string pasteRepeatHotkey = "%&v";
		
		public const int menuTopCategoryPriority = 1200;
		public const int menuSubCategoryPriority = 1400;
		private const string moreMenuName = "MivorTools";
		private const string topMenuItemNamePrefix = "MivorTools - ";

		protected static List<(UnityObject,string,int)> clipboard = new List<(UnityObject,string,int)>();

		// More Menu

		// Cut
		[MenuItem("Assets/"+moreMenuName+"/Cut "+cutHotkey, false, menuSubCategoryPriority+0)]
		public static void menuAssetCut(){
			if(Selection.objects.Length==0) return;
			foreach(UnityObject obj in Selection.objects){
				var path = AssetDatabase.GetAssetPath(obj);
				bool inClipboard = false;
				foreach((UnityObject obj2, string path2, int type) in clipboard){
					if(path2==path){ inClipboard=true; clipboard.Remove((obj2,path2,type)); break; }
				}
				if(inClipboard) continue;
				clipboard.Add((obj,path,2));
			}
		}
		[MenuItem("Assets/"+moreMenuName+"/Cut "+cutHotkey, true, menuSubCategoryPriority+0)]
		public static bool menuAssetCut_validate(){
			if(Selection.objects.Length<=0) return false;
			return true;
		}
		
		// Clear
		[MenuItem("Assets/"+moreMenuName+"/Clear", false, menuSubCategoryPriority+0)]
		public static void menuAssetClear(){
			clipboard.Clear();
		}
		[MenuItem("Assets/"+moreMenuName+"/Clear", true, menuSubCategoryPriority+0)]
		public static bool menuAssetClear_validate(){
			return clipboard.Count>0;
		}

		// Paste Repeat
		[MenuItem("Assets/"+moreMenuName+"/Paste and Repeat "+pasteRepeatHotkey, false, menuSubCategoryPriority+0)]
		public static void menuAssetPasteRepeat(){
			pasteFiles(Selection.objects,false);
		}
		[MenuItem("Assets/"+moreMenuName+"/Paste and Repeat "+pasteRepeatHotkey, true, menuSubCategoryPriority+0)]
		public static bool menuAssetPasteRepeat_validate(){
			if(Selection.objects.Length!=1 || clipboard.Count==0) return false;
			var path = getDirPathForFile(Selection.objects[0]);
			if(path==null || path.Length<=0) return false;
			if(!AssetDatabase.IsValidFolder(path)) return false;
			return true;
		}

		// Top Menu Items
		
		// Copy
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Copy "+copyHotkey, false, menuTopCategoryPriority+5)]
		public static void menuAssetCopy(){
			if(Selection.objects.Length==0) return;
			foreach(UnityObject obj in Selection.objects){
				var path = AssetDatabase.GetAssetPath(obj);
				bool inClipboard = false;
				foreach((UnityObject obj2, string path2, int type) in clipboard){
					if(path2==path){ inClipboard=true; clipboard.Remove((obj2,path2,type)); break; }
				}
				if(inClipboard) continue;
				clipboard.Add((obj,path,1));
			}
		}
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Copy "+copyHotkey, true, menuTopCategoryPriority+5)]
		public static bool menuAssetCopy_validate(){
			if(Selection.objects.Length<=0) return false;
			return true;
		}
		
		// Paste
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Paste "+pasteHotkey, false, menuTopCategoryPriority+5)]
		public static void menuAssetPaste(){
			pasteFiles(Selection.objects,true);
		}
		[MenuItem("Assets/"+topMenuItemNamePrefix+"Paste "+pasteHotkey, true, menuTopCategoryPriority+5)]
		public static bool menuAssetPaste_validate(){
			if(Selection.objects.Length!=1 || clipboard.Count==0) return false;
			var path = getDirPathForFile(Selection.objects[0]);
			if(path==null || path.Length<=0) return false;
			if(!AssetDatabase.IsValidFolder(path)) return false;
			return true;
		}

		private static void pasteFiles(UnityObject[] selectionObjects,bool clear=true){
			if(selectionObjects.Length<=0) return;
			var dirPath = getDirPathForFile(selectionObjects[0]);
			//AssetDatabase.Refresh();
			if(dirPath==null || dirPath.Length<=0) return;
			var newClipboard = new List<(UnityObject,string,int)>(clipboard);
			try {
				AssetDatabase.StartAssetEditing();
				foreach((UnityObject obj, string path, int type) in clipboard){
					string baseName = path.Substring(path.LastIndexOf("/")+1);
					string newPath = dirPath+"/"+baseName;
					if(path==newPath) continue;
					if(fileExists(newPath)){
						bool btn1 = EditorUtility.DisplayDialog("Copy-Paste Confirmation","\""+baseName+"\" exists at destination.","Rename new item","Rename old item");
						if(btn1){
							bool renamed = false;
							for(int i=2;i<1000;i++){
								string newPath2; string extra = " - Copy ("+i+")";
								if(!baseName.Contains(".")) newPath2 = dirPath+"/"+baseName+extra;
								else newPath2 = dirPath+"/"+baseName.Substring(0,baseName.LastIndexOf("."))+extra+baseName.Substring(baseName.LastIndexOf("."));
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
						else {
							bool renamed = false;
							for(int i=2;i<1000;i++){
								string oldPath; string extra = " - Old ("+i+")";
								if(!baseName.Contains(".")) oldPath = newPath+extra;
								else oldPath = dirPath+"/"+baseName.Substring(0,baseName.LastIndexOf("."))+extra+baseName.Substring(baseName.LastIndexOf("."));
								if(!fileExists(oldPath)){
									string err = AssetDatabase.MoveAsset(newPath,oldPath);
									if(err!=null && err!=""){
										Debug.LogWarning("Error moving file: "+err);
										break;
									}
									renamed = true;
									break;
								}
							}
							if(!renamed){
								Debug.LogWarning("Failed to find new filename for existing file: "+newPath);
								continue;
							}
						}
					}
					// Move or Copy Original Asset
					bool result = false;
					if(type==2){
						string err = AssetDatabase.MoveAsset(path,newPath);
						if(err==null || err=="") result = true;
					}
					else result = AssetDatabase.CopyAsset(path,newPath);
					if(!result) Debug.LogWarning("Failed to copy-paste files: "+path+", to: "+newPath);
					// Handle Clipboard
					if(!clear){
						newClipboard.Remove((obj,path,type));
						var repeatPath = path;
						var repeatType = type;
						if(type==2){ repeatPath=newPath; repeatType=1; }
						newClipboard.Add((obj,repeatPath,repeatType));
					}
				}
			}
			finally {
				AssetDatabase.StopAssetEditing();
			}
			if(clear) clipboard.Clear();
			else clipboard = newClipboard;
		}

		private static string getDirPathForFile(Object obj){
			var path = AssetDatabase.GetAssetPath(obj);
			if(path.Length>0 && !AssetDatabase.IsValidFolder(path)) path = path.Substring(0,path.LastIndexOf("/"));
			if(path.Length<=0) return null;
			return path;
		}

		private static bool fileExists(string path){
			UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
			if(objs.Length>0) return true;
			UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
			if(obj) return true;
			return false;
		}
		
	}
}

#endif
