using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crossroads.Diner.Editor
{
    public static class DinerInstaller
    {
        public const string Root="Assets/Crossroads";
        public const string ScenePath=Root+"/Scenes/CrossroadsDiner.unity";
        public const string ConfigPath=Root+"/Data/DinerConfig.asset";

        [MenuItem("Crossroads/Install or open game")]
        public static void Install()
        {
            var config=CreateCatalog();
            if(File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            BuildScene(config);
        }
        [MenuItem("Crossroads/Rebuild presentation from theme")]
        public static void RebuildPresentation()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding presentation.");
            var game=UnityEngine.Object.FindFirstObjectByType<DinerGame>();
            if(game==null) throw new InvalidOperationException("Open CrossroadsDiner scene first.");
            Undo.RegisterFullObjectHierarchyUndo(game.presentation.gameObject,"Rebuild diner presentation");
            for(int i=game.presentation.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(game.presentation.transform.GetChild(i).gameObject);
            game.presentation.layoutRoot=null; game.presentation.Build();
            PrefabUtility.SaveAsPrefabAssetAndConnect(game.presentation.gameObject,Root+"/Prefabs/DinerPresentation.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }
        public static DinerConfig CreateCatalog()
        {
            foreach(var path in new[]{"Data/Ingredients","Data/Tools","Data/Monsters","Data/Recipes","Visuals/Characters","Visuals/Items","Visuals/Stations","Visuals/Backgrounds","Visuals/UI","Audio","Scenes","Prefabs"}) Directory.CreateDirectory(Root+"/"+path);
            AssetDatabase.Refresh();
            var blood=Create<IngredientDefinition>("Ingredients","Blood","Blood",ArtKind.Blood,new Color(.68f,.2f,.23f));
            var meat=Create<IngredientDefinition>("Ingredients","Meat","Meat",ArtKind.Meat,new Color(.67f,.3f,.26f));
            var ecto=Create<IngredientDefinition>("Ingredients","Ecto","Ecto",ArtKind.Ecto,new Color(.37f,.72f,.64f));
            var garlic=Create<ToolDefinition>("Tools","Garlic","Garlic",ArtKind.Garlic,new Color(.91f,.84f,.66f));
            var chains=Create<ToolDefinition>("Tools","SilverChains","Silver chains",ArtKind.Chains,new Color(.66f,.78f,.8f));
            var salt=Create<ToolDefinition>("Tools","SpiritSalt","Spirit salt",ArtKind.Salt,new Color(.5f,.73f,.76f));
            var vampire=Create<MonsterDefinition>("Monsters","Vampire","Vampire",ArtKind.Vampire,new Color(.46f,.19f,.24f),m=>{m.weakness=garlic;m.ingredient=blood;m.ingredientYield=2;});
            var wolf=Create<MonsterDefinition>("Monsters","Werewolf","Werewolf",ArtKind.Werewolf,new Color(.57f,.48f,.34f),m=>{m.weakness=chains;m.ingredient=meat;m.ingredientYield=2;});
            var ghost=Create<MonsterDefinition>("Monsters","Ghost","Ghost",ArtKind.Ghost,new Color(.65f,.83f,.73f),m=>{m.weakness=salt;m.ingredient=ecto;m.ingredientYield=2;});
            var stew=Create<RecipeDefinition>("Recipes","BloodStew","Blood Stew",ArtKind.Stew,new Color(.68f,.26f,.19f),r=>{r.first=blood;r.second=meat;r.price=12;r.cookSeconds=4;});
            var spectral=Create<RecipeDefinition>("Recipes","SpectralStew","Spectral Stew",ArtKind.SpectralStew,new Color(.41f,.68f,.55f),r=>{r.first=ecto;r.second=meat;r.price=14;r.cookSeconds=4;});
            var soup=Create<RecipeDefinition>("Recipes","HauntedSoup","Haunted Soup",ArtKind.Soup,new Color(.61f,.42f,.64f),r=>{r.first=blood;r.second=ecto;r.price=16;r.cookSeconds=4;});
            var theme=AssetDatabase.LoadAssetAtPath<DinerTheme>(Root+"/Data/DinerTheme.asset");
            if(theme==null)
            {
                theme=ScriptableObject.CreateInstance<DinerTheme>(); theme.bodyFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); theme.titleFont=theme.bodyFont;
                AssetDatabase.CreateAsset(theme,Root+"/Data/DinerTheme.asset");
            }
            var config=AssetDatabase.LoadAssetAtPath<DinerConfig>(ConfigPath);
            if(config==null)
            {
                config=ScriptableObject.CreateInstance<DinerConfig>(); config.theme=theme;
                config.ingredients=new[]{blood,meat,ecto}; config.tools=new[]{garlic,chains,salt}; config.monsters=new[]{vampire,wolf,ghost}; config.recipes=new[]{stew,spectral,soup};
                config.openingGuests=new[]{new OpeningGuest{species=vampire,recipe=spectral},new OpeningGuest{species=wolf,recipe=soup},new OpeningGuest{species=ghost,recipe=stew}};
                AssetDatabase.CreateAsset(config,ConfigPath);
            }
            AssetDatabase.SaveAssets(); return config;
        }
        static T Create<T>(string folder,string id,string name,ArtKind kind,Color color,Action<T> configure=null) where T:DinerDefinition
        {
            string path=Root+"/Data/"+folder+"/"+id+".asset"; var asset=AssetDatabase.LoadAssetAtPath<T>(path); if(asset!=null) return asset;
            asset=ScriptableObject.CreateInstance<T>(); asset.id=id; asset.displayName=name; asset.placeholder=kind; asset.tint=color; configure?.Invoke(asset);
            asset.description="Replace icon or visualPrefab freely; keep the stable gameplay ID.";
            // Individual editable prefabs make every placeholder independently replaceable.
            var go=new GameObject(name+" visual",typeof(RectTransform),typeof(CanvasRenderer),typeof(DinerArtGraphic));
            var rect=go.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(180,180);
            var art=go.GetComponent<DinerArtGraphic>(); art.kind=kind; art.color=color; art.raycastTarget=false;
            string category=folder=="Monsters" ? "Characters" : "Items";
            asset.visualPrefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Visuals/"+category+"/"+id+".prefab");
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static void BuildScene(DinerConfig config)
        {
            var current=SceneManager.GetActiveScene();
            if(current.isDirty && !string.IsNullOrEmpty(current.path)) EditorSceneManager.SaveScene(current);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=5; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=config.theme.ink; camera.transform.position=new Vector3(0,0,-10);
            var light=new GameObject("Main Light",typeof(Light)); light.GetComponent<Light>().type=LightType.Directional; light.transform.rotation=Quaternion.Euler(50,-30,0);
            var canvasObject=new GameObject("Diner Presentation",typeof(RectTransform),typeof(Canvas));
            var presentation=canvasObject.AddComponent<DinerPresentation>(); presentation.config=config; presentation.Build();
            var root=new GameObject("RuntimeRoot"); var game=root.AddComponent<DinerGame>(); game.config=config; game.presentation=presentation; game.viewCamera=camera;
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject,Root+"/Prefabs/DinerPresentation.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var existing=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList(); existing.Insert(0,new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=existing.ToArray();
            AssetDatabase.SaveAssets(); Selection.activeGameObject=root;
            Debug.Log("Crossroads Diner installed. Open the Game view and press Play.");
        }
        [MenuItem("Crossroads/Validate catalog")]
        public static void Validate()
        {
            var config=AssetDatabase.LoadAssetAtPath<DinerConfig>(ConfigPath); var error=config==null ? "Missing catalog" : config.ValidateConfiguration();
            if(error!=null) Debug.LogError(error); else Debug.Log("Crossroads catalog valid.");
        }
    }
}
