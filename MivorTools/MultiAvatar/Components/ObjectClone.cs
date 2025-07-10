
using System;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar.Components {

	#if UNITY_EDITOR
	[ExecuteInEditMode]
	[AddComponentMenu("Scripts/MivorTools/Multi-Avatar/Internal (Ignore This)/Multi-Avatar: Cloned Object",1000)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	#endif
	[Serializable]
	public class ObjectClone : SerializableComponent {
		[SerializeField] public int modelVersion = 1;
		[SerializeReference] public GameObject originalObject = null;
		[SerializeReference] public GameObject ownObject = null;
		[SerializeField] public bool createdFromCode = false;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(ObjectClone))]
	public class ObjectCloneEditor : UnityEditor.Editor {
		
		public override void OnInspectorGUI(){
			if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused){ EditorGUILayout.LabelField("Component not editable during play mode.",new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) }); return; }
			
			ObjectClone instance = (ObjectClone)this.target;
			this.serializedObject.Update();
			if(MetaPackageInfo.DrawGUICriticalIssues()) return;

			#if VRC_SDK_VRCSDK3
			bool guiDrawn = false; bool removeComp = false;
			if(!guiDrawn && !instance.createdFromCode){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- This component cannot be created manually."); }
			if(!guiDrawn && instance.ownObject!=instance.gameObject){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- This component cannot be copied from a different object."); }
			if(!guiDrawn && !instance.originalObject){ guiDrawn=true; GUI_Error_Simple("- Missing Original Object that this object was cloned from."); }
			if(!guiDrawn){
				Component vrcad = instance.gameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if(vrcad){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- This component must not be on an Avatar Root Object."); }
			}
			if(!guiDrawn){
				AvatarClone AvatarClone = instance.gameObject.GetComponentInParent<AvatarClone>(true);
				if(!AvatarClone){ guiDrawn=true; GUI_Error_Simple("- Missing Multi-Avatar AvatarClone in object hierarchy."); }
				if(!guiDrawn){
					Component vrcad = AvatarClone.originalObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
					if(!vrcad){ guiDrawn=true; GUI_Error_Simple("- Missing VRC Avatar Descriptor on original avatar."); }
				}
				if(!guiDrawn){
					Component multiAvatarMain = AvatarClone.originalObject.GetComponent<AvatarMain>();
					if(!multiAvatarMain){ guiDrawn=true; GUI_Error_Simple("- Missing Multi-Avatar Manager on original avatar."); }
				}
			}
			if(guiDrawn && removeComp){
				bool removeCompBtn = GUILayout.Button(new GUIContent("Remove This Component"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
				if(removeCompBtn) EditorApplication.delayCall += ()=>{ DestroyImmediate(instance); };
			}
			if(!guiDrawn){
				guiDrawn=true; GUI_ObjectClone(instance);
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

		private void GUI_ObjectClone(ObjectClone instance){

			AvatarClone AvatarClone = instance.gameObject.GetComponentInParent<AvatarClone>(true);
			AvatarMain multiAvatarMain = AvatarClone.managedAvatar.avatarMain;
			ObjectConfig objectConfig = instance.originalObject.GetComponentInParent<ObjectConfig>(true);
			
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Original Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",instance.originalObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
				});
				bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Original GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
				if(openObjectInsp){ EditorUtility.OpenPropertyEditor(instance.originalObject); }
				if(objectConfig){
					bool openMultiAvatarObjConfig = EditorUtils.AddButton("Object Config","Opens Multi-Avatar Object Config in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
					if(openMultiAvatarObjConfig){ ObjectConfigWindow.OpenAsWindow(objectConfig); }
				}
			});
			
			// EditorGUILayout.LabelField(new GUIContent("Summary of changes:"),new GUIStyle(EditorHelpers.uiStyleDefault) { alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,10,0) },new GUILayoutOption[]{});
			EditorGUILayout.LabelField("Do not remove this component from this object.");

		}

	}
	#endif

}
