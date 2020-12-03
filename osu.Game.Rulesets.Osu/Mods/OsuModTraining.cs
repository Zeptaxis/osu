using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModTraining : ModTraining, IApplicableToDrawableHitObjects
    {
        [SettingSource("Circle size", "Maximal CS", FIRST_SETTING_ORDER - 1)]
        public BindableNumber<float> CircleSizeTarget { get; } = new BindableFloat
        {
            Precision = 0.1f,
            MinValue = 0,
            MaxValue = 10,
            Default = 4,
            Value = 4,
        };

        [SettingSource("Approach Rate", "Maximal AR", LAST_SETTING_ORDER + 1)]
        public BindableNumber<float> ApproachRateTarget { get; } = new BindableFloat
        {
            Precision = 0.1f,
            MinValue = 0,
            MaxValue = 11,
            Default = 9,
            Value = 9,
        };

        public override string SettingDescription
        {
            get
            {
                string circleSize = CircleSizeTarget.IsDefault ? string.Empty : $"CS {CircleSizeTarget.Value:N1}";
                string approachRate = ApproachRateTarget.IsDefault ? string.Empty : $"AR {ApproachRateTarget.Value:N1}";

                return string.Join(", ", new[]
                {
                    circleSize,
                    base.SettingDescription,
                    approachRate
                }.Where(s => !string.IsNullOrEmpty(s)));
            }
        }
        public override void Update(Playfield playfield)
        {
            base.Update(playfield);
        }


        protected override void TransferSettings(BeatmapDifficulty difficulty)
        {
            base.TransferSettings(difficulty);

            TransferSetting(CircleSizeTarget, difficulty.CircleSize);
            TransferSetting(ApproachRateTarget, difficulty.ApproachRate);
        }

        private double endTime;

        public override void ApplyToBeatmap(IBeatmap beatmap)
        {
            base.ApplyToBeatmap(beatmap);

            endTime = beatmap.HitObjects.Last().StartTime;

            //foreach (var hitObject in beatmap.HitObjects)
            //{
            //    double newApproachRate = ApproachRateTarget.Default + (ApproachRateTarget.Value - ApproachRateTarget.Default) * (hitObject.StartTime / endTime);
            //    double newTimePreempt = BeatmapDifficulty.DifficultyRange(newApproachRate, 1800, 1200, 450) * GetRateAtTime(hitObject.StartTime, endTime);
            //    float newCircleSize = (float)(CircleSizeTarget.Default + (CircleSizeTarget.Value - CircleSizeTarget.Default) * (hitObject.StartTime / endTime));
            //    float newScale = (1.0f - 0.7f * (newCircleSize - 5) / 5) / 2;
            //    switch (hitObject)
            //    {
            //        case HitCircle osuHitObject:
            //            osuHitObject.TimePreempt = newTimePreempt;
            //            osuHitObject.Scale = newScale;
            //            break;
            //        case Slider slider:
            //            slider.Scale = newScale;
            //            slider.HeadCircle.Scale = newScale;
            //            slider.TailCircle.Scale = newScale;
            //            break;
            //        default:
            //            break;
            //    }
                
            //}
        }

        public void ApplyToDrawableHitObjects(IEnumerable<DrawableHitObject> drawables)
        {
            foreach (var drawable in drawables)
            {
                drawable.HitObjectApplied += draw =>
                {
                    draw.LifetimeStart -= 10000;
                    var hitObject = draw.HitObject;
                    double newApproachRate = ApproachRateTarget.Default + (ApproachRateTarget.Value - ApproachRateTarget.Default) * (hitObject.StartTime / endTime);
                    double newTimePreempt = BeatmapDifficulty.DifficultyRange(newApproachRate, 1800, 1200, 450) * GetRateAtTime(hitObject.StartTime, endTime);
                    float newCircleSize = (float)(CircleSizeTarget.Default + (CircleSizeTarget.Value - CircleSizeTarget.Default) * (hitObject.StartTime / endTime));
                    float newScale = (1.0f - 0.7f * (newCircleSize - 5) / 5) / 2;
                    newTimePreempt = 5000;
                    switch (hitObject)
                    {
                        case HitCircle osuHitObject:
                            osuHitObject.TimePreempt = newTimePreempt;
                            osuHitObject.Scale = newScale;
                            // is this always true ? probably

                            if (draw is DrawableHitCircle drawableHitCircle)
                            {
                                double transformTime = hitObject.StartTime - osuHitObject.TimePreempt;
                                using (drawableHitCircle.BeginAbsoluteSequence(transformTime, true))
                                {
                                    drawableHitCircle.ApproachCircle.ScaleTo(4);
                                    drawableHitCircle.ApproachCircle.FadeIn(Math.Min(osuHitObject.TimeFadeIn, osuHitObject.TimePreempt));
                                    drawableHitCircle.ApproachCircle.ScaleTo(1f, osuHitObject.TimePreempt);
                                    drawableHitCircle.ApproachCircle.Expire(true);
                                }
                            }
                            break;
                        case Slider slider:
                            slider.HeadCircle.TimePreempt = newTimePreempt;
                            slider.Scale = newScale;
                            slider.HeadCircle.Scale = newScale;
                            slider.TailCircle.Scale = newScale;
                            break;
                        default:
                            break;
                    }
                };
            }
        }
    }
}
