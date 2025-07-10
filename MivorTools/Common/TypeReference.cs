
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MivorTools {
	
    [Serializable]
	public class TypeReference : UnityEngine.ISerializationCallbackReceiver, IEquatable<TypeReference> {
		void ISerializationCallbackReceiver.OnAfterDeserialize(){}
		void ISerializationCallbackReceiver.OnBeforeSerialize(){}
		
		[SerializeField] public int modelVersion = 1;
		[NonSerialized] public Type _type = null;
		[SerializeField] public string _typeFullName = null;
		
		#if UNITY_EDITOR
		public Type componentType {
			get => _type ??= (componentTypeNameFull!=null ? FindType(componentTypeNameFull) : null);
			set {
				_type = value;
				_typeFullName = _type?.FullName;
			}
		}
		public string componentTypeNameFull => _typeFullName;
		
		public static Type FindType(string name) => Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
		
		// Implicit Operators
		public static implicit operator Type(TypeReference tr) => tr?.componentType;
		public static implicit operator TypeReference(Type t) => t==null ? null : new TypeReference(){ componentType = t };
		public static implicit operator bool(TypeReference tr) => tr?.componentType!=null;
		
		// Collection Methods
		public bool isInList(List<TypeReference> list){
			if(!this || this is null || list is null) return false;
			foreach(TypeReference type in list){ if(type && type is not null && this.Equals(type)) return true; }
			return false;
		}
		public bool removeFromList(List<TypeReference> list){
			if(!this || this is null || list is null) return false;
			foreach(TypeReference type in list.ToArray()){ if(type && type is not null && this.Equals(type)) list.Remove(type); }
			return false;
		}
		
		// Equals Operators
		public bool Equals(TypeReference value=null) => (this is null && value is null)
			|| (this is not null && value is not null && this._type!=null && value?._type!=null && this._type==value?._type)
			|| (this is not null && value is not null && this._typeFullName==value._typeFullName);
		public override bool Equals(object value=null) => (this is null && value is null)
			|| (value is TypeReference @obj && Equals(@obj))
			|| (this is not null && value is not null && value is Type @type && this._typeFullName==type.FullName);
		public override int GetHashCode() => (this.GetType(), this._typeFullName).GetHashCode();
		#nullable enable
		public static bool operator ==(TypeReference? a, TypeReference? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as TypeReference));
		public static bool operator !=(TypeReference? a, TypeReference? b) => !(a==b);
		public static bool operator ==(Type? a, TypeReference? b) => (a is null && b is null) || (a is not null && b is not null && b.Equals(a as object));
		public static bool operator !=(Type? a, TypeReference? b) => !(a==b);
		public static bool operator ==(TypeReference? a, Type? b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as object));
		public static bool operator !=(TypeReference? a, Type? b) => !(a==b);
		public static bool operator ==(TypeReference? a, object b) => (a is null && b is null) || (a is not null && b is not null && a.Equals(b as object));
		public static bool operator !=(TypeReference? a, object b) => !(a==b);
		#nullable disable
		
		public override string ToString() => this.componentType?.ToString() ?? componentTypeNameFull;
		
		#else
		public bool Equals(TypeReference value=null) => false;
		#endif
	}
	
	public static class TypeReference_ExtensionMethods {
		
	}
	
}
