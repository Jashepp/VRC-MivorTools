
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;

#if UNITY_EDITOR
using VRC;
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    [Serializable]
	public partial class MappedComponents : SerializableObject {
		[SerializeField] public ComponentReference mainComp = null;
		[SerializeField] public ComponentReference clonedComp = null;
		[SerializeField] public bool copyToAvatar = false;
		[SerializeField] public bool keepExisting = false;
		
		#if UNITY_EDITOR
		public bool hasMain() => mainComp;
		public bool hasClone() => clonedComp;
		public void clearMain() => mainComp = null;
		public void clearClone() => clonedComp = null;
		public void setMain(Component comp) => mainComp = comp;
		public void setClone(Component comp) => clonedComp = comp;
		public bool isSameMain(Component comp) => mainComp?.With(comp);
		public bool isSameClone(Component comp) => clonedComp?.With(comp);
		
		public void hookOnSerialize(){
			if(mainComp && mainComp.createRefsOnSerialize==null) mainComp.createRefsOnSerialize = CreateRefsOnSerialize;
			if(clonedComp && clonedComp.createRefsOnSerialize==null) clonedComp.createRefsOnSerialize = CreateRefsOnSerialize;
		}
		private bool CreateRefsOnSerialize(ComponentReference compRef){
			if(mainComp && !clonedComp && copyToAvatar==true && keepExisting==false) return false;
			if(!mainComp && clonedComp && copyToAvatar==false && keepExisting==true) return false;
			return true;
		}
		
		public static MappedComponents Create(Component compMain=null,Component compCloned=null){
			MappedComponents componentMap = new MappedComponents(){};
			if(compMain!=null) componentMap.setMain(compMain);
			if(compCloned!=null) componentMap.setClone(compCloned);
			return componentMap;
		}
		
		public void CreateClone(ManagedAvatar managedAvatar){
			if(!managedAvatar) throw new ArgumentException("Missing managedAvatar");
			GameObject mainAvatarObj = managedAvatar.avatarMain.gameObject;
			GameObject cloneAvatarObj = managedAvatar.avatarCloneGameObject;
			Component compMain = mainComp?.With(mainAvatarObj);
			Component compCloned = clonedComp?.With(cloneAvatarObj);
			if(copyToAvatar && !keepExisting && compMain && !compCloned){
				compCloned = cloneAvatarObj.AddComponent(compMain.GetType());
				setClone(compCloned);
			}
			UpdateClone(managedAvatar);
		}
		
		public void UpdateClone(ManagedAvatar managedAvatar){
			if(!managedAvatar) throw new ArgumentException("Missing managedAvatar");
			GameObject mainAvatarObj = managedAvatar.avatarMain.gameObject;
			GameObject cloneAvatarObj = managedAvatar.avatarCloneGameObject;
			Component compMain = mainComp?.With(mainAvatarObj);
			Component compCloned = clonedComp?.With(cloneAvatarObj);
			if(compMain && compCloned){
				UnityEditor.EditorUtility.CopySerialized(compMain,compCloned);
				if(compCloned.GetType().ToString()=="d4rkAvatarOptimizer"){
					ManagedAvatarBuilder.fixClonedComponent_d4rkAvatarOptimizer(cloneAvatarObj,compMain,compCloned);
				}
			}
			if(compMain && compMain is VRC.SDK3.Avatars.Components.VRCAvatarDescriptor vrcadMain && compCloned && compCloned is VRC.SDK3.Avatars.Components.VRCAvatarDescriptor vrcadCloned){
				ManagedAvatarBuilder.fixClonedComponent_vrcAvatarDescriptor(cloneAvatarObj,vrcadMain,vrcadCloned);
			}
		}
		
		public void RemoveClone(){
			UnityEngine.Object.DestroyImmediate((Component)clonedComp);
			clearClone();
		}
		
		public static void ReOrderClonedComps(ManagedAvatar managedAvatar){
			if(!managedAvatar) throw new ArgumentException("Missing managedAvatar");
			//GameObject mainAvatarRootObj = managedAvatar.avatarMain.gameObject;
			GameObject cloneAvatarObj = managedAvatar.avatarCloneGameObject;
			List<Component> componentsCloned = cloneAvatarObj.GetComponents<Component>().ToList();
			foreach(Component comp in componentsCloned){
				for(int i=0;i<componentsCloned.Count;i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(comp);
			}
			foreach(MappedComponents mappedComp in managedAvatar.componentMap){
				Component compCloned = mappedComp?.clonedComp?.With(cloneAvatarObj);
				if(!compCloned) continue;
				for(int i=0;i<componentsCloned.Count;i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(compCloned);
			}
		}
		
		public static List<Type> IgnoreTypes = new List<Type>{
			typeof(AvatarMain), typeof(ObjectConfig), typeof(AvatarClone), typeof(ObjectClone), typeof(CloneConstraint),
			typeof(Transform), typeof(VRC.Core.PipelineManager),
		};
		
		public static List<Component> ReMapComponentMapping(ManagedAvatar managedAvatar){
			if(!managedAvatar.avatarMain?.gameObject || !managedAvatar.avatarCloneGameObject) return null;
			bool modifyComponentMap = true;
			List<Component> componentsMain = managedAvatar.avatarMain.gameObject.GetComponents<Component>().ToList();
			List<Component> componentsCloned = managedAvatar.avatarCloneGameObject.GetComponents<Component>().ToList();
			foreach(Component comp in componentsMain.ToArray()){ try{ comp.GetType(); }catch(Exception){ componentsMain.Remove(comp); } }
			foreach(Component comp in componentsCloned.ToArray()){ try{ comp.GetType(); }catch(Exception){ componentsCloned.Remove(comp); } }
			List<MappedComponents> compMapToRemove = new List<MappedComponents>();
			// Rebuild component map with valid entries
			for(int i=0;i<managedAvatar.componentMap.Count;i++){
				MappedComponents mappedComp = managedAvatar.componentMap.ElementAt(i);
				ComponentReference mainComp = mappedComp?.mainComp?.With(managedAvatar.avatarMain.gameObject);
				ComponentReference clonedComp = mappedComp?.clonedComp?.With(managedAvatar.avatarCloneGameObject);
				bool keep = true;
				if(!mainComp && !clonedComp) keep = false;
				else if(IgnoreTypes.Contains(mainComp?._type) || IgnoreTypes.Contains(clonedComp?._type)) keep = false;
				else {
					for(int j=0;j<managedAvatar.componentMap.Count;j++){
						MappedComponents mappedComp2 = managedAvatar.componentMap.ElementAt(j);
						if(mappedComp==mappedComp2) continue;
						if((mainComp && mappedComp2.mainComp==mainComp) || (clonedComp && mappedComp2.clonedComp==clonedComp)){
							keep = false;
							break;
						}
					}
				}
				if(!keep) compMapToRemove.Add(mappedComp);
			}
			foreach(MappedComponents mappedComp in compMapToRemove){
				//Debug.Log("Removed: "+mappedComp.mainComp+" -- "+mappedComp.clonedComp+" -- "+managedAvatar.componentMap.Count);
				managedAvatar.componentMap.Remove(mappedComp);
			}
			// Loop through main components
			List<Component> compCloneRemoveList = new List<Component>();
			foreach(Component compMain in componentsMain){
				MappedComponents componentMap = null;
				foreach(MappedComponents mappedComp in managedAvatar.componentMap){
					if(mappedComp.isSameMain(compMain)){ componentMap=mappedComp; break; }
				}
				if(!componentMap){
					Type type = compMain.GetType();
					if(IgnoreTypes.Contains(type)) continue;
					if(modifyComponentMap){
						componentMap = MappedComponents.Create(compMain:compMain,compCloned:null);
						componentMap.copyToAvatar = true;
						managedAvatar.componentMap.Add(componentMap);
						managedAvatar.MarkDirty();
						//Debug.Log("Create componentMap compMain: "+componentMap.mainComp);
					}
				}
				if(componentMap && componentMap.hasClone()){
					foreach(Component compCloned in componentsCloned){
						if(componentMap.isSameClone(compCloned)) compCloneRemoveList.Add(compCloned);
					}
				}
			}
			foreach(Component compCloned in compCloneRemoveList){
				componentsCloned.Remove(compCloned);
			}
			compCloneRemoveList.Clear();
			// Loop through cloned components
			foreach(Component compCloned in componentsCloned){
				MappedComponents componentMap = null;
				foreach(MappedComponents mappedComp in managedAvatar.componentMap){
					if(mappedComp.isSameClone(compCloned)){ componentMap=mappedComp; break; }
				}
				if(!componentMap){
					Type type = compCloned.GetType();
					if(IgnoreTypes.Contains(type)) continue;
					if(modifyComponentMap){
						componentMap = MappedComponents.Create(compMain:null,compCloned:compCloned);
						componentMap.keepExisting = true;
						managedAvatar.componentMap.Add(componentMap);
						managedAvatar.MarkDirty();
						//Debug.Log("Create componentMap compCloned: "+componentMap.clonedComp);
					}
				}
			}
			List<Component> components = componentsMain.Union(componentsCloned).ToList();
			componentsMain.Clear(); componentsCloned.Clear();
			foreach(MappedComponents mappedComp in managedAvatar.componentMap) mappedComp.hookOnSerialize();
			return components;
		}
		
		#endif
		
	}
	
}
