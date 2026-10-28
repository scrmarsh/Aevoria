using UnityEngine;

namespace Aevoria.Presentation
{
    public sealed class PlaceholderWorld : MonoBehaviour
    {
        private Mesh mesh;
        private Material material;

        public void Build()
        {
            var filter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = "Phase 0 flat geography placeholder" };
            mesh.vertices = new[] {
                new Vector3(-32, 0, -32), new Vector3(32, 0, -32),
                new Vector3(-32, 0, 32), new Vector3(32, 0, 32)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;
            var material = Resources.Load<Material>("PlaceholderGround");
            if (material == null) Debug.LogError("PlaceholderGround material is unavailable.");
            else renderer.sharedMaterial = material;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);

        }
    }
}