using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Loads the game's original compiled shader programs without recreating their source.
public sealed class RecoveryOriginalShaders : MonoBehaviour
{
    static AssetBundle bundle;
    static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
    float nextScan;
    public static Shader Get(string name) { Load(); return shaders.TryGetValue(name,out var shader) ? shader : null; }
    public static T GetAsset<T>(int id) where T:Object { Load(); return bundle ? bundle.LoadAsset<T>("recovery/"+id+".shader") : null; }
    public static void Load()
    {
        if(bundle) return;
        var path=Path.Combine(Application.streamingAssetsPath,"Recovery/original-shaders.bundle");
        if(!File.Exists(path)) return;
        foreach(var existing in AssetBundle.GetAllLoadedAssetBundles())
            if(existing.name=="recovery-original-shaders") {bundle=existing;break;}
        if(!bundle) bundle=AssetBundle.LoadFromFile(path);
        if(!bundle) return;
        foreach(var shader in bundle.LoadAllAssets<Shader>())
            if(shader.isSupported) shaders[shader.name]=shader;
    }
    public static void Apply()
    {
        Load();
        foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            var materials=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)
                if(materials[i] && materials[i].shader && shaders.TryGetValue(materials[i].shader.name,out var original)
                    && original.name.StartsWith("Shader Graphs/") && materials[i].shader!=original)
                {materials[i]=new Material(materials[i]) {shader=original};changed=true;}
            if(changed) renderer.sharedMaterials=materials;
        }
        if(RenderSettings.skybox && shaders.TryGetValue(RenderSettings.skybox.shader.name,out var sky) && RenderSettings.skybox.shader!=sky)
            RenderSettings.skybox=new Material(RenderSettings.skybox) {shader=sky};
        foreach(var graphic in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Include))
            if(graphic.material && graphic.material.shader && shaders.TryGetValue(graphic.material.shader.name,out var ui)
                && ui.name.StartsWith("Shader Graphs/") && graphic.material.shader!=ui)
                graphic.material=new Material(graphic.material) {shader=ui};
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Load(); Apply();
        var owner=new GameObject("Original shader recovery"); DontDestroyOnLoad(owner);owner.AddComponent<RecoveryOriginalShaders>();
    }
    void OnEnable() { SceneManager.sceneLoaded+=OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded-=OnSceneLoaded; }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode) { Apply(); }
    void Update() { if(Time.unscaledTime>=nextScan) {nextScan=Time.unscaledTime+1f;Apply();} }
}
