
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using HarmonyLib;
using System.Reflection;
using ReloadCB = UnityEditor.AssemblyReloadEvents.AssemblyReloadCallback;

namespace MivorTools {
	
	public class UnityEditorPatcher {
		
		private static Dictionary<string,Harmony> harmonyDict = new Dictionary<string,Harmony>(){};
		private static Dictionary<string,ReloadCB> unpatchDict = new Dictionary<string,ReloadCB>(){};
		
		// https://harmony.pardeike.net/articles/patching.html
		public static Harmony SetupHarmonyPatcher(string harmonyNS=""){
			string harmonyID = GenerateHarmonyID(harmonyNS);
			Harmony harmony = null;
			if(harmonyDict.ContainsKey(harmonyID)) harmony = harmonyDict[harmonyID];
			else {
				//UnityEngine.Debug.Log("SetupHarmonyPatcher: "+harmonyID);
				harmony = new Harmony(harmonyID);
				harmonyDict.Add(harmonyID,harmony);
			}
			if(!unpatchDict.ContainsKey(harmonyID)){
				ReloadCB cb = ()=>harmony.UnpatchAll(harmonyID);
				AssemblyReloadEvents.beforeAssemblyReload += cb;
				unpatchDict.Add(harmonyID,cb);
			}
			return harmony;
		}
		public static void UnpatchHarmony(string harmonyNS=""){
			string harmonyID = GenerateHarmonyID(harmonyNS);
			//UnityEngine.Debug.Log("UnpatchHarmony: "+harmonyID);
			if(harmonyDict.ContainsKey(harmonyID)){
				harmonyDict[harmonyID].UnpatchAll(harmonyID);
			}
			if(unpatchDict.ContainsKey(harmonyID)){
				ReloadCB cb = unpatchDict[harmonyID];
				AssemblyReloadEvents.beforeAssemblyReload -= cb;
				unpatchDict.Remove(harmonyID);
			}
		}
		public static string GenerateHarmonyID(string harmonyNS){
			return "mivortools.unityeditorpatcher:"+(harmonyNS==""||harmonyNS==null?"default":harmonyNS);
		}
		
		public enum PatchType : ushort { Nothing=0, Prefix=1, Postfix=2, Transpiler=3, Finalizer=4 }
		public static void ApplyPatch(string TypeName,string MethodName,PatchType patchType,Type targetType,string targetMethodName,string harmonyNS=""){
			Harmony harmony = SetupHarmonyPatcher(harmonyNS);
			var method = HarmonyLib.AccessTools.Method(HarmonyLib.AccessTools.TypeByName(TypeName),MethodName);
			if(patchType==PatchType.Prefix) harmony.Patch(method,prefix:new HarmonyMethod(targetType,targetMethodName));
			else if(patchType==PatchType.Postfix) harmony.Patch(method,postfix:new HarmonyMethod(targetType,targetMethodName));
			else if(patchType==PatchType.Transpiler) harmony.Patch(method,transpiler:new HarmonyMethod(targetType,targetMethodName));
			else if(patchType==PatchType.Finalizer) harmony.Patch(method,finalizer:new HarmonyMethod(targetType,targetMethodName));
		}
		public static void ApplyPatch(MethodBase method,PatchType patchType,MethodInfo targetMethod,string harmonyNS=""){
			Harmony harmony = SetupHarmonyPatcher(harmonyNS);
			if(patchType==PatchType.Prefix) harmony.Patch(method,prefix:new HarmonyMethod(targetMethod));
			else if(patchType==PatchType.Postfix) harmony.Patch(method,postfix:new HarmonyMethod(targetMethod));
			else if(patchType==PatchType.Transpiler) harmony.Patch(method,transpiler:new HarmonyMethod(targetMethod));
			else if(patchType==PatchType.Finalizer) harmony.Patch(method,finalizer:new HarmonyMethod(targetMethod));
		}
		public static void ApplyPatchWithGenericTypes(MethodInfo methodOriginal,PatchType patchType,MethodInfo methodPatch,Type[] types,string harmonyNS=""){
			methodOriginal = methodOriginal.GetGenericMethodDefinition();
			methodPatch = methodPatch.GetGenericMethodDefinition();
			foreach(Type t in types) ApplyPatch(methodOriginal.MakeGenericMethod(t),patchType,methodPatch.MakeGenericMethod(t),harmonyNS);
		}
		
	}
	
}

#endif
