
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class RemoveObject: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => false;
			public static new int ProcessOrder => 2;
			public static new string Name => "Remove Object";
			public static new string Description => "Removes the object and all child objects";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ObjectConfig };
			
			#if UNITY_EDITOR
			
			public override bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!originalObj || !clonedObj || originalObj!=gameObject || !isEnabled) return false;
				if(!managedAvatars.Contains(managedAvatar.avatarCloneGameObject)) return false;
				return true;
			}
			public override bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!ActionValidate(managedAvatar,originalObj,clonedObj)) return false;
				GameObject.DestroyImmediate(clonedObj);
				return true;
			}
			
			#endif
		}

	}

}
