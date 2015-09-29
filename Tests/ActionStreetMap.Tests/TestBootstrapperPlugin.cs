﻿using ActionStreetMap.Core.Tiling;
using ActionStreetMap.Core.Tiling.Models;
using ActionStreetMap.Core.Unity;
using ActionStreetMap.Explorer.Bootstrappers;
using ActionStreetMap.Explorer.Customization;
using ActionStreetMap.Infrastructure.Reactive;

namespace ActionStreetMap.Tests
{
    /// <summary> This plugin overrides registration of non-testable classes. </summary>
    public class TestBootstrapperPlugin: BootstrapperPlugin
    {
        public override string Name { get { return "test"; } }

        public override bool Run()
        {
            Scheduler.MainThread = new TestScheduler();

            CustomizationService
                .RegisterBehaviour("terrain_draw", typeof (TestModelBehaviour))
                .RegisterAtlas("main",
                    new TextureAtlas()
                        .Add("asphalt", new TextureGroup(4096, 4096, 2)
                            .Add(3060, 3596, 494, 500)
                            .Add(1545, 2074, 494, 500))
                        .Add("background", new TextureGroup(4096, 4096, 1)
                            .Add(2555, 2848, 494, 500))
                        .Add("bark", new TextureGroup(4096, 4096, 4)
                            .Add(3060, 3091, 494, 500)
                            .Add(3565, 3596, 494, 500)
                            .Add(2555, 2343, 494, 500)
                            .Add(3565, 3091, 494, 500))
                        .Add("barrier", new TextureGroup(4096, 4096, 2)
                            .Add(3036, 369, 494, 293)
                            .Add(3541, 1196, 494, 375))
                        .Add("brick", new TextureGroup(4096, 4096, 4)
                            .Add(3060, 2586, 494, 500)
                            .Add(3060, 2081, 494, 500)
                            .Add(3565, 2586, 494, 500)
                            .Add(3565, 2081, 494, 500))
                        .Add("canvas", new TextureGroup(4096, 4096, 1)
                            .Add(3541, 858, 494, 333))
                        .Add("concrete", new TextureGroup(4096, 4096, 5)
                            .Add(6, 2045, 494, 500)
                            .Add(6, 1540, 494, 500)
                            .Add(511, 1557, 494, 500)
                            .Add(6, 1035, 494, 500)
                            .Add(511, 1052, 494, 500))
                        .Add("floor", new TextureGroup(4096, 4096, 6)
                            .Add(523, 2062, 494, 1000)
                            .Add(1545, 2579, 494, 1000)
                            .Add(2050, 3096, 494, 1000)
                            .Add(1016, 1557, 494, 500)
                            .Add(6, 530, 494, 500)
                            .Add(1016, 1052, 494, 500))
                        .Add("glass", new TextureGroup(4096, 4096, 2)
                            .Add(6, 3584, 506, 512)
                            .Add(523, 3584, 506, 512))
                        .Add("grass", new TextureGroup(4096, 4096, 5)
                            .Add(511, 547, 494, 500)
                            .Add(1521, 1569, 494, 500)
                            .Add(6, 25, 494, 500)
                            .Add(2050, 1875, 494, 500)
                            .Add(1521, 1064, 494, 500))
                        .Add("ground", new TextureGroup(4096, 4096, 1)
                            .Add(1016, 547, 494, 500))
                        .Add("metal", new TextureGroup(4096, 4096, 7)
                            .Add(511, 42, 494, 500)
                            .Add(2026, 1370, 494, 500)
                            .Add(2555, 1838, 494, 500)
                            .Add(1521, 559, 494, 500)
                            .Add(1016, 42, 494, 500)
                            .Add(2026, 865, 494, 500)
                            .Add(2531, 1333, 494, 500))
                        .Add("panel", new TextureGroup(4096, 4096, 1)
                            .Add(1521, 54, 494, 500))
                        .Add("road_brick", new TextureGroup(4096, 4096, 2)
                            .Add(3060, 1576, 494, 500)
                            .Add(2026, 360, 494, 500))
                        .Add("roof_tiles", new TextureGroup(4096, 4096, 1)
                            .Add(2531, 400, 494, 423))
                        .Add("sand", new TextureGroup(4096, 4096, 3)
                            .Add(3036, 667, 494, 399)
                            .Add(2531, 828, 494, 500)
                            .Add(3036, 1071, 494, 500))
                        .Add("stone", new TextureGroup(4096, 4096, 3)
                            .Add(1040, 2079, 494, 1500)
                            .Add(2555, 3353, 494, 743)
                            .Add(2050, 2380, 494, 711))
                        .Add("tree", new TextureGroup(4096, 4096, 3)
                            .Add(1040, 3584, 506, 512)
                            .Add(6, 3067, 506, 512)
                            .Add(523, 3067, 506, 512))
                        .Add("water", new TextureGroup(4096, 4096, 1)
                            .Add(6, 2550, 506, 512))
                        .Add("wood", new TextureGroup(4096, 4096, 3)
                            .Add(2531, 27, 494, 368)
                            .Add(2026, 7, 494, 348)
                            .Add(3565, 1576, 494, 500)));

            return true;
        }

        /// <summary> Dummy model behavior. </summary>
        private class TestModelBehaviour : IModelBehaviour
        {
            public string Name { get; private set; }
            public TestModelBehaviour(string name) { Name = name; }
            public void Apply(IGameObject gameObject, Model model) { }
        }
    }
}
