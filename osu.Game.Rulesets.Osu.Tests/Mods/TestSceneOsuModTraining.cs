using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public class TestSceneOsuModTraining : OsuModTestScene
    {
        [Test]
        public void TestNoAdjustment() => CreateModTest(new ModTestData
        {
            Mod = new OsuModTraining(),
            Autoplay = true,
            PassCondition = checkSomeHit
        });

        private bool checkSomeHit()
        {
            return Player.ScoreProcessor.JudgedHits >= 20;
        }
    }
}
