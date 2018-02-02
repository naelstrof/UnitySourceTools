using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;

public class DecalBuilder : ThreadedJob {
    // Indata
    public Matrix4x4 mat;
    public Vector3 position;
    public float scale;
    public BSPTree tree;
    public float offset;
    public Decal decal;
    public GameObject target;
    public Quaternion rotation;
    public bool isStatic;
    public GameObject targetPos = null;

    // Outdata
    private List<Vector3> verts = new List<Vector3> ();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private LinkedList<int> tri = new LinkedList<int> ();
    private Dictionary<int, int> indexLookup;//= new Dictionary<int, int>();
    private void BuildMeshForObject() {
        List<int> triangles = new List<int>();
        // Use a BSP tree to find nearby triangles at log(n) speeds.
        tree.FindClosestTriangles (position, scale, triangles);
        // Calculate the matrix needed to transform a point from the obj's local mesh, to our local mesh.
        // Matrix4x4 mat = transform.worldToLocalMatrix * obj.transform.localToWorldMatrix;
        // Clear the index lookup, we use it to check which verticies are shared.
        // This keeps us from having to rebuild the mesh so intricately.
        indexLookup = new Dictionary<int, int>(triangles.Count);
        // TODO: split this up into more jobs.
        for (int i = 0; i < triangles.Count; i++) {
            // We use the indices of the original mesh's triangles to determine which
            // verticies are shared. So we don't have to calculate that ourselves.
            // we grab them here.
            int i1, i2, i3;
            tree.GetIndices (triangles [i], out i1, out i2, out i3);

            // Here we get the points of the obj's mesh in our local space.
            Vector3 v1, v2, v3;
            tree.GetVertices (triangles [i], out v1, out v2, out v3);
            v1 = mat.MultiplyPoint (v1);
            v2 = mat.MultiplyPoint (v2);
            v3 = mat.MultiplyPoint (v3);

            Vector3 n1, n2, n3;
            tree.GetNormals (triangles [i], out n1, out n2, out n3);

            // We do a quick normal calculation to see if the triangle is mostly facing us.
            // we have to recalculate it since the normal is different in our local space
            // (maybe we could just transform the original bsptree's precomputed normals with the matrix ?)
            Vector3 side1 = v2 - v1;
            Vector3 side2 = v3 - v1;
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            if (normal.y <= 0.2f) {
                continue;
            }
                
            // To prevent z-fighting, I randomly offset each vertex by the normal.
            v1 += normal * offset;
            v2 += normal * offset;
            v3 += normal * offset;

            // First we check to see if a vertex has already been grabbed and calculated.
            // If it has been, we just use that as the index for the triangle.
            // Otherwise we create a new vertex, with coorisponding UV mapping.
            // Since we're in a local space where the decal spans from -0.5f to 0.5f,
            // Our UV is just the x and z values in the space offset by 0.5f
            int ni1, ni2, ni3;
            if (!indexLookup.TryGetValue (i1, out ni1)) {
                verts.Add (v1);
                uvs.Add (new Vector2 (v1.x + 0.5f, v1.z + 0.5f));
                normals.Add (rotation*n1);
                ni1 = verts.Count - 1;
                indexLookup [i1] = ni1;
            }
            if (!indexLookup.TryGetValue (i2, out ni2)) {
                verts.Add (v2);
                uvs.Add (new Vector2 (v2.x + 0.5f, v2.z + 0.5f));
                normals.Add (rotation*n2);
                ni2 = verts.Count - 1;
                indexLookup [i2] = ni2;
            }
            if (!indexLookup.TryGetValue (i3, out ni3)) {
                verts.Add (v3);
                uvs.Add (new Vector2 (v3.x + 0.5f, v3.z + 0.5f));
                normals.Add (rotation*n3);
                ni3 = verts.Count - 1;
                indexLookup [i3] = ni3;
            }

            // Finally we add the triangle to the triangle list.
            tri.AddLast (ni1);
            tri.AddLast (ni2);
            tri.AddLast (ni3);
        }
    }
    protected override void ThreadFunction()
    {
        BuildMeshForObject ();
        // TODO: Split this into more jobs.
        DecalClipper clipper = new DecalClipper ();
        clipper.newTri = tri;
        clipper.newUV = uvs;
        clipper.newVerts = verts;
        clipper.newNormals = normals;
        clipper.Start ();
        while (!clipper.IsDone) {
            Thread.Sleep (1);
            // we wait for our jobs to be done.
        }
    }
    protected override void OnFinished()
    {
        decal.FinishMesh (isStatic, target, verts, normals, uvs, tri, targetPos);
    }
}
