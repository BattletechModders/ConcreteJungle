using BattleTech.Rendering.UI;
using BattleTech.UI;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMashup.UI
{
    // Lifted and modified from MPstark's AI Toolkit -
    // see https://github.com/mpstark/AIToolkit/blob/master/AIToolkit/Features/UI/InfluenceMapVisualization.cs
    // no license, but he agreed to let me use 'anything in it' in a DM
    internal class OriginVisualization
    {
        internal GameObject TopLevelGO;

        private List<GameObject> dots = new List<GameObject>();
        Vector3 originHex = Vector3.zero;
        private Mesh circleMesh = GenerateCircleMesh(4, 20); // Generate a mesh with a 4 meter radius and 20 verts
        private Vector3 groundOffset = 2 * Vector3.up;

        public OriginVisualization(Vector3 origin)
        {
            Mod.Log.Info?.Write($"Creating footprint visualization for origin: {origin}");

            string goName = $"CG_Ambush_Origin={origin}";
            TopLevelGO = new GameObject(goName);
            TopLevelGO.SetActive(false);
            TopLevelGO.transform.position = origin;

            originHex = SharedState.Combat.HexGrid.GetClosestPointOnGrid(origin);
        }

        public void Init()
        {
            if (dots.Count == 0)
            {
                // Initialize Dots
                try
                {
                    GameObject dot = CreateDot(originHex, Color.red);
                    this.dots.Add(dot);
                }
                catch (Exception e)
                {
                    Mod.Log.Warn?.Write(e, "Failed to create hex point!");
                }
            }
        }

        public void Show()
        {
            TopLevelGO.SetActive(true);
        }

        public void Hide()
        {
            TopLevelGO.SetActive(false);
        }

        public void Destroy()
        {
            GameObject.Destroy(TopLevelGO);
        }

        private GameObject CreateDot(Vector3 location, Color color)
        {
            string dotName = $"dot_{this.dots.Count}";
            GameObject dot = new GameObject(dotName);
            dot.transform.SetParent(TopLevelGO.transform);

            var meshFilter = dot.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = circleMesh;

            var meshRenderer = dot.AddComponent<MeshRenderer>();
            var movementDot = CombatMovementReticle.Instance.movementDotTemplate;
            meshRenderer.material = movementDot.GetComponent<MeshRenderer>().sharedMaterial;
            meshRenderer.material.enableInstancing = false;
            meshRenderer.receiveShadows = false;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var collider = dot.AddComponent<CapsuleCollider>();
            collider.center = Vector3.zero;
            collider.radius = 5f;
            collider.height = .5f;
            collider.isTrigger = true;

            dot.AddComponent<UISweep>();

            Vector3 terrainHeight = SharedState.Combat.MapMetaData.GetLerpedHeightAt(location, true) * Vector3.up;
            dot.transform.position = location + groundOffset + terrainHeight;
            dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var renderer = dot.GetComponent<MeshRenderer>();
            renderer.material.color = color;

            return dot;
        }

        private static Mesh GenerateCircleMesh(float radius, int numberOfPoints)
        {
            // from https://answers.unity.com/questions/944228/creating-a-smooth-round-flat-circle.html
            // not subject to license
            var angleStep = 360.0f / numberOfPoints;
            var vertexList = new List<Vector3>();
            var triangleList = new List<int>();
            var quaternion = Quaternion.Euler(0.0f, 0.0f, angleStep);

            vertexList.Add(new Vector3(0.0f, 0.0f, 0.0f));
            vertexList.Add(new Vector3(0.0f, radius, 0.0f));
            vertexList.Add(quaternion * vertexList[1]);
            triangleList.Add(0);
            triangleList.Add(1);
            triangleList.Add(2);

            for (var i = 0; i < numberOfPoints - 1; i++)
            {
                triangleList.Add(0);
                triangleList.Add(vertexList.Count - 1);
                triangleList.Add(vertexList.Count);
                vertexList.Add(quaternion * vertexList[vertexList.Count - 1]);
            }
            var mesh = new Mesh
            {
                vertices = vertexList.ToArray(),
                triangles = triangleList.ToArray()
            };

            return mesh;
        }
    }


}
