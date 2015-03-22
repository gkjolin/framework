﻿using System;
using System.Collections.Generic;
using ActionStreetMap.Core;
using ActionStreetMap.Core.MapCss.Domain;
using ActionStreetMap.Core.Scene.Details;
using ActionStreetMap.Core.Tiling.Models;
using ActionStreetMap.Core.Unity;
using ActionStreetMap.Explorer.Geometry.Utils;
using ActionStreetMap.Explorer.Helpers;

namespace ActionStreetMap.Explorer.Scene
{
    /// <summary> Provides the way to process splat areas. </summary>
    public class SplatModelBuilder : ModelBuilder
    {
        /// <inheritdoc />
        public override string Name { get { return "splat"; } }

        /// <inheritdoc />
        public override IGameObject BuildArea(Tile tile, Rule rule, Area area)
        {
            var points = ObjectPool.NewList<MapPoint>();
            PointUtils.GetPolygonPoints(tile.RelativeNullPoint, area.Points, points);
            tile.Canvas.AddArea(new Surface()
            {
                SplatIndex = rule.GetSplatIndex(),
                Points = points
            });

            if (rule.IsForest())
                GenerateTrees(tile, points, (int) area.Id);

            return null;
        }

        private void GenerateTrees(Tile tile, List<MapPoint> points, int seed)
        {
            // triangulate polygon
            var triangles = PolygonUtils.Triangulate(points, ObjectPool);
            
            var rnd = new Random(seed);
            // this cycle generate points inside each triangle
            // count of points is based on triangle area
            for (int i = 0; i < triangles.Count;)
            {
                // get triangle vertices
                var p1 = points[triangles[i++]];
                var p2 = points[triangles[i++]];
                var p3 = points[triangles[i++]];

                var area = TriangleUtils.GetTriangleArea(p1, p2, p3);
                var count = area / 200;
                for (int j = 0; j < count; j++)
                {
                    var point = TriangleUtils.GetRandomPoint(p1, p2, p3, rnd.NextDouble(), rnd.NextDouble());
                    // TODO use this different way
                    /*tile.Canvas.AddTree(new Tree()
                    {
                        Type = 0, // TODO
                        Point = point
                    });*/
                }
            }
        }
    }
}