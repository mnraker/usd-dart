// Copyright 2019 Jeremy Cowles. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;
using Scene = USD.NET.Scene;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.Formats.USD.Tests
{
    public class FBXHandednessModeTests : ScriptableObject
    {
        // Cannot store FBX as Object that can be set, as the fileID (stored in the meta file of this script), changes
        // depending on the Unity version.
        public Object withCameraUsd;
        public Object leftHandedWithCameraUsd;
        public Object bakedLeftHandedCube_slowAndSafeAsFbx;
        public Object bakedLeftHandedCube_slowAndSafe;
        public Object bakedLeftHandedCube_none;

        private GameObject fbxRoot;
        private GameObject usdRoot;

        private GameObject LoadUSD(Object usdObject, BasisTransformation changeHandedness = BasisTransformation.SlowAndSafeAsFBX)
        {
            InitUsd.Initialize();
            var usdPath = Path.GetFullPath(AssetDatabase.GetAssetPath(usdObject));
            var stage = pxr.UsdStage.Open(usdPath, pxr.UsdStage.InitialLoadSet.LoadNone);
            var scene = Scene.Open(stage);
            var importOptions = new SceneImportOptions();
            importOptions.changeHandedness = changeHandedness;
            importOptions.scale = 0.01f;
            importOptions.materialImportMode = MaterialImportMode.ImportDisplayColor;
            var usdRoot = ImportHelpers.ImportSceneAsGameObject(scene, importOptions: importOptions);
            scene.Close();
            return usdRoot;
        }

        [SetUp]
        public void SetUp()
        {
            var fbxPath = AssetDatabase.GUIDToAssetPath(TestDataGuids.CameraRelated.CameraIncludedFbx);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            fbxRoot = PrefabUtility.InstantiatePrefab(asset) as GameObject;

            usdRoot = LoadUSD(withCameraUsd);
        }

        [Test]
        public void LoadAsFbxCompareCameraTransforms()
        {
            // Compare camera transforms
            var usdCamTr = usdRoot.transform.Find("group2/camera1");
            var fbxCamTr = fbxRoot.transform.Find("camera1");

            Debug.Log("Comparing camera positions...");
            Assert.AreApproximatelyEqual(usdCamTr.position.x, fbxCamTr.position.x);
            Assert.AreApproximatelyEqual(usdCamTr.position.y, fbxCamTr.position.y);
            Assert.AreApproximatelyEqual(usdCamTr.position.z, fbxCamTr.position.z);
            Debug.Log("Comparing camera rotations...");
            Assert.AreApproximatelyEqual(usdCamTr.localRotation.x, fbxCamTr.localRotation.x);
            Assert.AreApproximatelyEqual(usdCamTr.localRotation.y, fbxCamTr.localRotation.y);
            Assert.AreApproximatelyEqual(usdCamTr.localRotation.z, fbxCamTr.localRotation.z);
        }

        [Test]
        public void LoadAsFbxCompareMeshTransforms()
        {
            // Compare camera transforms
            var usdMeshTr = usdRoot.transform.Find("group2/group1/pCube1");
            var fbxMeshTr = fbxRoot.transform.Find("group1/pCube1");

            Debug.Log("Comparing mesh positions...");
            Assert.AreApproximatelyEqual(usdMeshTr.position.x, fbxMeshTr.position.x);
            Assert.AreApproximatelyEqual(usdMeshTr.position.y, fbxMeshTr.position.y);
            Assert.AreApproximatelyEqual(usdMeshTr.position.z, fbxMeshTr.position.z);
            Debug.Log("Comparing mesh rotations...");
            Assert.AreApproximatelyEqual(usdMeshTr.localRotation.x, fbxMeshTr.localRotation.x);
            Assert.AreApproximatelyEqual(usdMeshTr.localRotation.y, fbxMeshTr.localRotation.y);
            Assert.AreApproximatelyEqual(usdMeshTr.localRotation.z, fbxMeshTr.localRotation.z);
        }

        private static bool CheckVector3Equality(Vector3 a, Vector3 b, float epsilon = 0.001f)
        {
            return Vector3.SqrMagnitude(a - b) < (epsilon * epsilon);
        }

        [TestCase(BasisTransformation.SlowAndSafe)]
        [TestCase(BasisTransformation.SlowAndSafeAsFBX)]
        [TestCase(BasisTransformation.None)]
        [TestCase(BasisTransformation.FastWithNegativeScale)]
        public void TestLeftHandedUsdMeshImport(BasisTransformation basisTransformation)
        {
            // set the baked mesh according to the basis transformation
            Object bakedLeftHandedMesh = null;
            switch (basisTransformation)
            {
                case BasisTransformation.SlowAndSafe:
                    bakedLeftHandedMesh = bakedLeftHandedCube_slowAndSafe;
                    break;
                case BasisTransformation.SlowAndSafeAsFBX:
                    bakedLeftHandedMesh = bakedLeftHandedCube_slowAndSafeAsFbx;
                    break;
                case BasisTransformation.None:
                case BasisTransformation.FastWithNegativeScale:
                    // When comparing the mesh, importing with FastWithNegativeScale and None
                    // will give the same result.
                    bakedLeftHandedMesh = bakedLeftHandedCube_none;
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            var rightHandedUsdRoot = LoadUSD(withCameraUsd, basisTransformation);
            Assert.IsNotNull(rightHandedUsdRoot);

            // Compare import of Left handed USD to right handed USD
            var leftHandedUsdRoot = LoadUSD(leftHandedWithCameraUsd, basisTransformation);
            Assert.IsNotNull(leftHandedUsdRoot);

            // check that the mesh does not match the right handed one
            var usdCube = rightHandedUsdRoot.transform.Find("group2/group1/pCube1");
            Assert.IsNotNull(usdCube);
            var leftHandedUsdCube = leftHandedUsdRoot.transform.Find("group2/group1/pCube1");
            Assert.IsNotNull(leftHandedUsdCube);

            var cubeMesh = usdCube.GetComponent<MeshFilter>().sharedMesh;
            var leftHandedCubeMesh = leftHandedUsdCube.GetComponent<MeshFilter>().sharedMesh;

            // The two files are different handedness (different winding order of vertices).
            // Test that both meshes have triangles using the same vertex coordinates, but with a different winding
            // order
            NUnit.Framework.Assert.That(leftHandedCubeMesh.vertices.Length, Is.EqualTo(cubeMesh.vertices.Length));
            NUnit.Framework.Assert.That(leftHandedCubeMesh.triangles.Length, Is.EqualTo(cubeMesh.triangles.Length));
            NUnit.Framework.Assert.That(leftHandedCubeMesh.normals.Length, Is.EqualTo(cubeMesh.normals.Length));

            // For each triangle
            int[] rightHandedToLeftHanded = {1, 0, 2};
            for (int t = 0; t < cubeMesh.triangles.Length % 3; t++)
            {
                // For each vertex
                for (int v = 0; v < 3; v++)
                {
                    // Check that vertex coordinates are the same (respect the winding order)
                    Assert.IsTrue(CheckVector3Equality(cubeMesh.vertices[cubeMesh.triangles[t*3+v]], leftHandedCubeMesh.vertices[leftHandedCubeMesh.triangles[t*3+rightHandedToLeftHanded[v]]]));

                    // Check that normals are flipped (respect the winding order)
                    Assert.IsTrue(CheckVector3Equality(cubeMesh.normals[cubeMesh.triangles[t*3+v]], -leftHandedCubeMesh.normals[leftHandedCubeMesh.triangles[t*3+rightHandedToLeftHanded[v]]]),
                        string.Format("Normals differ on a given vertex between the left and right handed cube meshes, expected equal\nExpected:{1}\nActual:{2}",
                            cubeMesh.normals[cubeMesh.triangles[t*3]], -leftHandedCubeMesh.normals[leftHandedCubeMesh.triangles[t*3+1]]));
                }
            }

            // Check that the imported left handed cube matches the baked cube.
            var bakedCubeMesh = bakedLeftHandedMesh as Mesh;
            Assert.IsNotNull(bakedCubeMesh);

            NUnit.Framework.Assert.That(leftHandedCubeMesh.triangles.Length, Is.EqualTo(bakedCubeMesh.triangles.Length));

            // For each triangle
            for (int t = 0; t < leftHandedCubeMesh.triangles.Length % 3; t++)
            {
                // For each vertex
                for (int v = 0; v < 3; v++)
                {
                    // Check that vertex coordinates are the same
                    Assert.IsTrue(CheckVector3Equality(leftHandedCubeMesh.vertices[leftHandedCubeMesh.triangles[t*3+v]], bakedCubeMesh.vertices[bakedCubeMesh.triangles[t*3+v]]));

                    // Check that normals are the same
                    Assert.IsTrue(CheckVector3Equality(leftHandedCubeMesh.normals[leftHandedCubeMesh.triangles[t*3+v]], bakedCubeMesh.normals[bakedCubeMesh.triangles[t*3+v]]),
                        string.Format("Normals differ on a given vertex between the left-handed and baked meshes, expected equal\nExpected:{1}\nActual:{2}",
                            leftHandedCubeMesh.normals[leftHandedCubeMesh.triangles[t*3]], bakedCubeMesh.normals[bakedCubeMesh.triangles[t*3]]));
                }
            }
        }
    }
}
