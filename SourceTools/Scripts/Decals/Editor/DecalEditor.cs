using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Decal))]
public class DecalEditor : Editor {
    public override void OnInspectorGUI() {
        Decal decal = (Decal)target;
        //decal.decal = AssetField<Material>("Material", decal.decal);
		decal.deleteIfEmpty = EditorGUILayout.Toggle("Destroy if empty", decal.deleteIfEmpty );
        decal.offset =  EditorGUILayout.FloatField("Offset", decal.offset);
        decal.randomRotateOnSpawn =  EditorGUILayout.Toggle("Spawn with random rotation", decal.randomRotateOnSpawn);
        decal.layerMask = LayerMaskField("Affected Layers", decal.layerMask);
        EditorGUILayout.Separator();
        if (GUI.changed) {
            decal.BuildDecal ();
        }
    }
    void OnSceneGUI() {
        Decal decal = (Decal)target;
        if (decal.transform.hasChanged) {
            foreach (GameObject obj in decal.subDecals) {
                if (obj == null) {
                    continue;
                }
                for (int i = 0; i < obj.transform.childCount; i++) {
                    if (obj.transform.GetChild (i) == null) {
                        continue;
                    }
                    DestroyImmediate (obj.transform.GetChild (i).gameObject, true);
                }
                DestroyImmediate (obj, true);
            }
            decal.subDecals.Clear ();
            decal.BuildDecal ();
            decal.transform.hasChanged = false;
        }
		decal.Update ();
    }
    private static LayerMask LayerMaskField(string label, LayerMask mask) {
        List<string> layers = new List<string>();
        for(int i=0; i<32; i++) {
            string name = LayerMask.LayerToName(i);
            if(name != "") layers.Add( name );
        }
        return EditorGUILayout.MaskField( label, mask, layers.ToArray() );
    }
    private static T AssetField<T>(string label, T obj) where T : Object {
        return (T) EditorGUILayout.ObjectField(label, (T)obj, typeof(T), false);
    }
}
