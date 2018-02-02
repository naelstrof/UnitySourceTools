using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(RopeSim))]
[RequireComponent(typeof(SkinnedMeshRenderer))]
[ExecuteInEditMode]
public class RopeMeshRenderer : MonoBehaviour {
    public float radius = 0.05f;
    private RopeSim ropesim;
	private Bounds biggestBounds;
	private float biggestBoundsVolume;
	private bool wroteBounds;
    void Start () {
        ropesim = GetComponent<RopeSim>();
        SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
        if ( !ropesim.sane ) {
            if ( renderer.sharedMesh != null ) {
                renderer.sharedMesh.Clear();
                renderer.sharedMesh = null;
            }
            return;
        }
        Mesh mesh = new Mesh();
        int parts = (int)(Vector3.Distance(ropesim.start.position, ropesim.end.position)*ropesim.boneDensity);
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<BoneWeight> weights = new List<BoneWeight>();
        float distMultiplier = Vector3.Distance(ropesim.start.position, ropesim.end.position)/(float)parts;

        // End cap
        BoneWeight capweight = new BoneWeight();
        capweight.boneIndex0 = ropesim.bones.Count-1;
        capweight.weight0 = 1f;
        verts.Add( new Vector3(-radius, ((float)parts)*distMultiplier, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 0 ) );
        verts.Add( new Vector3(-radius, ((float)parts)*distMultiplier, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 1 ) );
        verts.Add( new Vector3(radius, ((float)parts)*distMultiplier, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 1 ) );

        verts.Add( new Vector3(-radius, ((float)parts)*distMultiplier, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 0 ) );
        verts.Add( new Vector3(radius, ((float)parts)*distMultiplier, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 1 ) );
        verts.Add( new Vector3(radius, ((float)parts)*distMultiplier, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 0 ) );

        capweight.boneIndex0 = 0;
        capweight.weight0 = 1f;

        verts.Add( new Vector3(-radius, 0, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 0 ) );
        verts.Add( new Vector3(radius, 0, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 1 ) );
        verts.Add( new Vector3(-radius, 0, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 1 ) );

        verts.Add( new Vector3(-radius, 0, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 0, 0 ) );
        verts.Add( new Vector3(radius, 0, -radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 0 ) );
        verts.Add( new Vector3(radius, 0, radius ) );
        weights.Add( capweight );
        uvs.Add( new Vector2( 1, 1 ) );

        for( int i=0;i<parts;i++ ) {
            BoneWeight w1 = new BoneWeight();
            if ( i == parts || i == 0 ) {
                w1.boneIndex0 = i;
                w1.weight0 = 1f;
            } else {
                w1.boneIndex0 = i-1;
                w1.weight0 = 1f/4f;
                w1.boneIndex1 = i;
                w1.weight1 = 2f/4f;
                w1.boneIndex2 = i+1;
                w1.weight2 = 1f/4f;
            }

            BoneWeight w2 = new BoneWeight();
            if ( i+1 == parts ) {
                w2.boneIndex0 = i+1;
                w2.weight0 = 1f;
            } else {
                w2.boneIndex0 = i;
                w2.weight0 = 1f/4f;
                w2.boneIndex1 = i+1;
                w2.weight1 = 2f/4f;
                w2.boneIndex2 = i+2;
                w2.weight2 = 1f/4f;
            }

            verts.Add( new Vector3(-radius, i*distMultiplier, -radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 0, 0 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );
            verts.Add( new Vector3(radius, i*distMultiplier, -radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );

            verts.Add( new Vector3(radius, i*distMultiplier, -radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );
            verts.Add( new Vector3(radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 1, 1 ) );

            verts.Add( new Vector3(radius, i*distMultiplier, -radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 0, 0 ) );
            verts.Add( new Vector3(radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );
            verts.Add( new Vector3(radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );

            verts.Add( new Vector3(radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );
            verts.Add( new Vector3(radius, (i+1)*distMultiplier, radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 1, 1 ) );

            verts.Add( new Vector3(-radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 0, 0 ) );
            verts.Add( new Vector3(radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );

            verts.Add( new Vector3(radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(radius, (i+1)*distMultiplier, radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 1, 1 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );

            verts.Add( new Vector3(-radius, i*distMultiplier, -radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 0, 0 ) );
            verts.Add( new Vector3(-radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );

            verts.Add( new Vector3(-radius, i*distMultiplier, radius ) );
            weights.Add( w1 );
            uvs.Add( new Vector2( 1, 0 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 1, 1 ) );
            verts.Add( new Vector3(-radius, (i+1)*distMultiplier, -radius ) );
            weights.Add( w2 );
            uvs.Add( new Vector2( 0, 1 ) );
        }
        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        for (int i=0;i<verts.Count;i+=3) {
            tris.Add( i );
            tris.Add( i+1 );
            tris.Add( i+2 );
        }
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.boneWeights = weights.ToArray();

        List<Matrix4x4> bindPoses = new List<Matrix4x4>();

        foreach( Transform bone in ropesim.bones ) {
            bindPoses.Add( bone.worldToLocalMatrix * transform.localToWorldMatrix );
        }

        mesh.bindposes = bindPoses.ToArray();

        renderer.bones = ropesim.bones.ToArray();
        if ( renderer.sharedMesh != null ) {
            renderer.sharedMesh.Clear();
        }
        renderer.sharedMesh = mesh;
		TextAsset t = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/" + transform.parent.gameObject.name + ".txt") as TextAsset;
		if (t) {
			MatchCollection matches = Regex.Matches(t.text, @"-?\d+\.?\d*");
			Vector3 center = new Vector3 (float.Parse (matches [0].Value), float.Parse (matches [1].Value), float.Parse (matches [2].Value));
			Vector3 extents = new Vector3 (float.Parse (matches [3].Value), float.Parse (matches [4].Value), float.Parse (matches [5].Value));
			renderer.localBounds = new Bounds (center, extents);
			AssetDatabase.DeleteAsset ("Assets/" + transform.parent.gameObject.name + ".txt");
		}
    }

    void Update() {
        SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying ) {
#endif
            if ( renderer.sharedMesh == null ||
                 renderer.sharedMesh.triangles.Length <= 0 ||
                 ropesim.transform.hasChanged || 
                 renderer.bones[0] != ropesim.bones[0]) {
                Start();
                return;
            }
#if UNITY_EDITOR
        } else { 
#endif
            Vector3 max = ropesim.bones[0].localPosition;
            Vector3 min = ropesim.bones[0].localPosition;
            foreach( Transform bone in ropesim.bones ) {
                Vector3 pos = bone.localPosition;
                max.x = Mathf.Max(pos.x, max.x);
                max.y = Mathf.Max(pos.y, max.y);
                max.z = Mathf.Max(pos.z, max.z);

                min.x = Mathf.Min(pos.x, min.x);
                min.y = Mathf.Min(pos.y, min.y);
                min.z = Mathf.Min(pos.z, min.z);
            }
            max += new Vector3 (radius, radius, radius);
            min -= new Vector3 (radius, radius, radius);
            Vector3 center = min + (Vector3.Normalize (max - min) * Vector3.Distance(min,max)/2f);
            renderer.localBounds = new Bounds(center, max-min);
			float volume = (max - min).x*(max-min).y*(max-min).z;
			if (volume > biggestBoundsVolume) {
				biggestBounds = renderer.localBounds;
				biggestBoundsVolume = volume;
			}
			if (ropesim.recordAnimation && Time.time > ropesim.animationLength+ropesim.settleTime && !wroteBounds) {
				wroteBounds = true;
				StreamWriter writer = new StreamWriter("Assets/"+gameObject.transform.parent.gameObject.name+".txt", true);
				writer.WriteLine(biggestBounds.center + " " + biggestBounds.size);
				writer.Close();
			}
#if UNITY_EDITOR
        }
#endif
    }
}
