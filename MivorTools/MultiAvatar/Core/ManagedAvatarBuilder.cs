
using System.Globalization;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MivorTools.MultiAvatar;
using MivorTools.MultiAvatar.Components;
using MivorTools.MultiAvatar.ActionList;


#if UNITY_EDITOR
using UnityEngine.Events;
using VRC;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using System.Reflection;
#endif

namespace MivorTools.MultiAvatar {

	#if UNITY_EDITOR
    public partial class ManagedAvatarBuilder {
		
		public static bool CleanAvatar(ManagedAvatar managedAvatar,bool calledFromBuild=false){
			managedAvatar.MarkDirty();
			GameObject managedAvatarRootObj = managedAvatar.avatarCloneGameObject;
			
			bool progressBar = !calledFromBuild;
			int progressSteps = 6;
			string displayProgressBarTitle = "MivorTools Multi-Avatar - Cleaning Managed Avatar";
			void updateProgressBar(int step,string extraMsg) => EditorUtility.DisplayProgressBar(displayProgressBarTitle,extraMsg.Length>0?"Step "+step+": "+extraMsg:"Processing...",(float)step/(float)progressSteps);
			if(progressBar) updateProgressBar(0,"");
			
			try{
				// Record Changes - RedoUndo not working
				// if(progressBar) updateProgressBar(1,"Recording Changes");
				// List<UnityEngine.Object> recordObjects = new List<UnityEngine.Object>{ managedAvatarRootObj };
				// MiscUtils.forEachGameObjectChildren(
				// 	gameObjParent:managedAvatarRootObj, transverse:true,
				// 	callback:(GameObject gameObj)=>{
				// 		recordObjects.Add(gameObj);
				// 		return true;
				// 	}
				// );
				// MAUndo.Record("CleanCloneAvatar",true,recordObjects.ToArray());
				
				// Go through managed avatar, clear childObjects on CloneConstraints
				if(progressBar) updateProgressBar(1,"Cleaning CloneConstraints");
				CloneConstraint[] CloneConstraintComponents = managedAvatarRootObj.GetComponentsInChildren<CloneConstraint>(true);
				foreach(CloneConstraint compCloneConstraint in CloneConstraintComponents){
					if(compCloneConstraint && compCloneConstraint.childObjects.Count>0){
						bool hasObjectCloneChild = false;
						foreach(GameObject childObj in MiscUtils.getGameObjectChildrenArray(compCloneConstraint.gameObject)){
							if(childObj.GetComponent<ObjectClone>()){ hasObjectCloneChild = true; break; }
						}
						if(hasObjectCloneChild) compCloneConstraint.childObjects = new List<GameObject>{};
					}
				}
				
				// Go through managed avatar, add CloneConstraint to all new objects without ObjectClone
				if(progressBar) updateProgressBar(2,"Creating CloneConstraints");
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:managedAvatarRootObj, transverse:true,
					callback:(GameObject gameObj)=>{
						ObjectClone compSelfObjectClone = gameObj.GetComponent<ObjectClone>();
						CloneConstraint compSelfCloneConstraint = gameObj.GetComponent<CloneConstraint>();
						GameObject gameObjParent = gameObj.transform.parent ? gameObj.transform.parent.gameObject : null;
						ObjectClone compParentObjectClone = gameObjParent?.GetComponent<ObjectClone>();
						CloneConstraint compParentCloneConstraint = gameObjParent?.GetComponent<CloneConstraint>();
						// Custom Object under Cloned Object
						if(!compSelfObjectClone && compParentObjectClone && compParentObjectClone.originalObject && !compSelfCloneConstraint && !compParentCloneConstraint){
							compSelfCloneConstraint = gameObj.AddComponent<CloneConstraint>();
							compSelfCloneConstraint.createdFromCode = true;
							compSelfCloneConstraint.ownObject = gameObj;
							compSelfCloneConstraint.parentObject = compParentObjectClone.originalObject;
						}
						// Cloned Object under Custom Object
						if(compSelfObjectClone && compSelfObjectClone.originalObject && !compParentObjectClone && !compSelfCloneConstraint && compParentCloneConstraint){
							compParentCloneConstraint.childObjects.Add(compSelfObjectClone.originalObject);
						}
						if(compSelfCloneConstraint) for(int i=0;i<gameObj.GetComponents<Component>().Length;i++) UnityEditorInternal.ComponentUtility.MoveComponentUp(compSelfCloneConstraint);
						return true;
					}
				);
				
				// Go through managed avatar, reparent all objects with CloneConstraint
				if(progressBar) updateProgressBar(3,"Re-Parenting CloneConstraint Objects");
				List<UnityEngine.Object> CloneConstraintObjects = new List<UnityEngine.Object>{};
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:managedAvatarRootObj, transverse:true,
					callback:(GameObject gameObj)=>{
						CloneConstraint compCloneConstraint = gameObj.GetComponent<CloneConstraint>();
						if(compCloneConstraint){
							CloneConstraintObjects.Add(gameObj);
							return false;
						}
						return true;
					}
				);
				foreach(GameObject gameObj in CloneConstraintObjects){
					gameObj.transform.SetParent(managedAvatarRootObj.transform);
				}
				CloneConstraintObjects.Clear();
				
				// Go through managed avatar, remove objects with ObjectClone component
				if(progressBar) updateProgressBar(4,"Cleaning ObjectClones");
				List<UnityEngine.Object> ObjectCloneObjectsToRemove = new List<UnityEngine.Object>{};
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:managedAvatarRootObj, transverse:true,
					callback:(GameObject gameObj)=>{
						ObjectClone compObjectClone = gameObj.GetComponent<ObjectClone>();
						if(compObjectClone) ObjectCloneObjectsToRemove.Add(gameObj);
						return true;
					}
				);
				foreach(GameObject gameObj in ObjectCloneObjectsToRemove){
					GameObject.DestroyImmediate(gameObj);
				}
				ObjectCloneObjectsToRemove.Clear();
				
				// Update mapped components
				if(progressBar) updateProgressBar(5,"Updating Component Mappings");
				MappedComponents.ReMapComponentMapping(managedAvatar);
				
				// Go through managed avatar components, remove components that arent flagged as keep
				if(progressBar) updateProgressBar(6,"Removing Unkept Components");
				SafeRemoveClonedComponents(managedAvatar,onlyIfBoth:false);
			}
			catch(Exception e){
				Debug.Log("CleanAvatar: Error");
				Debug.LogException(e);
				if(progressBar) EditorUtility.ClearProgressBar();
				throw e;
			}
			
			// Done
			if(progressBar) EditorUtility.ClearProgressBar();
			//MAUndo.Flush();
			return true;
		}
		
		public static void SafeRemoveClonedComponents(ManagedAvatar managedAvatar,bool onlyIfBoth=false){
			if(!managedAvatar?.avatarCloneGameObject) return;
			// Go through managed avatar components, remove components that arent flagged as keep
			List<Component> componentsCloned = managedAvatar.avatarCloneGameObject.GetComponents<Component>().ToList();
			foreach(MappedComponents mappedComp in managedAvatar.componentMap.ToArray()){
				if(mappedComp.keepExisting || !mappedComp.hasClone()) continue;
				if(onlyIfBoth && !(mappedComp.hasMain() && mappedComp.copyToAvatar)) continue;
				foreach(Component compCloned in componentsCloned){
					if(mappedComp.isSameClone(compCloned)){
						mappedComp.RemoveClone();
					}
				}
				if(!mappedComp.mainComp && !mappedComp.clonedComp) managedAvatar.componentMap.Remove(mappedComp);
			}
		}
		
		public static List<Type> buildMainComponentsKeepOnManaged = null;
		public static List<Type> buildMainComponentsSkipCopy = null;
		
		public static bool BuildAvatar(ManagedAvatar managedAvatar,bool undoFailedChanges=!true){
			//Debug.Log("BuildAvatar: Start");
			managedAvatar.MarkDirty();
			int progressSteps = 14;
			string displayProgressBarTitle = "MivorTools Multi-Avatar - Updating Managed Avatar";
			bool cancelBuild = false;
			void updateProgressBar(int step,string extraMsg,bool canCancel=true){
				bool cancel = EditorUtility.DisplayCancelableProgressBar(displayProgressBarTitle,extraMsg.Length>0?"Step "+step+": "+extraMsg:"Processing...",(float)step/(float)progressSteps);
				if(cancel) cancelBuild = true;
				if(cancelBuild && canCancel) throw new Exception("BuildAvatar Cancelled");
			}
			updateProgressBar(0,"",false);
			GameObject mainAvatarRootObj = managedAvatar.avatarMain.gameObject;
			GameObject managedAvatarRootObj = managedAvatar.avatarCloneGameObject;
			GameObject freshClonedAvatar = null;
			buildMainComponentsKeepOnManaged = new List<Type>{};
			buildMainComponentsSkipCopy = new List<Type>{};
			bool fnResult = false;
			try{
				// Clean Managed Avatar
				updateProgressBar(1,"Cleaning Managed Avatar",false);
				bool avatarCleaned = CleanAvatar(managedAvatar,calledFromBuild:true);
				if(!avatarCleaned){
					undoFailedChanges = false;
					throw new Exception("Failed to clean Managed Avatar");
				}
				
				// Reposition Managed Avatar
				updateProgressBar(2,"Re-Positioning Managed Avatar");
				ManagedAvatar.RePositionManagedAvatar(managedAvatar,force:true);
				
				// Record Changes
				updateProgressBar(3,"Recording Changes For Undo/Redo");
				List<UnityEngine.Object> recordObjects = new List<UnityEngine.Object>{ managedAvatarRootObj };
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:managedAvatarRootObj, transverse:true,
					callback:(GameObject gameObj)=>{
						recordObjects.Add(gameObj);
						return true;
					}
				);
				MAUndo.Record("BuildAvatar",true,recordObjects.ToArray());
				recordObjects.Clear();
				
				// Create Fresh Clone of Original Avatar
				updateProgressBar(4,"Creating Fresh Clone");
				freshClonedAvatar = UnityEngine.Object.Instantiate<GameObject>(mainAvatarRootObj);
				freshClonedAvatar.name = managedAvatar.avatarName+" (Temp Clone)";
				freshClonedAvatar.transform.SetParent(mainAvatarRootObj.transform.parent);
				
				// Go through fresh clone, add ObjectClone component to every object
				updateProgressBar(5,"Adding ObjectClone Components");
				MiscUtils.forEachTransformSame(
					transformParent1:mainAvatarRootObj.transform, transformParent2:freshClonedAvatar.transform, transverse:true,
					callback:(Transform originalObjTransform,Transform clonedObjTransform)=>{
						GameObject originalObj = originalObjTransform.gameObject;
						GameObject clonedObj = clonedObjTransform.gameObject;
						ObjectClone compObjectClone = clonedObj.GetComponent<ObjectClone>();
						if(!compObjectClone){
							compObjectClone = clonedObj.AddComponent<ObjectClone>();
							compObjectClone.createdFromCode = true;
							compObjectClone.ownObject = clonedObj;
							compObjectClone.originalObject = originalObj;
						}
						bool isHidden = SceneVisibilityManager.instance.IsHidden(originalObj,false);
						if(isHidden) SceneVisibilityManager.instance.Hide(clonedObj,false);
						bool isPickingDisabled = SceneVisibilityManager.instance.IsPickingDisabled(originalObj,false);
						if(isPickingDisabled) SceneVisibilityManager.instance.DisablePicking(clonedObj,false);
						return true;
					}
				);
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:freshClonedAvatar, transverse:true,
					callback:(GameObject gameObj)=>{
						ObjectClone compObjectClone = gameObj.GetComponent<ObjectClone>();
						if(!compObjectClone){
							compObjectClone = gameObj.AddComponent<ObjectClone>();
							compObjectClone.createdFromCode = true;
							compObjectClone.ownObject = gameObj;
							compObjectClone.originalObject = null;
						}
						if(compObjectClone) for(int i=0;i<gameObj.GetComponents<Component>().Length;i++) UnityEditorInternal.ComponentUtility.MoveComponentUp(compObjectClone);
						return true;
					}
				);
				
				// Go through fresh clone, reparent objects to managed avatar
				updateProgressBar(6,"Re-Parenting Fresh Clone Objects to Managed Avatar");
				MiscUtils.forEachGameObjectChildren(
					gameObjParent:freshClonedAvatar, transverse:false,
					callback:(GameObject gameObj)=>{
						gameObj.transform.SetParent(managedAvatarRootObj.transform);
						return false;
					}
				);
				
				// Go through fresh clone, reparent all objects with CloneConstraint
				updateProgressBar(7,"Re-Parenting CloneConstraint Objects");
				List<CloneConstraint> CloneConstraintComponents = managedAvatarRootObj.GetComponentsInChildren<CloneConstraint>(true).ToList();
				List<ObjectClone> ObjectCloneComponents = managedAvatarRootObj.GetComponentsInChildren<ObjectClone>(true).ToList();
				foreach(ObjectClone compObjectClone in ObjectCloneComponents){
					if(compObjectClone && compObjectClone.originalObject) foreach(CloneConstraint compCloneConstraint in CloneConstraintComponents){
						if(compCloneConstraint && compCloneConstraint.parentObject==compObjectClone.originalObject){
							//if(MiscUtils.doesGameObjectHaveHierarchyParent(compObjectClone.gameObject,compCloneConstraint.gameObject)) continue;
							compCloneConstraint.gameObject.transform.SetParent(compObjectClone.gameObject.transform);
						}
					}
				}
				
				// Go through fresh clone, reparent all CloneConstraint child objects
				updateProgressBar(8,"Re-Parenting CloneConstraint Child Objects");
				foreach(CloneConstraint compCloneConstraint in CloneConstraintComponents){
					if(!compCloneConstraint || compCloneConstraint.childObjects.Count==0) continue;
					foreach(GameObject originalObj in compCloneConstraint.childObjects){
						foreach(ObjectClone compObjectClone in ObjectCloneComponents){
							if(compObjectClone && compObjectClone.originalObject && compObjectClone.originalObject==originalObj){
								if(MiscUtils.doesGameObjectHaveHierarchyParent(compCloneConstraint.gameObject,compObjectClone.gameObject)) continue;
								compObjectClone.gameObject.transform.SetParent(compCloneConstraint.gameObject.transform);
							}
						}
					}
				}
				CloneConstraintComponents.Clear();
				ObjectCloneComponents.Clear();
				
				// Go through fresh clone, remove cloned ObjectConfig components
				updateProgressBar(9,"Removing Cloned ObjectConfig Components");
				foreach(Component compObjectConfig in managedAvatarRootObj.GetComponentsInChildren<ObjectConfig>(true)){
					UnityEngine.Object.DestroyImmediate(compObjectConfig);
				}
				
				// Go through managed avatar, run ObjectConfig actions
				updateProgressBar(10,"Processing ObjectConfig Actions");
				//System.Threading.Thread.Sleep(100); // Force Progress Bar Update
				List<ActionToProcess> actionsToProcess = new List<ActionToProcess>{};
				foreach(ObjectClone compObjectClone in managedAvatarRootObj.GetComponentsInChildren<ObjectClone>(true)){
					GameObject originalObj = compObjectClone.originalObject;
					if(!originalObj) continue;
					ObjectConfig[] compObjectConfigs = originalObj.GetComponents<ObjectConfig>();
					if(compObjectConfigs.Length==0) continue;
					foreach(ObjectConfig compObjectConfig in compObjectConfigs){
						foreach(Action action in compObjectConfig.actions){
							actionsToProcess.Add(new ActionToProcess(){ action=action, originalObject=originalObj, clonedObject=compObjectClone.gameObject });
						}
					}
				}
				actionsToProcess = actionsToProcess.OrderBy(p=>p.action?.actionProcessOrder).ToList();
				foreach(ActionToProcess process in actionsToProcess){
					try{
						if(!process.action || !process.originalObject || !process.clonedObject) continue;
						if(!process.action.isEnabled) continue;
						if(!process.action.managedAvatars.Contains(managedAvatarRootObj)) continue;
						if(!MiscUtils.doesGameObjectHaveHierarchyParent(process.clonedObject,managedAvatarRootObj)) continue;
						string actionInfo = process.action.GetActionInfoString();
						updateProgressBar(10,"Processing ObjectConfig Action: "+process.action.GetActionName()+(actionInfo.Length>0?" - "+actionInfo:"")); // +" - "+process.action.GetActionDescription() //  on '"+process.clonedObject.name+"'
						bool result = process.action.ActionProcess(managedAvatar,process.originalObject,process.clonedObject);
						if(!result) Debug.LogWarning("ObjectConfig Action: "+process.action.GetActionName()+" failed");
					}
					catch(Exception e){
						Debug.Log("ObjectConfig ActionProcess: Error");
						Debug.LogException(e);
					}
				}
				actionsToProcess.Clear();
				
				// On fresh clone root object, run Managed Avatar actions
				updateProgressBar(11,"Processing Managed Avatar Actions");
				List<Action> mngActionsSorted = managedAvatar.actions.OrderBy(a=>a.actionProcessOrder).ToList();
				if(mngActionsSorted.Any(a=>a is ReplaceAll)) System.Threading.Thread.Sleep(100); // Force Progress Bar Update
				List<(Type,int)> ranActionBatches = new List<(Type,int)>{};
				foreach(Action action in mngActionsSorted){
					try{
						if(!action || !action.isEnabled) continue;
						Type actionType = action.GetType();
						if(ActionsHandler.UsesBatchProcessing(actionType)){
							if(ranActionBatches.Contains((actionType,action.actionProcessOrder))) continue;
							updateProgressBar(11,"Processing Managed Avatar Action: "+action.GetActionName());
							List<Action> batchedActions = mngActionsSorted.Where(a=>actionType==a.GetType() && action.actionProcessOrder==a.actionProcessOrder).ToList();
							bool result = ActionsHandler.RunActionBatchProcess(actionType,managedAvatar,mainAvatarRootObj,managedAvatarRootObj,batchedActions);
							if(!result) Debug.LogWarning("Managed Avatar Action: "+action.GetActionName()+" failed");
							ranActionBatches.Add((actionType,action.actionProcessOrder));
						}
						else {
							string actionInfo = action.GetActionInfoString();
							updateProgressBar(11,"Processing Managed Avatar Action: "+action.GetActionName()+(actionInfo.Length>0?" - "+actionInfo:"")); // +" - "+action.GetActionDescription()
							bool result = action.ActionProcess(managedAvatar,mainAvatarRootObj,managedAvatarRootObj);
							if(!result) Debug.LogWarning("Managed Avatar Action: "+action.GetActionName()+" failed");
						}
					}
					catch(Exception e){
						Debug.Log("ManagedAvatar ActionProcess: Error");
						Debug.LogException(e);
					}
				}
				mngActionsSorted.Clear();
				ranActionBatches.Clear();
				
				// Check root components against main avatar (not fresh clone)
				// Note: components with keepExisting=false are already removed
				updateProgressBar(12,"Updating Managed Avatar Components");
				foreach(MappedComponents mappedComp in managedAvatar.componentMap){
					mappedComp.CreateClone(managedAvatar);
				}
				 
				// Go through managed avatar components, re-order to be same as component mapping
				updateProgressBar(13,"Re-Ordering Managed Avatar Components");
				// List<Component> componentsCloned = managedAvatarRootObj.GetComponents<Component>().ToList();
				// foreach(Component comp in componentsCloned){
				// 	for(int i=0;i<componentsCloned.Count;i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(comp);
				// }
				// foreach(MappedComponents mappedComp in managedAvatar.componentMap){
				// 	Component compCloned = mappedComp?.clonedComp?.With(managedAvatarRootObj);
				// 	if(!compCloned) continue;
				// 	for(int i=0;i<componentsCloned.Count;i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(compCloned);
				// }
				MappedComponents.ReOrderClonedComps(managedAvatar);
				
				// Final Cleanup
				updateProgressBar(14,"Final Cleanup");
				UnityEngine.Object.DestroyImmediate(freshClonedAvatar);
				freshClonedAvatar = null;
				
				// Done
				//Debug.Log("BuildAvatar: Finish");
				fnResult = true;
			}
			catch(Exception e){
				Debug.Log("BuildAvatar: Error");
				Debug.LogException(e);
				//MAUndo.Flush();
				if(undoFailedChanges){
					updateProgressBar(0,"Error occurred. Attempting to clean Managed Avatar");
					Debug.Log("BuildAvatar: Cleaning Managed Avatar due to Error");
					try{ CleanAvatar(managedAvatar); }catch(Exception){}
				}
				try{ if(freshClonedAvatar) UnityEngine.Object.DestroyImmediate(freshClonedAvatar); }catch(Exception){}
				fnResult = false;
			}
			EditorUtility.ClearProgressBar();
			buildMainComponentsKeepOnManaged = null;
			buildMainComponentsSkipCopy = null;
			return fnResult;
		}
		
		public class ActionToProcess {
			public Action action = null;
			public GameObject originalObject;
			public GameObject clonedObject;
		}
		
		public static (GameObject original,GameObject cloned) findOriginalAndClonedPair(GameObject clonedAvatar,GameObject original=null,GameObject cloned=null){
			if(original==null && cloned==null) return (null,null);
			foreach(ObjectClone compObjectClone in clonedAvatar.GetComponentsInChildren<ObjectClone>(true)){
				GameObject clonedObj = compObjectClone.ownObject;
				GameObject originalObj = compObjectClone.originalObject;
				if(!clonedObj || !originalObj) continue;
				if(original!=null && original==originalObj) return (originalObj,clonedObj);
				if(cloned!=null && cloned==clonedObj) return (originalObj,clonedObj);
			}
			return (null,null);
		}
		public static (Transform original,Transform cloned) findOriginalAndClonedPair(GameObject clonedAvatar,Transform original=null,Transform cloned=null){
			(GameObject originalFound,GameObject clonedFound) = findOriginalAndClonedPair(clonedAvatar,original?.gameObject,cloned?.gameObject);
			if(originalFound==null || clonedFound==null) return (null,null);
			return (originalFound.transform,clonedFound.transform);
		}
		
		public static void fixClonedComponent_vrcAvatarDescriptor(GameObject clonedAvatar,VRCAvatarDescriptor originalComp,VRCAvatarDescriptor clonedComp){
			try{
				// If below becomes too cumbersome to manage, maybe use reflection instead, like how ReplaceAll action does.
				
				// Check Methods
				bool checkT(Transform o,Transform c) => o && o!=null && (!c || c==null || o==c || !MiscUtils.doesTransformHaveHierarchyParent(c,clonedAvatar.transform));
				//bool checkO(GameObject o,GameObject c) => o && o!=null && (!c || c==null || o==c || !MiscUtils.doesGameObjectHaveHierarchyParent(c,clonedAvatar));
				bool checkR(SkinnedMeshRenderer o,SkinnedMeshRenderer c) => o && o!=null && (!c || c==null || o==c || !MiscUtils.doesGameObjectHaveHierarchyParent(c.gameObject,clonedAvatar));
				bool checkCC(VRCAvatarDescriptor.ColliderConfig o,VRCAvatarDescriptor.ColliderConfig c) => o.transform && o.transform!=null && (!c.transform || c.transform==null || o.transform==c.transform || !MiscUtils.doesGameObjectHaveHierarchyParent(c.transform.gameObject,clonedAvatar));
				
				// Eye Look
				VRCAvatarDescriptor.CustomEyeLookSettings eyeLookOriginal = originalComp.customEyeLookSettings;
				VRCAvatarDescriptor.CustomEyeLookSettings eyeLookCloned = clonedComp.customEyeLookSettings;
				if(checkT(eyeLookOriginal.leftEye,eyeLookCloned.leftEye)){
					eyeLookCloned.leftEye = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.leftEye).cloned;
				}
				if(checkT(eyeLookOriginal.rightEye,eyeLookCloned.rightEye)){
					eyeLookCloned.rightEye = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.rightEye).cloned;
				}
				if(checkT(eyeLookOriginal.upperLeftEyelid,eyeLookCloned.upperLeftEyelid)){
					eyeLookCloned.upperLeftEyelid = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.upperLeftEyelid).cloned;
				}
				if(checkT(eyeLookOriginal.upperRightEyelid,eyeLookCloned.upperRightEyelid)){
					eyeLookCloned.upperRightEyelid = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.upperRightEyelid).cloned;
				}
				if(checkT(eyeLookOriginal.lowerLeftEyelid,eyeLookCloned.lowerLeftEyelid)){
					eyeLookCloned.lowerLeftEyelid = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.lowerLeftEyelid).cloned;
				}
				if(checkT(eyeLookOriginal.lowerRightEyelid,eyeLookCloned.lowerRightEyelid)){
					eyeLookCloned.lowerRightEyelid = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.lowerRightEyelid).cloned;
				}
				if(checkR(eyeLookOriginal.eyelidsSkinnedMesh,eyeLookCloned.eyelidsSkinnedMesh)){
					eyeLookCloned.eyelidsSkinnedMesh = findOriginalAndClonedPair(clonedAvatar,original:eyeLookOriginal.eyelidsSkinnedMesh.gameObject).cloned?.GetComponent<SkinnedMeshRenderer>();
				}
				clonedComp.customEyeLookSettings = eyeLookCloned;
				
				// Colliders
				if(checkCC(originalComp.collider_head,clonedComp.collider_head)){
					clonedComp.collider_head.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_head.transform).cloned;
				}
				if(checkCC(originalComp.collider_torso,clonedComp.collider_torso)){
					clonedComp.collider_torso.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_torso.transform).cloned;
				}
				if(checkCC(originalComp.collider_footR,clonedComp.collider_footR)){
					clonedComp.collider_footR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_footR.transform).cloned;
				}
				if(checkCC(originalComp.collider_footL,clonedComp.collider_footL)){
					clonedComp.collider_footL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_footL.transform).cloned;
				}
				if(checkCC(originalComp.collider_handR,clonedComp.collider_handR)){
					clonedComp.collider_handR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_handR.transform).cloned;
				}
				if(checkCC(originalComp.collider_handL,clonedComp.collider_handL)){
					clonedComp.collider_handL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_handL.transform).cloned;
				}
				// Colliders Fingers Left
				if(checkCC(originalComp.collider_fingerIndexL,clonedComp.collider_fingerIndexL)){
					clonedComp.collider_fingerIndexL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerIndexL.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerMiddleL,clonedComp.collider_fingerMiddleL)){
					clonedComp.collider_fingerMiddleL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerMiddleL.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerRingL,clonedComp.collider_fingerRingL)){
					clonedComp.collider_fingerRingL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerRingL.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerLittleL,clonedComp.collider_fingerLittleL)){
					clonedComp.collider_fingerLittleL.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerLittleL.transform).cloned;
				}
				// Colliders Fingers Right
				if(checkCC(originalComp.collider_fingerIndexR,clonedComp.collider_fingerIndexR)){
					clonedComp.collider_fingerIndexR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerIndexR.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerMiddleR,clonedComp.collider_fingerMiddleR)){
					clonedComp.collider_fingerMiddleR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerMiddleR.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerRingR,clonedComp.collider_fingerRingR)){
					clonedComp.collider_fingerRingR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerRingR.transform).cloned;
				}
				if(checkCC(originalComp.collider_fingerLittleR,clonedComp.collider_fingerLittleR)){
					clonedComp.collider_fingerLittleR.transform = findOriginalAndClonedPair(clonedAvatar,original:originalComp.collider_fingerLittleR.transform).cloned;
				}
				
				// lipSyncJawBone
				if(checkT(originalComp.lipSyncJawBone,clonedComp.lipSyncJawBone)){
					clonedComp.lipSyncJawBone = findOriginalAndClonedPair(clonedAvatar,original:originalComp.lipSyncJawBone).cloned;
				}
				// VisemeSkinnedMesh
				if(checkR(originalComp.VisemeSkinnedMesh,clonedComp.VisemeSkinnedMesh)){
					clonedComp.VisemeSkinnedMesh = findOriginalAndClonedPair(clonedAvatar,original:originalComp.VisemeSkinnedMesh.gameObject).cloned?.GetComponent<SkinnedMeshRenderer>();
				}
				
			}catch(Exception e){
				Debug.Log("BuildAvatar on fixClonedComponent_vrcAvatarDescriptor: Error:\n"+e.Message);
				Debug.LogException(e);
			}
		}
		
		public static void fixClonedComponent_d4rkAvatarOptimizer(GameObject clonedAvatar,Component originalComp,Component clonedComp){
			try{
				if(originalComp.GetType().ToString()!="d4rkAvatarOptimizer" || clonedComp.GetType().ToString()!="d4rkAvatarOptimizer") return;
				FieldInfo fieldExcludeTransforms = clonedComp.GetType().GetField("ExcludeTransforms",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.FlattenHierarchy);
				if(fieldExcludeTransforms==null){ Debug.LogWarning("Failed to find d4rkAvatarOptimizer.ExcludeTransforms"); return; }
				List<Transform> originalExcludeTransforms = fieldExcludeTransforms.GetValue(originalComp) as List<Transform>;
				List<Transform> clonedExcludeTransforms = fieldExcludeTransforms.GetValue(clonedComp) as List<Transform>;
				foreach(Transform transform in originalExcludeTransforms){
					foreach(ObjectClone compObjectClone in clonedAvatar.GetComponentsInChildren<ObjectClone>(true)){
						GameObject originalObj = compObjectClone.originalObject;
						if(!originalObj) continue;
						if(originalObj==transform.gameObject){
							clonedExcludeTransforms.Add(compObjectClone.transform);
							break;
						}
					}
				}
				fieldExcludeTransforms.SetValue(clonedComp,clonedExcludeTransforms.Distinct().ToList());
			}catch(Exception e){
				Debug.Log("BuildAvatar on fixClonedComponent_d4rkAvatarOptimizer: Error:\n"+e.Message);
				Debug.LogException(e);
			}
		}
		
	}
	#endif
	
}
