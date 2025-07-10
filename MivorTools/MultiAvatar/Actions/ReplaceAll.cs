
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using VRC;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace MivorTools.MultiAvatar {

    namespace ActionList {
		
		[Serializable]
        public class ReplaceAll: Action {
			
			[SerializeField] public int modelVersion = 1;
			public static new bool Multiple => true;
			public static new int ProcessOrder => 101;
			public static new string Name => "Replace All";
			public static new string Description => "Replace all instances of a material or asset";
			public static new ActionSource[] AllowedSources => new ActionSource[]{ ActionSource.ManagedAvatar };
			
			[SerializeReference] public UnityEngine.Object oldObject = null;
			[SerializeReference] public UnityEngine.Object newObject = null;
			
			#if UNITY_EDITOR
			
			public override bool ActionValidate(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				if(!originalObj || !clonedObj || clonedObj!=gameObject || !isEnabled) return false;
				//if(!managedAvatars.Contains(managedAvatar)) return true;
				if(oldObject==null) return false;
				//if(newObject==null) return false;
				if(newObject!=null && oldObject.GetType()!=newObject.GetType() && !oldObject.GetType().IsSubclassOf(newObject.GetType()) && !newObject.GetType().IsSubclassOf(oldObject.GetType())) return false;
				if(oldObject==newObject) return false;
				return true;
			}
			public override bool ActionProcess(ManagedAvatar managedAvatar,GameObject originalObj,GameObject clonedObj){
				Debug.Log("ReplaceAll ActionProcess");
				if(!ActionValidate(managedAvatar,originalObj,clonedObj)) return false;
				// Check on cloned avatar here
				List<Callback> callbacks = new List<Callback>();
				FindReferences(managedAvatar,clonedObj,
					createSetCB: true,
					onReference: (ReflectHelpers.Result result,Callback setNew)=>callbacks.Add(setNew)
				);
				// Perform Replacements
				foreach(Callback callback in callbacks) callback();
				return true;
			}
			
			public static new bool BatchProcess => true;
			public static new bool ActionBatchProcess(ManagedAvatar managedAvatar,GameObject mainAvatar,GameObject clonedAvatar,List<Action> actions){
				Debug.Log("ReplaceAll ActionBatchProcess");
				if(actions.Count==0) return false;
				var replacements = new List<(ReplaceAll action,UnityEngine.Object oldObject,UnityEngine.Object newObject)>(){};
				foreach(Action action in actions){
					if(action is ReplaceAll rAction){
						if(!rAction.ActionValidate(managedAvatar,mainAvatar,clonedAvatar)) continue;
						replacements.Add((rAction,rAction.oldObject,rAction.newObject));
					}
				}
				// Temp
				foreach(var r in replacements) r.action.ActionProcess(managedAvatar,mainAvatar,clonedAvatar);
				return true;
			}
			
			public override string GetActionInfoString(){
				return "Finding '"+oldObject?.name+"'";
			}
			
			public delegate void Callback();
			public delegate void CallbackOnReference(ReflectHelpers.Result result,Callback setNew=null);
			public bool FindReferences(ManagedAvatar managedAvatar,GameObject targetObj,CallbackOnReference onReference,bool createSetCB=false){
				try{
					Type findType = oldObject.GetType();
					if(!ReflectHelpers.typeWhitelist.Contains(findType)) ReflectHelpers.typeWhitelist.Add(findType);
					//if(!ReflectHelpers.typeSkipDeepScan.Contains(findType)) ReflectHelpers.typeSkipDeepScan.Add(findType);
					object findValue = oldObject;
					ReflectHelpers.RecursiveScan(targetObj,targetObj,findType,findValue,null,(ReflectHelpers.Result result,int listIndex)=>{
						if(result.parentObj is Action){ return; }
						//Debug.Log("ReplaceAll Action: Found: "+(listIndex>=0?" (index "+listIndex+")":"")+": "+result);
						//if(!result.isWritable()){ Debug.LogWarning("Not writable, skipping."); return; }
						// TODO: go through log and add some types to ignore
						Callback setNewCB = null;
						if(createSetCB) setNewCB = ()=>{
							if(result.parentObj.GetType().ToString().StartsWith("VF.Model.Guid")){
								try{
									PropertyInfo setterProp = result.parentObj.GetType().GetProperty("setter",BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.SetProperty);
									setterProp.SetValue(result.parentObj,newObject);
								}catch(Exception e){
									Debug.LogWarning("ReplaceAll Action: Error setting VFGuidWrapper value on "+result+"\n"+e.Message);
									Debug.LogException(e);
								}
							}
							else if(listIndex==-1){
								try{
									result.setValue(newObject);
								}catch(Exception e){
									Debug.LogWarning("ReplaceAll Action: Error setting value on "+result+"\n"+e.Message);
									Debug.LogException(e);
								}
							}
							else if(result.isList){
								try{
									result.setValueListEntry(newObject,listIndex);
								}catch(Exception e){
									Debug.LogWarning("ReplaceAll Action: Error setting list value "+listIndex+" on "+result+"\n"+e.Message);
									Debug.LogException(e);
								}
							}
							else if(result.isArray){
								try{
									result.setValueListEntry(newObject,listIndex);
								}catch(Exception e){
									Debug.LogWarning("ReplaceAll Action: Error setting array value "+listIndex+" on "+result+"\n"+e.Message);
									Debug.LogException(e);
								}
							}
							else return;
							Debug.Log("ReplaceAll Action: Updated: "+(listIndex>=0?" (index "+listIndex+")":"")+": "+result);
						};
						onReference(result,setNewCB);
					});
				}catch(Exception e){
					Debug.LogException(e);
				}
				return false;
			}
			
			public class ReflectHelpers {
				
				public static List<Type> typeWhitelist = new List<Type>{
					typeof(Component), typeof(GameObject),
					typeof(SkinnedMeshRenderer), typeof(MeshRenderer), typeof(Renderer),
						typeof(Mesh), typeof(Material),
					typeof(Animator),
						typeof(Avatar), typeof(AnimatorController),
					typeof(AnimationClip),
				};
				public static List<Type> typeSkipDeepScan = new List<Type>{
					typeof(Transform),
					typeof(Mesh), typeof(Material), typeof(AnimationClip),
					typeof(Avatar), typeof(AnimatorController),
					typeof(UnityEngine.Shader),
					typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu), typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters),
					typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone),
				};
				public static List<string> typeStrPrefixWhitelist = new List<string>{
					"System.Collections.", "System.Linq.",
					"MivorTools.", "VRC.SDK",
					"VF.Model.", "d4rkAvatarOptimizer",
				};
				public static List<string> typeStrWhitelist = new List<string>{
					"MivorTools.Placeholder-ReplaceAll"
				};
				// Whitelist props, as accessing some props can cause unwanted destructive side-effects
				public static Dictionary<Type, List<string>> propWhitelistDict = new Dictionary<Type, List<string>>(){
					{ typeof(SkinnedMeshRenderer), new List<string>{ "sharedMaterials", "sharedMesh", "rootBone" } },
				};
				
				public static bool isTypeWhitelisted(Type type,bool fromSelf=false){
					string typeStr = type.ToString();
					if(typeWhitelist.Contains(type)) return true;
					if(typeWhitelist.Any(t=>type.IsSubclassOf(t))) return true;
					if(typeStrWhitelist.Contains(typeStr)) return true;
					if(typeStrPrefixWhitelist.Any(str=>typeStr.StartsWith(str))) return true;
					if(fromSelf) return false;
					Type type2 = null;
					if(type.IsArray){ type2 = type.GetElementType(); }
					foreach(Type type3 in type.GetInterfaces()) {
						if(type3.IsGenericType && type3.GetGenericTypeDefinition()==typeof(IList<>)){ type2 = type3.GetGenericArguments()[0]; break; }
					}
					if(type2!=null) return isTypeWhitelisted(type2,true);
					//Debug.Log("Type not whitelisted: "+type);
					return false;
				}
				
				public delegate void CallbackResult(Result result,int arrayIndex);
				public static void RecursiveScan(object restrictWithin,object targetObject,Type findType,object findValue,List<object> checkedList,CallbackResult onResult){
					if(targetObject is null || targetObject+""=="null") return;
					Type type = targetObject.GetType();
					if(checkedList==null) checkedList = new List<object>();
					//if(targetObject is Component){}
					{
						if(checkedList.Contains(targetObject)){ return; }
						checkedList.Add(targetObject);
					}
					if(!isTypeWhitelisted(type)){ return; }
					if(typeSkipDeepScan.Contains(type)){ return; }
					if(restrictWithin is GameObject @gObj1 && targetObject is GameObject @gObj2 && @gObj1!=@gObj2){
						if(@gObj1 is null || @gObj2 is null || !MiscUtils.doesGameObjectHaveHierarchyParent(@gObj2,@gObj1)){ return; }
					}
					//Debug.Log("------------------------------------------------------------------------------------------------------");
					//Debug.Log("RecursiveScan: targetObject:"+targetObject);
					if(propWhitelistDict.ContainsKey(type)){
						foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.SetProperty)){
							if(!propWhitelistDict[type].Contains(prop.Name)) continue;
							//if(prop.PropertyType.IsSubclassOf(typeof(Component))) continue;
							//Debug.Log("Check Field: targetObject:"+targetObject+" -- prop:"+prop+" -- propName:"+prop.Name+" -- propType:"+prop.PropertyType);
							ScanPropField(restrictWithin,targetObject,findType,findValue,checkedList,prop,null,onResult);
						}
					}
					foreach(FieldInfo field in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.SetField)){
						//if(!isTypeWhitelisted(field.FieldType)) continue;
						//if(typeSkipDeepScan.Contains(field.FieldType)) continue;
						//Debug.Log("Check Prop: targetObject:"+targetObject+" -- field:"+field+" -- fieldName:"+field.Name+" -- fieldType:"+field.FieldType);
						ScanPropField(restrictWithin,targetObject,findType,findValue,checkedList,null,field,onResult);
					}
					if(targetObject is GameObject @gObj3){
						foreach(Component comp in @gObj3.GetComponents<Component>()){
							//Debug.Log("Check Component: targetObject:"+targetObject+" -- comp:"+comp+"");
							RecursiveScan(restrictWithin,comp,findType,findValue,checkedList,onResult);
						}
						foreach(GameObject childObj in MiscUtils.getGameObjectChildrenArray(@gObj3)){
							//Debug.Log("Check Child GameObject: "+targetObject+" -> "+childObj);
							RecursiveScan(restrictWithin,childObj,findType,findValue,checkedList,onResult);
						}
					}
				}
				
				public static void ScanPropField(object restrictWithin,object targetObject,Type findType,object findValue,List<object> checkedList,PropertyInfo prop,FieldInfo field,CallbackResult onResult){
					Type type = prop!=null ? prop.PropertyType : field.FieldType;
					string typeStr = type.ToString();
					Type typeList = type;
					if(typeList.IsArray){ typeList = typeList.GetElementType(); }
					foreach(Type type3 in type.GetInterfaces()) {
						if(type3.IsGenericType && type3.GetGenericTypeDefinition()==typeof(IList<>)){ typeList = type3.GetGenericArguments()[0]; break; }
					}
					if(!isTypeWhitelisted(type) && !isTypeWhitelisted(typeList)){ return; }
					if(type==typeList && typeSkipDeepScan.Contains(type)){ return; }
					// Check Attributes
					if(type.GetCustomAttributes(true).OfType<ObsoleteAttribute>().Count()>0){ return; } // Deprecated
					if(type!=typeList) if(typeList.GetCustomAttributes(true).OfType<ObsoleteAttribute>().Count()>0){ return; } // Deprecated
					// Workarounds
					{
						bool isVFGuidWrapper = false;
						if(typeStr=="VF.Model.GuidMaterial" && findType==typeof(Material)) isVFGuidWrapper = true;
						if(typeStr=="VF.Model.GuidAnimationClip" && findType==typeof(AnimationClip)) isVFGuidWrapper = true;
						if(typeStr=="VF.Model.GuidTexture2d" && findType==typeof(Texture2D)) isVFGuidWrapper = true;
						if(typeStr=="VF.Model.GuidController" && findType==typeof(RuntimeAnimatorController)) isVFGuidWrapper = true;
						if(typeStr=="VF.Model.GuidMenu" && findType==typeof(VRCExpressionsMenu)) isVFGuidWrapper = true;
						if(typeStr=="VF.Model.GuidParams" && findType==typeof(VRCExpressionParameters)) isVFGuidWrapper = true;
						if(isVFGuidWrapper) try{
							FieldInfo objField = type.GetField("objRef",BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.GetField);
							//Debug.Log("Force Workaround Result: type:"+type+" -- type2:"+type2+" -- propName:"+(prop!=null?prop.Name:"")+" -- fieldName:"+(field!=null?field.Name:"")+"");
							Result prevResult = CreateResult(prop,field,type,typeList,targetObject);
							object newTarget = prevResult.getValue();
							if(objField!=null){
								Type newType = objField.FieldType;
								Type newTypeList = newType;
								if(newTypeList.IsArray){ newTypeList = newTypeList.GetElementType(); }
								foreach(Type type3 in newType.GetInterfaces()) {
									if(type3.IsGenericType && type3.GetGenericTypeDefinition()==typeof(IList<>)){ newTypeList = type3.GetGenericArguments()[0]; break; }
								}
								//Debug.Log("Workaround Result: newTarget:"+newTarget+" type:"+newType+" -- type2:"+newTypeList+" -- propName:"+(prop!=null?prop.Name:"")+" -- fieldName:"+(objField!=null?objField.Name:"")+"");
								Result result = CreateResult(null,objField,newType,newTypeList,newTarget);
								//result.parentResult = CreateResult(null,field,type,typeList,targetObject);
								if(result!=null) ForEachResultValue(result,(object value,int index)=>{
									//Debug.Log("Workaround Value "+index+": "+value);
									if(CheckFoundValue(findValue,value)) onResult(result,index);
								});
							}
							return;
						}catch(Exception e){
							Debug.LogException(e);
							return;
						}
					}
					// Check Type
					Type foundType = null;
					if(
						findType==type
						|| findType.IsAssignableFrom(type) || type.IsAssignableFrom(findType)
						|| findType.IsEquivalentTo(type) || type.IsEquivalentTo(findType)
						|| findType.IsSubclassOf(type) || type.IsSubclassOf(findType)
						|| findType.Equals(type) || type.Equals(findType)
						|| findType.IsInstanceOfType(type) || type.IsInstanceOfType(findType)
					){ foundType = type; }
					if(type!=typeList &&
						(findType==typeList
						|| findType.IsAssignableFrom(typeList) || typeList.IsAssignableFrom(findType)
						|| findType.IsEquivalentTo(typeList) || typeList.IsEquivalentTo(findType)
						|| findType.IsSubclassOf(typeList) || typeList.IsSubclassOf(findType)
						|| findType.Equals(typeList) || typeList.Equals(findType)
						|| findType.IsInstanceOfType(typeList) || typeList.IsInstanceOfType(findType)
					)){ foundType = typeList; }
					// Check Result
					try{
						//Debug.Log("Result: targetObject:"+targetObject+" -- type:"+type+" -- type2:"+type2+" -- foundType:"+foundType+" -- propName:"+(prop!=null?prop.Name:"")+" -- fieldName:"+(field!=null?field.Name:"")+"");
						Result result = CreateResult(prop,field,type,typeList,targetObject);
						if(result!=null) ForEachResultValue(result,(object value,int index)=>{
							//Debug.Log("Value "+index+": "+value);
							if(CheckFoundValue(findValue,value)) onResult(result,index);
							else {
								RecursiveScan(restrictWithin,value,findType,findValue,checkedList,onResult);
							}
						});
						//else Debug.Log("Value not found -- prop:"+prop);
					}catch(Exception e){
						Debug.LogException(e);
						return;
					}
				}
				
				public static bool CheckFoundValue(object findValue,object value){
					if(findValue.Equals(value)) return true;
					Type type = findValue.GetType();
					if(type==value.GetType() && findValue+""==""+value){
						if(findValue is UnityEngine.Object @fv && findValue is UnityEngine.Object @v){
							if(@fv==@v || @fv.GetInstanceID()==@v.GetInstanceID()) return true;
						}
					}
					return false;
				}
				
				#nullable enable
				public delegate void CallbackResultValue(object? value,int index);
				#nullable disable
				public static void ForEachResultValue(Result result,CallbackResultValue callback){
					try{
						if(result.isArray){
							int i=0; foreach(var value in result.getValuesList()){ callback(value,i); i++; }
						}
						else if(result.isList){
							int i=0; foreach(var value in result.getValuesList()){ callback(value,i); i++; }
						}
						else callback(result.getValue(),-1);
					}
					catch(ArgumentException){}
					catch(NotSupportedException){}
					catch(TargetParameterCountException){}
				}
				
				public static Result CreateResult(PropertyInfo prop,FieldInfo field,Type type,Type typeList,object parentObj,bool checkValue=true){
					try{
						if(BlacklistedTypeFieldProp(type,prop!=null ? prop.Name : field.Name)) return null;
						if(type!=typeList) if(BlacklistedTypeFieldProp(typeList,prop!=null ? prop.Name : field.Name)) return null;
						if(type.ToString().StartsWith("System.Collections.Generic.Dictionary")) return null; // Not yet implemented here
						Result result = new Result { name=prop!=null ? prop.Name : field.Name, type=type, typeList=typeList, fieldInfo=field, propInfo=prop, parentObj=parentObj };
						if(typeList!=type && type.IsArray){
							result.isArray = true;
							if(checkValue && result.getValuesList()==null) return null; // Check for null or Exception
						}
						else if(typeof(IList).IsAssignableFrom(type)){
							result.isList = true;
							if(checkValue && result.getValuesList()==null) return null; // Check for null or Exception
						}
						else {
							if(checkValue && result.getValue()==null) return null; // Check for null or Exception
						}
						return result;
					}
					catch(ArgumentException){}
					catch(NotSupportedException){}
					catch(TargetParameterCountException){}
					return null;
				}
				
				// TryCatch does not catch these, so hardcode blacklist them
				public static bool BlacklistedTypeFieldProp(Type type,string name){
					string typeStr = type.ToString();
					if(typeStr=="UnityEngine.Component" && name=="rigidbody") return true;
					if(typeStr=="UnityEngine.Component" && name=="rigidbody2D") return true;
					if(typeStr=="UnityEngine.Component" && name=="camera") return true;
					if(typeStr=="UnityEngine.Component" && name=="light") return true;
					if(typeStr=="UnityEngine.Component" && name=="animation") return true;
					if(typeStr=="UnityEngine.Component" && name=="constantForce") return true;
					if(typeStr=="UnityEngine.Component" && name=="renderer") return true;
					if(typeStr=="UnityEngine.Component" && name=="audio") return true;
					if(typeStr=="UnityEngine.Component" && name=="networkView") return true;
					if(typeStr=="UnityEngine.Component" && name=="collider") return true;
					if(typeStr=="UnityEngine.Component" && name=="collider2D") return true;
					if(typeStr=="UnityEngine.Component" && name=="hingeJoint") return true;
					if(typeStr=="UnityEngine.Component" && name=="particleSystem") return true;
					if(typeStr=="UnityEngine.Transform[]" && name=="bones") return true; // can be too many, and we don't need these
					return false;
				}
				
				public class Result {
					public Result parentResult = null;
					public string name = null;
					public bool isArray = false;
					public bool isList = false;
					public Type type = null;
					public Type typeList = null;
					public PropertyInfo propInfo = null;
					public FieldInfo fieldInfo = null;
					public object parentObj = null;
					public override string ToString(){
						return ""+parentObj+" -> "+name+" ("+(fieldInfo!=null?"field":"prop")+") ("+(isArray?"array":(isList?"list":"value"))+") "+typeList.ToString()+"";
					}
					public IList getValuesList(){
						if(!isArray && !isList) return null;
						if(fieldInfo!=null) return fieldInfo.GetValue(parentObj) as IList;
						if(propInfo!=null) return propInfo.GetValue(parentObj) as IList;
						return null;
					}
					public object getValue(){
						if(isArray || isList) return null;
						if(propInfo!=null) return propInfo.GetValue(parentObj);
						else if(fieldInfo!=null) return fieldInfo.GetValue(parentObj);
						return null;
					}
					public void setValue(object value){
						if(isArray || isList) return;
						if(propInfo!=null) propInfo.SetValue(parentObj,value);
						else if(fieldInfo!=null) fieldInfo.SetValue(parentObj,value);
					}
					public void setValueListEntry(object value,int index){
						if(!isArray && !isList) return;
						IList current = getValuesList();
						if(index<0 || index>=current.Count) return;
						current[index] = value;
						if(propInfo!=null) propInfo.SetValue(parentObj,current);
						else if(fieldInfo!=null) fieldInfo.SetValue(parentObj,current);
					}
					public bool isWritable(){
						if(propInfo!=null && !propInfo.CanWrite) return true;
						if(fieldInfo!=null && !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral) return true;
						return false;
					}
				}
				
			}
			
			#endif
		}

	}

}
