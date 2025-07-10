
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if VRC_SDK_VRCSDK3
using VRC;
using System.Linq;

#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar.Components {

	#if UNITY_EDITOR
	[ExecuteInEditMode]
	[AddComponentMenu("MivorTools/Multi-Avatar/Multi-Avatar: Object Config",11)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	#endif
	[Serializable]
	public class ObjectConfig : SerializableComponent {
		[SerializeField] public int modelVersion = 1;
		[SerializeReference] public List<Action> actions = new List<Action>();
		
		[SerializeReference] public List<GameObject> displayActions = new List<GameObject>();
		[SerializeField] public bool displaySimple = true;
		[NonSerialized] public bool displayAddAction = false;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(ObjectConfig))]
	public class ObjectConfigEditor : UnityEditor.Editor {

		// Only set if opened within window
		public ObjectConfigWindow window = null;
		
		public override void OnInspectorGUI(){
			if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused){ EditorGUILayout.LabelField("Component not editable during play mode.",new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) }); return; }
			
			ObjectConfig objectConfig = (ObjectConfig)this.target;
			//this.serializedObject.Update();
			if(MetaPackageInfo.DrawGUICriticalIssues()) return;
			
			#if VRC_SDK_VRCSDK3
			bool guiDrawn = false;
			if(!guiDrawn){
				Component vrcad = objectConfig.gameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if(vrcad){
					Component multiAvatarMainOwn = objectConfig.gameObject.GetComponent<AvatarMain>();
					if(!multiAvatarMainOwn){
						guiDrawn=true; GUI_Error_Simple("- This component must not be on an Avatar Root Object.\n- Did you create this instead of a Multi-Avatar Manager?");
						// Auto-Fix Button
						bool autoFix = GUILayout.Button(new GUIContent("Auto-Fix: Create Multi-Avatar Manager","Creates Multi-Avatar Manager on Avatar Root Object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
						if(autoFix){
							MAUndo.Record("Auto-Fix ObjectConfig #1",objectConfig,objectConfig.gameObject);
							Component newComp = objectConfig.gameObject.AddComponent<AvatarMain>();
							objectConfig.gameObject.MarkDirty();
							if(objectConfig.actions.Count==0) EditorApplication.delayCall += ()=>{ DestroyImmediate(objectConfig); };
							MAUndo.Flush();
						}
					}
				}
				if(vrcad && !guiDrawn && objectConfig.actions.Count==0){
					guiDrawn=true; GUI_Error_Simple("- This component must not be on an Avatar Root Object.");
					bool removeCompBtn = GUILayout.Button(new GUIContent("Remove This Component"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
					if(removeCompBtn) EditorApplication.delayCall += ()=>{ DestroyImmediate(objectConfig); };
				}
				if(vrcad && !guiDrawn && objectConfig.actions.Count>0){ guiDrawn=true; GUI_Error_Simple("- This component must not be on an Avatar Root Object.\n- Please move it to a child object."); }
			}
			if(!guiDrawn){
				Component vrcad = objectConfig.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
				if(!vrcad){ guiDrawn=true; GUI_Error_Simple("- This component must be on a child object of an Avatar.\n- There's no VRC Avatar Descriptor found on any parent objects."); }
				if(!guiDrawn){
					AvatarMain compAvatarMain = vrcad.gameObject.GetComponent<AvatarMain>();
					AvatarClone compAvatarClone = vrcad.gameObject.GetComponent<AvatarClone>();
					ObjectClone compObjectClone = objectConfig.gameObject.GetComponent<ObjectClone>();
					if(!compAvatarMain && !(compAvatarClone && compObjectClone)){
						guiDrawn=true; GUI_Error_Simple("- There's no Multi-Avatar Manager found on the Avatar Root Object.");
						// Auto-Fix Button
						bool autoFix = GUILayout.Button(new GUIContent("Auto-Fix","Creates Multi-Avatar Manager on Avatar Root Object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
						if(autoFix){
							Component newComp = vrcad.gameObject.AddComponent<AvatarMain>();
							Component multiAvatarMainOwn = objectConfig.gameObject.GetComponent<AvatarMain>();
							if(multiAvatarMainOwn){
								EditorUtility.CopySerialized(multiAvatarMainOwn,newComp);
								EditorApplication.delayCall += ()=>{ DestroyImmediate(multiAvatarMainOwn); };
							}
						}
					}
				}
			}
			if(!guiDrawn){
				guiDrawn=true; GUI_ObjectConfig(objectConfig);
			}
			#endif
			
			//this.serializedObject.ApplyModifiedProperties();
		}

		private void GUI_Error_Simple(string message){
			ObjectConfig instance = (ObjectConfig)this.target;
			EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issues Found:</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
			EditorGUILayout.LabelField(message,new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) });
			if(instance.actions.Count>0){
				EditorGUILayout.LabelField(new GUIContent("Note: <b>This component has data</b> attached to it."),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
				// TODO: list simple summary of data
			}
		}

		private static void GUI_ObjectConfig(ObjectConfig objectConfig){
			Component vrcad = objectConfig.gameObject.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
			AvatarMain compAvatarMain = vrcad.gameObject.GetComponent<AvatarMain>();
			AvatarClone compAvatarClone = vrcad.gameObject.GetComponent<AvatarClone>();
			ObjectClone compObjectClone = objectConfig.gameObject.GetComponent<ObjectClone>();
			
			if(compAvatarClone && compObjectClone){
				EditorUtils.AddLabelField("<color='#FFA0A0'><b>Error:</b></color> This component has not been processed.");
				return;
			}
			
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Avatar: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",compAvatarMain.gameObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
				});
				bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Main Avatar GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
				if(openObjectInsp){ EditorUtility.OpenPropertyEditor(compAvatarMain.gameObject); }
				bool openMultiAvatarMain = EditorUtils.AddButton("Manager","Opens Multi-Avatar Manager in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
				if(openMultiAvatarMain){ AvatarMainWindow.OpenAsWindow(compAvatarMain,true); }
			});
			
			GUI_ObjectConfig_Body(objectConfig);

		}

		private static void GUI_ObjectConfig_Body(ObjectConfig objectConfig){
			AvatarMain avatarMain = objectConfig.gameObject.GetComponentInParent<AvatarMain>(true);
			
			// EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,5) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
			// 	EditorUtils.LayoutDisabled(objectConfig.displaySimple,()=>{
			// 		bool modeSimple = EditorUtils.AddButton(objectConfig.displaySimple?"Using Simple Mode":"Switch to Simple Mode","Easy-to-use Mode",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(10,10,5,5) },new object[]{ "AutoMinWidth" });
			// 		if(modeSimple){ objectConfig.displaySimple = true; }
			// 	});
			// 	EditorGUILayout.Space(5,false);
			// 	EditorUtils.LayoutDisabled(!objectConfig.displaySimple,()=>{
			// 		bool modeAdvanced = EditorUtils.AddButton(objectConfig.displaySimple?"Switch to Advanced Mode":"Using Advanced Mode","Complex Mode",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(10,10,5,5) },new object[]{ "AutoMinWidth" });
			// 		if(modeAdvanced){ objectConfig.displaySimple = false; }
			// 	});
			// });
			if(!objectConfig.displaySimple) objectConfig.displaySimple = true;
			
			ActionsHandler.FindActionTypes();
			if(objectConfig.displaySimple) GUI_Actions_Simple(objectConfig,avatarMain);
			else GUI_Actions_Advanced(objectConfig,avatarMain);
		}
		
		public static void GUI_Actions_Simple(ObjectConfig objectConfig,AvatarMain avatarMain){
			// Find Remove Action
			ActionList.RemoveObject mainRemoveAction  = null;
			foreach(Action action in objectConfig.actions.ToArray()){
				try{ if(action.gameObject!=objectConfig.gameObject) continue; }
				catch(NullReferenceException){ objectConfig.actions.Remove(action); continue; }
				if(mainRemoveAction==null && action is ActionList.RemoveObject @a){
					mainRemoveAction = @a;
				}
			}
			// Add Remove Action
			if(!mainRemoveAction){
				mainRemoveAction = ActionsHandler.CreateAction<ActionList.RemoveObject>();
				mainRemoveAction.gameObject = objectConfig.gameObject;
				mainRemoveAction.uiExpanded = true;
				objectConfig.actions.Add(mainRemoveAction);
				objectConfig.MarkDirty(); mainRemoveAction.MarkDirty();
			}
			objectConfig.actions = objectConfig.actions.OrderBy(a=>a==mainRemoveAction ? -1000 : 0).ToList(); // a.actionProcessOrder
			foreach(Action action in objectConfig.actions.ToArray()){
				bool isMainRemoveAction = action==mainRemoveAction;
				if(!isMainRemoveAction && action is ActionList.RemoveObject){
					objectConfig.actions.Remove(action);
					objectConfig.MarkDirty();
					continue;
				}
				EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
					float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,4,6), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
						bool isOnlyAction = objectConfig.actions.Count==1;
						// Foldout
						if(!isOnlyAction){
							Rect layoutRect = EditorUtils.previousLayoutRect;
							EditorGUILayout.Space(4,false);
							action.uiExpanded = EditorGUILayout.Toggle(action.uiExpanded, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(10.0f) });
							action.uiExpanded = GUI.Toggle(layoutRect, action.uiExpanded, GUIContent.none, GUIStyle.none);
						}
						else action.uiExpanded = true;
						// Label
						if(isMainRemoveAction){
							int avatarCount = avatarMain.avatars.Count-action.managedAvatars.Count;
							EditorUtils.AddLabelField((isOnlyAction?"":objectConfig.actions.IndexOf(action)+1+": ")+"<color='#F0F0F0'><b>Selected Avatars</b></color>  ( "+avatarCount+" / "+avatarMain.avatars.Count+" )","Choose which avatars this object will be included on",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
							EditorGUILayout.Space(0,true);
						} else {
							int avatarCount = action.managedAvatars.Count;
							EditorUtils.AddLabelField(""+(objectConfig.actions.IndexOf(action)+1)+": <color='#F0F0F0'><b>"+action.GetActionName()+"</b></color>  ( "+avatarCount+" / "+avatarMain.avatars.Count+" )",action.GetActionDescription(),new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoMaxWidth" });
							EditorGUILayout.Space(0,true);
						}
					});
					if(isMainRemoveAction){
						EditorGUILayout.Space(5,false);
						bool lockBtn = EditorUtils.AddButton(mainRemoveAction.managedAvatarsLock?"Unlock":"Lock","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,5,4,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
						if(lockBtn) mainRemoveAction.managedAvatarsLock = !mainRemoveAction.managedAvatarsLock;
					}
					if(!isMainRemoveAction){
						EditorGUILayout.Space(5,false);
						EditorUtils.LayoutDisabled(objectConfig.actions.IndexOf(action)<=1,()=>{
							bool upBtn = EditorUtils.AddButton("↑","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,3,3) },new object[]{ "AutoWidth" });
							if(upBtn){
								var pos = objectConfig.actions.IndexOf(action);
								if(pos>0){
									objectConfig.actions[pos] = objectConfig.actions[pos-1];
									objectConfig.actions[pos-1] = action;
								}
								objectConfig.MarkDirty();
							}
						});
						EditorUtils.LayoutDisabled(objectConfig.actions.Last()==action,()=>{
							bool dwnBtn = EditorUtils.AddButton("↓","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(4,3,3,3), margin=new RectOffset(0,0,3,3) },new object[]{ "AutoWidth" });
							if(dwnBtn){
								var pos = objectConfig.actions.IndexOf(action);
								if(pos<objectConfig.actions.Count-1){
									objectConfig.actions[pos] = objectConfig.actions[pos+1];
									objectConfig.actions[pos+1] = action;
								}
								objectConfig.MarkDirty();
							}
						});
						EditorGUILayout.Space(5,false);
						bool removeBtn = EditorUtils.AddButton("Remove Action",null,new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(7,6,3,3), margin=new RectOffset(0,5,3,3) },new object[]{ "AutoMaxWidth" });
						bool removeConfirm = !removeBtn ? false : EditorUtility.DisplayDialog("MivorTools: Multi-Avatar - Remove Action","Are you sure you want to remove the action \""+action.GetActionName()+"\"?","Yes","No");
						if(removeConfirm){
							MAUndo.Record("Remove Action",objectConfig,objectConfig.gameObject);
							objectConfig.actions.Remove(action);
							objectConfig.MarkDirty();
							MAUndo.Flush();
						}
					}
				});
				if(action.uiExpanded){
					EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,10) },()=>{
						EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
							if(isMainRemoveAction){
								if(!action.managedAvatarsLock) EditorUtils.AddLabelField("Avatars this object is used on:","Ticked = Include on Avatar\nUnticked = Remove from Avatar",null,new object[]{ "AutoWidth" });
								GUI_Actions_Simple_ChooseAvatar(objectConfig,avatarMain,action,compact:false,invertToggle:true);
							} else {
								GUI_Actions_Simple_Body(objectConfig,avatarMain,action);
							}
						});
					});
				}
			};
			if(objectConfig.displayAddAction) GUI_Actions_Simple_AddAction(objectConfig,avatarMain);
			else EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,5) },()=>{
				EditorGUILayout.Space(0,true);
				bool addActionBtn = EditorUtils.AddButton("Add New Action","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,0,0,0), padding=new RectOffset(30,30,5,5) },new object[]{ "AutoWidth" });
				if(addActionBtn) objectConfig.displayAddAction = true;
				EditorGUILayout.Space(0,true);
			});
		}
		
		public static void GUI_Actions_Simple_Body(ObjectConfig objectConfig,AvatarMain avatarMain,Action action){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,7,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					bool toggleShown = action.uiAvatarsExpanded;
					EditorGUILayout.Space(4,false);
					toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
					toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
					action.uiAvatarsExpanded = toggleShown;
					EditorUtils.AddLabelField("<color='#F0F0F0'>Run on Avatars</color>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(0,true);
				});
				EditorGUILayout.Space(5,false);
				bool lockBtn = EditorUtils.AddButton(action.managedAvatarsLock?"Unlock":"Lock","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,2,3,0), padding=new RectOffset(10,10,2,2) },new object[]{ "AutoWidth" });
				if(lockBtn) action.managedAvatarsLock = !action.managedAvatarsLock;
			});
			if(action.uiAvatarsExpanded){
				if(!action.managedAvatarsLock) EditorUtils.AddLabelField("Avatars this action runs on:","Ticked = Run action on Avatar\nUnticked = Ignore Avatar",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(7,0,0,0) },new object[]{ "AutoWidth" });
				GUI_Actions_Simple_ChooseAvatar(objectConfig,avatarMain,action,compact:true,invertToggle:false);
			}
			// EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,7,0) },()=>{
			// 	float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
			// 	EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
			// 		Rect layoutRect = EditorUtils.previousLayoutRect;
			// 		bool toggleShown = action.uiConfigExpanded;
			// 		EditorGUILayout.Space(4,false);
			// 		toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
			// 		toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
			// 		action.uiConfigExpanded = toggleShown;
			// 		EditorUtils.AddLabelField("<color='#F0F0F0'>Configure Action</color>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
			// 		EditorGUILayout.Space(0,true);
			// 	});
			// });
			// if(action.uiConfigExpanded) GUI_Actions_Simple_Configure(objectConfig,avatarMain,action);
			EditorGUILayout.Space(5,false);
			GUI_Actions_Simple_Configure(objectConfig,avatarMain,action);
		}
		
		public static void GUI_Actions_Simple_AddAction(ObjectConfig objectConfig,AvatarMain avatarMain){
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(0,0,3,3) }.CalcSize(new GUIContent("B")).y;
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
					EditorUtils.AddLabelField("<color='#F0F0F0'><b>Add New Action</b></color>",null,new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,2,0), margin=new RectOffset(0,0,2,0) },new object[]{ "AutoWidth" });
					EditorGUILayout.Space(0,true);
					bool closeBtn = EditorUtils.AddButton("Close","",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(0,5,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
					if(closeBtn) objectConfig.displayAddAction = false;
				});
			});
			EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,10) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					Type[] types = ActionsHandler.actionTypes.OrderBy(a=>ActionsHandler.GetProcessOrder(a)).ToArray();
					foreach(Type type in types){
						if(type==typeof(ActionList.RemoveObject)) continue;
						bool forObjectConfig = ActionsHandler.CheckActionAllowedSources(type,Action.ActionSource.ObjectConfig);
						if(!forObjectConfig) continue;
						bool hasAction = objectConfig.actions.Any(a=>a.GetType()==type);
						EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,5) },()=>{
							EditorUtils.LayoutDisabled(hasAction && !ActionsHandler.CanBeMultiple(type),()=>{
								bool addActionBtn = EditorUtils.AddButton("<color='#F0F0F0'><b>"+ActionsHandler.GetActionName(type)+"</b></color>\n<color='#E0E0E0'>"+ActionsHandler.GetActionDescription(type)+"</color>","",new GUIStyle(GUI.skin.GetStyle("Button")){ richText=true, alignment=TextAnchor.MiddleCenter, fontSize=12, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoMinWidth", GUILayout.ExpandWidth(true) });
								if(addActionBtn){
									MAUndo.Record("Add Action",objectConfig,objectConfig.gameObject);
									objectConfig.displayAddAction = false;
									Action action = ActionsHandler.CreateAction(type);
									action.gameObject = objectConfig.gameObject;
									action.uiExpanded = true;
									action.uiAvatarsExpanded = true;
									action.uiConfigExpanded = true;
									objectConfig.actions.Add(action);
									objectConfig.MarkDirty(); action.MarkDirty();
									MAUndo.Flush();
								}
							});
						});
					}
				});
			});
		}
		
		public static void GUI_Actions_Simple_ChooseAvatar(ObjectConfig objectConfig,AvatarMain avatarMain,Action action,bool compact=true,bool invertToggle=false){
			if(action.managedAvatarsLock) EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(compact?7:0,compact?7:0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					foreach(GameObject managedAvatarObj in avatarMain.avatars){
						if(!managedAvatarObj) continue;
						AvatarClone avatarClone = managedAvatarObj?.GetComponent<AvatarClone>();
						ManagedAvatar managedAvatar = avatarClone?.managedAvatar;
						if(managedAvatar && managedAvatar.avatarCloneGameObject!=managedAvatarObj) continue;
						if(managedAvatar && managedAvatar.avatarMain!=avatarMain) continue;
						bool isToggled = action.managedAvatars.Contains(managedAvatarObj);
						if(invertToggle) isToggled = !isToggled;
						if(isToggled){
							EditorUtils.AddLabelField(""+(managedAvatar?.avatarName ?? managedAvatarObj?.name)+"","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,2) },new object[]{ "AutoMaxWidth" });
						}
					}
				});
			});
			else EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(compact?7:0,compact?7:0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
					foreach(GameObject managedAvatarObj in avatarMain.avatars){
						if(!managedAvatarObj) continue;
						AvatarClone avatarClone = managedAvatarObj?.GetComponent<AvatarClone>();
						ManagedAvatar managedAvatar = avatarClone?.managedAvatar;
						if(managedAvatar && managedAvatar.avatarCloneGameObject!=managedAvatarObj) continue;
						if(managedAvatar && managedAvatar.avatarMain!=avatarMain) continue;
						bool isToggled = action.managedAvatars.Contains(managedAvatarObj);
						if(invertToggle) isToggled = !isToggled;
						EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,compact?3:5,compact?3:5) },()=>{
							EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,compact?3:5,compact?3:5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
								// Checkbox
								Rect layoutRect = EditorUtils.previousLayoutRect;
								EditorGUILayout.Space(4,false);
								isToggled = EditorGUILayout.Toggle(isToggled, EditorStyles.toggle, new GUILayoutOption[]{ GUILayout.MaxWidth(13.0f) });
								isToggled = GUI.Toggle(layoutRect, isToggled, GUIContent.none, GUIStyle.none);
								// Label
								EditorUtils.AddLabelField("<b>"+(managedAvatar?.avatarName ?? managedAvatarObj?.name)+"</b>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoMaxWidth" });
								EditorGUILayout.Space(0,true);
							});
							bool selectAvatar = EditorUtils.AddButton(">","Select Managed Avatar",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(2,5,4,0), padding=new RectOffset(5,4,2,3) },new object[]{ "AutoWidth" });
							if(selectAvatar){ Selection.objects = new UnityEngine.Object[]{ managedAvatarObj }; }
						});
						if(invertToggle) isToggled = !isToggled;
						// Update Action
						bool isDirty = false;
						if(action && isToggled && !action.managedAvatars.Contains(managedAvatarObj)){
							MAUndo.Record("Configure Action",objectConfig,objectConfig.gameObject);
							action.managedAvatars.Add(managedAvatarObj);
							isDirty = true;
						}
						else if(action && !isToggled && action.managedAvatars.Contains(managedAvatarObj)){
							MAUndo.Record("Configure Action",objectConfig,objectConfig.gameObject);
							action.managedAvatars.Remove(managedAvatarObj);
							isDirty = true;
						}
						if(isDirty){ objectConfig.MarkDirty(); action.MarkDirty(); MAUndo.Flush(); }
					}
				});
			});
		}
		
		public static void GUI_Actions_Simple_Configure(ObjectConfig objectConfig,AvatarMain avatarMain,Action action){
			EditorUtils.LayoutVertical(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(3,3,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				//EditorUtils.AddLabelField("<color='#F0F0F0'>Configure Action:</color>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(2,0,0,5) },new object[]{ "AutoMaxWidth" });
				action.DrawGUI(objectConfig,objectConfig.gameObject);
				// if(action is ActionList.SwapMaterial swapMat){
				// 	swapMat.DrawGUI(objectConfig,objectConfig.gameObject);
				// }
				// if(action is ActionList.RemoveComponent removeComp){
					
				// }
				// else {
				// 	EditorUtils.AddLabelField("Not yet implemented",null,new object[]{ "AutoWidth" });
				// }
			});
		}
		
		public static void GUI_Actions_Advanced(ObjectConfig objectConfig,AvatarMain avatarMain){
			// EditorHelpers.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,5,0) },()=>{
			// 	float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
			// 	EditorHelpers.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,4), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true), GUILayout.MinHeight(minLabelHeight+6) },()=>{
			// 		Rect layoutRect = EditorHelpers.previousLayoutRect;
			// 		bool toggleShown = instance.displayActions;
			// 		EditorGUILayout.Space(4,false);
			// 		toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
			// 		toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
			// 		instance.displayActions = toggleShown;
			// 		EditorHelpers.AddLabelField("Actions","Configure Actions",new GUIStyle(EditorHelpers.uiStyleDefault){ padding=new RectOffset(0,0,0,0), margin=new RectOffset(5,0,2,0) },new object[]{ "AutoMaxWidth" });
			// 		EditorGUILayout.Space(0,true);
			// 	});
			// });
			//if(instance.displayActions){
				EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,0) },()=>{
					EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(7,5,5,5), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
						EditorUtils.AddLabelField("Configure actions:",null,new object[]{ "AutoWidth" });
						
						EditorUtils.AddLabelField("TODO",null,new object[]{ "AutoWidth" });
						
						foreach(Type type in ActionsHandler.actionTypes){
							EditorGUILayout.LabelField("----------");
							EditorGUILayout.LabelField(type.Name);
							// var actionName = (string)ActionsHandler.GetActionStaticProp(type,"Name");
							// var actionDescription = (string)ActionsHandler.GetActionStaticProp(type,"Description");
							// var actionAllowedSources = (Action.ActionSource[])ActionsHandler.GetActionStaticProp(type,"AllowedSources");
							EditorGUILayout.LabelField("Name: "+ActionsHandler.GetActionName(type));
							EditorGUILayout.LabelField("Description: "+ActionsHandler.GetActionDescription(type));
							EditorGUILayout.LabelField("AllowedSources ManagedAvatar: "+ActionsHandler.CheckActionAllowedSources(type,Action.ActionSource.ManagedAvatar).ToString());
							EditorGUILayout.LabelField("AllowedSources ObjectConfig: "+ActionsHandler.CheckActionAllowedSources(type,Action.ActionSource.ObjectConfig).ToString());
							Action action = ActionsHandler.CreateAction(type);
							EditorGUILayout.LabelField("new action: "+action);
						}
					});
				});
			//}
		}
		
	}

	public class ObjectConfigWindow : UnityEditor.EditorWindow {

		// Only one window open at a time
		public static ObjectConfigWindow window = null;
		public static ObjectConfigEditor component = null;
		public static ObjectConfig instance = null;

		public static void OpenAsWindow(ObjectConfig modelInstance){
			instance = modelInstance;
			if(!window) window = (ObjectConfigWindow) EditorWindow.GetWindow(typeof(ObjectConfigWindow),false,"MivorTools Multi-Avatar");
			window.minSize = new Vector2(400,100);
			window.wantsMouseMove = false;
			window.wantsMouseEnterLeaveWindow = false;
			if(component) component.window = null;
			component = (ObjectConfigEditor) ObjectConfigEditor.CreateEditor(instance);
			component.window = window;
		}
		
		public static Vector2 scrollPos = Vector2.zero;
		
		public void OnGUI(){
			if(!window || !component || !instance){
				// TODO: List avatars with Multi-Avatar Manager Components
				EditorGUILayout.LabelField("Not Initialised.\nSelect an Cloned Object to open a Multi-Avatar Object Config in this window.",new GUIStyle(GUI.skin.GetStyle("Label")) { wordWrap=true });
				return;
			}
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
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
