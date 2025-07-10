
using System;
using UnityEngine;
using MivorTools.MultiAvatar.Components;

#if UNITY_EDITOR
using VRC;
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class MoveObject: Action {
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => false;
			public static new int ProcessOrder => 1;
			public static new string Name => "Move Object";
			public static new string Description => "Moves object to new parent object";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ObjectConfig };
			
			[SerializeReference] public GameObject targetObject = null;
			
			#if UNITY_EDITOR
			
			public override bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!originalObj || !clonedObj || originalObj!=gameObject || !isEnabled) return false;
				if(!managedAvatars.Contains(managedAvatar.avatarCloneGameObject)) return false;
				if(!targetObject || targetObject==null) return false;
				return true;
			}
			public override bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!ActionValidate(managedAvatar,originalObj,clonedObj)) return false;
				(GameObject originalTarget,GameObject clonedTarget) = ManagedAvatarBuilder.findOriginalAndClonedPair(managedAvatar.avatarCloneGameObject,targetObject,targetObject);
				if(!clonedTarget) return false;
				if(clonedTarget==clonedObj || MiscUtils.doesGameObjectHaveHierarchyParent(clonedTarget,clonedObj)) return false;
				clonedObj.transform.SetParent(clonedTarget.transform);
				return true;
			}
			
			public override void DrawGUI(ObjectConfig objectConfig,GameObject originalObj){
				EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,5) },()=>{
					GameObject originalTarget = targetObject;
					GameObject newTarget = targetObject;
					EditorUtils.AddLabelField("<b>Select New Parent</b>",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(3,0,0,0) },new object[]{ "AutoWidth" });
					EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },()=>{
						EditorUtils.AddLabelField("Parent Object: ","If empty, it will result in the original material",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
						newTarget = (GameObject)EditorGUILayout.ObjectField("",newTarget,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
					});
					if(newTarget!=null){
						AvatarMain compAvatarMain = objectConfig.gameObject.GetComponentInParent<AvatarMain>();
						if(compAvatarMain && !MiscUtils.doesGameObjectHaveHierarchyParent(newTarget,compAvatarMain.gameObject)) newTarget = null;
					}
					if(newTarget!=originalTarget){
						targetObject = newTarget;
						objectConfig.MarkDirty(); originalObj.MarkDirty();
					}
				});
			}
			
			#endif
		}

	}

}
