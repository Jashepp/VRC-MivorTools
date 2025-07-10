
using System;
using System.Linq;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEngine.Events;
using UnityEditor;
using HarmonyLib;
#endif

namespace MivorTools.MultiAvatar.Components {

	#if UNITY_EDITOR
	[ExecuteInEditMode]
	[AddComponentMenu("MivorTools/Multi-Avatar/Multi-Avatar: Managed Avatar",12)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	#endif
	[Serializable]
	public class AvatarClone : SerializableComponent {
		[SerializeField] public int modelVersion = 1;
		[SerializeReference] public GameObject ownObject = null;
		[SerializeReference] public GameObject originalObject = null;
		[SerializeField] public ManagedAvatar managedAvatar = null;
		[SerializeField] public bool setupViaCode = false;
		[NonSerialized] public GameObject reAddAvatarObjectSelect = null;
		[NonSerialized] public bool reAddAvatarIsNew = true;

		public static implicit operator ManagedAvatar(AvatarClone c) => c.managedAvatar;
		public static implicit operator AvatarMain(AvatarClone c) => c.managedAvatar.avatarMain;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(AvatarClone))]
	public class AvatarCloneEditor : UnityEditor.Editor {
		
		public override void OnInspectorGUI(){
			if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused){ EditorGUILayout.LabelField("Component not editable during play mode.",new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) }); return; }
			
			AvatarClone instance = (AvatarClone)this.target;
			this.serializedObject.Update();
			if(MetaPackageInfo.DrawGUICriticalIssues()) return;

			#if VRC_SDK_VRCSDK3
			bool guiDrawn = false;
			bool removeComp = false;
			bool reAddAvatar = false;
			bool firstTime = false;
			// Misc Error Check
			if(!guiDrawn && !instance.setupViaCode){ guiDrawn=true; firstTime=true; instance.managedAvatar=null; }
			if(!guiDrawn && instance.ownObject!=instance.gameObject){ guiDrawn=true; reAddAvatar=true; GUI_Error_Simple("- This component cannot be copied from a different object."); }
			if(!guiDrawn && (!instance.originalObject || instance.originalObject.scene!=instance.gameObject.scene)){ guiDrawn=true; reAddAvatar=true; GUI_Error_Simple("- Missing Original Avatar that this avatar is a clone of."); }
			if(!guiDrawn && !instance.managedAvatar.avatarMain){ guiDrawn=true; reAddAvatar=true; GUI_Error_Simple("- This avatar has been removed from its Multi-Avatar Manager."); }
			if(!guiDrawn && !instance.managedAvatar){ guiDrawn=true; reAddAvatar=true; GUI_Error_Simple("- Missing managed avatar?"); }
			if(!guiDrawn && instance.managedAvatar.avatarClone!=instance){ guiDrawn=true; removeComp=true; reAddAvatar=true; GUI_Error_Simple("- The managed avatar references a different cloned avatar?"); }
			if(!guiDrawn && instance.managedAvatar.avatarCloneGameObject!=instance.gameObject){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- The managed avatar references a different cloned gameObject?"); }
			// Re-Add Avatar
			if(reAddAvatar || firstTime){
				if(firstTime) EditorUtils.AddLabelField("<b>First Time Setup:</b>",new object[]{ "AutoWidth" });
				else EditorUtils.AddLabelField("<b>Attempt to Auto-Fix:</b>",new object[]{ "AutoWidth" });
				GameObject newAvatar = null;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.AddLabelField("Avatar: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					newAvatar = instance.reAddAvatarObjectSelect = (GameObject)EditorGUILayout.ObjectField("",instance.reAddAvatarObjectSelect,typeof(GameObject),true);
				});
				if(newAvatar) EditorUtils.LayoutVertical(()=>{
					bool hasIssues = false;
					if(!hasIssues && newAvatar==instance.gameObject){ hasIssues=true; GUI_Error_Simple("- Must select a different Avatar/Object, not this one."); }
					if(!hasIssues){
						Component avatarClone = newAvatar.GetComponentInParent<AvatarClone>(true);
						if(avatarClone){ hasIssues=true; GUI_Error_Simple("- Avatar must not be a cloned avatar like this one."); }
					}
					if(!hasIssues){
						Component vrcad = newAvatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
						if(!vrcad){ hasIssues=true; GUI_Error_Simple("- Missing VRC Avatar Descriptor on avatar."); }
					}
					if(hasIssues) newAvatar = null;
				});
				EditorUtils.LayoutDisabled(!newAvatar,()=>{
					if(!instance.managedAvatar) instance.reAddAvatarIsNew = EditorUtils.AddCheckbox(instance.reAddAvatarIsNew,"This is a new Avatar",null);
					else instance.reAddAvatarIsNew = false;
					bool autoFix = GUILayout.Button(new GUIContent("Add Avatar","Add this managed avatar onto the Multi-Avatar Manager for selected Avatar Root Object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
					if(newAvatar && autoFix){
						var prevManagedAvatar = instance.managedAvatar;
						if(instance.reAddAvatarIsNew) prevManagedAvatar = null;
						AvatarMain avatarMain = newAvatar.GetComponent<AvatarMain>();
						if(!avatarMain) avatarMain = newAvatar.AddComponent<AvatarMain>();
						ManagedAvatar newManagedAvatar = prevManagedAvatar;
						if(instance.reAddAvatarIsNew && prevManagedAvatar!=null) newManagedAvatar = CopyPaste.CopyManagedAvatar(prevManagedAvatar,false);
						ManagedAvatar.AddNewAvatar(avatarMain,instance.gameObject,newManagedAvatar);
					}
				});
			}
			if(!guiDrawn && instance.reAddAvatarObjectSelect) instance.reAddAvatarObjectSelect = null;
			// Auto-Fix Different Main Avatar
			if(!guiDrawn && instance.managedAvatar.avatarMain && instance.managedAvatar.avatarMain!=instance.originalObject.GetComponent<AvatarMain>()){
				guiDrawn=true; removeComp=true; GUI_Error_Simple("- The attached Multi-Avatar Manager references a different avatar?");
				bool autoFix = GUILayout.Button(new GUIContent("Auto-Fix: Use Avatar","Change this cloned avatar to use the different avatar"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
				if(autoFix){
					instance.originalObject = instance.managedAvatar.avatarMain.gameObject;
					ManagedAvatar.AddNewAvatar(instance.managedAvatar.avatarMain,instance.gameObject,instance.managedAvatar);
				}
			}
			// Auto-Fix Broken/Reset Multi-Avatar Manager (Missing This Avatar)
			if(!guiDrawn && instance.managedAvatar.avatarMain.avatars.Where(a=>a==instance.managedAvatar.avatarCloneGameObject).Count()==0){
				guiDrawn=true; GUI_Error_Simple("- This managed avatar has been removed from the Multi-Avatar Manager?");
				bool autoFix = GUILayout.Button(new GUIContent("Auto-Fix: Add Avatar","Add this managed avatar back onto the Multi-Avatar Manager"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
				if(autoFix) ManagedAvatar.AddNewAvatar(instance.managedAvatar.avatarMain,instance.gameObject,instance.managedAvatar);
			}
			// Misc Error Check
			if(!guiDrawn){
				Component vrcad = instance.originalObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if(!vrcad){ guiDrawn=true; GUI_Error_Simple("- Missing VRC Avatar Descriptor on original avatar."); }
			}
			if(!guiDrawn){
				Component multiAvatarMain = instance.originalObject.GetComponent<AvatarMain>();
				if(!multiAvatarMain){ guiDrawn=true; GUI_Error_Simple("- Missing Multi-Avatar Manager on original avatar."); }
			}
			if(guiDrawn && removeComp){
				if(reAddAvatar) EditorGUILayout.Space(10,false);
				bool removeCompBtn = GUILayout.Button(new GUIContent("Remove This Component"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
				if(removeCompBtn) EditorApplication.delayCall += ()=>{ DestroyImmediate(instance); };
			}
			// Draw Actual UI
			if(!guiDrawn){
				guiDrawn=true; GUI_AvatarClone(instance);
			}
			#endif

			//this.serializedObject.ApplyModifiedProperties();
		}
		
		private void GUI_Error_Simple(string message){
			EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issues Found:</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
			EditorGUILayout.LabelField(message,new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, wordWrap=true, padding=new RectOffset(5,5,5,5) });
			//EditorGUILayout.LabelField(new GUIContent("Note: This component might still have data attached to it."),new GUIStyle(GUI.skin.GetStyle("Label")) { alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
			// TODO: list simple summary of data
		}

		private void GUI_AvatarClone(AvatarClone instance){
			//AvatarMainEditor.GUI_Title(false);

			AvatarMain multiAvatarMain = instance.managedAvatar.avatarMain;

			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Original Avatar: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",instance.originalObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
				});
				bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Main Avatar GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
				if(openObjectInsp){ EditorUtility.OpenPropertyEditor(instance.originalObject); }
				bool openMultiAvatarMain = EditorUtils.AddButton("Manager","Opens Multi-Avatar Manager in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,5,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
				if(openMultiAvatarMain){ AvatarMainWindow.OpenAsWindow(multiAvatarMain,true,instance.managedAvatar); }
			});

			multiAvatarMain.avatarListCheckIssues = null;
			AvatarMainEditor.GUI_AvatarList_Header(multiAvatarMain,instance.managedAvatar.avatarCloneGameObject,showClean:true,showUpdate:!false,isToggle:false,reOrder:false,focusAvatar:true);
			AvatarMainEditor.GUI_AvatarList_Body(multiAvatarMain,instance.managedAvatar.avatarCloneGameObject,isOnAvatarMain:false);

		}
		
	}
	
	#endif

}
