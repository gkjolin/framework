﻿using System;
using System.Collections.Generic;
using ActionStreetMap.Core;
using ActionStreetMap.Core.MapCss.Domain;
using ActionStreetMap.Core.Polygons;
using ActionStreetMap.Core.Polygons.Geometry;
using ActionStreetMap.Core.Polygons.Meshing.Iterators;
using ActionStreetMap.Core.Polygons.Tools;
using ActionStreetMap.Core.Polygons.Topology;
using ActionStreetMap.Core.Terrain;
using ActionStreetMap.Core.Tiling.Models;
using ActionStreetMap.Core.Unity;
using ActionStreetMap.Core.Utilities;
using ActionStreetMap.Explorer.Helpers;
using ActionStreetMap.Explorer.Scene.Utils;
using ActionStreetMap.Explorer.Utils;
using ActionStreetMap.Infrastructure.Config;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Unity.Wrappers;
using UnityEngine;
using Color32 = UnityEngine.Color32;
using Mesh = UnityEngine.Mesh;

namespace ActionStreetMap.Explorer.Terrain
{
    internal class MeshTerrainBuilder : ITerrainBuilder, IConfigurable
    {
        private const string LogTag = "mesh.terrain";

        private readonly IElevationProvider _elevationProvider;
        private readonly IResourceProvider _resourceProvider;
        private readonly IGameObjectFactory _gameObjectFactory;
        private readonly MeshCellBuilder _meshCellBuilder;

        [Dependency]
        public ITrace Trace { get; set; }

        private float _maxCellSize = 100;

        [Dependency]
        public MeshTerrainBuilder(IElevationProvider elevationProvider, IResourceProvider resourceProvider,
            IGameObjectFactory gameObjectFactory)
        {
            _elevationProvider = elevationProvider;
            _resourceProvider = resourceProvider;
            _gameObjectFactory = gameObjectFactory;
            _meshCellBuilder = new MeshCellBuilder();
        }

        public IGameObject Build(Tile tile, Rule rule)
        {
            Trace.Debug(LogTag, "Started to build terrain");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var terrainObject = _gameObjectFactory.CreateNew("terrain", tile.GameObject);

            // detect grid parameters
            var cellRowCount = (int)Math.Ceiling(tile.Height / _maxCellSize);
            var cellColumnCount = (int)Math.Ceiling(tile.Width / _maxCellSize);
            var cellHeight = tile.Height/cellRowCount;
            var cellWidth = tile.Width/cellColumnCount;

            Trace.Debug(LogTag, "Building mesh canvas..");
            var meshCanvas = new MeshCanvasBuilder()
                .SetTile(tile)
                .SetScale(MeshCellBuilder.Scale)
                .Build();

            Trace.Debug(LogTag, "Building mesh cells..");
            var tasks = new List<IObservable<Unit>>(cellRowCount*cellColumnCount);
            for (int j = 0; j < cellRowCount; j++)
                for (int i = 0; i < cellColumnCount; i++)
                {
                    var tileBottomLeft = tile.Rectangle.BottomLeft;
                    var rectangle = new Rectangle(
                        tileBottomLeft.X + i * cellWidth,
                        tileBottomLeft.Y + j * cellHeight,
                        cellWidth,
                        cellHeight);
                    var name = String.Format("cell {0}_{1}", i, j);
                    tasks.Add(Observable.Start(() =>
                    {
                        try
                        {
                            var cell = _meshCellBuilder.Build(rectangle, meshCanvas);
                            BuildCell(tile, rule, terrainObject, cell, name);
                        }
                        catch (Exception ex)
                        {
                            Trace.Error(LogTag, ex, "Unable to build {0}", name);
                        }
                    }));
                }

            tasks.WhenAll().Wait();

            sw.Stop();
            Trace.Debug(LogTag, "Terrain is build in {0}ms", sw.ElapsedMilliseconds);

            return terrainObject;
        }

        private void BuildCell(Tile tile, Rule rule, IGameObject terrainObject, MeshCell cell, string name)
        {
            var relativeNullPoint = tile.RelativeNullPoint;
            var gradient = GetGradient();

            var terrainMesh = cell.Mesh;

            var vertices = new List<Vector3>(terrainMesh.Vertices.Count);
            var triangles = new List<int>(terrainMesh.Triangles.Count);
            var colors = new List<Color>(terrainMesh.Vertices.Count);

            var hashMap = new Dictionary<int, int>();
            Trace.Debug(LogTag, "Total triangles: {0}", terrainMesh.Triangles.Count);
            foreach (var triangle in terrainMesh.Triangles)
            {
                var p0 = triangle.GetVertex(0);
                var coord0 = GeoProjection.ToGeoCoordinate(relativeNullPoint,
                    new MapPoint((float) p0.X, (float) p0.Y));
                var ele0 = _elevationProvider.GetElevation(coord0.Latitude, coord0.Longitude);
                if (p0.Type == VertexType.FreeVertex)
                    ele0 += Noise.Perlin3D(new Vector3((float) p0.X, 0, (float) p0.Y), 0.1f);

                vertices.Add(new Vector3((float) p0.X, ele0, (float) p0.Y));

                var p1 = triangle.GetVertex(1);
                var coord1 = GeoProjection.ToGeoCoordinate(relativeNullPoint,
                    new MapPoint((float) p1.X, (float) p1.Y));
                var ele1 = _elevationProvider.GetElevation(coord1.Latitude, coord1.Longitude);
                if (p1.Type == VertexType.FreeVertex)
                    ele1 += Noise.Perlin3D(new Vector3((float)p1.X, 0, (float)p1.Y), 0.1f);
                vertices.Add(new Vector3((float) p1.X, ele1, (float) p1.Y));

                var p2 = triangle.GetVertex(2);
                var coord2 = GeoProjection.ToGeoCoordinate(relativeNullPoint,
                    new MapPoint((float) p2.X, (float) p2.Y));
                var ele2 = _elevationProvider.GetElevation(coord2.Latitude, coord2.Longitude);
                if (p2.Type == VertexType.FreeVertex)
                    ele2 += Noise.Perlin3D(new Vector3((float)p2.X, 0, (float)p2.Y), 0.1f);
                vertices.Add(new Vector3((float) p2.X, ele2, (float) p2.Y));

                var index = vertices.Count;
                triangles.Add(--index);
                triangles.Add(--index);
                triangles.Add(--index);

                var firstValue = (Noise.Perlin3D(new Vector3((float) p0.X, ele0, (float) p0.Y), 0.2f) + 1f)/2f;
                var triangleColor = gradient.Evaluate(firstValue);

                colors.Add(triangleColor);
                colors.Add(triangleColor);
                colors.Add(triangleColor);

                hashMap.Add(triangle.GetHashCode(), index);
            }
            FillRegions(tile, cell, vertices, triangles, colors, hashMap);

            var goCell = _gameObjectFactory.CreateNew(name, terrainObject);
            Scheduler.MainThread.Schedule(() => BuildGameObject(rule, goCell, vertices, triangles, colors));
        }

        private void FillRegions(Tile tile, MeshCell cell, List<Vector3> vertices, List<int> triangles, List<Color> colors, Dictionary<int, int> hashMap)
        {
            Trace.Debug(LogTag, "Start FillRegions");
            // TODO this should be refactored
            var tree = new QuadTree(cell.Mesh);
            RegionIterator iterator = new RegionIterator(cell.Mesh);
            var roadDeepLevel = 0.2f;
            foreach (var region in cell.CarRoads.FillRegions)
            {
                var point = region.Anchor;
                var start = (Triangle)tree.Query(point.X, point.Y);
                if (start == null)
                {
                    Trace.Warn(LogTag, "Broken car road region");
                    continue;
                }

                int count = 0;
                var color = Color.red;
   
                iterator.Process(start, triangle =>
                {
                    var index = hashMap[triangle.GetHashCode()];
                    colors[index] = color;
                    colors[index + 1] = color;
                    colors[index + 2] = color;

                    var p1 = vertices[index];
                    float ele1 = 0;
                    if (triangle.GetVertex(0).Type == VertexType.FreeVertex)
                        ele1 = Noise.Perlin3D(new Vector3(p1.x, 0, p1.z), 0.1f);
                    vertices[index] = new Vector3(p1.x, p1.y - roadDeepLevel - ele1, p1.z);

                    var p2 = vertices[index + 1];
                    float ele2 = 0;
                    if (triangle.GetVertex(1).Type == VertexType.FreeVertex)
                        ele2 = Noise.Perlin3D(new Vector3(p2.x, 0, p2.z), 0.1f);
         
                    vertices[index + 1] = new Vector3(p2.x, p2.y - roadDeepLevel - ele2, p2.z);

                    var p3= vertices[index + 2];
                    float ele3 = 0;
                    if (triangle.GetVertex(2).Type == VertexType.FreeVertex)
                        ele3 = Noise.Perlin3D(new Vector3(p3.x, 0, p3.z), 0.1f);
                    vertices[index + 2] = new Vector3(p3.x, p3.y - roadDeepLevel - ele3, p3.z);

                    count++;
                });
                Trace.Debug(LogTag, "Car road region processed: {0}", count);
            }

            foreach (var region in cell.WalkRoads.FillRegions)
            {
                var point = region.Anchor;
                var start = (Triangle)tree.Query(point.X, point.Y);
                if (start == null)
                {
                    Trace.Warn(LogTag, "Broken walk road region");
                    continue;
                }
                int count = 0;
                var color = Color.yellow;

                iterator.Process(start, triangle =>
                {
                    var index = hashMap[triangle.GetHashCode()];
                    colors[index] = color;
                    colors[index + 1] = color;
                    colors[index + 2] = color;

                    count++;
                });
                Trace.Debug(LogTag, "Walk road region processed: {0}", count);
            }

            foreach (var meshRegion in cell.Surfaces)
                foreach (var fillRegion in meshRegion.FillRegions)
                {
                    var point = fillRegion.Anchor;
                    var start = (Triangle) tree.Query(point.X, point.Y);
                    if (start == null)
                    {
                        Trace.Warn(LogTag, "Broken surface region");
                        continue;
                    }
                    int count = 0;
                    var color = GetColorBySplatId(fillRegion.SplatId);
                    iterator.Process(start, triangle =>
                    {
                        var index = hashMap[triangle.GetHashCode()];
                        colors[index] = color;
                        colors[index + 1] = color;
                        colors[index + 2] = color;
                        count++;
                    });
                    Trace.Debug(LogTag, "Surface region processed: {0}", count);
                }

            const float waterDeepLevel = 5;
            foreach (var region in cell.Water.FillRegions)
            {
                var point = region.Anchor;
                var start = (Triangle)tree.Query(point.X, point.Y);
                if (start == null)
                {
                    Trace.Warn(LogTag, "Broken water region");
                    continue;
                }
                int count = 0;
     
                iterator.Process(start, triangle =>
                {
                    var index = hashMap[triangle.GetHashCode()];

                    var p1 = vertices[index];
                    vertices[index] = new Vector3(p1.x, p1.y - waterDeepLevel, p1.z);

                    var p2 = vertices[index+1];
                    vertices[index + 1] = new Vector3(p2.x, p2.y - waterDeepLevel, p2.z);

                    var p3 = vertices[index+2];
                    vertices[index + 2] = new Vector3(p3.x, p3.y - waterDeepLevel, p3.z);

                    count++;
                });
                Trace.Debug(LogTag, "Water region processed: {0}", count);

            }
            BuildOffsetShape(tile, cell.Water, vertices, triangles, colors, waterDeepLevel);
            BuildOffsetShape(tile, cell.CarRoads, vertices, triangles, colors, roadDeepLevel);
            Trace.Debug(LogTag, "End FillRegions");
        }

        #region Offset processing

        private void BuildOffsetShape(Tile tile, MeshRegion region, List<Vector3> vertices, List<int> triangles, List<Color> colors, float deepLevel)
        {
            foreach (var contour in region.Contours)
            {
                var length = contour.Count;
                var vertOffset = vertices.Count;
                // vertices
                for (int i = 0; i < length; i++)
                {
                    var v2DIndex = i == (length - 1) ? 0 : i + 1;

                    var coord1 = GeoProjection.ToGeoCoordinate(tile.RelativeNullPoint, (float)contour[i].X, (float)contour[i].Y);
                    var ele1 = _elevationProvider.GetElevation(coord1.Latitude, coord1.Longitude);

                    var coord2 = GeoProjection.ToGeoCoordinate(tile.RelativeNullPoint, (float)contour[v2DIndex].X, (float)contour[v2DIndex].Y);
                    var ele2 = _elevationProvider.GetElevation(coord2.Latitude, coord2.Longitude);

                    vertices.Add(new Vector3((float)contour[i].X, ele1, (float)contour[i].Y));
                    vertices.Add(new Vector3((float)contour[v2DIndex].X, ele2, (float)contour[v2DIndex].Y));
                    vertices.Add(new Vector3((float)contour[v2DIndex].X, ele2 - deepLevel, (float)contour[v2DIndex].Y));
                    vertices.Add(new Vector3((float)contour[i].X, ele1 - deepLevel, (float)contour[i].Y));

                    colors.Add(Color.magenta);
                    colors.Add(Color.magenta);
                    colors.Add(Color.magenta);
                    colors.Add(Color.magenta);
                }

                // triangles
                for (int i = 0; i < length; i++)
                {
                    var vIndex = vertOffset + i*4;
                    triangles.Add(vIndex);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 1);

                    triangles.Add(vIndex + 3);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 0);
                }
            }
        }

        #endregion

        private Color GetColorBySplatId(int id)
        {
            switch (id%3)
            {
                case 0: return Color.yellow;
                case 1: return Color.green;
                default: return Color.blue;
            }
        }

        private void BuildGameObject(Rule rule, IGameObject cellObject, 
            List<Vector3> vertices, List<int> triangles, List<Color> colors)
        {
            var gameObject = cellObject.GetComponent<GameObject>();

            var meshData = new Mesh();
            meshData.vertices = vertices.ToArray();
            meshData.triangles = triangles.ToArray();
            meshData.colors = colors.ToArray();
            meshData.RecalculateNormals();

            gameObject.AddComponent<MeshRenderer>().material = rule.GetMaterial(_resourceProvider);
            gameObject.AddComponent<MeshFilter>().mesh = meshData;
            gameObject.AddComponent<MeshCollider>();
        }

        private static GradientWrapper GetGradient()
        {
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            var gck = new GradientWrapper.ColorKey[5];
            gck[0].Color = new Color32(152, 251, 152, 1); // pale green
            gck[0].Time = 0.0f;

            gck[1].Color = new Color32(173, 255, 47, 1); // green yellow
            gck[1].Time = 0.05f;

            gck[2].Color = new Color32(0, 100, 0, 1); // dark green
            gck[2].Time = 0.1f;

            gck[3].Color = new Color32(85, 107, 47, 1); // dark olive green
            gck[3].Time = 0.8f;

            gck[4].Color = new Color32(184, 134, 11, 1); // dark golden rod 
            gck[4].Time = 1f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            var gak = new GradientWrapper.AlphaKey[2];
            gak[0].Alpha = 1f;
            gak[0].Time = 0.0f;
            gak[1].Alpha = 1f;
            gak[1].Time = 1.0f;

            return new GradientWrapper(gck, gak);
        }

        public void Configure(IConfigSection configSection)
        {
            _maxCellSize = configSection.GetFloat("cell.size", 100);
            var maxArea = configSection.GetFloat("maxArea", 10);

            _meshCellBuilder.SetMaxArea(maxArea);
        }
    }
}