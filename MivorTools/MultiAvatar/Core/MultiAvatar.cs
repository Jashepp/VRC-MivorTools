
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if VRC_SDK_VRCSDK3
using VRC;
#endif

#if UNITY_EDITOR
using HarmonyLib;
using System.Reflection;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace MivorTools.MultiAvatar {

    // https://docs.unity3d.com/Manual/script-Serialization.html

    [Serializable]
	public abstract class SerializableComponent: MonoBehaviour, VRC.SDKBase.IEditorOnly, UnityEngine.ISerializationCallbackReceiver {
		[SerializeField] public bool modelIsFresh = true;
		[SerializeField] public bool modelWasDeserialized = false;
		[SerializeField] public bool modelWasSerialized = false;
		void ISerializationCallbackReceiver.OnAfterDeserialize(){ this.modelWasDeserialized=true; this.modelIsFresh=false; }
		void ISerializationCallbackReceiver.OnBeforeSerialize(){ this.modelWasSerialized=true; this.modelIsFresh=false; }
		
		#if UNITY_EDITOR
		public void MarkDirty(){
			// https://forum.unity.com/threads/variables-are-not-saved-with-the-scene-when-using-custom-editor.374984/#post-2470136
			#if VRC_SDK_VRCSDK3
			gameObject.MarkDirty();
			#endif
			EditorUtility.SetDirty(this);
			if(gameObject) EditorUtility.SetDirty(gameObject);
			EditorSceneManager.MarkSceneDirty(gameObject.scene);
		}
		
		[NonSerialized] public UnityEvent OnChanged = new UnityEvent();
		#endif
		
		public static implicit operator GameObject(SerializableComponent c) => c.gameObject;
	}

	[Serializable]
	// Must not extend UnityEngine.Object or UnityEngine.ScriptableObject, as VRC Avatar Builds break the serialisation of those.
	public abstract class SerializableObject : UnityEngine.ISerializationCallbackReceiver, IEquatable<SerializableObject> {
		[SerializeField] public bool modelIsFresh = true;
		[SerializeField] public bool modelWasDeserialized = false;
		[SerializeField] public bool modelWasSerialized = false;
		void ISerializationCallbackReceiver.OnAfterDeserialize(){ this.modelWasDeserialized=true; this.modelIsFresh=false; }
		void ISerializationCallbackReceiver.OnBeforeSerialize(){ this.modelWasSerialized=true; this.modelIsFresh=false; }
		
		[SerializeField] private string _instanceID = null;
		public SerializableObject(){ _instanceID=this.GetType()+"_"+Guid.NewGuid().ToString(); }
		
		#if UNITY_EDITOR
		public void MarkDirty(){
			// https://forum.unity.com/threads/variables-are-not-saved-with-the-scene-when-using-custom-editor.374984/#post-2470136
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}
		
		[NonSerialized] public UnityEvent OnChanged = new UnityEvent();
		#endif
		
		public bool Equals(SerializableObject value=null) => (this is null && value is null)
			|| (this is not null && value is not null && ReferenceEquals(this,value));
		public override bool Equals(object value=null) => (this is null && value is null)
			|| (value is SerializableObject @obj && Equals(@obj));
		public override int GetHashCode() => (typeof(SerializableObject).FullName,this._instanceID).GetHashCode();
		#nullable enable
		public static bool operator ==(SerializableObject? a, object? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as object));
		public static bool operator !=(SerializableObject? a, object? b) => !(a==b);
		public static implicit operator bool(SerializableObject? o) => o is not null && o?._instanceID!=null;
		#nullable disable
	}

	namespace ActionList { }

	#if UNITY_EDITOR
	public class ActionsHandler {

		public static Type[] actionTypes = new Type[]{};

		public static void FindActionTypes(){
			if(actionTypes.Length>0) return;
			foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()){
				if(type.Namespace!="MivorTools.MultiAvatar.ActionList") continue;
				//Debug.Log(type.Name+", "+type.Namespace+", "+type.IsSubclassOf(typeof(Action)).ToString());
				if(type.IsSubclassOf(typeof(Action))) actionTypes = actionTypes.AddToArray(type);
			}
		}

		// #if UNITY_EDITOR
		// [UnityEditor.InitializeOnLoad]
		// public class ActionsHandlerInitializer {
		// 	static ActionsHandlerInitializer() => ActionsHandler.FindActionTypes();
		// }
		// #endif

		public static Type GetActionTypeByName(string name){
			foreach(Type type in actionTypes){ if(type.Name==name) return type; }
			return null;
		}
		
		// Reflection Methods
		public static ActionType CreateAction<ActionType>(Type type) where ActionType : Action => (ActionType)type.GetConstructors()[0].Invoke(new object[]{});
		public static ActionType CreateAction<ActionType>() where ActionType : Action => (ActionType)CreateAction(typeof(ActionType));
		public static Action CreateAction(Type type) => CreateAction<Action>(type);

		public static object GetActionStaticProp(Type type,string propName) => type.GetProperty(propName,BindingFlags.Public | BindingFlags.Static).GetValue(null,null);
		
		// Utility Methods
		public static bool CanBeMultiple(Type type) => GetActionStaticProp(type,"Multiple") as bool? ?? false;
		public static int GetProcessOrder(Type type) => GetActionStaticProp(type,"ProcessOrder") as int? ?? 0;
		public static string GetActionName(Type type) => (string)GetActionStaticProp(type,"Name");
		public static string GetActionDescription(Type type) => (string)GetActionStaticProp(type,"Description");

		public static bool CheckActionAllowedSources(Type type,Action.ActionSource source) => ((Action.ActionSource[])GetActionStaticProp(type,"AllowedSources")).Where(s=>s==source).Count()>0;
		
		public static bool UsesBatchProcessing(Type type) => GetActionStaticProp(type,"BatchProcess") as bool? ?? false;
		public static bool RunActionBatchProcess(Type type,ManagedAvatar managedAvatar,GameObject mainAvatar,GameObject clonedAvatar,List<Action> actions){
			MethodInfo method = type.GetMethod("ActionBatchProcess",BindingFlags.Public | BindingFlags.Static);
			return (bool)method.Invoke(null,new object[]{ managedAvatar,mainAvatar,clonedAvatar,actions });
		}
		
	}

	public class CopyPaste {
		
		public static ManagedAvatar copiedManagedAvatar = null;
		public static Action copiedAction = null;

		public static ManagedAvatar CopyManagedAvatar(ManagedAvatar managedAvatar,bool save=false,ManagedAvatar target=null){
			ManagedAvatar targetManagedAvatar = target ?? ManagedAvatar.Create();
			//UnityEditor.EditorUtility.CopySerialized(managedAvatar,targetManagedAvatar);
			targetManagedAvatar.actions = new List<Action>();
			foreach(Action oldAction in managedAvatar.actions){
				Action newAction = CopyAction(oldAction,false);
				targetManagedAvatar.actions.Add(newAction);
			}
			if(save) copiedManagedAvatar = targetManagedAvatar;
			return targetManagedAvatar;
		}
		public static ActionType CopyAction<ActionType>(ActionType action,bool save=false,ActionType target=null) where ActionType : Action {
			ActionType targetAction = target ?? ActionsHandler.CreateAction<ActionType>(action.GetType());
			//UnityEditor.EditorUtility.CopySerialized(action,targetAction);
			if(save) copiedAction = targetAction;
			return targetAction;
		}

		public static ManagedAvatar CopySaveManagedAvatar(ManagedAvatar managedAvatar) => CopyManagedAvatar(managedAvatar,true);
		public static ActionType CopySaveAction<ActionType>(ActionType action) where ActionType : Action => CopyAction<ActionType>(action,true);

		public static ManagedAvatar PasteSavedManagedAvatar(ManagedAvatar target=null) => CopyManagedAvatar(copiedManagedAvatar,false,target);
		public static ActionType PasteSavedAction<ActionType>(ActionType target=null) where ActionType : Action => CopyAction<ActionType>((ActionType)copiedAction,false,target);
		
	}

	public class MAUndo {

		public static void Undo(){
			UnityEditor.Undo.PerformUndo();
			// Clear();
		}

		public static void Flush(){
			UnityEditor.Undo.FlushUndoRecordObjects();
			// objStack.Clear();
		}
		// public static void Clear(){
		// 	//UnityEditor.Undo.ClearAll();
		// 	objStack.Clear();
		// }
		
		// public static List<UnityEngine.Object> objStack = new List<UnityEngine.Object>{};
		// public static void OnlyFlushDirty(){
		// 	bool isDirty = false;
		// 	foreach(UnityEngine.Object obj in objStack){
		// 		if(UnityEditor.EditorUtility.IsDirty(obj)){ isDirty = true; break; }
		// 	}
		// 	if(isDirty){
		// 		Flush();
		// 		// foreach(UnityEngine.Object obj in objStack){
		// 		// 	UnityEditor.Undo.RegisterCompleteObjectUndo(obj,"MivorTools MultiAvatar: Modified: "+obj.name);
		// 		// 	if(obj.GetType().IsSubclassOf(typeof(SerializableComponent))) UnityEditor.EditorApplication.delayCall += ()=>((SerializableComponent)obj).OnChanged.Invoke();
		// 		// }
		// 	}
		// 	// else Clear();
		// }

		// Currently not working correctly
		public static void Record(string message,bool flush=true,params UnityEngine.Object[] objects){
			// if(flush) Flush();
			// //if(clearStack) Clear();
			// //UnityEditor.Undo.RecordObjects(objects,"MivorTools MultiAvatar: "+message);
			// foreach(UnityEngine.Object obj in objects){
			// 	//objStack.Add(obj);
			// 	UnityEditor.Undo.RegisterCompleteObjectUndo(obj,"MivorTools MultiAvatar: "+message);
			// }
		}

		// public delegate void PerformChanges();
		// public static void Record(string message,UnityEngine.Object[] objects,PerformChanges performChangesCallback=null){
		// 	UnityEditor.Undo.FlushUndoRecordObjects();
		// 	Record(message,clearStack:true,objects);
		// 	performChangesCallback();
		// 	OnlyFlushDirty();
		// }

	}
	
	public class MetaPackageInfo {
		public string version = null;
		
		public static string GetVersion() => Resolve().version;
		public static MetaPackageInfo cached = null;
		public static MetaPackageInfo Resolve(){
			if(cached!=null) return cached;
			try {
				#if UNITY_EDITOR
				string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath("5cd5091ed81c73041b6a69a92da21e30");
				string jsonString = string.IsNullOrEmpty(assetPath) ? null : System.IO.File.ReadAllText(assetPath);
				if(jsonString!=null) return cached = JsonUtility.FromJson<MetaPackageInfo>(jsonString);
				#endif
			} catch (System.Exception e) {
				Debug.LogError(e);
			}
			return new MetaPackageInfo();
		}
		public static bool DrawGUICriticalIssues(){
			if(!IsUnityVersionSupported()){
				EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>MivorTools Multi-Avatar does not support this version of Unity. \n\nPlease upgrade unity to 2022.3 or a newer version which VRChat SDK supports.</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft, wordWrap=true },new GUILayoutOption[]{});
				return true;
			}
			if(!IsVRCSDKVersionSupported()){
				EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>MivorTools Multi-Avatar does not support this version of VRChat SDK. \n\nPlease upgrade VRChat SDK to SDK3 or a newer version.</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft, wordWrap=true },new GUILayoutOption[]{});
				return true;
			}
			return false;
		}
		public static bool IsUnityVersionSupported(){
			#if UNITY_2022_3_OR_NEWER
				return true;
			#else
				return false;
			#endif
		}
		public static bool IsVRCSDKVersionSupported(){
			#if VRC_SDK_VRCSDK3
				return true;
			#else
				return false;
			#endif
		}
	}
	
	#endif

}
