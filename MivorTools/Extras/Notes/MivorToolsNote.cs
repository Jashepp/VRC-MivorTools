
#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using VRC;

namespace MivorTools.Extras.Note {

	[ExecuteInEditMode]
	[AddComponentMenu("MivorTools/Extras/Notes",1)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	public class MivorToolsNote : MonoBehaviour, VRC.SDKBase.IEditorOnly {

		[SerializeField]
		public bool locked = false;

		[SerializeField]
		public String noteTitle = "Note By YourName";

		[SerializeField]
		public String noteText = "Note Text";

		[SerializeField]
		public int height = 15*4;

		[HideInInspector]
		[SerializeField]
		public int modelVersion = 1;

	}

	[CustomEditor(typeof(MivorToolsNote))]
	public class NotesEditor : UnityEditor.Editor {
		public override void OnInspectorGUI(){
			MivorToolsNote instance = (MivorToolsNote)this.target;
			this.serializedObject.Update();
			UnityEditor.Undo.RecordObject(instance,"MivorTools Notes: Changes");
			bool isChanged = false;

			EditorGUILayout.BeginHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleRight });
			{
				if(instance.locked){
					EditorGUILayout.LabelField(instance.noteTitle,new GUIStyle(GUI.skin.GetStyle("Label")){ fontSize=12, margin=new RectOffset(-4,0,2,0), padding=new RectOffset(0,0,0,0) });
				}
				else {
					string noteTitle = guiPreventRepaintSelection(()=>EditorGUILayout.TextField(instance.noteTitle,new GUIStyle(GUI.skin.GetStyle("TextField")){ fontSize=12, stretchWidth=true, margin=new RectOffset(-2,5,2,0) }));
					if(noteTitle!=instance.noteTitle){ instance.noteTitle = noteTitle; isChanged = true; }
				}
				if(!instance.locked && GUILayout.Button(new GUIContent("-","Decrease Note Height"),GUILayout.Width(30))){
					instance.height -= 15;
					if(instance.height<=0) instance.height = 0;
					else if(instance.height<15*1) instance.height = 15*1;
					isChanged = true;
				}
				if(!instance.locked && GUILayout.Button(new GUIContent("+","Increase Note Height"),GUILayout.Width(30))){
					instance.height += 15;
					if(instance.height>15*20) instance.height = 15*20;
					isChanged = true;
				}
				if(GUILayout.Button(new GUIContent("L","Lock/Unlock Notes"),GUILayout.Width(30))){
					instance.locked = !instance.locked;
					isChanged = true;
				}
			}
			EditorGUILayout.EndHorizontal();
			
			if(instance.height>0){
				if(instance.locked){
					EditorGUILayout.SelectableLabel(instance.noteText,new GUIStyle(GUI.skin.box){ fontSize=12, stretchWidth=true, alignment=TextAnchor.UpperLeft, wordWrap=true, margin=new RectOffset(0,0,5,0), padding=new RectOffset(3,3,3,3), normal=GUI.skin.GetStyle("TextArea").normal },GUILayout.MinHeight(instance.height+6),GUILayout.ExpandHeight(true));
				}
				else {
					string noteText = guiPreventRepaintSelection(()=>EditorGUILayout.TextArea(instance.noteText,new GUIStyle(GUI.skin.GetStyle("TextArea")){ fontSize=12, stretchWidth=true, alignment=TextAnchor.UpperLeft, wordWrap=true, margin=new RectOffset(0,0,7,0), padding=new RectOffset(3,3,3,3) },GUILayout.MinHeight(instance.height+6),GUILayout.ExpandHeight(true)));
					if(noteText!=instance.noteText){ instance.noteText = noteText; isChanged = true; }
				}
			}

			this.serializedObject.ApplyModifiedProperties();
			if(isChanged) instance.gameObject.MarkDirty();
		}

		// https://stackoverflow.com/questions/44097608/how-can-i-stop-immediate-gui-from-selecting-all-text-on-click
		private T guiPreventRepaintSelection<T>(Func<T> guiCall){
			bool preventSelection = Event.current.type != EventType.Repaint;
			Color oldCursorColor = GUI.skin.settings.cursorColor;
			if (preventSelection) GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
			T value = guiCall();
			if (preventSelection) GUI.skin.settings.cursorColor = oldCursorColor;
			return value;
		}
	}

}

#endif
