
#if UNITY_EDITOR
using UnityEngine;
using System;
using UnityEditor;

namespace MivorTools.Extras.PlayFocusObject {
	
	[AddComponentMenu("MivorTools/Extras/Focus Object On Play Mode",1)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	public class PlayFocusObject : MonoBehaviour, VRC.SDKBase.IEditorOnly {

		[SerializeField] public int focusAfterNFrames = 5;

		[NonSerialized] [HideInInspector] private int frameCount = 0;
		
		[SerializeField] public bool pressKeyToFocusObject = true;
		[SerializeField] public KeyCode focusKey = KeyCode.R;

		// Start is called before the first frame update
		void Start(){
			frameCount = 0;
		}

		// Update is called once per frame
		void Update(){
			if(pressKeyToFocusObject && Input.GetKeyDown(focusKey)){
				Selection.objects = new UnityEngine.Object[]{ this.gameObject };
			}
			if(frameCount>=focusAfterNFrames) return;
			frameCount++;
			if(frameCount==focusAfterNFrames){
				Selection.objects = new UnityEngine.Object[]{ this.gameObject };
			}
		}

	}

}

#endif
