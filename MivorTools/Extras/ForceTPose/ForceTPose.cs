#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

// Reference: https://forum.unity.com/threads/reset-to-t-pose.397760/#post-8480540

public class ForceTPose_MivorToolsVer {
	// This method fixes an avatar which is stuck in a bicycle pose.
	// It alters only those bones that are in a typical humanoid armature, so ears and tail will be unaffected.
	// For most bones, only the rotation will be reverted to the T-pose, but for the hips, the position needs to be reset as well.
	
	[MenuItem("GameObject/MivorTools/Enforce T-Pose", true, 21)]
	static bool TPose_Validate(){
		GameObject selected = Selection.activeGameObject;
		if(!selected) return false; // If no object was selected, exit.
		Animator animator = selected.GetComponent<Animator>();
		if(!animator){
			selected = selected.transform.root.gameObject;
			animator = selected.GetComponent<Animator>();
		}
		if(!animator) return false; // If the selected object has no animator, exit.
		if(!animator.avatar) return false;
		return true;
	}
	
	[MenuItem("GameObject/MivorTools/Enforce T-Pose", false, 21)]
	static void TPose(){
		GameObject selected = Selection.activeGameObject;
		if(!selected) return; // If no object was selected, exit.
		Animator animator = selected.GetComponent<Animator>();
		if(!animator){
			selected = selected.transform.root.gameObject;
			animator = selected.GetComponent<Animator>();
		}
		if(!animator) return; // If the selected object has no animator, exit.
		if(!animator.avatar) return;
		SkeletonBone[] skeletonbones = animator.avatar?.humanDescription.skeleton; // Get the list of bones in the armature.
		foreach(SkeletonBone sb in skeletonbones){ // Loop through all bones in the armature.
			foreach(HumanBodyBones hbb in Enum.GetValues(typeof(HumanBodyBones))){
				if(hbb!=HumanBodyBones.LastBone){
					Transform bone = animator.GetBoneTransform(hbb);
					if(bone!=null && sb.name==bone.name){ // If this bone is a normal humanoid bone (as opposed to an ear or tail bone), reset its transform.
						// The bicycle pose happens when for some reason the transforms of an avatar's bones are incorectly saved in a state that is not the t-pose.
						// For most of the bones this affects only their rotation, but for the hips, the position is affected as well.
						// As the scale should be untouched, and the user may have altered these intentionally, we should leave them alone.
						bool edited = false;
						if(hbb==HumanBodyBones.Hips && bone.localPosition!=sb.position){ bone.localPosition = sb.position; edited=true; }
						if(bone.localRotation!=sb.rotation){ bone.localRotation = sb.rotation; edited = true; }
						if(edited) EditorUtility.SetDirty(bone.gameObject);
						break; // We found a humanbodybone that matches, so we need not check the rest against this skeleton bone.
					}
				}
			}
		}
	}
	
}

#endif
