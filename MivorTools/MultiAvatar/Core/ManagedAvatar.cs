
using System.Globalization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if VRC_SDK_VRCSDK3
using VRC;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    [Serializable]
	public partial class ManagedAvatar: SerializableObject {
		[SerializeField] public int modelVersion = 1;
		[SerializeField] public bool isEnabled = true;
		[SerializeField] public string avatarName = null;
		[SerializeReference] public GameObject avatarCloneGameObject = null; // Same as avatarClone.gameObject
		[SerializeReference] public AvatarMain avatarMain = null;
		[SerializeReference] public AvatarClone avatarClone = null;
		[SerializeField] public string vrcAvatarID = null;
		[SerializeReference] public List<Action> actions = new List<Action>(); // TODO: test. this might not be saving.
		[SerializeField] public List<MappedComponents> componentMap = new List<MappedComponents>();
		[SerializeField] public bool hasChanged = true;
		[SerializeField] public bool uiExpanded = true;
		[NonSerialized] public bool displayComponentMap = false;
		[NonSerialized] public bool displayActions = false;
		[NonSerialized] public bool displayAddAction = false;
		[NonSerialized] public bool displayActionsSummary = false;
		[NonSerialized] public bool displayObjectsRemoved = false;
		
		public enum IssuesEnum : ushort { NoIssue=0, MissingCloneObj, MissingCloneComp, MissingManagedAvtr, DiffManagedAvtr, DiffAvatarMain, DiffAvatarClone, HasCompAvatarMain, HasCompIsCloned, HasParentCompVRCAD, NotInPosition };
		
		public override string ToString() => "ManagedAvatar: "+avatarName+", "+actions.Count+" Actions"+(isEnabled?"":" (Disabled)")+"";
		
		public static implicit operator GameObject(ManagedAvatar ma) => ma.avatarCloneGameObject;
		public static implicit operator AvatarClone(ManagedAvatar ma) => ma.avatarClone;
		public static implicit operator AvatarMain(ManagedAvatar ma) => ma.avatarMain;
		
	}

	#if UNITY_EDITOR
	public partial class ManagedAvatar: SerializableObject {
		
		public (bool hasIssue,IssuesEnum issueCode,string issueStr) CheckForIssues(){
			if(!this.avatarCloneGameObject) return (true,IssuesEnum.MissingCloneObj,"Missing Unity Object for this Managed Avatar.");
			if(!this.avatarClone) return (true,IssuesEnum.MissingCloneObj,"Missing Managed Avatar's 'Cloned Avatar' component.");
			if(!this.avatarClone.gameObject) return (true,IssuesEnum.MissingCloneObj,"Issues on Managed Avatar's 'Cloned Avatar' component.");
			if(this.avatarCloneGameObject.scene!=this.avatarMain.gameObject.scene) return (true,IssuesEnum.MissingCloneObj,"Missing unity object for this Managed Avatar.");
			if(this.avatarCloneGameObject!=this.avatarClone.gameObject) return (true,IssuesEnum.MissingCloneObj,"Missing unity object for this Managed Avatar.");
			var comp = this.avatarCloneGameObject.GetComponent<AvatarClone>();
			if(!comp) return (true,IssuesEnum.MissingCloneComp,"Missing 'Cloned Avatar' component on Managed Avatar.");
			if(comp.managedAvatar==null) return (true,IssuesEnum.MissingManagedAvtr,"The 'Cloned Avatar' component is missing Managed Avatar.");
			if(comp.managedAvatar!=this) return (true,IssuesEnum.DiffManagedAvtr,"The 'Cloned Avatar' component on Managed Avatar is meant for a different Managed Avatar.");
			if(comp.managedAvatar.avatarMain!=this.avatarMain) return (true,IssuesEnum.DiffAvatarMain,"The 'Cloned Avatar' component on Managed Avatar is meant for a different Multi-Avatar Manager.");
			if(comp!=this.avatarClone) return (true,IssuesEnum.DiffAvatarClone,"The 'Cloned Avatar' component on Managed Avatar differs from the one meant for this Managed Avatar.");
			var comp2 = this.avatarCloneGameObject.GetComponent<AvatarMain>();
			if(comp2) return (true,IssuesEnum.HasCompAvatarMain,"The Managed Avatar has a 'Multi-Avatar Manager' component on it, which it shouldn't have.");
			var comp3 = this.avatarCloneGameObject.GetComponent<ObjectClone>();
			if(comp3) return (true,IssuesEnum.HasCompIsCloned,"The Managed Avatar has a 'Cloned Object' component on it, which it shouldn't have.");
			Component vrcad1 = null;
			#if VRC_SDK_VRCSDK3
				if(this.avatarCloneGameObject.transform.parent && this.avatarCloneGameObject.transform.parent.gameObject) vrcad1 = this.avatarCloneGameObject.transform.parent.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
				Component vrcad2 = this.avatarCloneGameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if((vrcad1 && vrcad2 && vrcad1!=vrcad2) || (vrcad1 && !vrcad2)) return (true,IssuesEnum.HasParentCompVRCAD,"The Managed Avatar has a 'VRC Avatar Descriptor' on a parent object. The Managed Avatar must not be a child of an Avatar Root Object.");
			#endif
			if(!IsManagedAvatarInPosition(this)) return (true,IssuesEnum.NotInPosition,"The Managed Avatar is not in the correct position.");
			// TODO: check transform values
			return (false,0,null);
		}

		public string GetVRCAvatarIDComp(){
			#if VRC_SDK_VRCSDK3
			try{
				VRC.Core.PipelineManager vrcPipelineComp = avatarCloneGameObject.GetComponent<VRC.Core.PipelineManager>();
				if(vrcPipelineComp && vrcPipelineComp.blueprintId!=null && vrcPipelineComp.blueprintId.Length>0) return vrcPipelineComp.blueprintId;
				if(vrcPipelineComp) return "";
			}catch(Exception e){
				Debug.LogException(e);
			}
			#endif
			return null;
		}
		public void GenerateVRCAvatarIDComp(){
			#if VRC_SDK_VRCSDK3
			try{
				VRC.Core.PipelineManager vrcPipelineComp = avatarCloneGameObject.GetComponent<VRC.Core.PipelineManager>();
				if(vrcPipelineComp && vrcPipelineComp.blueprintId!=null && vrcPipelineComp.blueprintId.Length>0) return;
				if(vrcPipelineComp){
					vrcPipelineComp.contentType = VRC.Core.PipelineManager.ContentType.avatar;
					vrcPipelineComp.AssignId();
				}
			}catch(Exception e){
				Debug.LogException(e);
			}
			#endif
		}
		public void CreateVRCAvatarIDComp(){
			#if VRC_SDK_VRCSDK3
			try{
				VRC.Core.PipelineManager vrcPipelineComp = avatarCloneGameObject.GetComponent<VRC.Core.PipelineManager>();
				if(vrcPipelineComp) return;
				vrcPipelineComp = avatarCloneGameObject.AddComponent<VRC.Core.PipelineManager>();
				vrcPipelineComp.contentType = VRC.Core.PipelineManager.ContentType.avatar;
			}catch(Exception e){
				Debug.LogException(e);
			}
			#endif
		}
		public bool SetVRCAvatarIDComp(string id){
			#if VRC_SDK_VRCSDK3
			try{
				VRC.Core.PipelineManager vrcPipelineComp = avatarCloneGameObject.GetComponent<VRC.Core.PipelineManager>();
				if(!vrcPipelineComp) vrcPipelineComp = avatarCloneGameObject.AddComponent<VRC.Core.PipelineManager>();
				if(vrcPipelineComp){
					vrcPipelineComp.contentType = VRC.Core.PipelineManager.ContentType.avatar;
					vrcPipelineComp.blueprintId = id;
					return true;
				}
			}catch(Exception e){
				Debug.LogException(e);
			}
			#endif
			return false;
		}
		
		public new void MarkDirty(){
			//if(avatarMain) avatarMain.MarkDirty();
			//if(avatarMain && avatarMain.gameObject) avatarMain.gameObject.MarkDirty();
			if(avatarClone) avatarClone.MarkDirty();
			if(avatarClone && avatarClone.gameObject) avatarClone.gameObject.MarkDirty();
			base.MarkDirty();
			//EditorUtility.SetDirty(this);
		}
		
		public bool isAvatarBuilt(){
			if(avatarCloneGameObject.GetComponentInChildren<ObjectClone>()!=null) return true;
			return false;
		}
		
		public bool isAvatarFocused(){
			if(avatarMain.gameObject.activeSelf) return false;
			if(avatarCloneGameObject.activeSelf) return true;
			return false;
		}
		
		public void FocusAvatar(bool enableFocus=true){
			foreach(GameObject managedAvatarObj in avatarMain.avatars.ToArray()){
				if(!managedAvatarObj) continue;
				managedAvatarObj.SetActive(false);
			}
			avatarMain.gameObject.SetActive(!enableFocus);
			avatarCloneGameObject.SetActive(enableFocus);
			if(enableFocus && isAvatarBuilt()) VRCSDK_SelectAvatar(avatarCloneGameObject);
			if(enableFocus) IsolateAvatar();
			else IsolateExit();
		}
		
		public void CleanAvatar(){
			IsolateExit();
			ManagedAvatarBuilder.CleanAvatar(this);
			avatarCloneGameObject.SetActive(false);
			bool hasActive = false;
			foreach(GameObject managedAvatarObj in avatarMain.avatars.ToArray()){
				if(managedAvatarObj && managedAvatarObj.activeSelf){ hasActive = true; break; }
			}
			if(!hasActive) avatarMain.gameObject.SetActive(true);
			Selection.objects = new UnityEngine.Object[]{ avatarCloneGameObject };
		}
		
		public void UpdateAvatar(){
			IsolateExit();
			ManagedAvatarBuilder.BuildAvatar(this);
			FocusAvatar(true);
		}
		
		public void IsolateAvatar(){
			IsolateExit();
			List<GameObject> hiddenObjects = new List<GameObject>();
			foreach(ObjectClone compObjectClone in avatarCloneGameObject.GetComponentsInChildren<ObjectClone>(true)){
				GameObject originalObj = compObjectClone.originalObject;
				GameObject clonedObj = compObjectClone.ownObject;
				if(!originalObj) continue;
				bool isHidden = SceneVisibilityManager.instance.IsHidden(originalObj,false);
				if(isHidden) hiddenObjects.Add(clonedObj);
			}
			SceneVisibilityManager.instance.Isolate(avatarCloneGameObject,true);
			foreach(GameObject clonedObj in hiddenObjects){
				SceneVisibilityManager.instance.Hide(clonedObj,false);
			}
		}
		
		public static void IsolateExit(){
			if(SceneVisibilityManager.instance.IsCurrentStageIsolated()) SceneVisibilityManager.instance.ExitIsolation();
		}
		
		public static void VRCSDK_SelectAvatar(GameObject rootAvatarObject){
			#if VRC_SDK_VRCSDK3
			try{
				// run findavatars first
				// VRC.SDK3.Avatars.Components.VRCAvatarDescriptor avatarDescriptor = rootAvatarObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				// if(avatarDescriptor){
				// 	VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.SelectAvatar(avatarDescriptor);
				// }
			}catch(Exception){}
			#endif
		}
		
		//public static ManagedAvatar Create() => UnityEngine.ScriptableObject.CreateInstance<ManagedAvatar>();
		public static ManagedAvatar Create() => new ManagedAvatar();

		public static ManagedAvatar AddNewAvatar(AvatarMain avatarMain,GameObject avatarObject,ManagedAvatar managedAvatar=null,bool addToList=true){
			if(managedAvatar==null) managedAvatar = ManagedAvatar.Create();
			AvatarClone avatarClone = avatarObject.GetComponent<AvatarClone>();
			MAUndo.Record("Prepare Add Managed Avatar",avatarObject);
			if(!avatarClone) avatarClone = avatarObject.AddComponent<AvatarClone>();
			MAUndo.Record("Add Managed Avatar",avatarMain,avatarClone);
			avatarClone.setupViaCode = true;
			avatarClone.ownObject = avatarObject;
			avatarClone.originalObject = avatarMain.gameObject;
			avatarClone.managedAvatar = managedAvatar;
			managedAvatar.avatarCloneGameObject = avatarObject;
			GameObjectUtility.EnsureUniqueNameForSibling(avatarObject);
			if(managedAvatar.avatarName==null) managedAvatar.avatarName = avatarObject.name;
			managedAvatar.avatarMain = avatarMain;
			managedAvatar.avatarClone = avatarClone;
			if(addToList) avatarMain.avatars.Add(avatarObject);
			avatarMain.MarkDirty();
			avatarMain.gameObject.MarkDirty();
			avatarClone.MarkDirty();
			avatarClone.gameObject.MarkDirty();
			managedAvatar.MarkDirty();
			// TODO: Check existing componments (such as avatar descriptor), and add action to keep those and remove all others
			MAUndo.Flush();
			return managedAvatar;
		}

		public static ManagedAvatar CreateNewAvatar(AvatarMain avatarMain){
			GameObject newAvatarObj = new GameObject(){ name="Multi-Avatar: Unnamed New Avatar" };
			ManagedAvatar newManagedAvatar = AddNewAvatar(avatarMain,newAvatarObj);
			RePositionManagedAvatar(newManagedAvatar);
			return newManagedAvatar;
		}

		public static bool IsManagedAvatarInScene(ManagedAvatar managedAvatar){
			if(!managedAvatar.avatarMain || !managedAvatar.avatarMain.gameObject || !managedAvatar.avatarCloneGameObject) return false;
			if(managedAvatar.avatarMain.gameObject.scene==managedAvatar.avatarCloneGameObject.scene) return true;
			return false;
		}
		
		public static bool IsManagedAvatarInPosition(ManagedAvatar managedAvatar){
			//if(!IsManagedAvatarInScene(managedAvatar)) return false;
			if(!managedAvatar.avatarMain || !managedAvatar.avatarMain.gameObject || !managedAvatar.avatarCloneGameObject) return false;
			if(managedAvatar.avatarMain.gameObject.transform.parent==managedAvatar.avatarCloneGameObject.transform.parent) return true;
			return false;
		}

		public static bool RePositionManagedAvatar(ManagedAvatar managedAvatar,bool force=false){
			if(!force && IsManagedAvatarInPosition(managedAvatar)) return true;
			if(!managedAvatar.avatarMain || !managedAvatar.avatarMain.gameObject || !managedAvatar.avatarCloneGameObject) return false;
			Transform mainAvatarTransform = managedAvatar.avatarMain.gameObject.transform;
			Transform clonedObjTransform = managedAvatar.avatarCloneGameObject.transform;
			MAUndo.Record("RePosition Managed Avatar",managedAvatar.avatarCloneGameObject,clonedObjTransform);
			//UnityEditor.EditorUtility.CopySerialized(mainAvatarTransform,clonedObjTransform);
			if(mainAvatarTransform.parent!=clonedObjTransform.parent) clonedObjTransform.SetParent(mainAvatarTransform.parent);
			clonedObjTransform.SetPositionAndRotation(mainAvatarTransform.position,mainAvatarTransform.rotation);
			managedAvatar.avatarCloneGameObject.MarkDirty();
			MAUndo.Flush();
			return true;
		}

		public static void RemoveAvatar(AvatarMain avatarMain,ManagedAvatar managedAvatar){
			if(managedAvatar==null) return;
			MAUndo.Record("Remove Managed Avatar",avatarMain);
			if(managedAvatar.avatarCloneGameObject) avatarMain.avatars.Remove(managedAvatar.avatarCloneGameObject);
			avatarMain.MarkDirty();
			avatarMain.gameObject.MarkDirty();
			managedAvatar.MarkDirty();
			managedAvatar.avatarMain = null;
			if(managedAvatar.avatarClone) managedAvatar.avatarClone.originalObject = null;
			if(managedAvatar.avatarClone) managedAvatar.avatarClone.MarkDirty();
			if(managedAvatar.avatarClone && managedAvatar.avatarClone.gameObject) managedAvatar.avatarClone.gameObject.MarkDirty();
			MAUndo.Flush();
		}
		
		public static void ReOrderAvatarUp(AvatarMain avatarMain,GameObject managedAvatarObj){
			MAUndo.Record("ReOrder Managed Avatar Up",avatarMain);
			var pos = avatarMain.avatars.IndexOf(managedAvatarObj);
			if(pos>0){
				avatarMain.avatars[pos] = avatarMain.avatars[pos-1];
				avatarMain.avatars[pos-1] = managedAvatarObj;
				avatarMain.MarkDirty();
			}
			MAUndo.Flush();
		}
		public static void ReOrderAvatarDown(AvatarMain avatarMain,GameObject managedAvatarObj){
			MAUndo.Record("ReOrder Managed Avatar Down",avatarMain);
			var pos = avatarMain.avatars.IndexOf(managedAvatarObj);
			if(pos<avatarMain.avatars.Count-1){
				avatarMain.avatars[pos] = avatarMain.avatars[pos+1];
				avatarMain.avatars[pos+1] = managedAvatarObj;
				avatarMain.MarkDirty();
			}
			MAUndo.Flush();
		}

	}
	#endif

}
