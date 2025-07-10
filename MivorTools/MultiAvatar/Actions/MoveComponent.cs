
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class MoveComponent: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => true;
			public static new int ProcessOrder => 20;
			public static new string Name => "Move Component";
			public static new string Description => "Moves component to a different object";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ObjectConfig };
			
			[SerializeField] public ComponentReference component = null;
			[SerializeField] public GameObject targetObject = null;
			
			#if UNITY_EDITOR

			// If remove component action also exists on managed avatar, on same target object, with same type, fail ActionValidate
			// If remove and this exist on normal object, then run remove first, then this

			// If Target is managed avatar, during process, add comp type to buildMainComponentsKeepOnManaged, but don't add it to buildMainComponentsSkipCopy

			#endif
		}

	}

}
