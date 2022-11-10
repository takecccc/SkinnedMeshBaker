// Created by SHAJIKUworks

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SupportScripts
{
    public static class SkinnedMeshBaker
    {
        static readonly string PROGRESS_TITLE = "SkinnedMeshBaker";

        [MenuItem("GameObject/BakeSkinnedMeshPose", false, 20)]
        public static void Execution()
        {
            BakeMeshes(Selection.activeGameObject, "BakeResult/");
        }

        public static GameObject BakeMeshes(GameObject root, string outputPath)
        {
            if (root == null)
            {
                Debug.LogError("対象GameObjectがnullです");
                return null;
            }

            EditorUtility.DisplayProgressBar(PROGRESS_TITLE, "Copy & Bake...", 0f);

            // SkinnedではないMeshのコピーを生成
            var newMeshObjects = new List<GameObject>();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(m => m.enabled))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var newMeshObject = GenerateMeshObject("Clone_" + filter.sharedMesh.name, filter.sharedMesh, renderer);
                newMeshObject.transform.SetParent(renderer.transform.parent);
                CopyTransform(renderer.transform, newMeshObject.transform);

                newMeshObjects.Add(newMeshObject);
            }

            // ボーンに対応していないSkinnedMeshをMeshとしてコピー生成
            foreach (var skin in GetSkinnedMeshesInChildrenWithoutBones(root).Where(m => m.enabled))
            {
                var newMeshObject = GenerateMeshObject("Clone_" + skin.sharedMesh.name, skin.sharedMesh, skin);
                newMeshObject.transform.SetParent(skin.transform.parent);
                CopyTransform(skin.transform, newMeshObject.transform, true, true, false);

                newMeshObjects.Add(newMeshObject);
            }

            // SkinnedMeshを現在の形状でBake
            var skinnedMeshes = GetSkinnedMeshesInChildrenWithBones(root)
                .Where(s => s.enabled && s.sharedMesh != null).Select(s => new { skin = s, mesh = new Mesh() }).ToList();
            foreach (var s in skinnedMeshes)
            {
                // Bake後の情報がズレるのでTransformを初期化してからBake
                var skinTransform = s.skin.transform;
                var beforeTransformParams = new { skinTransform.localPosition, skinTransform.localRotation, skinTransform.localScale };
                skinTransform.localPosition = Vector3.zero;
                skinTransform.localRotation = Quaternion.identity;
                skinTransform.localScale = Vector3.one;

                s.skin.BakeMesh(s.mesh);

                skinTransform.localPosition = beforeTransformParams.localPosition;
                skinTransform.localRotation = beforeTransformParams.localRotation;
                skinTransform.localScale = beforeTransformParams.localScale;
            }

            // Bake後のMeshオブジェクトを生成
            var meshIndex = 0;
            foreach (var s in skinnedMeshes)
            {
                var newMesh = s.mesh;
                newMesh.name = "mesh" + (++meshIndex).ToString("000") + "_" + s.skin.sharedMesh.name;

                var newMeshObject = GenerateMeshObject(newMesh.name, newMesh, s.skin);
                var newMeshTransform = newMeshObject.transform;
                newMeshTransform.SetParent(s.skin.transform.parent);
                newMeshTransform.localPosition = Vector3.zero;
                newMeshTransform.localRotation = Quaternion.identity;
                // Scaleは元を保ったままにするため初期化しない

                newMeshObjects.Add(newMeshObject);
            }

            // Meshがひとつもなければ終了
            if (meshIndex == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(PROGRESS_TITLE, "Bake対象のMeshが存在しません.", "OK");
                return null;
            }

            // Bake後のオブジェクトツリーを生成
            var newRoot = new GameObject("Baked_" + root.name).transform;
            CopyTransform(root.transform, newRoot.transform);

            foreach (var f in newMeshObjects)
                f.transform.SetParent(newRoot);

            // 同Materialのサブメッシュを統合したい
            // むしろコンバインしたい

            // Bake後のMeshを保存
            var destDirectory = Path.Combine(Path.Combine("Assets", outputPath), newRoot.gameObject.name);
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            var count = 0;
            foreach (var s in skinnedMeshes)
            {
                EditorUtility.DisplayProgressBar(PROGRESS_TITLE, "Meshの保存中...[" + s.mesh.name + "]", 1f * (++count) / meshIndex);
                SaveAsset(s.mesh, Path.Combine(destDirectory, s.mesh.name + ".asset"));
            }

            // Prefabの保存
            var prefabPath = Path.Combine(destDirectory, newRoot.name + ".prefab").Replace("\\", "/");
            PrefabUtility.CreatePrefab(prefabPath, newRoot.gameObject);
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog(PROGRESS_TITLE, "MeshのBakeが完了しました.\n" + prefabPath, "OK");

            return newRoot.gameObject;
        }

        static IEnumerable<SkinnedMeshRenderer> GetSkinnedMeshesInChildrenWithBones(GameObject target)
        {
            return target.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.bones.Any());
        }

        static IEnumerable<SkinnedMeshRenderer> GetSkinnedMeshesInChildrenWithoutBones(GameObject target)
        {
            return target.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => !s.bones.Any());
        }

        static GameObject GenerateMeshObject(string objName, Mesh mesh, Renderer renderer)
        {
            var newMeshObject = new GameObject(objName);
            var newMeshFilter = newMeshObject.AddComponent<MeshFilter>();
            var newMeshRenderer = newMeshObject.AddComponent<MeshRenderer>();
            var newMeshTransform = newMeshObject.transform;

            newMeshFilter.sharedMesh = mesh;
            CopyRenderer(renderer, newMeshRenderer);

            return newMeshObject;
        }

        static void CopyTransform(Transform source, Transform dest, bool isCopyPosition = true, bool isCopyRotation = true, bool isCopyScale = true)
        {
            if (source == null || dest == null)
                return;

            var beforeSourceParent = source.parent;
            var beforeDestParent = dest.parent;

            source.SetParent(null);
            dest.SetParent(null);

            if (isCopyPosition)
                dest.position = source.position;
            if (isCopyRotation)
                dest.rotation = source.rotation;
            if (isCopyScale)
                dest.localScale = source.localScale;

            source.SetParent(beforeSourceParent);
            dest.SetParent(beforeDestParent);
        }

        static void CopyRenderer(Renderer source, Renderer dest)
        {
            dest.lightProbeUsage = source.lightProbeUsage;
            dest.reflectionProbeUsage = source.reflectionProbeUsage;
            dest.probeAnchor = source.probeAnchor;
            dest.shadowCastingMode = source.shadowCastingMode;
            dest.receiveShadows = source.receiveShadows;
            dest.motionVectorGenerationMode = source.motionVectorGenerationMode;

            dest.sharedMaterials = source.sharedMaterials;
            dest.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        }

        static Object SaveAsset(Object asset, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            return asset;
        }
    }
}