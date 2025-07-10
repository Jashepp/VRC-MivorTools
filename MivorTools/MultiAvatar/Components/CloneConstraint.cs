
using System;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar.Components {

	#if UNITY_EDITOR
	[ExecuteInEditMode]
	[AddComponentMenu("Scripts/MivorTools/Multi-Avatar/Internal (Ignore This)/Multi-Avatar: Clone Constraint",1000)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	#endif
	[Serializable]
	public class CloneConstraint : SerializableComponent {
		[SerializeField] public int modelVersion = 1;
		[SerializeReference] public GameObject ownObject = null;
		[SerializeReference] public GameObject parentObject = null; // Parent Original Object from Main Avatar
		[SerializeReference] public List<GameObject> childObjects = new List<GameObject>{}; // Child Original Object from Main Avatar
		[SerializeField] public bool createdFromCode = false;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(CloneConstraint))]
	public class CloneConstraintEditor : UnityEditor.Editor {
		
		public override void OnInspectorGUI(){
			if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused){ EditorGUILayout.LabelField("Component not editable during play mode.",new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, richText=true, wordWrap=true, padding=new RectOffset(5,5,5,5) }); return; }
			
			CloneConstraint instance = (CloneConstraint)this.target;
			this.serializedObject.Update();
			if(MetaPackageInfo.DrawGUICriticalIssues()) return;

			#if VRC_SDK_VRCSDK3
			bool guiDrawn = false; bool removeComp = false;
			//if(!guiDrawn && !instance.createdFromCode){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- This component cannot be created manually."); }
			//if(!guiDrawn && instance.ownObject!=instance.gameObject){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- This component cannot be copied from a different object."); }
			//if(!guiDrawn && !instance.parentObject){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- Missing Original Parent Object that this object sits within."); }
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
			// Auto-Fix Parent Object
			if(!guiDrawn){
				GameObject parentCloneObject = instance.gameObject.transform.parent ? instance.gameObject.transform.parent.gameObject : null;
				AvatarClone parentAvatarClone = parentCloneObject ? parentCloneObject.GetComponent<AvatarClone>() : null;
				ObjectClone parentObjectClone = parentCloneObject ? parentCloneObject.GetComponent<ObjectClone>() : null;
				GameObject originalObject = parentObjectClone ? parentObjectClone.originalObject : (parentAvatarClone ? parentAvatarClone.originalObject : null);
				if(parentCloneObject && !parentAvatarClone && !parentObjectClone){ guiDrawn=true; removeComp=true; GUI_Error_Simple("- Parent Object has no Multi-Avatar Components."); }
				if(parentCloneObject && !guiDrawn && !originalObject){ guiDrawn=true; GUI_Error_Simple("- Parent Object has no Original Avatar Object. Check Issues on Parent Object."); }
				if(parentCloneObject && !guiDrawn && originalObject!=instance.parentObject){
					guiDrawn=true; GUI_Error_Simple("- Current Parent Object does not match current Configured Parent Object.");
					// Auto-Fix Button
					EditorUtils.AddLabelField("Current Parent Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					EditorUtils.LayoutDisabled(()=>{
						EditorGUILayout.ObjectField("",originalObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
					});
					EditorUtils.AddLabelField("Configured Parent Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
					EditorUtils.LayoutDisabled(()=>{
						EditorGUILayout.ObjectField("",instance.parentObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
					});
					EditorUtils.AddLabelField("<b>If this avatar is not built, you may ignore this issue.</b>");
					bool autoFix1 = GUILayout.Button(new GUIContent("Auto-Fix: Use Current Parent Object","Set Configured Parent Object to Current Parent Object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,5,0) });
					if(autoFix1){
						MAUndo.Record("Auto-Fix CloneConstraint",instance);
						instance.parentObject = originalObject;
						if(instance.ownObject!=instance.gameObject) instance.ownObject = instance.gameObject;
						if(!instance.createdFromCode) instance.createdFromCode = true;
					}
					// Auto-Fix Button
					AvatarClone AvatarClone = parentCloneObject.GetComponentInParent<AvatarClone>(true);
					GameObject foundParentObject = null;
					if(AvatarClone){
						if(AvatarClone.originalObject==instance.parentObject) foundParentObject = AvatarClone.gameObject;
						if(!foundParentObject) foreach(ObjectClone oic in AvatarClone.gameObject.GetComponents<ObjectClone>()){
							if(oic.originalObject==instance.parentObject){ foundParentObject = oic.gameObject; break; }
						}
					}
					if(foundParentObject){
						bool autoFix2 = GUILayout.Button(new GUIContent("Auto-Fix: Use Configured Parent Object","Re-Position this object onto the cloned object with the configured parent object"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,10,0) });
						if(autoFix2){
							MAUndo.Record("Auto-Fix CloneConstraint",instance.gameObject.transform);
							instance.gameObject.transform.SetParent(foundParentObject.transform);
							if(instance.ownObject!=instance.gameObject) instance.ownObject = instance.gameObject;
							if(!instance.createdFromCode) instance.createdFromCode = true;
						}
					}
					if(removeComp) EditorGUILayout.Space(10,false);
					GUI_CloneConstraint(instance,showParentObj:false);
				}
			}
			if(guiDrawn && removeComp){
				bool removeCompBtn = GUILayout.Button(new GUIContent("Remove This Component"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(5,5,5,5) });
				if(removeCompBtn) EditorApplication.delayCall += ()=>{ DestroyImmediate(instance); };
			}
			if(!guiDrawn){
				guiDrawn=true; GUI_CloneConstraint(instance);
			}
			#endif

			//this.serializedObject.ApplyModifiedProperties();
		}
		
		private void GUI_Error_Simple(string message){
			EditorGUILayout.LabelField(new GUIContent("<color='#FFA0A0'><b>Issues Found:</b></color>"),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, alignment=TextAnchor.MiddleLeft },new GUILayoutOption[]{});
			EditorGUILayout.LabelField(message,new GUIStyle(GUI.skin.GetStyle("HelpBox")){ fontSize=12, wordWrap=true, padding=new RectOffset(5,5,5,5) });
		}

		private void GUI_CloneConstraint(CloneConstraint instance,bool showParentObj=true){

			GameObject parentCloneObject = instance.gameObject.transform.parent ? instance.gameObject.transform.parent.gameObject : null;
			AvatarClone parentAvatarClone = parentCloneObject ? parentCloneObject.GetComponent<AvatarClone>() : null;
			ObjectClone parentObjectClone = parentCloneObject ? parentCloneObject.GetComponent<ObjectClone>() : null;
			GameObject originalObject = parentObjectClone ? parentObjectClone.originalObject : (parentAvatarClone ? parentAvatarClone.originalObject : null);
			AvatarClone AvatarClone = parentCloneObject.GetComponentInParent<AvatarClone>(true);
			// AvatarMain multiAvatarMain = AvatarClone.managedAvatar.avatarMain;
			AvatarMain originalAvatarMain = originalObject ? originalObject.GetComponent<AvatarMain>() : null;
			ObjectConfig originalObjectConfig = originalObject ? originalObject.GetComponent<ObjectConfig>() : null;

			if(showParentObj) EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
				EditorUtils.AddLabelField("Parent Object: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",originalObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
				});
				bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Original GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
				if(openObjectInsp){ EditorUtility.OpenPropertyEditor(originalObject); }
				if(originalObjectConfig){
					bool openMultiAvatarObjConfig = EditorUtils.AddButton("Object Config","Opens Multi-Avatar Object Config in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
					if(openMultiAvatarObjConfig){ ObjectConfigWindow.OpenAsWindow(originalObjectConfig); }
				}
				if(originalAvatarMain){
					bool openMultiAvatarManager = EditorUtils.AddButton("Manager","Opens Multi-Avatar Manager in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
					if(openMultiAvatarManager){ AvatarMainWindow.OpenAsWindow(originalAvatarMain,true,AvatarClone.managedAvatar); }
				}
			});
			
			List<GameObject> useChildObjects = new List<GameObject>{};
			if(instance.childObjects.Count==0){
				foreach(ObjectClone comp in instance.gameObject.GetComponentsInChildren<ObjectClone>()){
					if(comp.gameObject.transform.parent==instance.gameObject.transform) useChildObjects.Add(comp.originalObject);
				}
			}
			else useChildObjects = instance.childObjects;
			useChildObjects = useChildObjects.Distinct().ToList();
			if(useChildObjects.Count==0){
				EditorUtils.AddLabelField("Child Objects: None",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
			}
			if(useChildObjects.Count>0){
				EditorUtils.AddLabelField("Child Objects: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,0,0) },new object[]{ "AutoWidth" });
				foreach(GameObject childObj in useChildObjects){
					ObjectConfig childObjObjectConfig = childObj ? childObj.GetComponent<ObjectConfig>() : null;
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
						EditorUtils.LayoutDisabled(()=>{
							EditorGUILayout.ObjectField("",childObj,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
						});
						bool openObjectInsp = EditorUtils.AddButton("^","Opens inspector window for the Original GameObject",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(7,0,0,0), padding=new RectOffset(5,5,3,3) },new object[]{ "AutoWidth" });
						if(openObjectInsp){ EditorUtility.OpenPropertyEditor(childObj); }
						if(childObjObjectConfig){
							bool openMultiAvatarObjConfig = EditorUtils.AddButton("Object Config","Opens Multi-Avatar Object Config in a window",new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, margin=new RectOffset(5,0,0,0), padding=new RectOffset(10,10,3,3) },new object[]{ "AutoWidth" });
							if(openMultiAvatarObjConfig){ ObjectConfigWindow.OpenAsWindow(childObjObjectConfig); }
						}
					});
				}
			}

			EditorGUILayout.LabelField(new GUIContent("This component helps keep track of this object & it's child objects when the avatar is updated."),new GUIStyle(GUI.skin.GetStyle("Label")) { richText=true, wordWrap=true, alignment=TextAnchor.MiddleLeft, margin=new RectOffset(0,0,10,0) },new GUILayoutOption[]{});

			// TODO: Convert Button - Moves object to main avatar, adds object config with actions to remove for all avatars except current cloned one - and add vrcfury remove on upload (if vrcf exists)
			
		}

	}
	#endif

}
