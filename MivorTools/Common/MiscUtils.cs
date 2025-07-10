
#if UNITY_EDITOR
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace MivorTools {

	public partial class MiscUtils {
		
		public static bool doesTransformHaveHierarchyParent(Transform transformCheck,Transform transformParent){
			Transform transform = transformCheck;
			while(transform=transform.parent){
				if(!transform) break;
				if(transform==transformParent) return true;
			}
			return false;
		}
		public static bool doesGameObjectHaveHierarchyParent(GameObject gameObjToCheck,GameObject gameObjParent) => doesTransformHaveHierarchyParent(gameObjToCheck.transform,gameObjParent.transform);
		
		public static int getParentGapCount(Transform transformCheck,Transform transformParent){
			Transform transform = transformCheck;
			int count = 0;
			while(transform=transform.parent){
				if(!transform) break;
				if(transform==transformParent) return count;
				count++;
			}
			return count;
		}
		
		public static Transform[] getTransformParentArray(Transform transformCheck){
			List<Transform> arr = new List<Transform>{};
			Transform transform = transformCheck;
			while(transform=transform.parent){
				if(!transform) break;
				arr.Add(transform);
			}
			return arr.ToArray();
		}
		
		public delegate void Callback();
		
		public static Transform[] getTransformChildrenArray(Transform transformParent){
			Transform[] arr = new Transform[transformParent.childCount];
			for(int i=0; i<transformParent.childCount; i++){
				Transform child = transformParent.GetChild(i);
				arr[i] = child;
			}
			return arr;
		}
		public static GameObject[] getGameObjectChildrenArray(GameObject gameObjParent){
			GameObject[] arr = new GameObject[gameObjParent.transform.childCount];
			for(int i=0; i<gameObjParent.transform.childCount; i++){
				Transform child = gameObjParent.transform.GetChild(i);
				arr[i] = child.gameObject;
			}
			return arr;
		}
		
		public delegate bool CallbackTransform(Transform transform);
		
		public static void forEachTransformChildren(Transform transformParent,bool transverse,CallbackTransform callback){
			foreach(Transform child in getTransformChildrenArray(transformParent)){
				bool transverseChildren = callback(child);
				if(transverse && transverseChildren && child.childCount>0) forEachTransformChildren(child,true,callback);
			}
		}
		
		public static void forEachTransformParent(Transform transformStart,CallbackTransform callback){
			Transform transform = transformStart;
			while(transform=transform.parent){
				if(!transform) break;
				bool transverse = callback(transform);
				if(!transverse) break;
			}
		}
		
		public delegate bool CallbackGameObject(GameObject gameObj);
		
		public static void forEachGameObjectChildren(GameObject gameObjParent,bool transverse,CallbackGameObject callback){
			forEachTransformChildren(gameObjParent.transform,transverse,(transform)=>callback(transform.gameObject));
		}
		
		public static void forEachGameObjectParent(GameObject gameObjStart,CallbackGameObject callback){
			forEachTransformParent(gameObjStart.transform,(transform)=>callback(transform.gameObject));
		}
		
		public delegate bool CallbackTransformDuo(Transform transform1,Transform transform2);
		
		public static void forEachTransformSame(Transform transformParent1,Transform transformParent2,bool transverse,CallbackTransformDuo callback){
			if(transformParent1.childCount!=transformParent2.childCount) return;
			for(int i=0; i<transformParent1.childCount; i++){
				Transform child1 = transformParent1.GetChild(i);
				Transform child2 = null;
				if(!child2){
					Transform t = transformParent2.GetChild(i);
					if(t.gameObject.name==child1.gameObject.name) child2 = t;
				}
				if(!child2) continue;
				bool transverseChildren = callback(child1,child2);
				if(transverse && transverseChildren && child1.childCount>0) forEachTransformSame(child1,child2,true,callback);
			}
		}
		
		public static string GetComponentTitle(Component comp){
			string compTypePath = comp.GetType().FullName;
			string compTypeName = comp.GetType().Name;
			if(compTypePath=="VF.Model.VRCFury"){
				FieldInfo vrcfField = comp.GetType().GetField("content",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.GetField|BindingFlags.FlattenHierarchy);
				if(vrcfField!=null){
					object vrcfContent = vrcfField.GetValue(comp);
					if(vrcfContent!=null) return "VRCFury: "+vrcfContent.GetType().Name;
				}
			}
			UnityEditor.Editor compEditor = null;
			UnityEditor.Editor.CreateCachedEditor(comp,null,ref compEditor);
			if(compEditor){
				PropertyInfo prop = typeof(UnityEditor.Editor).GetProperty("targetTitle",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.GetProperty);
				if(prop!=null) return prop.GetValue(compEditor,null) as string;
			}
			return compTypeName;
		}
		
	}
	
}

#endif
