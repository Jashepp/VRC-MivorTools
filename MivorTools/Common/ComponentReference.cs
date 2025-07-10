
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools {

    [Serializable]
	public class ComponentReference : UnityEngine.ISerializationCallbackReceiver, IEquatable<ComponentReference> {
		[SerializeField] public int modelVersion = 1;
		[NonSerialized] public Component _component = null;
		[SerializeReference] public GameObject _gameObj = null;
		[SerializeField] public TypeReference _type = null;
		[SerializeField] public int _compCount = 0;
		[SerializeField] public int _compIndex = 0;
		
		#if UNITY_EDITOR
		public Type componentType => _type;
		public Component component { get => ResolveComponent(forceResolve:true); set => SetComponent(value,createReferences:false); }
		
		private Component ResolveComponent(bool forceResolve=false){
			if(_component!=null && _gameObj!=null && _component?.gameObject!=_gameObj) component = null;
			if(_component!=null) return _component;
			if(_gameObj==null || _type is null) return null;
			bool hasRefs = _compIndex!=0;
			List<Component> comps = null;
			// Find by index
			if(hasRefs){
				comps = _gameObj.GetComponents<Component>().ToList();
				int rawCompCount = comps.Count;
				if(_compCount==rawCompCount) for(int i=0;i<comps.Count;i++){
					Component comp = comps.ElementAt(i);
					try{ comp.GetType(); }catch(Exception){ continue; }
					if(_compIndex==i+1 && comp.GetType()==_type){
						_component = comp;
						_gameObj = comp.gameObject;
						_type = comp.GetType();
						//Debug.Log("ResolveComponent byIndex Success:\n_component: "+_component+"\n_gameObj: "+_gameObj+"\n_type: "+_type+"\n_compCount: "+_compCount+"\n_compIndex: "+_compIndex+"\nmodelIsFresh: "+modelIsFresh+"\nmodelWasDeserialized: "+modelWasDeserialized+"\nmodelWasSerialized: "+modelWasSerialized+"\n\n");
						//RemoveComponentReferences();
						return _component;
					}
				}
			}
			// Find by type
			if(forceResolve || hasRefs){
				if(!inCorrectMode) return null;
				if(comps==null) comps = _gameObj.GetComponents<Component>().ToList();
				Component comp = null;
				foreach(Component comp2 in comps){
					if(comp is not null){ comp=null; break; }
					try{ comp2.GetType(); }catch(Exception){ continue; }
					if(_type==comp2.GetType()){
						//if(comp!=null){ comp=null; break; }
						comp = comp2;
					}
				}
				if(comp){
					_component = comp;
					_gameObj = comp.gameObject;
					_type = comp.GetType();
					//Debug.Log("ResolveComponent byType Success:\n_component: "+_component+"\n_gameObj: "+_gameObj+"\n_type: "+_type+"\n_compCount: "+_compCount+"\n_compIndex: "+_compIndex+"\nmodelIsFresh: "+modelIsFresh+"\nmodelWasDeserialized: "+modelWasDeserialized+"\nmodelWasSerialized: "+modelWasSerialized+"\n\n");
					//RemoveComponentReferences();
					return _component;
				}
			}
			//Debug.Log("ResolveComponent FAILED:\n_component: "+_component+"\n_gameObj: "+_gameObj+"\n_type: "+_type+"\n_compCount: "+_compCount+"\n_compIndex: "+_compIndex+"\nmodelIsFresh: "+modelIsFresh+"\nmodelWasDeserialized: "+modelWasDeserialized+"\nmodelWasSerialized: "+modelWasSerialized+"\n\n");
			//if(forceResolve && (_component!=null || _gameObj!=null || _type is null)) component = null;
			return null;
		}
		
		private void SetComponent(Component comp,bool createReferences=false){
			if(!inCorrectMode) return;
			if(comp is null || !comp.gameObject){
				_component = null;
				_gameObj = null;
				_type = null;
				return;
			}
			if(comp is null || !comp.gameObject || comp!=_component || comp.gameObject!=_gameObj){
				RemoveComponentReferences();
				createReferences = true;
			}
			_component = comp;
			_gameObj = comp.gameObject;
			_type = comp.GetType();
			if(createReferences) CreateComponentReferences();
		}
		
		private void RemoveComponentReferences(){
			if(!inCorrectMode) return;
			_compCount = 0;
			_compIndex = 0;
		}
		
		private void CreateComponentReferences(){
			if(!inCorrectMode) return;
			if(_component==null || _gameObj==null) return;
			RemoveComponentReferences();
			List<Component> comps = _gameObj.GetComponents<Component>().ToList();
			_compCount = comps.Count;
			for(int i=0;i<comps.Count;i++){
				Component comp = comps.ElementAt(i);
				try{ comp.GetType(); }catch(Exception){ continue; }
				if(comp==_component){ _compIndex = i+1; }
			}
			//Debug.Log("CreateComponentReferences:\n_component: "+_component+"\n_gameObj: "+_gameObj+"\n_type: "+_type+"\n_compCount: "+_compCount+"\n_compIndex: "+_compIndex+"\nmodelIsFresh: "+modelIsFresh+"\nmodelWasDeserialized: "+modelWasDeserialized+"\nmodelWasSerialized: "+modelWasSerialized+"\n\n");
		}
		
		private static bool needsToSerialize = false;
		[InitializeOnLoadMethod]
		private static void ListenUnload(){
			needsToSerialize=false;
			AssemblyReloadEvents.beforeAssemblyReload += ()=>{ needsToSerialize=true; };
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += (UnityEngine.SceneManagement.Scene scene,string path)=>{ needsToSerialize=true; };
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += (UnityEngine.SceneManagement.Scene scene)=>{ needsToSerialize=false; };
			EditorApplication.playModeStateChanged += (PlayModeStateChange state)=>{
				playModeState=state;
				if(playModeState==PlayModeStateChange.EnteredPlayMode) needsToSerialize=false;
				if(playModeState==PlayModeStateChange.ExitingPlayMode) needsToSerialize=false;
				if(playModeState==PlayModeStateChange.ExitingEditMode) needsToSerialize=true;
			};
		}
		
		private static PlayModeStateChange playModeState;
		private static bool inCorrectMode { get {
			if(playModeState==PlayModeStateChange.EnteredEditMode) return true;
			if(playModeState==PlayModeStateChange.EnteredPlayMode) return false;
			if(playModeState==PlayModeStateChange.ExitingPlayMode) return false;
			if(playModeState==PlayModeStateChange.ExitingEditMode) return true;
			// TryCatch since (de)serialize can be ran off the main thread when entering and exiting play mode
			try{ if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused) return false; }catch(Exception){ return false; }
			return true;
		} }
		
		public delegate bool CallbackCreateRefsOnSerialize(ComponentReference compRef);
		[NonSerialized] public CallbackCreateRefsOnSerialize createRefsOnSerialize = null;
		
		[SerializeField] public bool modelIsFresh = true;
		[SerializeField] public bool modelWasDeserialized = false;
		[SerializeField] public bool modelWasSerialized = false;
		void ISerializationCallbackReceiver.OnAfterDeserialize(){
			//Debug.Log("OnAfterDeserialize playModeState:"+playModeState+" inCorrectMode:"+inCorrectMode+" needsToSerialize:"+needsToSerialize+" hasComponent:"+(_component!=null));
			if(!inCorrectMode || this.modelWasDeserialized) return;
			this.modelWasDeserialized=true; this.modelWasSerialized=false; this.modelIsFresh=false;
			if(_component==null) EditorApplication.delayCall += ()=>ResolveComponent();
		}
		void ISerializationCallbackReceiver.OnBeforeSerialize(){
			//Debug.Log("OnBeforeSerialize playModeState:"+playModeState+" inCorrectMode:"+inCorrectMode+" needsToSerialize:"+needsToSerialize+" hasComponent:"+(_component!=null));
			if(!inCorrectMode || this.modelWasSerialized || !needsToSerialize) return;
			this.modelWasDeserialized=false; this.modelWasSerialized=true; this.modelIsFresh=false;
			if(_component!=null){
				bool serialize = true;
				if(createRefsOnSerialize!=null) serialize = createRefsOnSerialize(this);
				if(serialize) SetComponent(_component,createReferences:true);
			}
		}
		
		public ComponentReference With(GameObject obj) => this._gameObj==obj ? this : null;
		public ComponentReference With(Type type) => this._type==type ? this : null;
		public ComponentReference With(Component comp) => this.component==comp ? this : null;
		
		// Implicit Operators
		public static implicit operator Type(ComponentReference cr) => cr?._type;
		public static implicit operator GameObject(ComponentReference cr) => cr?._gameObj;
		public static implicit operator Component(ComponentReference cr) => cr?.component;
		public static implicit operator ComponentReference(Component c) => c==null ? null : new ComponentReference(){ component = c };
		public static implicit operator bool(ComponentReference cr) => cr?.component!=null;
		
		// Collection Methods
		public bool isInList(List<ComponentReference> list){
			if(!this || this is null || list is null) return false;
			foreach(ComponentReference comp in list){ if(comp && comp is not null && this.Equals(comp)) return true; }
			return false;
		}
		public bool removeFromList(List<ComponentReference> list){
			if(!this || this is null || list is null) return false;
			foreach(ComponentReference comp in list.ToArray()){ if(comp && comp is not null && this.Equals(comp)) list.Remove(comp); }
			return false;
		}
		
		// Equals Operators
		public bool Equals(ComponentReference value=null) => (this is null && value is null)
			|| (this is not null && value is not null && _component!=null && value?._component!=null && _component==value?._component)
			|| (this is not null && value is not null && _gameObj==value?._gameObj && _type==value?._type && _compCount==value?._compCount && _compIndex!=0 && value?._compIndex!=0 && _compIndex==value?._compIndex);
		public override bool Equals(object value=null) => (this is null && value is null)
			|| (value is ComponentReference @obj && Equals(@obj))
			|| (this is not null && value is not null && value is Component @comp && @comp.Equals(component))
			|| (this is not null && value is not null && value is Type @type && @type.Equals(_type))
			|| (this is not null && value is not null && value is GameObject @gObj && @gObj.Equals(_gameObj));
		public override int GetHashCode() => (this.GetType(), _gameObj, _type, _compCount, _compIndex).GetHashCode();
		#nullable enable
		public static bool operator ==(ComponentReference? a, ComponentReference? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as ComponentReference));
		public static bool operator !=(ComponentReference? a, ComponentReference? b) => !(a==b);
		public static bool operator ==(Component? a, ComponentReference? b) => (a is null && b is null) || (a is not null && b is not null && b.Equals(a as object));
		public static bool operator !=(Component? a, ComponentReference? b) => !(a==b);
		public static bool operator ==(ComponentReference? a, Component? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as object));
		public static bool operator !=(ComponentReference? a, Component? b) => !(a==b);
		public static bool operator ==(ComponentReference? a, object? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as object));
		public static bool operator !=(ComponentReference? a, object? b) => !(a==b);
		#nullable disable
		public override string ToString() => this?.component?.ToString() ?? this?.componentType?.ToString();
		
		#else
		void ISerializationCallbackReceiver.OnAfterDeserialize(){}
		void ISerializationCallbackReceiver.OnBeforeSerialize(){}
		public bool Equals(ComponentReference value=null) => false;
		#endif
		
	}
	
}
