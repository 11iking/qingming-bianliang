using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Qingming;

public static class ImportGodotWorld
{
    const string Folder="Assets/Resources/Imported";
    const string Prefab=Folder+"/BianliangWorld.prefab";
    [MenuItem("Qingming/Import Godot Demo")]
    public static void Build()
    {
        if(!File.Exists(Folder+"/World.bytes"))throw new FileNotFoundException("Exported world is missing");
        string marker=Folder+"/UnityImportComplete.txt";
        if(File.Exists(marker)&&File.ReadAllText(marker).Contains("geometryRevision=4")&&File.Exists(Prefab)&&File.Exists("Assets/Scenes/Qingming.unity")){Debug.Log("Qingming import already complete");return;}
        Directory.CreateDirectory("Assets/Scenes");
        var nodes=new List<GameObject>();
        var materialCache=new Dictionary<string,Material>();
        var materials=new List<Material>();
        var meshes=new List<Mesh>();
        Font font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotoSerifSC.ttf");
        var worldTextMaterial=new Material(Shader.Find("Qingming/WorldText")){name="WorldText",mainTexture=font.material.mainTexture};
        materials.Add(worldTextMaterial);
        using(var reader=new BinaryReader(File.OpenRead(Folder+"/World.bytes"),Encoding.UTF8)){
            if(reader.ReadUInt32()!=0x514d5731)throw new Exception("Invalid world header");
            int count=reader.ReadInt32();
            for(int i=0;i<count;i++){
                int parent=reader.ReadInt32();
                var go=new GameObject(ReadString(reader));nodes.Add(go);
                if(parent>=0)go.transform.SetParent(nodes[parent].transform,false);
                go.transform.localPosition=ReadVector(reader);
                float qx=reader.ReadSingle(),qy=reader.ReadSingle(),qz=reader.ReadSingle(),qw=reader.ReadSingle();
                go.transform.localRotation=new Quaternion(-qx,-qy,qz,qw);
                go.transform.localScale=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                go.SetActive(reader.ReadByte()!=0);
                int surfaces=reader.ReadInt32();
                for(int s=0;s<surfaces;s++){
                    Color color=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    bool water=reader.ReadByte()!=0,outlined=reader.ReadByte()!=0;
                    string key=ColorUtility.ToHtmlStringRGBA(color)+"_"+water+"_"+outlined;
                    if(!materialCache.TryGetValue(key,out var mat)){
                        var shader=Shader.Find(water?"Qingming/InkWater":"Qingming/InkToon");
                        if(!shader)throw new Exception("Qingming shader missing");
                        mat=new Material(shader){name="Pigment_"+key};
                        if(!water){mat.SetColor("_Color",color);mat.SetFloat("_Outline",outlined?1.1f:0);}
                        materialCache[key]=mat;materials.Add(mat);
                    }
                    int n=reader.ReadInt32();var vertices=new Vector3[n];var normals=new Vector3[n];
                    for(int v=0;v<n;v++)vertices[v]=ReadVector(reader);
                    for(int v=0;v<n;v++)normals[v]=ReadVector(reader);
                    int ni=reader.ReadInt32();var indices=new int[ni];
                    for(int j=0;j<ni;j++)indices[j]=reader.ReadInt32();
                    // Godot's exported winding maps to Unity's front faces after the Z reflection.
                    var mesh=new Mesh{name="Mesh_"+i+"_"+s,indexFormat=IndexFormat.UInt32};
                    mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=indices;mesh.RecalculateBounds();meshes.Add(mesh);
                    var obj=s==0?go:new GameObject("Surface_"+s);
                    if(s>0)obj.transform.SetParent(go.transform,false);
                    obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
                    renderer.shadowCastingMode=water?ShadowCastingMode.Off:ShadowCastingMode.On;
                }
                if(reader.ReadByte()!=0){
                    string text=ReadString(reader);int size=reader.ReadInt32();float pixels=reader.ReadSingle();
                    Color color=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    var label=go.AddComponent<TextMesh>();label.font=font;label.text=text;label.fontSize=size;
                    label.characterSize=pixels*10;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color;
                    go.GetComponent<MeshRenderer>().sharedMaterial=worldTextMaterial;
                }
            }
        }
        string meshPath=Folder+"/WorldMeshes.asset",matPath=Folder+"/WorldMaterials.asset";
        if(File.Exists(meshPath))AssetDatabase.DeleteAsset(meshPath);
        if(File.Exists(matPath))AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(meshes[0],meshPath);for(int i=1;i<meshes.Count;i++)AssetDatabase.AddObjectToAsset(meshes[i],meshPath);
        AssetDatabase.CreateAsset(materials[0],matPath);for(int i=1;i<materials.Count;i++)AssetDatabase.AddObjectToAsset(materials[i],matPath);
        var root=nodes[0];root.AddComponent<BianliangWorld>();
        var meta=JObject.Parse(File.ReadAllText(Folder+"/WorldMetadata.json"));
        var sun=root.transform.Find((string)meta["sun"]).gameObject.AddComponent<Light>();
        sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.intensity=.62f;
        PrefabUtility.SaveAsPrefabAsset(root,Prefab);UnityEngine.Object.DestroyImmediate(root);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var controller=new GameObject("Qingming Demo").AddComponent<BianliangDemo>();controller.world=instance.GetComponent<BianliangWorld>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Qingming.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Qingming.unity",true)};
        PlayerSettings.companyName="Qingming";PlayerSettings.productName="汴梁一日 · Unity";
        PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        QualitySettings.antiAliasing=2;QualitySettings.shadowDistance=85;
        File.WriteAllText(marker,$"geometryRevision=4\nImported {nodes.Count} nodes, {meshes.Count} meshes, {materials.Count} materials.\n");
        AssetDatabase.SaveAssets();Debug.Log("QINGMING_IMPORT_PASS "+File.ReadAllText(marker));
    }
    static string ReadString(BinaryReader reader)=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    static Vector3 ReadVector(BinaryReader reader)=>new Vector3(reader.ReadSingle(),reader.ReadSingle(),-reader.ReadSingle());
    [MenuItem("Qingming/Build Windows Demo")]
    public static void BuildWindows()
    {
        Build();
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build-unity"));
        Directory.CreateDirectory(folder);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Qingming.unity"},
            locationPathName=Path.Combine(folder,"BianliangUnity.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Unity build failed");
        Debug.Log("QINGMING_BUILD_PASS "+result.summary.totalSize+" bytes");
    }
}
