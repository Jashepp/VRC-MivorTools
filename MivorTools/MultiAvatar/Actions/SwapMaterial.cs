
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
        public class SwapMaterial: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => true;
			public static new int ProcessOrder => 100;
			public static new string Name => "Swap Material";
			public static new string Description => "Replaces the materials of the object";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ObjectConfig };
			
			[SerializeReference] public Material[] newMaterials = null;
			
			#if UNITY_EDITOR
			
			public override bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!originalObj || !clonedObj || originalObj!=gameObject || !isEnabled) return false;
				if(!managedAvatars.Contains(managedAvatar.avatarCloneGameObject)) return false;
				if(newMaterials==null) return false;
				return true;
			}
			public override bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!ActionValidate(managedAvatar,originalObj,clonedObj)) return false;
				Renderer rendererOriginal = originalObj.GetComponent<Renderer>();
				Renderer rendererCloned = clonedObj.GetComponent<Renderer>();
				if(!rendererOriginal || !rendererCloned) return false;
				if(newMaterials.Length!=rendererCloned.sharedMaterials.Length) return false;
				Material[] newMaterialsArr = new Material[rendererCloned.sharedMaterials.Length];
				for(int i=0;i<newMaterialsArr.Length;i++){
					if(newMaterials[i]!=null) newMaterialsArr[i] = newMaterials[i];
					else newMaterialsArr[i] = rendererCloned.sharedMaterials[i];
				}
				rendererCloned.sharedMaterials = newMaterialsArr; // Only updates on set new array
				clonedObj.MarkDirty();
				return true;
			}
			
			public override void DrawGUI(ObjectConfig objectConfig,GameObject originalObj){
				Renderer renderer = originalObj.GetComponent<Renderer>();
				if(!renderer){
					EditorUtils.AddLabelField("No renderer found",null,new object[]{ "AutoWidth" });
					return;
				}
				if(renderer.sharedMaterials.Length==0){
					EditorUtils.AddLabelField("No material slots found",null,new object[]{ "AutoWidth" });
					return;
				}
				if(this.newMaterials==null) this.newMaterials = new Material[renderer.sharedMaterials.Length];
				if(this.newMaterials.Length!=renderer.sharedMaterials.Length){
					Material[] oldMats = this.newMaterials;
					this.newMaterials = new Material[renderer.sharedMaterials.Length];
					for(int i=0;i<renderer.sharedMaterials.Length && i<oldMats.Length;i++){
						this.newMaterials[i] = oldMats[i];
					}
				}
				for(int i=0;i<this.newMaterials.Length;i++){
					EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(5,5,5,5), margin=new RectOffset(0,0,0,5) },()=>{
						Material originalMaterial = renderer.sharedMaterials[i];
						Material newMaterial = this.newMaterials[i];
						EditorUtils.AddLabelField("<b>Material Slot "+i+"</b>",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(3,0,0,0) },new object[]{ "AutoWidth" });
						EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) },()=>{
							EditorUtils.AddLabelField("Original: ",null,new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
							EditorUtils.LayoutDisabled(()=>{
								EditorGUILayout.ObjectField("",originalMaterial,typeof(Material),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
							});
						});
						EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,2,0) },()=>{
							EditorUtils.AddLabelField("New Material: ","If empty, it will result in the original material",new GUIStyle(EditorUtils.uiStyleDefault){ alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
							newMaterial = (Material)EditorGUILayout.ObjectField("",newMaterial,typeof(Material),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
						});
						if(newMaterial!=this.newMaterials[i]){
							this.newMaterials[i] = newMaterial;
							objectConfig.MarkDirty(); originalObj.MarkDirty();
						}
					});
				}
			}
			
			#endif
		}

	}

}
