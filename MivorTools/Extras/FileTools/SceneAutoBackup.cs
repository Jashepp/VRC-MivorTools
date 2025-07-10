
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using VRC.SDK3A.Editor;
using MivorTools.Extras.FileTools;

namespace MivorTools.Extras.SceneAutoBackup {
	
	public class SceneAutoBackup {

		static SceneAutoBackup(){}

		private static bool currentlySavingSceneHere = false;
		private static bool sceneNeedsBackup = false;
		
		private static void saveSceneAsset(Scene scene,string note="",bool promptSave=true,bool saveSuffix=false){
			var unityScenePath = scene.path;
			if(scene.isDirty && promptSave){
				//MivorTools.Editor.AssetBackups.backupFile(unityScenePath,note+(saveSuffix?" - Unsaved":""));
				bool save = true;
				if(!menuSceneAutoBackupPref.optionAutoSave.value()){
					save = EditorUtility.DisplayDialog("MivorTools: Scene Auto-Backup","Do you want to save and backup the scene first?","Yes","No");
				}
				if(save){
					currentlySavingSceneHere = true;
					EditorSceneManager.SaveScene(scene);
					currentlySavingSceneHere = false;
				}
			}
			AssetBackups.backupFile(unityScenePath,note+(saveSuffix?" - Saved":""));
			sceneNeedsBackup = false;
		}
		
		[InitializeOnLoad]
		public class menuSceneAutoBackupPref {
			private const string prefPrefix = "net.mivor.mivortools.extras.sceneautobackup";
			private const string menuMainPrefix = "Tools/MivorTools/Scene Auto-Backup";
			private const string menuAssetPrefix = "Assets/MivorTools/Scene Auto-Backup";

			public const int menuTopCategoryPriority = 1200;
			public const int menuSubCategoryPriority = 1200;

			public class optionAutoSave {
				public const string menu = "Auto Save (Skip Prompt)\t(setting)";
				public const string pref = "autosave";
				public const bool def = true;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionOnlyBackupIfSceneDirty {
				public const string menu = "Only backup if changes were made\t(setting)";
				public const string pref = "onlyifdirty";
				public const bool def = true;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionAutoOnEditorQuit {
				public const string menu = "On: Unity Exit (All Scenes)\t(backup trigger)";
				public const string pref = "oneditorquit";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionAutoOnSceneSave {
				public const string menu = "On: Scene Save\t(backup trigger)";
				public const string pref = "onscenesave";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionAutoOnPlayMode {
				public const string menu = "On: Play Mode\t(backup trigger)";
				public const string pref = "onplaymode";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionAutoOnVRCBuild {
				public const string menu = "On: VRC Build\t(backup trigger)";
				public const string pref = "onvrcbuild";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			
			static menuSceneAutoBackupPref() {
				EditorApplication.delayCall += UpdateMenu;
			}

			private static void UpdateMenu() {
				// Main Menu
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionAutoSave.menu,optionAutoSave.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionOnlyBackupIfSceneDirty.menu,optionOnlyBackupIfSceneDirty.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionAutoOnEditorQuit.menu,optionAutoOnEditorQuit.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionAutoOnSceneSave.menu,optionAutoOnSceneSave.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionAutoOnPlayMode.menu,optionAutoOnPlayMode.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionAutoOnVRCBuild.menu,optionAutoOnVRCBuild.value());
				// Assets Menu
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionAutoSave.menu,optionAutoSave.value());
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionOnlyBackupIfSceneDirty.menu,optionOnlyBackupIfSceneDirty.value());
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionAutoOnEditorQuit.menu,optionAutoOnEditorQuit.value());
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionAutoOnSceneSave.menu,optionAutoOnSceneSave.value());
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionAutoOnPlayMode.menu,optionAutoOnPlayMode.value());
				UnityEditor.Menu.SetChecked(menuAssetPrefix+"/"+optionAutoOnVRCBuild.menu,optionAutoOnVRCBuild.value());
			}

			[MenuItem(menuMainPrefix+"/"+optionAutoSave.menu, false, menuSubCategoryPriority)]
			[MenuItem(menuAssetPrefix+"/"+optionAutoSave.menu, false, menuSubCategoryPriority+0)]
			private static void clickAutoSave() {
				EditorPrefs.SetBool(prefPrefix+"."+optionAutoSave.pref,!optionAutoSave.value());
				UpdateMenu();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionOnlyBackupIfSceneDirty.menu, false, menuSubCategoryPriority)]
			[MenuItem(menuAssetPrefix+"/"+optionOnlyBackupIfSceneDirty.menu, false, menuSubCategoryPriority+0)]
			private static void clickOnlyIfSceneDirty() {
				EditorPrefs.SetBool(prefPrefix+"."+optionOnlyBackupIfSceneDirty.pref,!optionOnlyBackupIfSceneDirty.value());
				UpdateMenu();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionAutoOnEditorQuit.menu, false, menuSubCategoryPriority+50)]
			[MenuItem(menuAssetPrefix+"/"+optionAutoOnEditorQuit.menu, false, menuSubCategoryPriority+50)]
			private static void clickAutoOnEditorQuit() {
				EditorPrefs.SetBool(prefPrefix+"."+optionAutoOnEditorQuit.pref,!optionAutoOnEditorQuit.value());
				UpdateMenu();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionAutoOnSceneSave.menu, false, menuSubCategoryPriority+50)]
			[MenuItem(menuAssetPrefix+"/"+optionAutoOnSceneSave.menu, false, menuSubCategoryPriority+50)]
			private static void clickAutoOnSceneSave() {
				EditorPrefs.SetBool(prefPrefix+"."+optionAutoOnSceneSave.pref,!optionAutoOnSceneSave.value());
				UpdateMenu();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionAutoOnPlayMode.menu, false, menuSubCategoryPriority+50)]
			[MenuItem(menuAssetPrefix+"/"+optionAutoOnPlayMode.menu, false, menuSubCategoryPriority+50)]
			private static void clickAutoOnPlayMode() {
				EditorPrefs.SetBool(prefPrefix+"."+optionAutoOnPlayMode.pref,!optionAutoOnPlayMode.value());
				UpdateMenu();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionAutoOnVRCBuild.menu, false, menuSubCategoryPriority+50)]
			[MenuItem(menuAssetPrefix+"/"+optionAutoOnVRCBuild.menu, false, menuSubCategoryPriority+50)]
			private static void clickAutoOnVRCBuild() {
				EditorPrefs.SetBool(prefPrefix+"."+optionAutoOnVRCBuild.pref,!optionAutoOnVRCBuild.value());
				UpdateMenu();
			}
			
		}

		// AutoBackup - On Editor Quit
		[InitializeOnLoad]
		public class AssetBackups_OnEditorQuit {
			static AssetBackups_OnEditorQuit(){
				EditorApplication.quitting += OnEditorQuit;
			}

			public static void OnEditorQuit(){
				if(!menuSceneAutoBackupPref.optionAutoOnEditorQuit.value()) return;
				// Backup
				for(int i=0; i<SceneManager.sceneCount; i++) {
					Scene scene = SceneManager.GetSceneAt(i);
					if(menuSceneAutoBackupPref.optionOnlyBackupIfSceneDirty.value() && !scene.isDirty && !sceneNeedsBackup) continue;
					saveSceneAsset(scene," - Auto On Unity Exit");
				}
			}
		}
		
		// AutoBackup - On Scene Save
		[InitializeOnLoad]
		public class AssetBackups_OnSceneSave {
			static AssetBackups_OnSceneSave(){
				// Before: sceneSaving, After: sceneSaved
				EditorSceneManager.sceneSaved += OnSceneSave;
			}

			public static void OnSceneSave(Scene scene){
				if(currentlySavingSceneHere) return;
				if(!menuSceneAutoBackupPref.optionAutoOnSceneSave.value()){
					sceneNeedsBackup = true;
					return;
				}
				//if(menuSceneAutoBackupPref.optionOnlyBackupIfSceneDirty.value() && !scene.isDirty) return;
				// Backup
				saveSceneAsset(scene," - Auto On Scene Save",false,false);
			}
		}
		
		// AutoBackup - On Play Mode
		[InitializeOnLoad]
		public class AssetBackups_PlayModeTrigger {
			static AssetBackups_PlayModeTrigger(){
				EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			}

			public static void OnPlayModeStateChanged(PlayModeStateChange state){
				if(!menuSceneAutoBackupPref.optionAutoOnPlayMode.value()) return;
				if(state == PlayModeStateChange.ExitingEditMode){
					// Backup
					Scene scene = SceneManager.GetActiveScene();
					if(menuSceneAutoBackupPref.optionOnlyBackupIfSceneDirty.value() && !scene.isDirty && !sceneNeedsBackup) return;
					saveSceneAsset(scene," - Auto On Play Mode");
				}
			}
		}
		
		// AutoBackup - On VRC Build
		[InitializeOnLoad]
		public class AssetBackups_PreInstantiateHook {
			static AssetBackups_PreInstantiateHook(){
				VRCSdkControlPanel.OnSdkPanelEnable += (sender, e)=>{
					if(VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)){
						builder.OnSdkBuildStart += (sender2, target)=>{
							if(target is GameObject targetObj) OnVRCBuild(targetObj);
						};
					}
				};
			}

			public static void OnVRCBuild(GameObject targetObj){
				if(!menuSceneAutoBackupPref.optionAutoOnVRCBuild.value()) return;
				// Backup
				Scene scene = targetObj.scene;
				if(menuSceneAutoBackupPref.optionOnlyBackupIfSceneDirty.value() && !scene.isDirty && !sceneNeedsBackup) return;
				saveSceneAsset(scene," - Auto On VRC Build");
			}
		}

	}
}

#endif
