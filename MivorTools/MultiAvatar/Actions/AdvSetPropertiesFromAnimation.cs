
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class AdvSetPropertiesFromAnimation: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => true;
			public static new int ProcessOrder => 110;
			public static new string Name => "Advanced Set Properties - From Animation";
			public static new string Description => "Perform same changes an animation keyframe does";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ManagedAvatar };
			
			#if UNITY_EDITOR
			#endif
		}
	}

}
