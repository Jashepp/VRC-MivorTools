
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MivorTools.MultiAvatar.Components;

#if UNITY_EDITOR
using VRC;
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {
	
    namespace ActionList {
		
		[Serializable]
        public class RemoveComponent: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => false;
			public static new int ProcessOrder => 10;
			public static new string Name => "Remove Components";
			public static new string Description => "Removes components from the object";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ObjectConfig };
			
			[SerializeField] public List<ComponentReference> components = new List<ComponentReference>(); // Original Components
			
			#if UNITY_EDITOR
			
			private static List<Type> IgnoreCompTypes => MappedComponents.IgnoreTypes;
			
			public override bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!originalObj || !clonedObj || originalObj!=gameObject || !isEnabled) return false;
				if(!managedAvatars.Contains(managedAvatar.avatarCloneGameObject)) return false;
				return true;
			}
			public override bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!ActionValidate(managedAvatar,originalObj,clonedObj)) return false;
				if(!managedAvatars.Contains(managedAvatar)) return false;
				if(components.Count==0) return false;
				List<ComponentReference> originalComps = originalObj.GetComponents<Component>()
					.Where(c=>!IgnoreCompTypes.Contains(c.GetType()))
					.Select(c=>(ComponentReference)c)
					.ToList();
				List<ComponentReference> clonedComps = clonedObj.GetComponents<Component>()
					.Where(c=>!IgnoreCompTypes.Contains(c.GetType()))
					.Select(c=>(ComponentReference)c)
					.ToList();
				// At the time when this action runs, components should be the same anyway
				if(originalComps.Count!=clonedComps.Count) return false;
				for(int i=0; i<originalComps.Count; i++){
					ComponentReference originalComp = originalComps[i];
					ComponentReference clonedComp = clonedComps[i];
					if(originalComp._type!=clonedComp._type) return false;
					if(originalComp._compCount!=clonedComp._compCount) return false;
					if(originalComp._compIndex!=clonedComp._compIndex) return false;
				}
				List<(ComponentReference original,ComponentReference cloned)> removeComps = new List<(ComponentReference,ComponentReference)>();
				for(int i=0; i<originalComps.Count; i++){
					ComponentReference originalComp = originalComps[i];
					ComponentReference clonedComp = clonedComps[i];
					if(originalComp.isInList(components)) removeComps.Add((originalComp,clonedComp));
					//foreach(ComponentReference comp in components) if(comp==originalComp){ removeComps.Add((originalComp,clonedComp)); break; }
				}
				foreach((ComponentReference original,ComponentReference cloned) in removeComps){
					UnityEngine.Object.DestroyImmediate((Component)cloned);
				}
				return true;
			}
			
			public override void DrawGUI(ObjectConfig objectConfig,GameObject originalObj){
				EditorUtils.AddLabelField("Components to remove:","Ticked = Component will not be cloned\nUnticked = Component will be cloned",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(7,0,0,0) },new object[]{ "AutoWidth" });
				List<ComponentReference> comps = originalObj.GetComponents<Component>()
					.Where(c=>!IgnoreCompTypes.Contains(c.GetType()))
					.Select(c=>(ComponentReference)c)
					.ToList();
				if(comps.Count==0){
					EditorUtils.AddLabelField("No components found",null,new object[]{ "AutoWidth" });
					return;
				}
				foreach(ComponentReference comp in components.ToArray()){
					if(!comp || comp is null || !comp.isInList(comps)) components.Remove(comp);
				}
				foreach(ComponentReference comp in comps){
					bool inList = comp.isInList(components);
					bool isToggled = inList;
					EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,3,3), margin=new RectOffset(7,0,3,3) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
						// Checkbox
						Rect layoutRect = EditorUtils.previousLayoutRect;
						EditorGUILayout.Space(4,false);
						isToggled = EditorGUILayout.Toggle(isToggled, EditorStyles.toggle, new GUILayoutOption[]{ GUILayout.MaxWidth(13.0f) });
						isToggled = GUI.Toggle(layoutRect, isToggled, GUIContent.none, GUIStyle.none);
						// Label
						EditorUtils.AddLabelField("<b>"+MiscUtils.GetComponentTitle(comp)+"</b>","",new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(0,0,0,0) },new object[]{ "AutoMaxWidth" });
						EditorGUILayout.Space(0,true);
					});
					if(isToggled && !inList){
						components.Add(comp);
						objectConfig.MarkDirty(); originalObj.MarkDirty();
					}
					else if(!isToggled && inList){
						comp.removeFromList(components);
						objectConfig.MarkDirty(); originalObj.MarkDirty();
					}
				}
			}
			
			#endif
		}

	}

}
