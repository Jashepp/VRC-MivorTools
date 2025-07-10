
using System;
using System.Collections.Generic;
using UnityEngine;
using MivorTools.MultiAvatar.Components;

#if VRC_SDK_VRCSDK3
using VRC;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MivorTools.MultiAvatar {
	
    [Serializable]
	public class Action: SerializableObject {
		[SerializeField] public bool isEnabled = true;
		[SerializeReference] public GameObject gameObject = null;
		[SerializeReference] public List<GameObject> managedAvatars = new List<GameObject>{};
		[SerializeField] public bool managedAvatarsLock = false;
		[SerializeField] public bool uiExpanded = false;
		[SerializeField] public bool uiAvatarsExpanded = false;
		[SerializeField] public bool uiConfigExpanded = false;
		
		public static bool Multiple => false;
		public static int ProcessOrder => 0;
		public static string Name => "Unknown Action";
		public static string Description => "Not Yet Implemented";
		public enum ActionSource : ushort { ManagedAvatar=0, ObjectConfig=1 }
		public static ActionSource[] AllowedSources => new ActionSource[]{};
		
		public static bool BatchProcess => false;
		
		#if UNITY_EDITOR
		public new void MarkDirty(){
			#if VRC_SDK_VRCSDK3
			if(gameObject) gameObject.MarkDirty();
			#endif
			if(gameObject) EditorUtility.SetDirty(gameObject);
			if(gameObject) EditorSceneManager.MarkSceneDirty(gameObject.scene);
			//base.MarkDirty();
		}
		
		public virtual void OnGUIEdit(){
			EditorUtils.AddLabelField("Not Yet Implemented",null,null,new object[]{ "AutoWidth" });
		}
		public virtual void OnGUISummary(){
			EditorUtils.AddLabelField("Not Yet Implemented",null,null,new object[]{ "AutoWidth" });
		}
		
		public virtual int actionProcessOrder => ActionsHandler.GetActionStaticProp(this.GetType(),"ProcessOrder") as int? ?? 0;
		public virtual bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj) => false;
		public virtual bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj) => false;
		
		// ObjectConfig Actions
		//public static bool ActionBatchProcess(ManagedAvatar managedAvatar,List<(Action,GameObject,GameObject)> batched) => false;
		// ManagedAvatar Actions
		public static bool ActionBatchProcess(ManagedAvatar managedAvatar,GameObject mainAvatar,GameObject clonedAvatar,List<Action> actions) => false;
		
		public virtual void DrawGUI(ObjectConfig objectConfig,GameObject originalObj) => EditorUtils.AddLabelField("Not Yet Implemented",null,null,new object[]{ "AutoWidth" });
		public virtual void DrawGUI(ManagedAvatar managedAvatar,GameObject originalObj) => EditorUtils.AddLabelField("Not Yet Implemented",null,null,new object[]{ "AutoWidth" });
		
		public virtual string GetActionName() => ActionsHandler.GetActionName(this.GetType());
		public virtual string GetActionDescription() => ActionsHandler.GetActionDescription(this.GetType());
		public virtual string GetActionInfoString() => "";
		
		public override string ToString() => "Action: "+this.GetActionName()+(isEnabled?"":" (Disabled)")+"";
		#endif
		
	}
	
}
