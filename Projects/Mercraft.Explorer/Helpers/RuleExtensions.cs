﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mercraft.Core.MapCss.Domain;
using Mercraft.Core.Scene;
using Mercraft.Core.Scene.Models;
using Mercraft.Core.Utilities;
using Mercraft.Explorer.Builders;
using Mercraft.Explorer.Interactions;
using UnityEngine;

namespace Mercraft.Explorer.Helpers
{
    /// <summary>
    /// Provides methods for basic mapcss properties receiving
    /// </summary>
    public static class RuleExtensions
    {
        public static Material GetMaterial(this Rule rule)
        {
            var path =  rule.Evaluate<string>("material");
            return Resources.Load<Material>(@"Materials/" + path);
        }

        public static int GetLevels(this Rule rule, int @default = 0)
        {
            return rule.EvaluateDefault("levels", @default);
        }

        public static float GetHeight(this Rule rule)
        {
            return rule.Evaluate<float>("height");
        }

        public static float GetHeight(this Rule rule, float defaultValue = 0)
        {
            return rule.EvaluateDefault<float>("height", defaultValue);
        }

        public static float GetMinHeight(this Rule rule, float defaultValue = 0)
        {
            return rule.EvaluateDefault<float>("min_height", defaultValue);
        }

        public static string GetBuildingStyle(this Rule rule)
        {
            return rule.Evaluate<string>("building-style");
        }

        public static IModelBuilder GetModelBuilder(this Rule rule, IEnumerable<IModelBuilder> builders)
        {
            var builderName = rule.Evaluate<string>("builder");
            return builders.Single(mb => mb.Name == builderName);
        }

        public static IModelBehaviour GetModelBehaviour(this Rule rule, IEnumerable<IModelBehaviour> behaviours)
        {
            var builderName = rule.EvaluateDefault<string>("behaviour", null);
            if (builderName == null)
                return null;
            return behaviours.Single(mb => mb.Name == builderName);
        }

        public static bool IsSkipped(this Rule rule)
        {
            return rule.EvaluateDefault("skip", false);
        }

        public static Color32 GetFillColor(this Rule rule)
        {
            var coreColor = rule.Evaluate("fill-color", ColorUtility.FromUnknown);
            return new Color32(coreColor.r, coreColor.g, coreColor.b, coreColor.a);
        }

        /// <summary>
        /// Z-index is just the lowest y coordinate
        /// </summary>
        public static float GetZIndex(this Rule rule)
        {
            return rule.Evaluate<float>("z-index");      
        }

        /// <summary>
        /// Gets width
        /// </summary>
        public static float GetWidth(this Rule rule)
        {
            return rule.Evaluate<float>("width");
        }

        public static bool IsRoad(this Rule rule)
        {
            return rule.EvaluateDefault("road", false);
        }

        public static bool IsTerrain(this Rule rule)
        {
            return rule.EvaluateDefault("terrain", false);
        }

        public static int GetSplatIndex(this Rule rule)
        {
            return rule.Evaluate<int>("terrainIndex");
        }

        public static SplatPrototype[] GetSplatPrototypes(this Rule rule)
        {
            var textures = rule.EvaluateList<List<string>>("texture");
            var splatPrototypes = new SplatPrototype[textures.Count];
            for (int i = 0; i < textures.Count; i++)
            {
                var texture = textures[i];
                var splatPrototype = new SplatPrototype();
                splatPrototype.texture = Resources.Load<Texture2D>("Textures/" + texture[1].Trim());
                splatPrototype.tileSize = new Vector2(int.Parse(texture[2]), int.Parse(texture[3]));

                splatPrototypes[i] = splatPrototype;
            }
            return splatPrototypes;
        }

        public static int GetAlphaMapSize(this Rule rule)
        {
            return rule.Evaluate<int>("alphamapsize"); 
        }
    }
}
