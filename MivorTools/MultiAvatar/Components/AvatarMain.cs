
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if VRC_SDK_VRCSDK3
using VRC;
using MivorTools.MultiAvatar.ActionList;

#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar.Components {

	#if UNITY_EDITOR
	[ExecuteInEditMode]
	[AddComponentMenu("MivorTools/Multi-Avatar/Multi-Avatar: Manager",10)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	#endif
	[Serializable]
	public class AvatarMain : SerializableComponent {
		[SerializeField] public int modelVersion = 1;
		//[SerializeReference]
		[SerializeReference] public List<GameObject> avatars = new List<GameObject>();

		[NonSerialized] public GameObject addAvatarObject = null;
		[NonSerialized] public bool displayAddNewAvatar = false;
		[NonSerialized] public object avatarListCheckIssues = null;
		[NonSerialized] public GameObject[] avatarsReAddSelect = new GameObject[]{};
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(AvatarMain))]
	public class AvatarMainEditor : UnityEditor.Editor {

		// Only set if opened within window
		public AvatarMainWindow window = null;
		
		public override void OnInspectorGUI(){
			if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused){ EditorGUILayout.LabelField("Component not editable during play mode.",new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) }); return; }
			
			AvatarMain instance = (AvatarMain)this.target;
			//if(!window && AvatarMainWindow.window) AvatarMainWindow.window.Close();
			//if(!window && AvatarMainWindow.window) AvatarMainWindow.OpenAsWindow(instance);
			this.serializedObject.Update();
			if(MetaPackageInfo.DrawGUICriticalIssues()) return;

			#if VRC_SDK_VRCSDK3
			bool guiDrawn = false;
			if(!guiDrawn){
				Component vrcad = instance.gameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if(!vrcad && instance.gameObject.transform.parent){
					Component vrcad2 = instance.gameObject.transform.parent.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
					if(vrcad2 && vrcad2.gameObject!=instance.gameObject){
						Component multiAvatarMain = vrcad2.gameObject.GetComponent<AvatarMain>();
						if(multiAvatarMain){ guiDrawn=true; GUI_Error_Simple("- A Multi-Avatar Manager already exists on a parent object."); }
					}
					if(!guiDrawn && vrcad2){
						guiDrawn=true; GUI_Error_Simple("- This component must be on an Avatar Root Object.\n- A parent object has a VRC Avatar Descriptor.");
						Component objectConfigOwn = instance.gameObject.GetComponent<ObjectConfig>();
						// Auto-Fix Buttons
						bool autoFix = GUILayout.Button(new GUIContent("Auto-Fix: Move to Avatar","Moves this component to Avatar Root Object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
						bool autoFix2 = false;
						if(!objectConfigOwn) autoFix2 = GUILayout.Button(new GUIContent("Auto-Fix: Move to Avatar & Create Object Config","Moves this component to Avatar Root Object, and also creates a Multi-Avatar Object Config on this object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
						if(autoFix || autoFix2){
							MAUndo.Record("Auto-Fix AvatarMain #1",instance.gameObject);
							if(autoFix2) instance.gameObject.AddComponent<ObjectConfig>();
							Component newComp = vrcad2.gameObject.AddComponent<AvatarMain>();
							EditorUtility.CopySerialized(instance,newComp);
							Selection.objects = new UnityEngine.Object[]{ vrcad2.gameObject };
							SceneView.FrameLastActiveSceneView();
							EditorApplication.delayCall += ()=>{
								MAUndo.Record("Auto-Fix AvatarMain #2",instance.gameObject);
								DestroyImmediate(instance);
							};
						}
					}
				}
				if(!guiDrawn && !vrcad){ guiDrawn=true; GUI_Error_Simple("- This component must be on an Avatar Root Object.\n- This object has no VRC Avatar Descriptor."); }
			}
			if(!guiDrawn){
				guiDrawn=true; GUI_AvatarMain(instance);
			}
			#endif

			//this.serializedObject.ApplyModifiedProperties();
		}
		
		private void GUI_Error_Simple(string message){
			AvatarMain instance = (AvatarMain)this.target;
			EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issues Found:</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
			EditorGUILayout.LabelField(message,new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
			if(instance.avatars.Count>0){
				EditorGUILayout.LabelField(new GUIContent("Note: <b>This component has data</b> attached to it."),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
				// TODO: list simple summary of data
			}
		}

		public static void GUI_AvatarMain(AvatarMain instance){
			
			// Title
			GUI_Title(true);

			// Quick Tiny Buttons
			// bool btn1 = EditorHelpers.AddButton("How to start","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=10, margin=new RectOffset(5,5,0,0), padding=new RectOffset(6,6,3,3) },new object[]{ "AutoWidth", "AutoHeight" });
			// if(btn1){  }
			
			// Add First Avatar or List Avatars
			if(instance.avatars.Count==0) GUI_AddFirstAvatar(instance);
			else GUI_AvatarList(instance);

			//if(isChanged) instance.gameObject.MarkDirty();
			//if(isChanged) instance.OnChanged.Invoke();
		}
		
		public static void GUI_Title(bool isMainComp=false){
			// Title
			EditorUtils.AddLabelField("MivorTools Multi-Avatar",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.UpperCenter, fontSize=20, margin=new RectOffset(5,5,12,0) },new object[]{ "AutoMinWidth", "AutoHeight" });
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
				string version = MetaPackageInfo.GetVersion();
				EditorGUILayout.Separator();
				EditorUtils.AddLabelField(version!=null?"v"+version:"v?","Multi-Avatar Version, click to see latest releases",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.LowerCenter, fontSize=12 },new object[]{ "AutoWidth" });
				if(EditorUtils.eventLastRectClicked()){
					Application.OpenURL("https://mivortools.mivor.net/"); // TODO: Releases page (or VCC VRM instructions page)
				}
				EditorUtils.AddLabelField(" | ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.LowerCenter, fontSize=12, normal=new GUIStyleState(){ textColor=Color.grey } },new object[]{ "AutoWidth" });
				EditorUtils.AddLabelField("MivorTools","Click to visit GitHub Repo",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.LowerCenter, fontSize=12 },new object[]{ "AutoWidth" });
				if(EditorUtils.eventLastRectClicked()){
					Application.OpenURL("https://mivortools.mivor.net/"); // TODO: Github Repo
				}
				EditorGUILayout.Separator();
			});
		}

		public static void GUI_AddFirstAvatar(AvatarMain instance){
			EditorUtils.AddLabelField("To get started, add an Avatar:",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
			EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,5,0) },()=>{
				GUI_AddAvatar(instance);
			});
		}

		public static bool GUI_AddAvatar(AvatarMain instance){
			bool addedAvatar = false;
			#if VRC_SDK_VRCSDK3
			EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				GameObject avatarObject = null;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
					EditorUtils.AddLabelField("Select blank Avatar/Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					avatarObject = (GameObject)EditorGUILayout.ObjectField("",instance.addAvatarObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
					instance.addAvatarObject = avatarObject;
				});
				if(avatarObject) EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,10) },()=>{
					bool hasIssues = false;
					// Check for isssues
					{
						// Check For Issue
						if(avatarObject==instance.gameObject){
							hasIssues = true;
							EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> Must select a different Avatar/Object, not this one.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						}
						// Check For Issue
						if(avatarObject.GetComponent<AvatarMain>()==instance){
							hasIssues = true;
							EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object must be outside of this Avatar Root Object, not a child object.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						}
						// Check For Issue
						Component vrcad1 = null;
						if(avatarObject.transform.parent && avatarObject.transform.parent.gameObject) vrcad1 = avatarObject.transform.parent.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
						Component vrcad2 = avatarObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
						if((vrcad1 && vrcad2 && vrcad1!=vrcad2) || (vrcad1 && !vrcad2)){
							hasIssues = true;
							EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object must be the Avatar Root Object itself, not a child object of it.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						}
						// Check For Issue
						if(avatarObject.scene!=instance.gameObject.scene){
							hasIssues = true;
							EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object is on a different scene.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						}
						// Check For Issue
						// if(avatarObject.transform.childCount>0){
						// 	hasIssues = true;
						// 	EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object has children. It must be an empty object.",""),new GUIStyle(EditorHelpers.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						// }
						// Check For Issue
						Component avatarClone = avatarObject.GetComponentInParent<AvatarClone>(true);
						Component avatarMain = avatarObject.GetComponentInParent<AvatarMain>(true);
						if(avatarClone || avatarMain){
							hasIssues = true;
							EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object is already used in a Multi-Avatar Manager.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
						}
					}
					// Continue
					if(hasIssues){
						EditorGUILayout.LabelField(new GUIContent("If you're unsure what to do, click 'Create New Avatar' below.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					if(!hasIssues){
						EditorGUILayout.LabelField(new GUIContent("<color='#A0FFA0'><b>Good:</b></color> The Avatar/Object seems good to use.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					bool addAvatar = false;
					EditorUtils.LayoutDisabled(hasIssues,()=>{
						addAvatar = EditorUtils.AddButton("Add Selected Avatar","Links a selected Scene Object to use as a Cloned Avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoMinWidth" });
					});
					if(!hasIssues && addAvatar){
						ManagedAvatar newManagedAvatar = ManagedAvatar.AddNewAvatar(instance,avatarObject);
						Selection.objects = new UnityEngine.Object[]{ newManagedAvatar.avatarCloneGameObject };
						addedAvatar = true;
						// UI will be redrawn with avatar list
					}
				});
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
					EditorUtils.AddLabelField("Or: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					bool createNewAvatar = EditorUtils.AddButton("Create New Avatar","Creates and links a new Scene Object to use as a Cloned Avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoMinWidth" });
					if(createNewAvatar){
						ManagedAvatar newManagedAvatar = ManagedAvatar.CreateNewAvatar(instance);
						Selection.objects = new UnityEngine.Object[]{ newManagedAvatar.avatarCloneGameObject };
						addedAvatar = true;
						// UI will be redrawn with avatar list
					}
				});
			});
			if(addedAvatar) instance.addAvatarObject = null;
			#endif
			return addedAvatar;
		}

		public static bool GUI_ReAddAvatar(AvatarMain instance,ManagedAvatar managedAvatar,GameObject managedAvatarObj=null){
			bool addedAvatar = false;
			#if VRC_SDK_VRCSDK3
			GameObject avatarObject = null;
			int arrPos = instance.avatars.IndexOf(managedAvatarObj);
			if(instance.avatarsReAddSelect.Length!=instance.avatars.Count) Array.Resize(ref instance.avatarsReAddSelect,instance.avatars.Count);
			avatarObject = arrPos>=0 ? instance.avatarsReAddSelect[arrPos] : null;
			if(avatarObject==null && managedAvatarObj) avatarObject = managedAvatarObj;
			// UI
			EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
					EditorUtils.AddLabelField("Select Avatar/Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					avatarObject = (GameObject)EditorGUILayout.ObjectField("",avatarObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
					instance.avatarsReAddSelect[arrPos] = avatarObject;
				});
				bool hasIssues = false;
				// Check for isssues
				if(avatarObject) EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
					// Check For Issue
					if(avatarObject==instance.gameObject){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> Must select a different Avatar/Object, not this one.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					// Check For Issue
					if(avatarObject.GetComponent<AvatarMain>()==instance){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object must be outside of this Avatar Root Object, not a child object.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					// Check For Issue
					Component vrcad1 = null;
					if(avatarObject.transform.parent && avatarObject.transform.parent.gameObject) vrcad1 = avatarObject.transform.parent.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
					Component vrcad2 = avatarObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
					if((vrcad1 && vrcad2 && vrcad1!=vrcad2) || (vrcad1 && !vrcad2)){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object must be the Avatar Root Object itself, not a child object of it.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					// Check For Issue
					if(avatarObject.scene!=instance.gameObject.scene){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object is on a different scene.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					// Check For Issue
					AvatarClone avatarCloneOwn = avatarObject.GetComponent<AvatarClone>();
					AvatarMain avatarMainOwn = avatarObject.GetComponent<AvatarMain>();
					AvatarClone avatarCloneParent = avatarObject.transform.parent ? avatarObject.transform.parent.gameObject.GetComponentInParent<AvatarClone>(true) : null;
					AvatarMain avatarMainParent = avatarObject.transform.parent ? avatarObject.transform.parent.gameObject.GetComponentInParent<AvatarMain>(true) : null;
					if(avatarMainOwn || avatarMainParent || avatarCloneParent){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object is already used in a Multi-Avatar Manager.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					else if(avatarCloneOwn && avatarCloneOwn.managedAvatar!=null && avatarCloneOwn.managedAvatar!=managedAvatar && avatarCloneOwn.managedAvatar.avatarMain && avatarCloneOwn.managedAvatar.avatarMain!=instance){
						hasIssues = true;
						EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issue:</b></color> The Avatar/Object is already a Manager Avatar elsewhere.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
					}
					// Continue
					if(!hasIssues){
						EditorGUILayout.LabelField(new GUIContent("<color='#A0FFA0'><b>Good:</b></color> The Avatar/Object seems good to use.",""),new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,0,0) });
					}
				});
				// Use Avatar Button & Action
				bool addAvatar = false;
				EditorUtils.LayoutDisabled(hasIssues || !avatarObject,()=>{
					addAvatar = EditorUtils.AddButton("Use Selected Avatar","Links a selected Scene Object to use for this Managed Avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,0,5,0) },new object[]{ "AutoMinWidth" });
				});
				if(!hasIssues && addAvatar){
					ManagedAvatar.AddNewAvatar(instance,avatarObject,managedAvatar,addToList:false);
					//Selection.objects = new UnityEngine.Object[]{ managedAvatar.avatarCloneGameObject };
					addedAvatar = true;
					// UI will be redrawn with avatar list
				}
			});
			if(addedAvatar) instance.avatarsReAddSelect[arrPos] = null;
			#endif
			return addedAvatar;
		}

		public static void GUI_AvatarList(AvatarMain instance){
			{
				EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
					float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,6,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+10) },()=>{
						Rect layoutRect = EditorUtils.previousLayoutRect;
						EditorUtils.AddLabelField("<color='#F0F0F0'><b>Main Avatar</b></color>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
						EditorGUILayout.Space(0,true);
					});
					if(true){
						EditorGUILayout.Space(5,false);
						bool isFocused = instance.gameObject.gameObject.activeSelf;
						EditorUtils.LayoutEnabled(!isFocused,()=>{
							bool focusBtn = EditorUtils.AddButton("Focus","View only this main avatar in viewport",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,5,5,5) },new object[]{ "AutoMaxWidth" });
							if(focusBtn){
								foreach(GameObject managedAvatarObj in instance.avatars.ToArray()){
									AvatarClone avatarClone = managedAvatarObj?.GetComponent<AvatarClone>();
									ManagedAvatar managedAvatar = avatarClone?.managedAvatar;
									if(managedAvatar.isAvatarFocused()) managedAvatar.FocusAvatar(false);
								}
							}
						});
					}
				});
			}
			foreach(GameObject managedAvatarObj in instance.avatars.ToArray()){
				if(!managedAvatarObj){
					instance.avatars.Remove(managedAvatarObj);
					continue;
				}
				instance.avatarListCheckIssues = null;
				GUI_AvatarList_Header(instance,managedAvatarObj,showClean:true,showUpdate:true,isToggle:true,reOrder:true);
				GUI_AvatarList_Body(instance,managedAvatarObj,onlyOnUiExpanded:true);
				instance.avatarListCheckIssues = null;
			}
			EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
				if(!instance.displayAddNewAvatar) EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorGUILayout.Space(0,true);
					if(instance.addAvatarObject) instance.addAvatarObject = null;
					bool addAvatarBtn = EditorUtils.AddButton("Add New Avatar","Use/create a new avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(30,30,5,5) },new object[]{ "AutoWidth" });
					if(addAvatarBtn) instance.displayAddNewAvatar = true;
					EditorGUILayout.Space(0,true);
				});
				if(instance.displayAddNewAvatar) EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
						EditorUtils.AddLabelField("<color='#F0F0F0'><b>Add New Avatar:</b></color>",null,null,new object[]{ "AutoMinWidth" });
						bool addAvatarBtn = EditorUtils.AddButton("Close","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoMinWidth" });
						if(addAvatarBtn) instance.displayAddNewAvatar = false;
					});
					bool addedAvatar = GUI_AddAvatar(instance);
					if(addedAvatar) instance.displayAddNewAvatar = false;
				});
			});
		}

		public static void GUI_AvatarList_Header(AvatarMain instance,GameObject managedAvatarObj,bool showClean=true,bool showUpdate=true,bool isToggle=true,bool reOrder=true,bool focusAvatar=false){
			if(!managedAvatarObj) return;
			AvatarClone avatarClone = managedAvatarObj?.GetComponent<AvatarClone>();
			ManagedAvatar managedAvatar = avatarClone?.managedAvatar;
			if(managedAvatar && managedAvatar.avatarCloneGameObject!=managedAvatarObj) managedAvatar.avatarCloneGameObject = managedAvatarObj;
			if(managedAvatar && managedAvatar.avatarMain!=instance) managedAvatar.avatarMain = instance;
			bool enableUpdate = true;
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,6,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+10) },()=>{
					bool toggleShown = managedAvatar ? managedAvatar.uiExpanded : true;
					Rect layoutRect = EditorUtils.previousLayoutRect;
					if(isToggle) EditorGUILayout.Space(4,false);
					if(isToggle) toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					if(isToggle) toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					if(managedAvatar) managedAvatar.uiExpanded = toggleShown;
					//EditorHelpers.LayoutDisabled(()=>EditorGUILayout.ObjectField("",managedAvatar.avatarCloneGameObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) }));
					if(managedAvatar && managedAvatar.avatarCloneGameObject && managedAvatar.avatarCloneGameObject.name!=managedAvatar.avatarName){
						GameObjectUtility.EnsureUniqueNameForSibling(managedAvatar.avatarCloneGameObject);
						managedAvatar.avatarName = managedAvatar.avatarCloneGameObject.name;
					}
					EditorUtils.AddLabelField("<color='#F0F0F0'><b>"+(managedAvatar?.avatarName ?? managedAvatarObj?.name)+"</b></color>",isToggle&&!toggleShown?"Configure this Managed Avatar":"",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(isToggle?0:5,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
					if(instance.avatarListCheckIssues==null && managedAvatar) instance.avatarListCheckIssues = managedAvatar.CheckForIssues();
					if(instance.avatarListCheckIssues!=null){
						var checkIssues = ((bool hasIssue,ManagedAvatar.IssuesEnum issueCode,string issueStr)) instance.avatarListCheckIssues;
						if(checkIssues.hasIssue){
							EditorUtils.AddLabelField("<color='#FFB0B0'>Issue Found</color>","Issue: "+checkIssues.issueStr,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMinWidth" });
							enableUpdate = false;
						}
					}
					else if(!avatarClone || !managedAvatar){
						if(!avatarClone) EditorUtils.AddLabelField("<color='#FFB0B0'>Issue Found</color>","Issue: Missing Managed Avatar Component.",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMinWidth" });
						else if(!managedAvatar) EditorUtils.AddLabelField("<color='#FFB0B0'>Issue Found</color>","Issue: Missing managedAvatar instance.",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMinWidth" });
						enableUpdate = false;
					}
				});
				if(true || focusAvatar){
					EditorGUILayout.Space(5,false);
					bool isBuilt = managedAvatar.isAvatarBuilt();
					bool isFocused = managedAvatar.isAvatarFocused();
					string text = isFocused?"Unfocus":"Focus";
					//if(!focusAvatar) text = isFocused?"UF":"F";
					EditorUtils.LayoutEnabled(isBuilt || isFocused,()=>{
						bool focusBtn = EditorUtils.AddButton(text,"View only this avatar in viewport",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoMaxWidth" });
						if(focusBtn) managedAvatar.FocusAvatar(!isFocused);
					});
				}
				if(reOrder){ //EditorHelpers.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(5,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(false) },()=>{
					EditorGUILayout.Space(5,false);
					EditorUtils.LayoutDisabled(instance.avatars.First()==managedAvatarObj,()=>{
						bool upBtn = EditorUtils.AddButton("↑","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoWidth" });
						if(upBtn) ManagedAvatar.ReOrderAvatarUp(instance,managedAvatarObj);
						if(upBtn) instance.gameObject.MarkDirty();
					});
					EditorUtils.LayoutDisabled(instance.avatars.Last()==managedAvatarObj,()=>{
						bool dwnBtn = EditorUtils.AddButton("↓","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoWidth" });
						if(dwnBtn) ManagedAvatar.ReOrderAvatarDown(instance,managedAvatarObj);
						if(dwnBtn) instance.gameObject.MarkDirty();
					});
				} //});
				if(showClean){
					EditorGUILayout.Space(5,false);
					EditorUtils.LayoutEnabled(enableUpdate && managedAvatar,()=>{
						bool cleanBtn = EditorUtils.AddButton("Clean","Clean Managed Avatar\n(Removes cloned objects)",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoMaxWidth" });
						if(cleanBtn) managedAvatar.CleanAvatar();
					});
				}
				if(showUpdate){
					EditorGUILayout.Space(5,false);
					EditorUtils.LayoutEnabled(enableUpdate && managedAvatar,()=>{
						bool updateBtn = EditorUtils.AddButton("Update","Clean & Update Managed Avatar\n(Re-Adds cloned objects)",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoMaxWidth" });
						if(updateBtn) managedAvatar.UpdateAvatar();
					});
				}
				if(focusAvatar && showUpdate){
					EditorGUILayout.Space(5,false);
					EditorUtils.LayoutEnabled(enableUpdate && managedAvatar,()=>{
						bool playBtn = EditorUtils.AddButton("▶","Enter Play Mode with this avatar\n(Updates if nessecary)",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(6,5,3,3), margin=new RectOffset(0,0,5,5) },new object[]{ "AutoMaxWidth" });
						if(playBtn){ // ▷
							bool isBuilt = managedAvatar.isAvatarBuilt();
							bool isFocused = managedAvatar.isAvatarFocused();
							if(isBuilt && !isFocused) managedAvatar.FocusAvatar(true);
							else if(!isBuilt) managedAvatar.UpdateAvatar();
							if(managedAvatar.isAvatarBuilt()) EditorApplication.delayCall += ()=>EditorApplication.EnterPlaymode();
						}
					});
				}
				if(reOrder || showUpdate) EditorGUILayout.Space(5,false);
			});
		}
		
		public static void GUI_AvatarList_Body(AvatarMain instance,GameObject managedAvatarObj,bool isOnAvatarMain=true,bool onlyOnUiExpanded=false){
			if(!managedAvatarObj) return;
			AvatarClone avatarClone = managedAvatarObj?.GetComponent<AvatarClone>();
			ManagedAvatar managedAvatar = avatarClone?.managedAvatar;
			if(onlyOnUiExpanded && managedAvatar && !managedAvatar.uiExpanded) return;
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					// Display issues
					{
						bool majorIssue = false;
						if(!avatarClone || !managedAvatar){
							majorIssue = true;
							if(!avatarClone) EditorGUILayout.LabelField("<color='#FFA0A0'><b>Issue Found:</b></color> Missing Managed Avatar Component.",new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
							else if(!managedAvatar) EditorGUILayout.LabelField("<color='#FFA0A0'><b>Issue Found:</b></color> Missing managedAvatar instance.",new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
							// Re-Add Avatar
							EditorUtils.AddLabelField("<b>Attempt to Auto-Fix:</b>",null,new GUIStyle(EditorUtils.uiStyleDefault){ richText=true, alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,5,5) },new object[]{ "AutoWidth" });
							GUI_ReAddAvatar(instance,null,managedAvatarObj);
						}
						if(instance.avatarListCheckIssues==null) instance.avatarListCheckIssues = managedAvatar?.CheckForIssues();
						if(instance.avatarListCheckIssues!=null){
							var checkIssues = ((bool hasIssue,ManagedAvatar.IssuesEnum issueCode,string issueStr)) instance.avatarListCheckIssues;
							if(checkIssues.hasIssue){
								EditorGUILayout.LabelField("<color='#FFA0A0'><b>Issue Found:</b></color> "+checkIssues.issueStr,new GUIStyle(EditorUtils.uiStyleDefault){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
							}
							if(checkIssues.issueCode==ManagedAvatar.IssuesEnum.MissingCloneObj || checkIssues.issueCode==ManagedAvatar.IssuesEnum.MissingCloneComp){
								majorIssue = true;
								// Re-Add Avatar
								EditorUtils.AddLabelField("<b>Attempt to Auto-Fix:</b>",null,new GUIStyle(EditorUtils.uiStyleDefault){ richText=true, alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,5,5) },new object[]{ "AutoWidth" });
								GUI_ReAddAvatar(instance,managedAvatar,managedAvatarObj);
								EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
									EditorUtils.AddLabelField("Or: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
									bool removeBtn = EditorUtils.AddButton("Remove Managed Avatar",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoMinWidth" });
									if(removeBtn) ManagedAvatar.RemoveAvatar(instance,managedAvatar);
								});
							}
							if(checkIssues.issueCode==ManagedAvatar.IssuesEnum.NotInPosition){
								majorIssue = true;
								bool reposBtn = GUILayout.Button(new GUIContent("Re-Position Managed Avatar",""),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,0,0,5) });
								if(reposBtn) ManagedAvatar.RePositionManagedAvatar(managedAvatar);
							}
							// TODO: Fix transform values (to original avatar's transform values)
						}
						if(majorIssue) return;
					}
					// Show Clone Object
					if(isOnAvatarMain) EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
						EditorUtils.AddLabelField("Avatar Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
						EditorUtils.LayoutDisabled(()=>{
							EditorGUILayout.ObjectField("",managedAvatar.avatarCloneGameObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
						});
						bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Managed Avatar GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
						if(openObjectInsp){ EditorUtility.OpenPropertyEditor(managedAvatar.avatarCloneGameObject); }
					});
					// VRC Avatar ID
					GUI_ManagedAvatar_VRCAvatarID(instance,managedAvatar);
					// Actions
					ActionsHandler.FindActionTypes();
					GUI_ManagedAvatar_Actions(instance,managedAvatar);
					// Main Avatar Components
					GUI_ManagedAvatar_ComponentMap(instance,managedAvatar);
				});
			});
		}
		
		public static void GUI_ManagedAvatar_VRCAvatarID(AvatarMain instance,ManagedAvatar managedAvatar){
			if(managedAvatar.vrcAvatarID=="") managedAvatar.vrcAvatarID = null;
			string vrcAvatarID = managedAvatar.GetVRCAvatarIDComp();
			bool hasSavedID_emptyCurrentID = managedAvatar.vrcAvatarID!=null && (vrcAvatarID==null || vrcAvatarID=="");
			bool hasSavedID_differentCurrentID = managedAvatar.vrcAvatarID!=null && managedAvatar.vrcAvatarID!=vrcAvatarID;
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("VRC Avatar ID: ",new object[]{ "AutoWidth" });
				if(vrcAvatarID!=null && vrcAvatarID!=""){
					if(managedAvatar.vrcAvatarID==null) managedAvatar.vrcAvatarID = vrcAvatarID;
					EditorUtils.AddLabelField(""+vrcAvatarID+"",isSelectable:true,style:new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,5,4,0), margin=new RectOffset(0,0,0,0) },options:new object[]{ "AutoWidth","AutoHeight" });
					EditorGUILayout.Space(0,true);
				}
				else if(vrcAvatarID==""){
					EditorUtils.AddLabelField("<i>Not yet generated</i>",new object[]{ "AutoWidth" });
					EditorGUILayout.Space(0,true);
					// bool genID = EditorUtils.AddButton("Generate ID",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					// if(genID) managedAvatar.GenerateVRCAvatarIDComp();
				}
				else if(vrcAvatarID==null){
					EditorUtils.AddLabelField("<i>Missing Component</i>",new object[]{ "AutoWidth" });
					EditorGUILayout.Space(0,true);
					bool addComp = EditorUtils.AddButton("Add Component",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					if(addComp) managedAvatar.CreateVRCAvatarIDComp();
				}
				if(hasSavedID_differentCurrentID){
					bool useCurrent = EditorUtils.AddButton("Use Current ID",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					if(useCurrent) managedAvatar.vrcAvatarID = vrcAvatarID;
				}
			});
			if(hasSavedID_emptyCurrentID){
				EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.AddLabelField("Saved Avatar ID: ",new object[]{ "AutoWidth" });
					EditorUtils.AddLabelField(""+managedAvatar.vrcAvatarID+"",isSelectable:true,style:new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,5,4,0), margin=new RectOffset(0,0,0,0) },options:new object[]{ "AutoMaxWidth","AutoHeight" });
					EditorGUILayout.Space(0,true);
					bool useSaved = EditorUtils.AddButton("Use Saved ID",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					if(useSaved) managedAvatar.SetVRCAvatarIDComp(managedAvatar.vrcAvatarID);
				});
			}
			else if(hasSavedID_differentCurrentID){
				EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.AddLabelField("Saved Avatar ID: ",new object[]{ "AutoWidth" });
					EditorUtils.AddLabelField(""+managedAvatar.vrcAvatarID+"",isSelectable:true,style:new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,5,4,0), margin=new RectOffset(0,0,0,0) },options:new object[]{ "AutoMaxWidth","AutoHeight" });
					EditorGUILayout.Space(0,true);
					bool useSaved = EditorUtils.AddButton("Use Saved ID",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					if(useSaved) managedAvatar.SetVRCAvatarIDComp(managedAvatar.vrcAvatarID);
				});
			}
		}
		
		public static void GUI_ManagedAvatar_Actions(AvatarMain instance,ManagedAvatar managedAvatar){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					bool toggleShown = managedAvatar.displayActions;
					EditorGUILayout.Space(4,false);
					toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					managedAvatar.displayActions = toggleShown;
					EditorUtils.AddLabelField("Actions","Configure Actions",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
				});
			});
			if(managedAvatar.displayActions){
				EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.LayoutVertical(new GUIStyle(){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
						EditorUtils.AddLabelField("Configure actions for managed avatar:",null,new object[]{ "AutoWidth" });
						//managedAvatar.actions = managedAvatar.actions.OrderBy(a=>a==mainRemoveAction ? -1000 : 0).ToList(); // a.actionProcessOrder
						foreach(Action action in managedAvatar.actions.ToArray()){
							EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
								float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
								EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,4,6), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
									bool isOnlyAction = managedAvatar.actions.Count==1;
									// Foldout
									if(!isOnlyAction){
										Rect layoutRect = EditorUtils.previousLayoutRect;
										EditorGUILayout.Space(4,false);
										action.uiExpanded = EditorGUILayout.Toggle(action.uiExpanded, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(10.0f) });
										action.uiExpanded = GUI.Toggle(layoutRect, action.uiExpanded, GUIContent.none, GUIStyle.none);
									}
									else action.uiExpanded = true;
									// Label
									int avatarCount = action.managedAvatars.Count;
									EditorUtils.AddLabelField(""+(managedAvatar.actions.IndexOf(action)+1)+": <color='#F0F0F0'><b>"+action.GetActionName()+"</b></color>",action.GetActionDescription(),new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
									EditorGUILayout.Space(0,true);
								});
								EditorGUILayout.Space(5,false);
								EditorUtils.LayoutDisabled(managedAvatar.actions.First()==action,()=>{
									bool upBtn = EditorUtils.AddButton("↑","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,3,3) },new object[]{ "AutoWidth" });
									if(upBtn){
										var pos = managedAvatar.actions.IndexOf(action);
										if(pos>0){
											managedAvatar.actions[pos] = managedAvatar.actions[pos-1];
											managedAvatar.actions[pos-1] = action;
										}
										managedAvatar.avatarCloneGameObject.MarkDirty();
									}
								});
								EditorUtils.LayoutDisabled(managedAvatar.actions.Last()==action,()=>{
									bool dwnBtn = EditorUtils.AddButton("↓","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,3,3) },new object[]{ "AutoWidth" });
									if(dwnBtn){
										var pos = managedAvatar.actions.IndexOf(action);
										if(pos<managedAvatar.actions.Count-1){
											managedAvatar.actions[pos] = managedAvatar.actions[pos+1];
											managedAvatar.actions[pos+1] = action;
										}
										managedAvatar.avatarCloneGameObject.MarkDirty();
									}
								});
								EditorGUILayout.Space(5,false);
								bool removeBtn = EditorUtils.AddButton("Remove Action",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,5,3,3) },new object[]{ "AutoMaxWidth" });
								bool removeConfirm = !removeBtn ? false : EditorUtility.DisplayDialog("MivorTools: Multi-Avatar - Remove Action","Are you sure you want to remove the action \""+action.GetActionName()+"\"?","Yes","No");
								if(removeConfirm){
									MAUndo.Record("Remove Action",managedAvatar.avatarCloneGameObject);
									managedAvatar.actions.Remove(action);
									managedAvatar.avatarCloneGameObject.MarkDirty();
									MAUndo.Flush();
								}
							});
							if(action.uiExpanded){
								EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,10) },()=>{
									EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
										GUI_ManagedAvatar_Action_Configure(managedAvatar,action);
									});
								});
							}
						}
						if(managedAvatar.displayAddAction) GUI_ManagedAvatar_AddAction(managedAvatar);
						else EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,5) },()=>{
							EditorGUILayout.Space(0,true);
							bool addActionBtn = EditorUtils.AddButton("Add New Action","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(30,30,5,5) },new object[]{ "AutoWidth" });
							if(addActionBtn) managedAvatar.displayAddAction = true;
							EditorGUILayout.Space(0,true);
						});
					});
				});
			}
			GUI_ManagedAvatar_Actions_Summary(instance,managedAvatar);
			GUI_ManagedAvatar_Actions_Removed(instance,managedAvatar);
		}
		
		public static void GUI_ManagedAvatar_AddAction(ManagedAvatar managedAvatar){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					EditorUtils.AddLabelField("<color='#F0F0F0'><b>Add New Action</b></color>",null,new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,2,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoWidth" });
					EditorGUILayout.Space(0,true);
					bool closeBtn = EditorUtils.AddButton("Close","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,5,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
					if(closeBtn) managedAvatar.displayAddAction = false;
				});
			});
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,10) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					// EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,5), margin=new RectOffset(0,0,0,0) },()=>{
					// 	EditorUtils.AddLabelField("Select Type: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,5) },new object[]{ "AutoWidth" });
					// 	var selectList = ActionsHandler.actionTypes.Select(t=>ActionsHandler.GetActionName(t)).ToList();
					// 	selectList.Insert(0,"None");
					// 	int selectedIndex = EditorGUILayout.Popup(0,selectList.ToArray(),new GUIStyle(GUI.skin.GetStyle("ObjectField")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,5,0,0) });
					// });
					Type[] types = ActionsHandler.actionTypes.OrderBy(a=>ActionsHandler.GetProcessOrder(a)).ToArray();
					foreach(Type type in types){
						bool forObjectConfig = ActionsHandler.CheckActionAllowedSources(type,Action.ActionSource.ManagedAvatar);
						if(!forObjectConfig) continue;
						bool hasAction = managedAvatar.actions.Any(a=>a.GetType()==type);
						EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
							EditorUtils.LayoutDisabled(hasAction && !ActionsHandler.CanBeMultiple(type),()=>{
								bool addActionBtn = EditorUtils.AddButton("<color='#F0F0F0'><b>"+ActionsHandler.GetActionName(type)+"</b></color>\n<color='#E0E0E0'>"+ActionsHandler.GetActionDescription(type)+"</color>","",new GUIStyle(GUI.skin.GetStyle("Button")){ richText=true, alignment=TextAnchor.MiddleCenter, fontSize=12, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoMinWidth", GUILayout.ExpandWidth(true) });
								if(addActionBtn){
									MAUndo.Record("Add Action",managedAvatar.avatarCloneGameObject);
									managedAvatar.displayAddAction = false;
									Action action = ActionsHandler.CreateAction(type);
									action.gameObject = managedAvatar.avatarCloneGameObject;
									action.uiExpanded = true;
									action.uiAvatarsExpanded = true;
									action.uiConfigExpanded = true;
									managedAvatar.actions.Add(action);
									managedAvatar.avatarCloneGameObject.MarkDirty(); action.MarkDirty();
									MAUndo.Flush();
								}
							});
						});
					}
				});
			});
		}
		
		public static void GUI_ManagedAvatar_Action_Configure(ManagedAvatar managedAvatar,Action action){
			EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(3,3,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				//EditorUtils.AddLabelField("<color='#F0F0F0'>Configure Action:</color>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(2,0,0,5) },new object[]{ "AutoMaxWidth" });
				if(action is ActionList.ReplaceAll @a){
					EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,0) },()=>{
						UnityEngine.Object oldObj = @a.oldObject;
						UnityEngine.Object newObj = @a.newObject;
						EditorUtils.AddLabelField(""+action.GetActionDescription()+":",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(3,0,0,0) },new object[]{ "AutoWidth" });
						EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
							EditorUtils.AddLabelField("Original: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
							oldObj = (UnityEngine.Object)EditorGUILayout.ObjectField("",oldObj,typeof(UnityEngine.Object),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
							if(oldObj){
								bool checkBtn = EditorUtils.AddButton("Check","Count number of references of this on the main avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(7,5,3,3) },new object[]{ "AutoWidth" });
								if(checkBtn){
									int count = 0;
									EditorUtility.DisplayProgressBar("MivorTools: Multi-Avatar - 'Replace All' Action Check","Please wait, finding references for "+oldObj,0.1f);
									// Check on original avatar here
									@a.FindReferences(managedAvatar,managedAvatar.avatarMain.gameObject,
										createSetCB: false,
										onReference: (ActionList.ReplaceAll.ReflectHelpers.Result result,ActionList.ReplaceAll.Callback setNew)=>{
											count++;
											if(count==1) EditorUtility.DisplayProgressBar("MivorTools: Multi-Avatar - 'Replace All' Action Check","Please wait, finding references for "+oldObj,0.5f);
										}
									);
									EditorUtility.ClearProgressBar();
									EditorUtility.DisplayDialog("MivorTools: Multi-Avatar - 'Replace All' Action Check","Found "+count+" references on main avatar for "+oldObj,"Ok");
								}
							}
						});
						EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },()=>{
							EditorUtils.AddLabelField("New Asset: ","If empty, it will result in the original material",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
							newObj = (UnityEngine.Object)EditorGUILayout.ObjectField("",newObj,typeof(UnityEngine.Object),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
						});
						if(oldObj!=@a.oldObject || newObj!=@a.newObject){
							@a.oldObject = oldObj;
							@a.newObject = newObj;
							action.MarkDirty();
							managedAvatar.avatarCloneGameObject.MarkDirty();
						}
					});
				}
				else {
					EditorUtils.AddLabelField("Not yet implemented",null,new object[]{ "AutoWidth" });
				}
			});
		}
		
		public static void GUI_ManagedAvatar_Actions_Summary(AvatarMain instance,ManagedAvatar managedAvatar){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					bool toggleShown = managedAvatar.displayActionsSummary;
					EditorGUILayout.Space(4,false);
					toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					managedAvatar.displayActionsSummary = toggleShown;
					EditorUtils.AddLabelField("Actions Summary of Object Configs","View Summary of Object Config Actions",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
				});
			});
			if(!managedAvatar.displayActionsSummary) return;
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					EditorUtils.AddLabelField("The following objects have actions for this avatar:",null,new object[]{ "AutoWidth" });
					List<ObjectConfig> comps = managedAvatar.avatarMain.gameObject.GetComponentsInChildren<ObjectConfig>(true)
						.Where(c=>c.actions.Where(a=>a.managedAvatars.Contains(managedAvatar.avatarCloneGameObject)).Any())
						.ToList();
					if(comps.Count==0){
						EditorUtils.AddLabelField("No objects configured.","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(3,0,0,0) });
					}
					foreach(ObjectConfig compObjectConfig in comps){
						//bool actionThisAvatar = compObjectConfig.actions.Where(a=>a.managedAvatars.Contains(managedAvatar.avatarCloneGameObject)).Any();
						//if(!actionThisAvatar) continue;
						EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,5) },()=>{
							EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
								//EditorUtils.AddLabelField("Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
								EditorUtils.LayoutDisabled(()=>{
									EditorGUILayout.ObjectField("",compObjectConfig.gameObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
								});
							});
							//EditorUtils.AddLabelField("Actions: ",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(4,0,0,0) },new object[]{ "AutoWidth" });
							foreach(Action action in compObjectConfig.actions){
								if(!action.managedAvatars.Contains(managedAvatar.avatarCloneGameObject)) continue;
								EditorUtils.AddLabelField(""+(compObjectConfig.actions.IndexOf(action)+1)+": "+action.GetActionName()+" ( "+action.managedAvatars.Count+" / "+managedAvatar.avatarMain.avatars.Count+" )",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(4,0,0,0) },new object[]{ "AutoWidth" });
							}
						});
					}
				});
			});
		}
		
		public static void GUI_ManagedAvatar_Actions_Removed(AvatarMain instance,ManagedAvatar managedAvatar){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					bool toggleShown = managedAvatar.displayObjectsRemoved;
					EditorGUILayout.Space(4,false);
					toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					managedAvatar.displayObjectsRemoved = toggleShown;
					EditorUtils.AddLabelField("Objects not included on this Avatar","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
				});
			});
			if(!managedAvatar.displayObjectsRemoved) return;
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					EditorUtils.AddLabelField("The following objects will not be included on this avatar:","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(0,0,0,5) },new object[]{ "AutoWidth" });
					List<ObjectConfig> comps = managedAvatar.avatarMain.gameObject.GetComponentsInChildren<ObjectConfig>(true)
						.Where(oc=>oc.actions.Any(a=>a is RemoveObject && a.managedAvatars.Contains(managedAvatar.avatarCloneGameObject)))
						.ToList();
					if(comps.Count==0){
						EditorUtils.AddLabelField("No objects configured.","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(3,0,0,0) });
					}
					foreach(ObjectConfig objectConfig in comps){
						GameObject obj = objectConfig.gameObject;
						RemoveObject ra = objectConfig.actions.Where(a=>a is RemoveObject).First() as RemoveObject;
						EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,2) },()=>{
							EditorUtils.LayoutDisabled(()=>{
								EditorGUILayout.ObjectField("",obj,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
							});
							bool keepBtn = EditorUtils.AddButton("Keep",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,2,2), margin=new RectOffset(5,5,0,0) },new object[]{ "AutoMaxWidth" });
							bool keepConfirm = !keepBtn ? false : EditorUtility.DisplayDialog("MivorTools: Multi-Avatar","Are you sure you want to keep the object \n'"+obj.name+"' instead?","Yes","No");
							if(keepConfirm){
								ra.managedAvatars.Remove(managedAvatar.avatarCloneGameObject);
								if(objectConfig.actions.Count==1 && ra.managedAvatars.Count==0){
									UnityEngine.Object.DestroyImmediate(objectConfig);
								}
							}
						});
					}
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,5) },()=>{
						EditorUtils.AddLabelField("Add: ",null,new object[]{ "AutoWidth" });
						GameObject toAdd = EditorGUILayout.ObjectField("",null,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) }) as GameObject;
						GameObject[] addObjects = new GameObject[]{ toAdd };
						if(toAdd && Selection.objects.Contains(toAdd)){
							addObjects = Selection.objects.Cast<GameObject>().ToArray();
						}
						foreach(GameObject obj in addObjects){
							if(obj && (obj==managedAvatar.avatarMain.gameObject || !MiscUtils.doesGameObjectHaveHierarchyParent(obj,managedAvatar.avatarMain.gameObject))) continue;
							if(obj){
								ObjectConfig objectConfig = obj.GetComponent<ObjectConfig>();
								if(!objectConfig) objectConfig = obj.AddComponent<ObjectConfig>();
								RemoveObject action = null;
								if(objectConfig.actions.Any(a=>a is RemoveObject)) action = objectConfig.actions.Where(a=>a is RemoveObject).First() as RemoveObject;
								if(!action){
									action = ActionsHandler.CreateAction<RemoveObject>();
									action.gameObject = objectConfig.gameObject;
									action.uiExpanded = true;
									action.managedAvatarsLock = true;
									objectConfig.actions.Add(action);
								}
								if(!action.managedAvatars.Contains(managedAvatar.avatarCloneGameObject)){
									action.managedAvatars.Add(managedAvatar.avatarCloneGameObject);
									objectConfig.MarkDirty(); action.MarkDirty();
								}
							}
						}
					});
					EditorUtils.AddLabelField("This is the exact same functionality as Object Config 'Selected Avatars' feature (Remove Object action), but simply as another way to configure it, on the avatar as a whole.","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(0,0,5,0) });
				});
			});
		}
		
		public static void GUI_ManagedAvatar_ComponentMap(AvatarMain instance,ManagedAvatar managedAvatar){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					bool toggleShown = managedAvatar.displayComponentMap;
					EditorGUILayout.Space(4,false);
					toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					managedAvatar.displayComponentMap = toggleShown;
					EditorUtils.AddLabelField("Components","Configure Component Cloning",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
				});
			});
			if(!managedAvatar.displayComponentMap) return;
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					List<Type> ignoreTypes = MappedComponents.IgnoreTypes;
					EditorUtils.AddLabelField("Configure how components are cloned:",null,new object[]{ "AutoWidth" });
					List<Component> components = MappedComponents.ReMapComponentMapping(managedAvatar);
					uint sameCompIndex = 0; string prevCompTitle = null; GameObject prevCompObj = null;
					foreach(Component comp in components){
						Type type = comp.GetType();
						if(ignoreTypes.Contains(type)) continue;
						MappedComponents componentMap = null;
						//Rect compRect = EditorUtils.previousLayoutRect;
						EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ padding=new RectOffset(5,5,2,7), margin=new RectOffset(0,0,5,0) },new GUILayoutOption[]{ },()=>{
							foreach(MappedComponents mappedComp in managedAvatar.componentMap){
								if(mappedComp.isSameMain(comp) || mappedComp.isSameClone(comp)){ componentMap=mappedComp; break; }
							}
							if(!componentMap){
								EditorUtils.AddLabelField("Unable to find: "+comp,null,new object[]{ "AutoWidth" });
								return;
							}
							bool isOnMain = componentMap.hasMain();
							bool isOnClone = componentMap.hasClone();
							bool isOnMainOnly = isOnMain && !isOnClone;
							bool isOnCloneOnly = !isOnMain && isOnClone;
							bool copyToAvatar = componentMap ? componentMap.copyToAvatar : isOnMainOnly;
							bool keepExisting = componentMap ? componentMap.keepExisting : isOnCloneOnly;
							if(isOnMainOnly) keepExisting = false;
							if(isOnCloneOnly) copyToAvatar = false;
							EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
								string compTitle = MiscUtils.GetComponentTitle(comp);
								if(prevCompTitle!=compTitle || prevCompObj!=comp.gameObject) sameCompIndex = 1;
								else sameCompIndex++;
								prevCompTitle = compTitle;
								if(sameCompIndex>1) compTitle += " ("+sameCompIndex+")";
								EditorUtils.AddLabelField(compTitle,null,new object[]{ "AutoWidth" });
								EditorGUILayout.Space(0,true);
								string msg = isOnMain ? (isOnClone ? "<b>Both</b>" : "Main Only") : (isOnClone ? "Clone Only" : "None");
								EditorUtils.AddLabelField("["+msg+"]",EditorUtils.uiStyleDefault,new object[]{ "AutoWidth" });
							});
							EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(10,0,1,0) },()=>{
								bool copyToAvatarToggle = isOnMain;
								bool keepExistingToggle = isOnClone || (isOnMain && isOnClone && !copyToAvatar);
								EditorUtils.LayoutDisabled(!copyToAvatarToggle,()=>{
									copyToAvatar = EditorUtils.AddCheckboxSmall(copyToAvatar,"Copy to avatar",null);
								});
								EditorGUILayout.Space(10,false);
								EditorUtils.LayoutDisabled(!keepExistingToggle,()=>{
									keepExisting = EditorUtils.AddCheckboxSmall(keepExisting,"Keep existing",null);
								});
								EditorGUILayout.Space(0,true);
								if(isOnMainOnly && componentMap.copyToAvatar){
									bool copyBtn = EditorUtils.AddButton("Clone","Clone Component",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
									if(copyBtn){
										componentMap.CreateClone(managedAvatar);
										MappedComponents.ReOrderClonedComps(managedAvatar);
									}
								}
								if(isOnClone && !componentMap.copyToAvatar && !componentMap.keepExisting){
									bool removeBtn = EditorUtils.AddButton("Remove","Remove Component",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
									if(removeBtn){
										componentMap.RemoveClone();
										//managedAvatar.componentMap.Remove(componentMap);
										// TODO: issue when removing vrcfury comp, then re-adding it.
									}
								}
								if(isOnClone && componentMap.copyToAvatar && !componentMap.keepExisting){
									bool reCloneBtn = EditorUtils.AddButton("Update","Re-Clone Component",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,3,3), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
									if(reCloneBtn){
										componentMap.UpdateClone(managedAvatar);
									}
								}
							});
							if(componentMap){
								if(isOnMain && isOnClone && copyToAvatar && keepExisting && !componentMap.keepExisting) copyToAvatar = false;
								if(isOnMain && isOnClone && copyToAvatar && keepExisting && !componentMap.copyToAvatar) keepExisting = false;
								if(copyToAvatar!=componentMap.copyToAvatar){ componentMap.copyToAvatar = copyToAvatar; managedAvatar.MarkDirty(); }
								if(keepExisting!=componentMap.keepExisting){ componentMap.keepExisting = keepExisting; managedAvatar.MarkDirty(); }
							}
						});
						prevCompObj = comp.gameObject;
						// if(componentMap.showGUI){
						// 	if(!componentMap.editor) UnityEditor.Editor.CreateCachedEditor(comp,null,ref componentMap.editor);
						// 	Type editorType = componentMap.editor.GetType();
						// 	if(componentMap.editor) EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ padding=new RectOffset(5,5,0,0), margin=new RectOffset(0,0,0,0), wordWrap=true },new GUILayoutOption[]{ GUILayout.MaxWidth(compRect.width), GUILayout.ExpandWidth(false) },()=>{
						// 		if(editorType.ToString()=="VF.Inspector.VRCFuryEditor") //EditorUtils.AddLabelField("Component cannot be displayed here.",null,new object[]{ "AutoWidth" }); // componentMap.editor.CreateInspectorGUI();
						// 		else componentMap.editor.OnInspectorGUI();
						// 	});
						// }
					}
					EditorUtils.AddLabelField("<color='#FFB0B0'>Warning:</color> There's a small chance that configuration may be lost when components have been re-ordered, added or removed.","Due to the way Unity handles components. Multi-Avatar uses it's own methods to try to keep track of components.",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, margin=new RectOffset(0,0,5,0) },new object[]{});
				});
			});
			//managedAvatar.displayComponentMap = false;
		}
		
	}

	public class AvatarMainWindow : UnityEditor.EditorWindow {

		// Only one window open at a time
		public static AvatarMainWindow window = null;
		public static AvatarMainEditor component = null;
		public static AvatarMain instance = null;

		public static void OpenAsWindow(AvatarMain modelInstance,bool collapseAvatars=true,ManagedAvatar expandAvatar=null){
			instance = modelInstance;
			if(!window) window = (AvatarMainWindow) EditorWindow.GetWindow(typeof(AvatarMainWindow),false,"MivorTools Multi-Avatar");
			window.minSize = new Vector2(400,100);
			window.wantsMouseMove = false;
			window.wantsMouseEnterLeaveWindow = false;
			if(component) component.window = null;
			component = (AvatarMainEditor) AvatarMainEditor.CreateEditor(instance);
			component.window = window;
			if(collapseAvatars) foreach(GameObject managedAvatarObj in instance.avatars.ToArray()){
				AvatarClone comp = managedAvatarObj.GetComponent<AvatarClone>();
				if(!comp) continue;
				ManagedAvatar managedAvatar = comp.managedAvatar;
				if(managedAvatar==null) continue;
				managedAvatar.uiExpanded = false;
			}
			if(expandAvatar) expandAvatar.uiExpanded = true;
		}
		
		public static Vector2 scrollPos = Vector2.zero;
		
		public void OnGUI(){
			if(!window || !component || !instance){
				// TODO: List avatars with Multi-Avatar Manager Components
				EditorGUILayout.LabelField("Not Initialised.\nSelect an Object Config to open a Multi-Avatar Manager in this window.",new GUIStyle(GUI.skin.GetStyle("Label")) { wordWrap=true });
				return;
			}
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Avatar: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",instance.gameObject,typeof(GameObject),true);
				});
			});
			// Draw Component UI
			scrollPos = EditorUtils.LayoutScroll(scrollPos,()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ padding=new RectOffset(5,5,0,0) },new GUILayoutOption[]{ },()=>{
					component.OnInspectorGUI();
				});
			},alwaysHorizontal:false,alwaysVertical:true,hideHorizontal:true,hideVertical:false,new GUILayoutOption[]{ GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) });
		}
		
		void OnDestroy(){
			if(component) component.window = null;
		}
	}
	#endif

}
