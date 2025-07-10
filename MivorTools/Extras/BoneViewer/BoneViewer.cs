
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MivorTools.Extras.BoneViewer {
	
	[AddComponentMenu("MivorTools/Extras/Bone Viewer",1)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	public class BoneViewer : MonoBehaviour
		#if VRC_SDK_VRCSDK3
		, VRC.SDKBase.IEditorOnly
		#endif
		{
		
		[SerializeField] public bool firstTimeGUI = true;
		[SerializeField] public bool showThisRendererOnBones = true;
		[SerializeField] public bool isAutoCreated = false;
		
		#if UNITY_EDITOR
		
		[NonSerialized] public static Dictionary<Transform,List<Component>> cachedBoneRenderers = new Dictionary<Transform,List<Component>>();
		[NonSerialized] public static Dictionary<SkinnedMeshRenderer,List<Transform>> cachedBoneWeights = new Dictionary<SkinnedMeshRenderer,List<Transform>>();
		[NonSerialized] public static List<GameObject> cachedCheckedNoBones = new List<GameObject>();
		[NonSerialized] public static List<BoneViewer> cachedOnBoneViewers = new List<BoneViewer>();
		
		[NonSerialized] public List<Component> uiExpandRenderer = new List<Component>();
		[NonSerialized] public List<(Component comp,GameObject bone)> uiExpandBone = new List<(Component,GameObject)>();
		
		public static void clearCache(){
			cachedBoneRenderers = new Dictionary<Transform,List<Component>>();
			cachedBoneWeights = new Dictionary<SkinnedMeshRenderer,List<Transform>>();
			cachedCheckedNoBones = new List<GameObject>();
			// do not clear cachedOnBoneViewers
		}
		
		public static List<Component> getBoneRenderers(GameObject gameObject){
			Transform root = gameObject?.transform.root;
			if(cachedCheckedNoBones.Contains(gameObject)) return new List<Component>();
			try{
				if(!cachedBoneRenderers.ContainsKey(root)){
					SkinnedMeshRenderer[] renderersSM = new SkinnedMeshRenderer[]{};
					if(BoneViewerEditor.showAllBoneInfo){
						renderersSM = root.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
						.Select(renderer=>{
							if(!renderer || renderer.bones.Length==0) return null;
							return renderer;
						})
						.Where(renderer=>renderer!=null)
						.ToArray();
					}
					else {
						renderersSM = root.gameObject.GetComponentsInChildren<BoneViewer>(true)
						.Select(comp=>{
							if(!comp.showThisRendererOnBones) return null;
							SkinnedMeshRenderer renderer = comp.gameObject.GetComponent<SkinnedMeshRenderer>();
							if(!renderer || renderer.bones.Length==0) return null;
							//if(!renderer.bones.Contains(gameObject.transform)) return null;
							return renderer;
						})
						.Where(renderer=>renderer!=null)
						.ToArray();
					}
					List<Component> comps = renderersSM.Select(c=>(Component)c).ToList();
					if(comps.Count==0) cachedCheckedNoBones.Add(gameObject);
					comps.Sort((comp1,comp2)=>{
						if(comp1 is SkinnedMeshRenderer r1 && comp2 is SkinnedMeshRenderer r2 && r1.sharedMesh==r2.sharedMesh) return 1;
						return 0;
					});
					cachedBoneRenderers[root] = comps;
				}
				cachedBoneRenderers[root] = cachedBoneRenderers[root].Where(comp=>comp && comp!=null && comp.transform.root==root).ToList();
				if(!cachedBoneRenderers[root].Any(comp=>{
					if(comp is SkinnedMeshRenderer renderer && renderer.bones.Contains(gameObject.transform)) return true;
					return false;
				})) return new List<Component>();
				return cachedBoneRenderers[root];
			}catch(Exception e){
				Debug.LogException(e);
				return new List<Component>();
			}
		}
		
		public static List<Transform> getWeightedBones(SkinnedMeshRenderer renderer,bool onlyIfCached=false){
			if(cachedBoneWeights.ContainsKey(renderer)) return cachedBoneWeights[renderer];
			if(onlyIfCached) return null;
			List<Transform> bones = new List<Transform>();
			foreach(BoneWeight boneWeight in renderer.sharedMesh.boneWeights){
				Transform[] arr = new Transform[]{
					renderer.bones[boneWeight.boneIndex0], renderer.bones[boneWeight.boneIndex1], renderer.bones[boneWeight.boneIndex2], renderer.bones[boneWeight.boneIndex3]
				};
				foreach(Transform bone in arr){
					if(!bone) continue;
					if(!bones.Contains(bone)) bones.Add(bone);
				}
			}
			foreach(BoneWeight1 boneWeight in renderer.sharedMesh.GetAllBoneWeights()){
				Transform bone = renderer.bones[boneWeight.boneIndex];
				if(!bone) continue;
				if(!bones.Contains(bone)) bones.Add(bone);
			}
			foreach(Transform t in bones.ToArray()){
				foreach(Transform t2 in MiscUtils.getTransformParentArray(t)){
					if(!bones.Contains(t2)) bones.Add(t2);
				}
			}
			bones = renderer.bones.Intersect(bones).ToList();
			cachedBoneWeights.Add(renderer,bones);
			return bones;
		}
		
		[SerializeField] public static bool showGizmos = false;
		[NonSerialized] public static BoneViewerEditor.Callback drawGizmosCallback = null;
		void OnDrawGizmos(){
			if(drawGizmosCallback!=null) drawGizmosCallback();
		}
		
		public static void TidyUp(){
			foreach(BoneViewer boneViewer in cachedOnBoneViewers.ToArray()){
				if(boneViewer && boneViewer.isAutoCreated) UnityEngine.Object.DestroyImmediate(boneViewer);
			}
			clearCache();
		}
		
		[InitializeOnLoadMethod]
		private static void Listeners(){
			AssemblyReloadEvents.beforeAssemblyReload += ()=>{ TidyUp(); };
			AssemblyReloadEvents.afterAssemblyReload += ()=>{ clearCache(); };
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += (UnityEngine.SceneManagement.Scene scene,string path)=>{ TidyUp(); };
		}
		
		#endif
		
	}
	
	#if UNITY_EDITOR
	
	[CustomEditor(typeof(BoneViewer))]
	public class BoneViewerEditor : UnityEditor.Editor {
		
		public override void OnInspectorGUI(){
			try{
				BoneViewer boneViewer = (BoneViewer)this.target;
				SkinnedMeshRenderer renderer = boneViewer.gameObject.GetComponent<SkinnedMeshRenderer>();
				bool drawRefreshButton(){
					bool refreshBtn = GUILayout.Button(new GUIContent("Refresh Bone Information","Clears Internal BoneViewer Caches"),new GUIStyle(GUI.skin.GetStyle("Button")){ fontSize=12, padding=new RectOffset(10,10,3,3), margin=new RectOffset(0,0,0,0) });
					if(refreshBtn){
						EditorUtils.AddLabelField("Clearing Cache...");
						BoneViewer.clearCache();
						BoneViewer.TidyUp();
					}
					return refreshBtn;
				}
				if(boneViewer.isAutoCreated && !BoneViewer.cachedOnBoneViewers.Contains(boneViewer)) BoneViewer.cachedOnBoneViewers.Add(boneViewer);
				if(boneViewer.isAutoCreated && !renderer){
					if(drawRefreshButton()) return;
					BoneViewer.showGizmos = EditorUtils.AddCheckbox(BoneViewer.showGizmos,"Show gizmo lines for bones",null);
					List<Component> renderers = BoneViewer.getBoneRenderers(boneViewer.gameObject);
					drawBoneInfo(renderers,boneViewer.gameObject);
					if(BoneViewerEditor.showAllBoneInfo){
						EditorUtils.AddLabelField("Currently showing info on all bone objects.","To change this, go to 'Tools / MivorTools / Bone Viewer', and toggle 'Always Show Bone Info'.\nThis setting is slow, so it's suggested to keep it off.",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(0,0,0,0) });
					}
					else {
						EditorUtils.AddLabelField("To see bone information for other Mesh Renderers, add 'Bone Viewer' to them.","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(0,0,0,0) });
					}
				}
				if(!boneViewer.isAutoCreated && !renderer){
					EditorUtils.AddLabelField("This object has no Mesh Renderer.");
					return;
				}
				if(renderer){
					if(boneViewer.firstTimeGUI){
						boneViewer.firstTimeGUI = false;
						EditorUtils.AddLabelField("Initialising Bone Viewer...");
						BoneViewer.clearCache(); return;
					}
					if(drawRefreshButton()) return;
					if(!BoneViewerEditor.usePatcher){
						EditorUtils.AddLabelField("No info will be shown on bone objects.","To change this, go to 'Tools / MivorTools / Bone Viewer', and toggle 'Auto-Create Bone Viewer'.");
					}
					else if(BoneViewerEditor.showAllBoneInfo){
						EditorUtils.AddLabelField("Currently showing info on all bone objects.","To change this, go to 'Tools / MivorTools / Bone Viewer', and toggle 'Always Show Bone Info'.\nThis setting is slow, so it's suggested to keep it off.");
					}
					else {
						bool showInfo = boneViewer.showThisRendererOnBones;
						showInfo = EditorUtils.AddCheckbox(showInfo,"Show related info on bone objects",null);
						if(showInfo!=boneViewer.showThisRendererOnBones){
							EditorUtility.SetDirty(boneViewer);
							boneViewer.showThisRendererOnBones = showInfo;
							EditorUtils.AddLabelField("Clearing Cache...");
							BoneViewer.clearCache(); return;
						}
					}
					BoneViewer.showGizmos = EditorUtils.AddCheckbox(BoneViewer.showGizmos,"Show gizmo lines for bones",null);
					drawBoneInfo(boneViewer.gameObject.GetComponents<SkinnedMeshRenderer>().Select(r=>(Component)r).ToList());
					EditorGUILayout.Space(5,false);
				}
			}catch(Exception e){
				Debug.LogException(e);
			}
		}
		
		public void drawBoneInfo(List<Component> renderers,GameObject currentBoneObject=null){
			BoneViewer boneViewer = (BoneViewer)this.target;
			//EditorUtils.AddLabelField("Renderer Count: "+renderers.Count);
			int count = 0; int drawCount = 0;
			SkinnedMeshRenderer prevRenderer = null;
			foreach(Component comp in renderers){
				if(comp is SkinnedMeshRenderer renderer){
					if(!showAllBoneInfo){
						BoneViewer rendererBoneViewer = comp.gameObject.GetComponent<BoneViewer>();
						if(!rendererBoneViewer || (currentBoneObject && !rendererBoneViewer.showThisRendererOnBones)) continue;
					}
					if(currentBoneObject && !renderer.bones.Contains(currentBoneObject.transform)) continue;
					// Draw Renderer Box
					List<Transform> bones = BoneViewer.getWeightedBones(renderer,onlyIfCached:true);
					if(bones!=null && currentBoneObject && !bones.Contains(currentBoneObject.transform)) continue; // MiscUtils.getTransformParentArray(currentBoneObject.transform).Any(b=>!bones.Contains(b))
					List<Transform> bonesPrev = !prevRenderer ? null : BoneViewer.getWeightedBones(prevRenderer,onlyIfCached:true);
					bool sameBones = bones!=null && bonesPrev!=null && bones.SequenceEqual(bonesPrev);
					prevRenderer = renderer;
					count++;
					// Find bones if not cached
					if(bones==null){
						if(currentBoneObject || boneViewer.uiExpandRenderer.Contains(comp)) EditorApplication.delayCall += ()=>BoneViewer.getWeightedBones(renderer);
						if(currentBoneObject) continue;
					}
					// Draw
					drawCount++;
					drawBoneInfo_RendererBox(renderer,withToggle:!currentBoneObject && !sameBones);
					gizmosRenderer = renderer;
					gizmosSelectedBone = currentBoneObject?.transform;
					// Draw Bones
					if(!currentBoneObject && !boneViewer.uiExpandRenderer.Contains(comp)) continue;
					EditorUtils.LayoutHorizontal(new GUIStyle(){ padding=new RectOffset(2,2,0,0), margin=new RectOffset(0,0,0,10) },()=>{
						EditorUtils.LayoutVertical(new GUIStyle(GUI.skin.box){ padding=new RectOffset(0,0,2,2), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{ GUILayout.ExpandWidth(true) },()=>{
							if(sameBones){
								EditorUtils.AddLabelField("This renderer has the same bone information as above.","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(5,5,1,0) });
							}
							else {
								if(bones==null){
									EditorUtils.AddLabelField("Finding bones, please wait...","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(5,5,1,0) });
									return;
								}
								foreach(Transform bone in bones){
									if(bones.Contains(bone.parent)) continue;
									if(MiscUtils.getTransformParentArray(bone).Any(b=>bones.Contains(b))) continue;
									drawBoneInfo_BoneBox(renderer,bone.gameObject,currentBoneObject,foundBone:bone.gameObject==currentBoneObject,boneRoot:bone.parent);
								}
							}
						});
					});
				}
			}
			if(count==0){
				EditorUtils.AddLabelField("No renderers found with this bone.","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(0,0,10,10) });
			}
			if(count!=0 && drawCount==0){
				EditorUtils.AddLabelField("<color='#7DADF4'>Finding bones, please wait...</color>","",new GUIStyle(EditorUtils.uiStyleDefault){ wordWrap=true, padding=new RectOffset(5,5,10,10) });
			}
			if(gizmosRenderer!=null) BoneViewer.drawGizmosCallback = ()=>OnDrawGizmosCallback();
			else if(BoneViewer.drawGizmosCallback!=null) BoneViewer.drawGizmosCallback = null;
		}
		
		public void drawBoneInfo_RendererBox(SkinnedMeshRenderer renderer,bool withToggle=true){
			BoneViewer boneViewer = (BoneViewer)this.target;
			EditorUtils.LayoutHorizontal(new GUIStyle(GUI.skin.GetStyle("HelpBox")){ alignment=TextAnchor.MiddleCenter, padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,10,0) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("ObjectField")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				GUIContent title = new GUIContent("<color='#F0F0F0'>Renderer</color>","");
				GUIStyle titleStyle = new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(5,0,0,0), margin=new RectOffset(5,0,2,0) };
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,2), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{
					GUILayout.MaxWidth(titleStyle.CalcSize(title).x+10f), GUILayout.MinHeight(minLabelHeight+4)
				},()=>{
					if(withToggle){
						Rect layoutRect = EditorUtils.previousLayoutRect;
						bool toggleShown = boneViewer.uiExpandRenderer.Contains(renderer);
						EditorGUILayout.Space(4,false);
						toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(10.0f) });
						toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
						if(toggleShown && !boneViewer.uiExpandRenderer.Contains(renderer)) boneViewer.uiExpandRenderer.Add(renderer);
						if(!toggleShown && boneViewer.uiExpandRenderer.Contains(renderer)) boneViewer.uiExpandRenderer.Remove(renderer);
					}
					EditorUtils.AddLabelField(title.text,title.tooltip,titleStyle,new object[]{ "AutoMaxWidth" });
					EditorGUILayout.Space(10,false);
				});
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",renderer,typeof(SkinnedMeshRenderer),true,new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
				});
			});
		}
		
		public delegate void Callback();
		public void drawBoneInfo_BoneBox(SkinnedMeshRenderer renderer,GameObject boneObject,GameObject currentBoneObject=null,bool foundBone=false,Transform boneRoot=null){
			List<Transform> bones = BoneViewer.getWeightedBones(renderer);
			BoneViewer boneViewer = (BoneViewer)this.target;
			int depth = MiscUtils.getParentGapCount(boneObject.transform,boneRoot);
			EditorUtils.LayoutHorizontal(new GUIStyle(){ richText=true, alignment=TextAnchor.MiddleCenter, padding=new RectOffset(depth*10,0,0,0), margin=new RectOffset(0,0,2,2) },()=>{
				float minLabelHeight = new GUIStyle(GUI.skin.GetStyle("ObjectField")){ fontSize=12, padding=new RectOffset(0,0,1,1) }.CalcSize(new GUIContent("B")).y;
				GUIContent title = new GUIContent(""+boneObject.name+"  ","");
				title.text = "<color='"+(currentBoneObject==boneObject?"#7DADF4":"#F0F0F0")+"'>"+title.text+"</color>";
				bool hasChildren = boneObject.transform.childCount>0 && MiscUtils.getTransformChildrenArray(boneObject.transform).Any(t=>bones.Contains(t));
				GUIStyle titleStyle = new GUIStyle(EditorUtils.uiStyleDefault){ padding=new RectOffset(hasChildren?0:22,0,0,0), margin=new RectOffset(5,0,2,0) };
				EditorUtils.LayoutHorizontal(new GUIStyle(){ alignment=TextAnchor.MiddleLeft, padding=new RectOffset(0,0,2,2), margin=new RectOffset(0,0,0,0) },new GUILayoutOption[]{
					GUILayout.MaxWidth(titleStyle.CalcSize(title).x), GUILayout.MinHeight(minLabelHeight+4)
				},()=>{
					Rect layoutRect = EditorUtils.previousLayoutRect;
					if(hasChildren){
						bool toggleShown = boneViewer.uiExpandBone.Contains((renderer,boneObject));
						if(currentBoneObject && currentBoneObject!=boneObject && !toggleShown && !foundBone && MiscUtils.doesTransformHaveHierarchyParent(currentBoneObject.transform,boneObject.transform)) toggleShown = true;
						EditorGUILayout.Space(4,false);
						toggleShown = EditorGUILayout.Toggle(toggleShown, EditorStyles.foldout, new GUILayoutOption[]{ GUILayout.MaxWidth(15.0f) });
						toggleShown = GUI.Toggle(layoutRect, toggleShown, GUIContent.none, GUIStyle.none);
						if(toggleShown && !boneViewer.uiExpandBone.Contains((renderer,boneObject))) boneViewer.uiExpandBone.Add((renderer,boneObject));
						if(!toggleShown && boneViewer.uiExpandBone.Contains((renderer,boneObject))) boneViewer.uiExpandBone.Remove((renderer,boneObject));
					}
					EditorUtils.AddLabelField(title.text,title.tooltip,titleStyle,new object[]{ "AutoMaxWidth" });
				});
				EditorUtils.LayoutDisabled(()=>{
					EditorGUILayout.ObjectField("",boneObject,typeof(GameObject),true,new GUILayoutOption[]{ GUILayout.MinWidth(15f), GUILayout.ExpandWidth(true) });
				});
			});
			if(boneViewer.uiExpandBone.Contains((renderer,boneObject))){
				foreach(Transform bone in bones){
					if(bone.parent!=boneObject.transform) continue;
					if(bone.gameObject==currentBoneObject) foundBone = true;
					drawBoneInfo_BoneBox(renderer,bone.gameObject,currentBoneObject,foundBone,boneRoot);
				}
			}
		}
		
		public static SkinnedMeshRenderer gizmosRenderer = null;
		public static Transform gizmosSelectedBone = null;
		//public static List<Transform> gizmosBones = new List<Transform>();
		public void OnDrawGizmosCallback(){
			if(!BoneViewer.showGizmos) return;
			List<Transform> bones = BoneViewer.getWeightedBones(gizmosRenderer); // gizmosRenderer.bones
			if(!Selection.objects.Any(o=>
				o is GameObject go
				&& (
					(
						gizmosRenderer!=null
						&& (go==gizmosRenderer?.gameObject || MiscUtils.doesGameObjectHaveHierarchyParent(gizmosRenderer.gameObject,go))
					)
					|| bones.Contains(go.transform)
				)
			)) return;
			if(gizmosRenderer==null) return;
			MiscUtils.forEachTransformChildren(bones.First().parent,true,(t)=>{
				if(!bones.Contains(t)) return false;
				Gizmos.color = Color.white;
				if(gizmosSelectedBone!=null && gizmosSelectedBone.parent==t) Gizmos.color = Color.yellow;
				if(gizmosSelectedBone!=null && gizmosSelectedBone==t) Gizmos.color = Color.blue;
				if(gizmosSelectedBone!=null && gizmosSelectedBone==t.parent) Gizmos.color = Color.green;
				Gizmos.DrawLine(t.parent.position*0.01f+t.position*0.99f, t.parent.position*0.99f+t.position*0.01f);
				return true;
			});
		}
		
		public static bool showAllBoneInfo => menuPref.optionShowAllBoneInfo.value();
		
		public static bool usePatcher => menuPref.optionUsePatcher.value();
		
		[InitializeOnLoad]
		public class menuPref {
			private const string prefPrefix = "net.mivor.mivortools.extras.boneviewer";
			private const string menuMainPrefix = "Tools/MivorTools/Bone Viewer";
			
			public const int menuTopCategoryPriority = 1200;
			public const int menuSubCategoryPriority = 1200;
			
			public class optionShowAllBoneInfo {
				public const string menu = "Always Show Bone Info\t(setting)";
				public const string pref = "showAllBoneInfo";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			public class optionUsePatcher {
				public const string menu = "Auto-Create Bone Viewer\t(setting)";
				public const string pref = "usePatcher";
				public const bool def = false;
				public static bool value(){ return EditorPrefs.GetBool(prefPrefix+"."+pref,def); }
			}
			static menuPref() {
				EditorApplication.delayCall += UpdateMenu;
			}
			
			private static void UpdateMenu() {
				// Main Menu
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionShowAllBoneInfo.menu,optionShowAllBoneInfo.value());
				UnityEditor.Menu.SetChecked(menuMainPrefix+"/"+optionUsePatcher.menu,optionUsePatcher.value());
			}
			
			[MenuItem(menuMainPrefix+"/"+optionShowAllBoneInfo.menu, false, menuSubCategoryPriority)]
			private static void toggleShowAllBoneInfo() {
				EditorPrefs.SetBool(prefPrefix+"."+optionShowAllBoneInfo.pref,!optionShowAllBoneInfo.value());
				if(optionShowAllBoneInfo.value()==true) EditorPrefs.SetBool(prefPrefix+"."+optionUsePatcher.pref,true);
				UpdateMenu();
				BoneViewer.clearCache();
			}
			
			[MenuItem(menuMainPrefix+"/"+optionUsePatcher.menu, false, menuSubCategoryPriority)]
			private static void toggleUsePatcher() {
				EditorPrefs.SetBool(prefPrefix+"."+optionUsePatcher.pref,!optionUsePatcher.value());
				if(optionUsePatcher.value()==false) EditorPrefs.SetBool(prefPrefix+"."+optionShowAllBoneInfo.pref,false);
				if(optionUsePatcher.value()==true) BoneViewerHarmonyPatcher.Init();
				UpdateMenu();
				BoneViewer.clearCache();
			}
			
		}
		
	}
	
	public class BoneViewerHarmonyPatcher {
		public static bool isPatched = false;
		[InitializeOnLoadMethod]
		public static void Init(){
			if(!BoneViewerEditor.usePatcher) return;
			if(isPatched) return;
			UnityEditorPatcher.ApplyPatch("UnityEditor.PropertyEditor","DrawEditors",UnityEditorPatcher.PatchType.Prefix,typeof(BoneViewerHarmonyPatcher),nameof(OnPropertyEditorDraw));
			isPatched = true;
		}
		// https://harmony.pardeike.net/articles/patching-prefix.html
		public static void OnPropertyEditorDraw(UnityEditor.Editor[] editors){
			if(!BoneViewerEditor.usePatcher) return;
			foreach(UnityEditor.Editor editor in editors){
				try{
					if(!(editor.target is GameObject)) continue;
					GameObject gameObject = (GameObject)editor.target;
					bool showOnBones = BoneViewerEditor.showAllBoneInfo || gameObject.transform.root.gameObject.GetComponentsInChildren<BoneViewer>().Any(c=>c.showThisRendererOnBones);
					BoneViewer boneViewerComp = gameObject.GetComponent<BoneViewer>();
					SkinnedMeshRenderer renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
					if(showOnBones){
						List<Component> boneReferences = BoneViewer.getBoneRenderers(gameObject);
						if(showOnBones && !boneViewerComp && (boneReferences.Count>0 || renderer)){
							boneViewerComp = gameObject.AddComponent<BoneViewer>();
							boneViewerComp.isAutoCreated = true;
							boneViewerComp.hideFlags = HideFlags.DontSave;
							Component[] comps = gameObject.GetComponents<Component>();
							int moveUpCount = comps.Length;
							if(renderer) moveUpCount -= comps.ToList().IndexOf(renderer)+2;
							for(int i=0;i<moveUpCount;i++) UnityEditorInternal.ComponentUtility.MoveComponentUp(boneViewerComp);
							EditorUtility.ClearDirty(boneViewerComp);
							BoneViewer.cachedOnBoneViewers.Add(boneViewerComp);
						}
						if(boneViewerComp && boneViewerComp.isAutoCreated && boneReferences.Count==0 && !renderer){
							UnityEngine.Object.DestroyImmediate(boneViewerComp);
							if(!BoneViewer.cachedOnBoneViewers.Contains(boneViewerComp)) BoneViewer.cachedOnBoneViewers.Remove(boneViewerComp);
						}
					}
					if(!showOnBones && boneViewerComp && boneViewerComp.isAutoCreated){
						UnityEngine.Object.DestroyImmediate(boneViewerComp);
						if(!BoneViewer.cachedOnBoneViewers.Contains(boneViewerComp)) BoneViewer.cachedOnBoneViewers.Remove(boneViewerComp);
					}
				}catch(Exception e){
					Debug.LogException(e);
				}
			}
		}
	}
	
	#endif
	
}
