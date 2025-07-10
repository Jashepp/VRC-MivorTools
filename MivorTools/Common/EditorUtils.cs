
#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace MivorTools {
	
	public partial class EditorUtils {
		
		public delegate void Callback();
		
		public static bool eventLastRectClicked(){
			return Event.current.type==EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
		}
		
		//public static GUIStyle uiStyleDefault = new GUIStyle(){ richText=true, alignment=TextAnchor.MiddleLeft, normal=new GUIStyleState() { textColor=GUI.skin.GetStyle("Label").normal.textColor } }; // textColor=new Color(179,179,179)
		public static GUIStyle uiStyleDefault = new GUIStyle(){ richText=true, alignment=TextAnchor.MiddleLeft, normal=GUI.skin.GetStyle("Label").normal, wordWrap=false, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0) }; // textColor=new Color(179,179,179)
		
		public static GUILayoutOption[] CustomHandleOptions(GUIContent content, GUIStyle style, object[] options){
			Vector2 size = style.CalcSize(content);
			for(int i = 0; i < options.Length; i++){
				if(options[i]!=null && options[i] is string optStr){
					if(optStr=="AutoWidth") options[i] = GUILayout.Width(size.x);
					if(optStr=="AutoMaxWidth") options[i] = GUILayout.MaxWidth(size.x);
					if(optStr=="AutoMinWidth") options[i] = GUILayout.MinWidth(size.x);
					if(optStr=="AutoHeight") options[i] = GUILayout.Height(size.y);
					if(optStr=="AutoMaxHeight") options[i] = GUILayout.MaxHeight(size.y);
					if(optStr=="AutoMinHeight") options[i] = GUILayout.MinHeight(size.y);
				}
				if(options[i]==null || options[i].GetType()!=typeof(GUILayoutOption)) options[i] = GUILayout.ExpandWidth(false);
			}
			return Array.ConvertAll(options,item=>(GUILayoutOption)item);
		}
		
		public static void AddLabelField(string text, string mouseover=null) => AddLabelField(text,mouseover,uiStyleDefault);
		public static void AddLabelField(string text, params object[] options) => AddLabelField(text,null,null,options);
		public static void AddLabelField(string text, GUIStyle style, params object[] options) => AddLabelField(text,null,style,options);
		public static void AddLabelField(string text, string mouseover=null, TextAnchor align=TextAnchor.MiddleLeft,params object[] options) => AddLabelField(text,mouseover,new GUIStyle(uiStyleDefault) { alignment=align },options);
		public static void AddLabelField(string text, string mouseover=null, GUIStyle style=null, params object[] options) => AddLabelField(text,mouseover,style,false,options);
		public static void AddLabelField(string text, string mouseover=null, GUIStyle style=null, bool isSelectable=false, params object[] options){
			if(style==null) style = uiStyleDefault;
			var content = new GUIContent(text,mouseover);
			if(isSelectable) EditorGUILayout.SelectableLabel(text, style, CustomHandleOptions(content,style,options));
			else EditorGUILayout.LabelField(content, style, CustomHandleOptions(content,style,options));
		}
		
		// private T guiPreventRepaintSelection<T>(Func<T> guiCall){
		// 	bool preventSelection = Event.current.type != EventType.Repaint;
		// 	Color oldCursorColor = GUI.skin.settings.cursorColor;
		// 	if (preventSelection) GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
		// 	T value = guiCall();
		// 	if (preventSelection) GUI.skin.settings.cursorColor = oldCursorColor;
		// 	return value;
		// }
		
		public static bool AddButton(string text, string mouseover=null) => AddButton(text,mouseover);
		public static bool AddButton(string text, string mouseover=null, GUIStyle style=null, params object[] options){
			if(style==null) style = new GUIStyle(GUI.skin.GetStyle("Button")){ richText=true, alignment=TextAnchor.MiddleCenter };
			var content = new GUIContent(text,mouseover);
			return GUILayout.Button(content, style, CustomHandleOptions(content,style,options));
		}
		
		public static bool AddCheckbox(bool value,string text, string mouseover=null){
			EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,0,3), margin=new RectOffset(0,0,0,0) },()=>{
				value = AddCheckboxSmall(value,text,mouseover);
				EditorGUILayout.Space(0,true);
			});
			return value;
		}
		public static bool AddCheckboxSmall(bool value,string text, string mouseover=null){
			value = EditorGUILayout.Toggle("",value,new GUILayoutOption[]{ GUILayout.Width(15) });
			EditorUtils.AddLabelField(text,mouseover,new GUIStyle(uiStyleDefault) { alignment=TextAnchor.MiddleLeft },new object[]{ "AutoWidth" });
			value = GUI.Toggle(GUILayoutUtility.GetLastRect(), value, GUIContent.none, GUIStyle.none);
			return value;
		}
		
		public static Rect previousLayoutRect;
		public static void LayoutVertical(GUIStyle style, GUILayoutOption[] options, Callback callback){
			previousLayoutRect = EditorGUILayout.BeginVertical(style,options);
			try{ callback(); }catch(Exception e){ Debug.LogException(e); }
			EditorGUILayout.EndVertical();
		}
		public static void LayoutVertical(GUIStyle style, Callback callback) => LayoutVertical(style,new GUILayoutOption[0],callback);
		public static void LayoutVertical(GUILayoutOption[] options, Callback callback) => LayoutVertical(GUIStyle.none,options,callback);
		public static void LayoutVertical(Callback callback) => LayoutVertical(GUIStyle.none,new GUILayoutOption[0],callback);
		
		public static void LayoutHorizontal(GUIStyle style, GUILayoutOption[] options,Callback callback){
			previousLayoutRect = EditorGUILayout.BeginHorizontal(style,options);
			try{ callback(); }catch(Exception e){ Debug.LogException(e); }
			EditorGUILayout.EndHorizontal();
		}
		public static void LayoutHorizontal(GUIStyle style, Callback callback) => LayoutHorizontal(style,new GUILayoutOption[0],callback);
		public static void LayoutHorizontal(GUILayoutOption[] options, Callback callback) => LayoutHorizontal(GUIStyle.none,options,callback);
		public static void LayoutHorizontal(Callback callback) => LayoutHorizontal(GUIStyle.none,new GUILayoutOption[0],callback);
		
		public static void LayoutDisabled(bool isLayoutDisabled,Callback callback){
			EditorGUI.BeginDisabledGroup(isLayoutDisabled);
			try{ callback(); }catch(ExitGUIException){}catch(Exception e){ Debug.LogException(e); }
			EditorGUI.EndDisabledGroup();
		}
		public static void LayoutDisabled(Callback callback) => LayoutDisabled(true,callback);
		public static void LayoutEnabled(bool isLayoutEnabled,Callback callback) => LayoutDisabled(!isLayoutEnabled,callback);
		
		public static Vector2 LayoutScroll(Vector2 scrollPos, Callback callback, bool alwaysHorizontal=false, bool alwaysVertical=true, bool hideHorizontal=false, bool hideVertical=false, GUILayoutOption[] options=null){
			scrollPos = EditorGUILayout.BeginScrollView(scrollPosition:scrollPos, background:GUIStyle.none, alwaysShowHorizontal:alwaysHorizontal, alwaysShowVertical:alwaysVertical, options:options, horizontalScrollbar:hideHorizontal?GUIStyle.none:GUI.skin.horizontalScrollbar, verticalScrollbar:hideVertical?GUIStyle.none:GUI.skin.verticalScrollbar);
			try{ callback(); }catch(Exception e){ Debug.LogException(e); }
			EditorGUILayout.EndScrollView();
			return scrollPos;
		}
		
	}
}

#endif
