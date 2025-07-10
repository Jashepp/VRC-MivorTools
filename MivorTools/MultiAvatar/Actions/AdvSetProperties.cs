
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class AdvSetProperties: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => true;
			public static new int ProcessOrder => 111;
			public static new string Name => "Advanced Set Properties";
			public static new string Description => "Change a property/field of an object component";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ManagedAvatar, ActionSource.ObjectConfig };

			#if UNITY_EDITOR
			#endif
		}
	}

}
