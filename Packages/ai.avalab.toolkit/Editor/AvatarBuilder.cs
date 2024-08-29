/**
 * Copyright 2024 Nameraka Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using AI.Avalab.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if VRC_AVATAR_SDK3
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace AI.Avalab.ToolkitEditor
{
    public class AvatarBuilder
    {
        static public List<string> WhitelistComponents = new()
        {
            "AimConstraint",
            "Animation",
            "Animator",
            "AudioSource",
            "Camera",
            "Cloth",
            "Collider",
            "FlareLayer",
            "Joints",
            "Light",
            "LineRenderer",
            "LookAtConstraint",
            "MeshFilter",
            "MeshRenderer",
            "ParentConstraint",
            "ParticleSystemRenderer",
            "ParticleSystem",
            "PositionConstraint",
            "Rigidbody",
            "RotationConstraint",
            "ScaleConstraint",
            "SkinnedMeshRenderer",
            "TrailRenderer",
            "Transform",
        };

        public class BuildAvatarPaths
        {
            public string avatarFilePath;
            public string thumbnailFilePath;
        }

        public class ScreenShots
        {
            public Texture2D bustUp;
            public Texture2D fullBody;
        }

        static public ScreenShots TakeTestScreenshot(Animator targetAvatar)
        {
            var avatar = UnityEngine.Object.Instantiate(targetAvatar.gameObject);
            avatar.transform.position = Vector3.zero;

#if VRC_AVATAR_SDK3
            VRCBuildPipelineCallbacks.OnPreprocessAvatar(avatar);
#endif
            int layerNo = 21;
            void SetLayer(Transform transform)
            {
                transform.gameObject.layer = layerNo;
                foreach (Transform child in transform)
                {
                    SetLayer(child);
                }
            }
            SetLayer(avatar.transform);

            // カメラを用意
            var cameraGameObject = new GameObject();
            var camera = cameraGameObject.AddComponent<Camera>();
            camera.cullingMask = 1 << layerNo;
            camera.fieldOfView = 30;
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = new Color(1, 1, 1, 1);

            AnimationMode.StartAnimationMode();
            var testAnimationGuids = AssetDatabase.FindAssets("avalab_test_pose1 t:animation");
            if (testAnimationGuids.Length == 0)
            {
                Debug.LogError("必要なアニメーションファイルがありません");
                return null;
            }
            var testAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(testAnimationGuids[0]));
            if (testAnimation == null)
            {
                Debug.LogError("必要なアニメーションファイルがありません");
                return null;
            }

            var screenshots = new ScreenShots();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(avatar, testAnimation, 0);
            AnimationMode.EndSampling();

            camera.targetTexture = new RenderTexture(599, 599, 24);
            RenderTexture.active = camera.targetTexture;

            {
                float distanceScaleFactor = 1.0f;
                Bounds bounds = CalculateBoundsForEachChildren(targetAvatar.gameObject);
                float fov = camera.fieldOfView / 360.0f * 2 * Mathf.PI;
                Vector3 pos = bounds.center;
                float max = Mathf.Max(bounds.extents.x, bounds.extents.y) * distanceScaleFactor;
                float tan = Mathf.Tan(fov / 2);
                pos.z += bounds.extents.z / 2 + max / tan;
                Quaternion rot = Quaternion.LookRotation(bounds.center - pos, Vector3.up);
                camera.transform.position = pos;
                camera.transform.rotation = rot;

                camera.Render();

                screenshots.fullBody = new Texture2D(599, 599, TextureFormat.ARGB32, false);
                screenshots.fullBody.ReadPixels(new Rect(0, 0, 599, 599), 0, 0);
                screenshots.fullBody.Apply();
            }

            {
                float distanceScaleFactor = 1.0f;
                Bounds fullbodyBounds = CalculateBoundsForEachChildren(targetAvatar.gameObject);
                Bounds bounds = CalculateBoundsForFace(targetAvatar, fullbodyBounds);
                float fov = camera.fieldOfView / 360.0f * 2 * Mathf.PI;
                Vector3 pos = bounds.center;
                float max = Mathf.Max(bounds.extents.x, bounds.extents.y) * distanceScaleFactor;
                float tan = Mathf.Tan(fov / 2);
                pos.z += bounds.extents.z / 2 + max / tan;
                Quaternion rot = Quaternion.LookRotation(bounds.center - pos, Vector3.up);
                camera.transform.position = pos;
                camera.transform.rotation = rot;

                camera.Render();

                RenderTexture.active = camera.targetTexture;
                screenshots.bustUp = new Texture2D(599, 599, TextureFormat.ARGB32, false);
                screenshots.bustUp.ReadPixels(new Rect(0, 0, 599, 599), 0, 0);
                screenshots.bustUp.Apply();
            }

            AnimationMode.StopAnimationMode();

            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(avatar);

            return screenshots;
        }

        internal static List<string> ListAvatarNames()
        {
            List<string> avatarNames = new();

            void listAvatar(Transform transform)
            {
#if VRC_AVATAR_SDK3
                var avatar = transform.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                if (avatar != null && avatar.isActiveAndEnabled)
                {
                    avatarNames.Add(transform.name);
                }
#else
                var animator = transform.GetComponent<Animator>();
                if (animator != null && animator.isHuman && animator.isActiveAndEnabled)
                {
                    avatarNames.Add(transform.name);
                }
#endif
                for (var i = 0; i < transform.childCount; i++)
                {
                    listAvatar(transform.GetChild(i));
                }
            }
            foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                listAvatar(rootGameObject.transform);
            }
            return avatarNames;
        }

        static public BuildAvatarPaths BuildAvatar(Animator targetAvatar)
        {
            var avatar = UnityEngine.Object.Instantiate(targetAvatar.gameObject);
            avatar.transform.position = Vector3.zero;

#if VRC_AVATAR_SDK3
            VRCBuildPipelineCallbacks.OnPreprocessAvatar(avatar);
#endif

            void removeComponentByWhitelist(Transform obj)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj.gameObject);
                foreach (var component in obj.gameObject.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        continue;
                    }
                    if (WhitelistComponents.IndexOf(component.GetType().Name) == -1)
                    {
                        UnityEngine.Object.DestroyImmediate(component);
                    }
                }
                for (var i = 0; i < obj.childCount; i++)
                {
                    removeComponentByWhitelist(obj.GetChild(i));
                }
            };
            removeComponentByWhitelist(avatar.transform);

            // スクリーンショットを撮る
            int reverveLayerNo = 21;
            void SetLayer(Transform transform, int layerNo)
            {
                transform.gameObject.layer = layerNo;
                foreach (Transform child in transform)
                {
                    SetLayer(child, layerNo);
                }
            }
            SetLayer(avatar.transform, reverveLayerNo);

            // カメラを用意
            var cameraGameObject = new GameObject();
            var camera = cameraGameObject.AddComponent<Camera>();
            camera.cullingMask = 1 << reverveLayerNo;
            camera.fieldOfView = 30;
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = new Color(1, 1, 1, 1);

            camera.targetTexture = new RenderTexture(599, 599, 24);
            RenderTexture.active = camera.targetTexture;

            float viewHeight = 1.5f;
            viewHeight = targetAvatar.GetBoneTransform(HumanBodyBones.Head).position.y * 1.0f;
#if VRC_AVATAR_SDK3
            var vrcAvatar = targetAvatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            viewHeight = vrcAvatar.ViewPosition.y;
#endif
            camera.transform.position = new Vector3(0, viewHeight, 1.5f);
            camera.transform.LookAt(new Vector3(0, viewHeight, 0), Vector3.up);

            AnimationMode.StartAnimationMode();
            var testAnimationGuids = AssetDatabase.FindAssets("avalab_test_pose1 t:animation");
            if (testAnimationGuids.Length == 0)
            {
                throw new Exception("必要なアニメーションファイルがないためアバターをビルドできません");
            }
            var testAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(testAnimationGuids[0]));
            if (testAnimation == null)
            {
                throw new Exception("必要なアニメーションファイルがないためアバターをビルドできません");
            }

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(avatar, testAnimation, 0);
            AnimationMode.EndSampling();

            camera.Render();

            var thumbnailTexture = new Texture2D(599, 599, TextureFormat.ARGB32, false);
            thumbnailTexture.ReadPixels(new Rect(0, 0, 599, 599), 0, 0);
            thumbnailTexture.Apply();

            AnimationMode.StopAnimationMode();

            var thumbnailPath = Application.temporaryCachePath + "/" + GUID.Generate() + ".png";
            File.WriteAllBytes(thumbnailPath, thumbnailTexture.EncodeToPNG());

            SetLayer(avatar.transform, 0);
            UnityEngine.Object.DestroyImmediate(cameraGameObject);

            var manifest = ScriptableObject.CreateInstance<AvatarManifest>();

            var directoryPath = "Assets/" + GUID.Generate();
            Directory.CreateDirectory(directoryPath);

            var animatorPath = directoryPath + "/" + "animator.prefab";
            manifest.rootAnimator = PrefabUtility.SaveAsPrefabAsset(avatar, animatorPath).GetComponent<Animator>();

            var manifestPath = directoryPath + "/" + "manifest.asset";
            AssetDatabase.CreateAsset(manifest, manifestPath);

            var assetNames = new List<string>
            {
                manifestPath,
                animatorPath
            };
            foreach (var skinnedMeshRenderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (var material in skinnedMeshRenderer.sharedMaterials)
                {
                    var materialAssetName = AssetDatabase.GetAssetPath(material);
                    if (materialAssetName == null)
                    {
                        continue;
                    }
                    if (assetNames.IndexOf(materialAssetName) != -1)
                    {
                        continue;
                    }
                    assetNames.Add(materialAssetName);
                }
            }

            foreach (var meshRenderer in avatar.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    var materialAssetName = AssetDatabase.GetAssetPath(material);
                    if (materialAssetName == null)
                    {
                        continue;
                    }
                    if (assetNames.IndexOf(materialAssetName) != -1)
                    {
                        continue;
                    }
                    assetNames.Add(materialAssetName);
                }
            }

            var assetBundleBuilds = new List<AssetBundleBuild>();
            var assetBundleBuild = new AssetBundleBuild
            {
                assetBundleName = "m.assetbundle",
                assetNames = assetNames.ToArray()
            };
            assetBundleBuilds.Add(assetBundleBuild);

            var outputPath = Application.temporaryCachePath;
            var outputBundlePath = outputPath + "/" + assetBundleBuild.assetBundleName;
            if (File.Exists(outputBundlePath))
            {
                File.Delete(outputBundlePath);
            }
            BuildPipeline.BuildAssetBundles(outputPath, assetBundleBuilds.ToArray(), BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneLinux64);

            AssetDatabase.DeleteAsset(directoryPath);
            UnityEngine.Object.DestroyImmediate(avatar);

            var returnValue = new BuildAvatarPaths()
            {
                avatarFilePath = outputBundlePath,
                thumbnailFilePath = thumbnailPath,
            };
            return returnValue;

        }

        internal static List<string> GetDennyComponentNames(Animator avatar)
        {
            var dennyComponentNames = new List<string>();
            void listDennyComponents(Transform transform)
            {
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        continue;
                    }
                    var componentName = component.GetType().Name;
                    if (WhitelistComponents.IndexOf(componentName) != -1)
                    {
                        continue;
                    }
#if VRC_AVATAR_SDK3
                    if (component as VRC.SDKBase.IEditorOnly != null)
                    {
                        continue;
                    }
#endif
                    if (dennyComponentNames.IndexOf(componentName) == -1)
                    {
                        dennyComponentNames.Add(componentName);
                    }
                }
                for (var i = 0; i < transform.childCount; i++)
                {
                    listDennyComponents(transform.GetChild(i));
                }
            }
            listDennyComponents(avatar.transform);

            return dennyComponentNames;
        }

        internal static List<GameObject> GetMissingComponents(Animator animator)
        {
            var missingComponentObject = new List<GameObject>();
            void findMissingComponent(Transform transform)
            {
                if (transform.GetComponents<Component>().Any(com => com == null))
                {
                    missingComponentObject.Add(transform.gameObject);
                }
                for (var i = 0; i < transform.childCount; i++)
                {
                    findMissingComponent(transform.GetChild(i));
                }
            }
            findMissingComponent(animator.transform);

            return missingComponentObject;
        }

        internal static float GetAvatarHeight(Animator animator)
        {
            float avatarHeight = 1.5f;
            avatarHeight = animator.GetBoneTransform(HumanBodyBones.Hips).position.y * 2.0f;
#if VRC_AVATAR_SDK3
            var avatar = animator.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (avatar)
            {
                avatarHeight = avatar.ViewPosition.y * 1.1f;
            }
#endif
            return avatarHeight;
        }

        internal static Vector3 GetAvatarBoundsSize(Animator animator)
        {
            var boundsMin = Vector3.positiveInfinity;
            var boundsMax = Vector3.negativeInfinity;
            foreach (var meshRenderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var rootBone = meshRenderer.rootBone != null ? meshRenderer.rootBone : meshRenderer.transform;
                var max = meshRenderer.bounds.max;
                var min = meshRenderer.bounds.min;
                var boundCorners = new List<Vector3>() {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z),
                };
                foreach (var corner in boundCorners)
                {
                    var worldCorner = rootBone.TransformPoint(corner);
                    boundsMin = Vector3.Min(boundsMin, worldCorner);
                    boundsMax = Vector3.Max(boundsMax, worldCorner);
                }
            }
            boundsMin -= animator.transform.position;
            boundsMax -= animator.transform.position;
            var boundsSize = boundsMax - boundsMin;

            return boundsSize;
        }

        internal static Bounds CalculateBoundsForEachChildren(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds();

            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }

        internal static Bounds CalculateBoundsForFace(Animator anim, Bounds fullbody)
        {
            var bodyC = fullbody.center;
            var top = new Vector3(bodyC.x, fullbody.extents.y + bodyC.y, bodyC.z);
            var chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                     ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                     ?? anim.GetBoneTransform(HumanBodyBones.Spine)
                     ?? throw new System.InvalidOperationException(
                        "Cannot find chest bone from given model. Please make sure it is a Unity Humanoid compatible."
                    );

            var bottom = new Vector3(bodyC.x, chest.transform.position.y, bodyC.z);

            var center = (top + bottom) / 2;
            var extents = new Vector3(fullbody.extents.x, top.y - bottom.y, fullbody.extents.z);

            return new Bounds(center, extents);
        }
    }

}
