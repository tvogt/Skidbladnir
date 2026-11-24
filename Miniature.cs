using System.Collections.Generic;
using UnityEngine;

namespace Skidbladnir
{
    /// <summary>
    /// Replaces the hammer model of the cloned item with a miniature of the longship: the longship's static
    /// meshes (no sail cloth, particles, water mask or lower LODs), scaled down. The hammer's renderers are
    /// switched off, not removed, so colliders and attach points stay as they are.
    /// </summary>
    internal static class Miniature
    {
        private const float Length = 0.7f; // metres, longest side of the miniature

        internal static void Apply(GameObject item, GameObject ship)
        {
            foreach (Renderer r in item.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            GameObject model = Build(ship);
            if (!model)
            {
                Plugin.Log.LogWarning("Could not build the miniature longship; the folded ship is invisible when dropped or held.");
                return;
            }
            // VisEquipment shows the "attach" child in the hand; on the ground the whole prefab is shown.
            // If the hammer has an attach child, the model goes there so it appears in both places once.
            Transform attach = FindChild(item.transform, "attach");
            Transform parent = attach ? attach : item.transform;
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Plugin.Log.LogInfo($"Miniature longship attached to '{parent.name}'.");
        }

        private static Transform FindChild(Transform t, string name)
        {
            foreach (Transform c in t)
            {
                if (c.name == name) return c;
                Transform r = FindChild(c, name);
                if (r) return r;
            }
            return null;
        }

        private static bool ActiveInPrefab(Transform t, Transform root)
        {
            for (Transform x = t; x && x != root; x = x.parent)
                if (!x.gameObject.activeSelf) return false;
            return true;
        }

        private static GameObject Build(GameObject ship)
        {
            var lowerLods = new HashSet<Renderer>();
            foreach (LODGroup g in ship.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = g.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                    foreach (Renderer r in lods[i].renderers)
                        if (r) lowerLods.Add(r);
            }

            var root = new GameObject("SkidbladnirMiniature");
            var inner = new GameObject("model");
            inner.transform.SetParent(root.transform, false);

            Matrix4x4 toShip = ship.transform.worldToLocalMatrix;
            bool any = false;
            Bounds bounds = default;
            int count = 0;
            foreach (MeshRenderer mr in ship.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.enabled || lowerLods.Contains(mr) || !ActiveInPrefab(mr.transform, ship.transform)) continue;
                if (mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                if (IsHelper(mr)) continue;

                Matrix4x4 m = toShip * mr.transform.localToWorldMatrix;
                var go = new GameObject(mr.name);
                go.layer = 0;
                go.transform.SetParent(inner.transform, false);
                go.transform.localPosition = m.GetColumn(3);
                go.transform.localRotation = m.rotation;
                go.transform.localScale = m.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                MeshRenderer copy = go.AddComponent<MeshRenderer>();
                copy.sharedMaterials = mr.sharedMaterials;

                Bounds b = mf.sharedMesh.bounds;
                Vector3 c = b.center, e = b.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
                count++;
            }
            if (!any)
            {
                Object.Destroy(root);
                return null;
            }
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = longest > 0.01f ? Length / longest : 1f;
            inner.transform.localPosition = -bounds.center * scale;
            inner.transform.localScale = Vector3.one * scale;
            Plugin.Log.LogInfo($"Miniature longship: {count} meshes, size {bounds.size}, scale {scale:0.000}.");
            return root;
        }

        // Water masks and similar helpers would punch holes or draw oddly at this size.
        private static bool IsHelper(MeshRenderer mr)
        {
            string n = mr.name.ToLowerInvariant();
            if (n.Contains("watermask") || n.Contains("water_mask") || n.Contains("collider")) return true;
            foreach (Material mat in mr.sharedMaterials)
            {
                if (!mat || !mat.shader) continue;
                string s = mat.shader.name;
                if (s.Contains("WaterMask") || s.Contains("Hidden/")) return true;
            }
            return false;
        }
    }
}
