using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DecalClipper : ThreadedJob {
    // Indata
    public List<Vector3> newVerts;
    public List<Vector2> newUV;
    public List<Vector3> newNormals;
    public LinkedList<int> newTri;

    protected override void ThreadFunction()
    {
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( Vector3.right, Vector3.right/2f ));
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( -Vector3.right, -Vector3.right/2f ));
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( Vector3.forward, Vector3.forward/2f ));
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( -Vector3.forward, -Vector3.forward/2f ));
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( Vector3.up, Vector3.up/2f ));
        ClipPlane( newVerts, newNormals, newUV, newTri, new Plane( -Vector3.up, -Vector3.up/2f ));
    }

    protected override void OnFinished()
    {
    }

    // This function takes a mesh, a triangle index, and a plane
    // Then it tries to clip the triangle by the plane, creating new
    // triangles if needed. It returns how many triangle indices have
    // been removed (and thus, if iterating, would use that value to offset the current index pointer.)
    private LinkedListNode<int> ClipTriangle( List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, LinkedList<int> tris, LinkedListNode<int> triangle, Plane plane) {
        // Detect violating vertices.
        bool[] violating = new bool[3];
        int violationCount = 0;
        int[] tids = new int[3];
        tids[0] = triangle.Value;
        tids[1] = triangle.Next.Value;
        tids[2] = triangle.Next.Next.Value;
        LinkedListNode<int> nextnode = triangle.Next.Next.Next;
        for (int i = 0; i < 3; i++) {
            violating[i] = plane.GetSide (verts[tids[i]]);
            if (violating[i]) {
                violationCount++;
            }
        }
        // I couldn't think of the general case of generating new triangles.
        // So i break the problem into each of its pieces.
        switch( violationCount ) {
        // If no vertices were outside the plane, we do nothing.
        case 0:
            return nextnode;
            // If one vertex is outside the plane..
        case 1:
            // It was difficult for me to think of a general solution
            // So I first find which of the three vertices are the violating one.
            Vector3 v1 = Vector3.zero, v2 = Vector3.zero, v3 = Vector3.zero;
            Vector3 n1 = Vector3.zero, n2 = Vector3.zero, n3 = Vector3.zero;
            int i1 = -1, i2 = -1, i3 = -1;
            for (int i = 0; i < 3; i++) {
                // Once found, I record their indices, and vertex locations while keeping
                // their order intact.
                if (violating [i]) {
                    i1 = tids [i];
                    i2 = tids [(i + 1) % 3];
                    i3 = tids [(i + 2) % 3];
                    v1 = verts [i1];
                    v2 = verts [i2];
                    v3 = verts [i3];
                    n1 = norms [i1];
                    n2 = norms [i2];
                    n3 = norms [i3];
                    break;
                }
            }
            // Create triangle number one
            Vector3 nv = LineCast (plane, v1, v2);
            verts.Add (nv);
            norms.Add (n1);
            uvs.Add (new Vector2 (nv.x + 0.5f, nv.z + 0.5f));
            int i4 = verts.Count - 1;
            tris.AddLast (i4);
            tris.AddLast (i2);
            tris.AddLast (i3);

            // Create triangle number two
            tris.AddLast (i3);
            nv = LineCast (plane, v3, v1);
            verts.Add (nv);
            norms.Add (n1);
            uvs.Add (new Vector2 (nv.x + 0.5f, nv.z + 0.5f));
            tris.AddLast (verts.Count - 1);
            tris.AddLast (i4);
            // Delete the old triangle.
            tris.Remove(triangle.Next.Next);
            tris.Remove(triangle.Next);
            tris.Remove(triangle);
            return nextnode;
            // If two vertices are outside the plane...
        case 2:
            for (int i = 0; i < 3; i++) {
                // If we're a vertex thats not outside the plane, we are going to be part of the new triangle.
                if (!violating [i]) {
                    tris.AddLast (tids[i]);
                }
                // We use XOR to check if we cross the plane while going to the next vertex.
                if (violating [i] ^ violating [(i + 1) % 3]) {
                    // If we did cross a plane, we create a new vertex and use that as part of our triangle.
                    Vector3 v = LineCast (plane, verts [tids[i]], verts [tids[(i + 1) % 3]]);
                    verts.Add (v);
                    norms.Add (norms [tids[(i + 1) % 3]]);
                    uvs.Add (new Vector2 (v.x + 0.5f, v.z + 0.5f));
                    tris.AddLast (verts.Count - 1);
                }
            }
            // Delete the old triangle.
            tris.Remove(triangle.Next.Next);
            tris.Remove(triangle.Next);
            tris.Remove(triangle);
            return nextnode;
            // If all of our vertices are outside the plane, we just delete the whole triangle and exit.
        case 3:
            // Delete the old triangle.
            tris.Remove(triangle.Next.Next);
            tris.Remove(triangle.Next);
            tris.Remove(triangle);
            return nextnode;
        }
        return nextnode;
    }
    // Clips the given mesh to fit entirely on one side of the plane.
    private void ClipPlane( List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, LinkedList<int> tris, Plane plane ) {
        int triCount = tris.Count/3;
        int i = 0;
        LinkedListNode<int> node = tris.First;
        while (i < triCount) {
            node = ClipTriangle (verts, norms, uvs, tris, node, plane);
            i++;
        }
    }
    // Find the point on a plane where a line intersects.
    private static Vector3 LineCast(Plane plane, Vector3 a, Vector3 b) {
        float dis;
        Ray ray = new Ray(a, b-a);
        plane.Raycast( ray, out dis );
        return ray.GetPoint(dis);
    }
}
