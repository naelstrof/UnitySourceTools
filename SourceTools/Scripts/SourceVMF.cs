// README:
// 1. Convert bsp to vmf with bspsrc
// 2. Use HL2's hammer to open vmf, and compile it using zfbx (effectively extracting textures and models)
// 3. Change .vmf to .txt, and drag into unity with compiled fbx
// 4. Configure crafty (nem's tools), usually requires extracting .paks, and setting custom mount options. Make sure it can open bsp files properly.
// 5. Export .obj of compiled bsp map with crafty (without the models).
// 6. Use vim macros to fix the path names in the .mtl file.
// 7. Use blender to open obj (make sure the textures load properly), then export as fbx.
// 8. Import this new compiled bsp mesh into unity, set its import scale to 0.01 (to match the vmf mesh scale exported from zfbx)

// At this point you have compiled all the required data for this script to function... not so bad, huh? :v

// 9. Run from the menu bar Source Tools/LoadVMF
// 10. Drag and drop the compiled bsp mesh fbx onto the MapFBX spot
// 11. Drag and drop the .vmf (which should be a .txt file) into the VMF spot.
// 12. Specify the folder that contains your "models" folder. Should start with "Assets" and end with a "/", an example is already put there.
// 13. Press create!


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class SourceVMF : ScriptableWizard {
	public TextAsset vmfText;
	public GameObject mapFBX;
	public Shader decalShader;
	public GameObject ropePrefab;
	public GameObject decalPrefab;
	private float sourceToUnityScale = 2.54f; // This is the scale that converts zfbx's output units to unity's units.
	public string modelParentFolder = "Assets/maps/d2_coast_03_d/";

	private GameObject world;
	private List<SourceEntity> entities;
	[MenuItem("Source Tools/LoadVMF")]
	static void CreateWizard() {
		ScriptableWizard.DisplayWizard<SourceVMF>("Load VMF", "Create", "Cancel");
	}
	void OnWizardUpdate() {
		helpString = "Feed me vmf";
	}
	void Read() {
		entities = new List<SourceEntity> ();
		int depth = 0;
		string header = "";
		foreach (string line in vmfText.text.Split('\n')) {			
			if (line.Trim () == "{") {
				depth++;
			} else if ( depth == 0 ) {
				header = line.Trim ();
				entities.Add (new SourceEntity (header));
			}
			if (line.Trim () == "}") {
				depth--;
			}
			if (depth != 1) {
				continue;
			}
			MatchCollection matches = Regex.Matches(line, "\\\"(.*?)\\\" \\\"(.*?)\\\"");
			if (matches.Count > 0) {
				entities [entities.Count - 1].SetKey (matches [0].Groups [1].Value, matches [0].Groups [2].Value);
			}
		}
	}
	void PlaceMap() {
		if (mapFBX == null) {
			throw new UnityException ("Didn't specify map.");
		}
		GameObject map = Instantiate (mapFBX, Vector3.zero, Quaternion.identity);
		if (map == null) {
			throw new UnityException ("Failed to make map.");
		}
		foreach (MeshFilter mf in map.GetComponentsInChildren<MeshFilter>()) {
			mf.gameObject.AddComponent<MeshCollider> ().sharedMesh = mf.sharedMesh;
			mf.gameObject.AddComponent<BSPTree> ();
		}
		map.transform.localScale = new Vector3 (sourceToUnityScale, sourceToUnityScale, sourceToUnityScale);
		map.transform.eulerAngles = new Vector3 (-90f, 180f, 0f);
		map.transform.SetParent (world.transform);
	}
	T GetAsset<T>( string path, string ext) where T : UnityEngine.Object {
		// separate the model name from the path.
		List<string> paths = new List<string> (path.Split ('/'));
		string texturename = paths [paths.Count - 1];
		paths.Remove (texturename);

		string[] guids = AssetDatabase.FindAssets(texturename, new string[] {modelParentFolder + string.Join("/",paths.ToArray())});
		if (guids.Length <= 0) {
			Debug.LogError("Couldn't find " + path);
			return default(T);
		}
		string assetPath = "";
		for ( int i=0;i<guids.Length; i++) {
			assetPath = AssetDatabase.GUIDToAssetPath (guids[i]);
			if (assetPath.IndexOf(texturename + "." + ext) == -1) {
				assetPath = "";
				continue;
			} else {
				break;
			}
		}
		if (assetPath == "") {
			Debug.LogError("Couldn't find " + path);
			return default(T);
		}
		Debug.Log ("Found " + assetPath + "!");
		return AssetDatabase.LoadAssetAtPath<T>(assetPath) as T;
	}
	GameObject CreateModel( string path ) {
		GameObject mdl = GetAsset<GameObject>(path, "fbx");
		if (mdl == null) {
			return null;
		}
		return PrefabUtility.InstantiatePrefab(mdl) as GameObject;
	}
	void PlaceStaticProps() {
		GameObject staticContainer = new GameObject ();
		staticContainer.name = "StaticEntities";
		staticContainer.transform.SetParent (world.transform);
		foreach( SourceEntity ent in entities ) {
			if (ent.GetStringValue ("classname") != "prop_static") {
				continue;
			}
			string modelname = ent.GetStringValue ("model");
			// malformed prop??
			if (modelname == "") {
				continue;
			}
			// lop off the .mdl part
			modelname = modelname.Substring (0, modelname.Length - 4);
			GameObject model = CreateModel(modelname);
			// Failed to find model, it's ok though just skip it.
			if (model == null) {
				continue;
			}
			foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>()) {
				MeshCollider mc = mf.gameObject.AddComponent<MeshCollider> ();
				mc.sharedMesh = mf.sharedMesh;
				mf.gameObject.AddComponent<BSPTree> ();
			}
			model.transform.position = SourceToUnityPosition(ent.GetVectorValue ("origin"));
			model.transform.eulerAngles = SourceToUnityRotation (ent.GetVectorValue ("angles"));
			//model.transform.RotateAround (model.transform.position, model.transform.up, 90f);
			model.transform.localScale = new Vector3 (sourceToUnityScale, sourceToUnityScale, sourceToUnityScale);
			model.transform.SetParent (staticContainer.transform);
		}
	}
	void PlaceDynamicProps() {
		GameObject staticContainer = new GameObject ();
		staticContainer.name = "DynamicEntities";
		staticContainer.transform.SetParent (world.transform);
		foreach( SourceEntity ent in entities ) {
			if (ent.GetStringValue ("classname") != "prop_dynamic" && ent.GetStringValue ("classname") != "prop_physics") {
				continue;
			}
			string modelname = ent.GetStringValue ("model");
			// malformed prop??
			if (modelname == "") {
				continue;
			}
			// lop off the .mdl part
			modelname = modelname.Substring (0, modelname.Length - 4);
			GameObject model = CreateModel(modelname);
			// Failed to find model, it's ok though just skip it.
			if (model == null) {
				continue;
			}
			foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>()) {
				MeshCollider mc = mf.gameObject.AddComponent<MeshCollider> ();
				mc.sharedMesh = mf.sharedMesh;
				mc.convex = true;
			}
			Rigidbody rb = model.AddComponent<Rigidbody> ();
			rb.mass = model.GetComponentInChildren<MeshRenderer> ().bounds.size.magnitude*10f;
			model.transform.position = SourceToUnityPosition(ent.GetVectorValue ("origin"));
			model.transform.eulerAngles = SourceToUnityRotation (ent.GetVectorValue ("angles"));
			model.transform.localScale = new Vector3 (sourceToUnityScale, sourceToUnityScale, sourceToUnityScale);
			model.transform.SetParent (staticContainer.transform);
			SpecialExceptions (model);
		}
	}
	void CreateDecal( Material mat, Texture tex, Vector3 pos, Vector3 facing, Transform parent ) {
		GameObject decal = Instantiate(decalPrefab);
		decal.name = tex.name;
		decal.GetComponentInChildren<MeshRenderer> ().sharedMaterial = mat;
		decal.transform.localScale = new Vector3 ((float)tex.width * 0.0025f * sourceToUnityScale, 0.5f, (float)tex.height * 0.0025f * sourceToUnityScale);
		decal.transform.position = pos;
		decal.transform.rotation = Quaternion.LookRotation (facing, Vector3.up) * Quaternion.AngleAxis (-90f, Vector3.right);
		decal.transform.position -= decal.transform.up*0.22f;
		//decal.transform.eulerAngles = new Vector3 (decal.transform.eulerAngles.x, 0, decal.transform.eulerAngles.z);
		decal.transform.SetParent (parent);
	}
	void SpecialExceptions( GameObject obj ) {
		if (obj.name == "wood_fence01a" || obj.name == "wood_fence01b" || obj.name == "plasterwall029c_window01a_bars" || obj.name =="wood_fence01c" || obj.name == "plasterwall029g_window01a_bars") {
			obj.transform.Rotate (Vector3.up, 90f);
		}
	}
	void PlaceDecals() {
		GameObject staticContainer = new GameObject ();
		staticContainer.name = "Decals";
		staticContainer.transform.SetParent (world.transform);
		foreach( SourceEntity ent in entities ) {
			if (ent.GetStringValue ("classname") != "infodecal") {
				continue;
			}
			string texture = ent.GetStringValue ("texture");
			// malformed decal??
			if (texture == "") {
				continue;
			}
			Material mat = new Material (decalShader);
			Texture tex = GetAsset<Texture> ("textures/"+texture, "tga");
			// Failed to find texture for decal :/
			if (tex == null) {				
				continue;
			}
			mat.SetTexture ("_MainTex", tex);
			mat.SetFloat ("_Smoothness", 0);
			mat.SetFloat ("_Mode", 2);
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			mat.SetInt("_ZWrite", 0);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.EnableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.renderQueue = 3000;
			RaycastHit hit;
			Vector3 pos = SourceToUnityPosition (ent.GetVectorValue ("origin"));
			CreateDecal (mat, tex, pos, Vector3.up, staticContainer.transform);
			CreateDecal (mat, tex, pos, Vector3.down, staticContainer.transform);
			CreateDecal (mat, tex, pos, Vector3.left, staticContainer.transform);
			CreateDecal (mat, tex, pos, Vector3.right, staticContainer.transform);
			CreateDecal (mat, tex, pos, Vector3.back, staticContainer.transform);
			CreateDecal (mat, tex, pos, Vector3.forward, staticContainer.transform);
		}
	}
	void PlaceRopes() {
		GameObject staticContainer = new GameObject ();
		staticContainer.name = "Ropes";
		staticContainer.transform.SetParent (world.transform);
		int ropenum = 1;
		foreach (SourceEntity ent in entities) {
			if (ent.GetStringValue ("classname") != "keyframe_rope" && ent.GetStringValue ("classname") != "move_rope") {
				continue;
			}
			SourceEntity next = GetSourceEntityByName (ent.GetStringValue ("NextKey"));
			if (next == null) {
				continue;
			}
			GameObject ropeRoot = Instantiate (ropePrefab);
			RopeSim rope = ropeRoot.GetComponentInChildren<RopeSim> ();
			rope.start.position = SourceToUnityPosition(ent.GetVectorValue ("origin"));
			rope.end.position = SourceToUnityPosition(next.GetVectorValue ("origin"));
			ropeRoot.transform.SetParent (staticContainer.transform);
			ropeRoot.name = "Rope" + ropenum;
			ropenum++;
		}
	}
	void PlaceLights() {
		GameObject staticContainer = new GameObject ();
		staticContainer.name = "Lights";
		staticContainer.transform.SetParent (world.transform);
		foreach (SourceEntity ent in entities) {
			if (ent.GetStringValue ("classname") == "point_spotlight") {
				GameObject spotlightroot = new GameObject ();
				spotlightroot.name = "PointSpotLight";
				Light spotlight = spotlightroot.AddComponent<Light> ();
				spotlight.lightmapBakeType = LightmapBakeType.Baked;
				spotlight.type = LightType.Point;
				spotlight.range = float.Parse (ent.GetStringValue ("spotlightlength"))*0.01f*sourceToUnityScale;
				//spotlight.spotAngle = float.Parse (ent.GetStringValue ("spotlightwidth"));
				Vector3 col = ent.GetVectorValue ("rendercolor").normalized;
				spotlight.color = new Color (col.x, col.y, col.z);
				spotlightroot.transform.position = SourceToUnityPosition(ent.GetVectorValue ("origin"));
				//spotlightroot.transform.eulerAngles = SourceToUnityRotation (ent.GetVectorValue ("angles"));
				spotlightroot.transform.SetParent (staticContainer.transform);
				continue;
			}
			if (ent.GetStringValue ("classname") == "light_spot") {
				GameObject spotlightroot = new GameObject ();
				spotlightroot.name = "LightSpot";
				Light spotlight = spotlightroot.AddComponent<Light> ();
				spotlight.lightmapBakeType = LightmapBakeType.Mixed;
				spotlight.type = LightType.Spot;
				spotlight.range = 10f;
				spotlight.spotAngle = float.Parse (ent.GetStringValue ("_cone"));
				Vector3 col = ent.GetVectorValue ("_light").normalized;
				spotlight.color = new Color (col.x, col.y, col.z);
				spotlightroot.transform.position = SourceToUnityPosition(ent.GetVectorValue ("origin"));
				Vector3 ang = SourceToUnityRotation (ent.GetVectorValue ("angles"));
				ang.x *= -1f;
				spotlightroot.transform.eulerAngles = ang;
				spotlightroot.transform.SetParent (staticContainer.transform);
				continue;
			}
			if (ent.GetStringValue ("classname") == "light") {
				GameObject spotlightroot = new GameObject ();
				spotlightroot.name = "Light";
				Light spotlight = spotlightroot.AddComponent<Light> ();
				spotlight.lightmapBakeType = LightmapBakeType.Baked;
				spotlight.type = LightType.Point;
				spotlight.range = 10f;
				Vector3 col = ent.GetVectorValue ("_light").normalized;
				spotlight.color = new Color (col.x, col.y, col.z);
				spotlightroot.transform.position = SourceToUnityPosition (ent.GetVectorValue ("origin"));
				spotlightroot.transform.SetParent (staticContainer.transform);
				continue;
			}
			if (ent.GetStringValue ("classname") == "light_environment") {
				GameObject spotlightroot = new GameObject ();
				spotlightroot.name = "LightEnvironment";
				Light spotlight = spotlightroot.AddComponent<Light> ();
				spotlight.lightmapBakeType = LightmapBakeType.Mixed;
				spotlight.type = LightType.Directional;
				Vector3 col = ent.GetVectorValue ("_light").normalized;
				spotlight.color = new Color (col.x, col.y, col.z);
				spotlightroot.transform.position = SourceToUnityPosition (ent.GetVectorValue ("origin"));
				Vector3 ang = SourceToUnityRotation (ent.GetVectorValue ("angles"));
				ang.x = -float.Parse (ent.GetStringValue ("pitch"));
				spotlightroot.transform.eulerAngles = ang;
				spotlightroot.transform.SetParent (staticContainer.transform);
				continue;
			}
		}
	}
	void Abort() {
		if (world != null) {
			DestroyImmediate (world);
		}
	}
	Vector3 SourceToUnityPosition(Vector3 source) {
		return (new Vector3 (source.x, source.z, source.y))*sourceToUnityScale*0.01f;
	}
	Vector3 SourceToUnityRotation( Vector3 source ) {
		return new Vector3 (source.x, -source.y+90f, -source.z);
	}
	void OnWizardCreate() {
		Read ();

		world = new GameObject ();
		world.name = "World";
		world.transform.position = Vector3.zero;
		try {
			PlaceMap ();
			PlaceStaticProps();
			PlaceDynamicProps();
			PlaceDecals();
			PlaceRopes();
			PlaceLights();
		} catch( UnityException e ) {
			Abort ();
			throw e;
		}
	}
	SourceEntity GetSourceEntityByName( string name ) {
		if (name == "") {
			return null;
		}
		foreach (SourceEntity ent in entities) {
			if (ent.GetStringValue ("targetname") == name) {
				return ent;
			}
		}
		return null;
	}
}

public class SourceEntity {
	public Dictionary<string,string> data;
	public string header;
	public SourceEntity( string header ) {
		this.header = header;
		data = new Dictionary<string,string>();
	}
	public void SetKey( string key, string value ) {
		data [key] = value;
	}
	public string GetStringValue( string key ) {
		if (!data.ContainsKey (key)) {
			return "";
		}
		return data [key];
	}
	public Vector3 GetVectorValue( string key ) {
		MatchCollection matches = Regex.Matches(data[key], @"-?\d+\.?\d*");
		return new Vector3 (float.Parse (matches [0].Value), float.Parse (matches [1].Value), float.Parse (matches [2].Value));
	}
}
