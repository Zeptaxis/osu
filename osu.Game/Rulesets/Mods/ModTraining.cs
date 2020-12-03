using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModTraining : Mod, IApplicableToDifficulty, IUpdatableByPlayfield, IApplicableToBeatmap, IApplicableToAudio
    {
        public override string Name => @"Training";

        public override string Description => @"Increase/Decrease difficulty based on your performance.";

        public override string Acronym => "TR";

        public override ModType Type => ModType.Conversion;

        public override IconUsage? Icon => FontAwesome.Solid.SpaceShuttle;

        public override double ScoreMultiplier => 1.0;

        public override bool RequiresConfiguration => true;

        public override Type[] IncompatibleMods => new[]
        {
            typeof(ModDoubleTime),
            typeof(ModHalfTime),
            typeof(ModDifficultyAdjust),
            typeof(ModHardRock),
            typeof(ModEasy),
            // TODO probably a few more
        };

        protected const int FIRST_SETTING_ORDER = 1;

        protected const int LAST_SETTING_ORDER = 101;


        [SettingSource("Speed", "Maximal speed", FIRST_SETTING_ORDER)]
        public BindableNumber<double> SpeedTarget { get; } = new BindableDouble
        {
            Precision = 0.01,
            MinValue = 1.0,
            MaxValue = 2.0,
            Default = 1,
            Value = 1
        };

        [SettingSource("Accuracy", "Maximal OD", LAST_SETTING_ORDER)]
        public BindableNumber<float> OverallDifficultyTarget { get; } = new BindableFloat
        {
            Precision = 0.1f,
            MinValue = 0,
            MaxValue = 11,
            Default = 10,
            Value = 10,
        };

        public override string SettingDescription
        {
            get
            {
                string speedRate = SpeedTarget.IsDefault ? string.Empty : $"{SpeedTarget.Value:N1}x";
                string overallDifficultyRate = OverallDifficultyTarget.IsDefault ? string.Empty : $"OD {OverallDifficultyTarget.Value:N1}";

                return string.Join(", ", new[]
                {
                    speedRate,
                    overallDifficultyRate
                }.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        protected ITrack Track;
        protected BeatmapDifficulty Difficulty;

        public BindableNumber<double> SpeedChange { get; } = new BindableDouble
        {
            Default = 1,
            Value = 1,
            Precision = 0.01,
        };

        protected ModTraining()
        {
            SpeedTarget.BindValueChanged(val => SpeedChange.Value = SpeedTarget.Value, true);
        }

        public void ApplyToTrack(ITrack track)
        {
            Track = track;

            SpeedTarget.TriggerChange();
            applyPitchAdjustment();
        }

        public void ApplyToSample(DrawableSample sample)
        {
            sample.AddAdjustment(AdjustableProperty.Frequency, SpeedChange);
        }

        public virtual void ApplyToBeatmap(IBeatmap beatmap)
        {
            SpeedChange.SetDefault();
        }

        public virtual void Update(Playfield playfield)
        {
            applyRateAdjustment();
        }

        protected double GetCurrentRate()
        {
            return GetRateAtTime(Track.CurrentTime, Track.Length);
        }

        protected double GetRateAtTime(double time, double max)
        {
            double pos = time / max;
            return 1.0 + (SpeedTarget.Value - 1.0) * Math.Clamp(pos, 0, 1);
        }

        private void applyRateAdjustment() => SpeedChange.Value = GetCurrentRate();

        private void applyPitchAdjustment()
        {
            Track?.RemoveAdjustment(AdjustableProperty.Tempo, SpeedChange);
            Track?.AddAdjustment(AdjustableProperty.Tempo, SpeedChange);
        }
            
        public void ReadFromDifficulty(BeatmapDifficulty difficulty)
        {
            if (Difficulty == null || Difficulty.ID != difficulty.ID)
            {
                TransferSettings(difficulty);
                Difficulty = difficulty;
            }
        }

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            // nothing to do
        }

        protected virtual void TransferSettings(BeatmapDifficulty difficulty)
        {
            TransferSetting(OverallDifficultyTarget, difficulty.OverallDifficulty);
        }

        private readonly Dictionary<IBindable, bool> userChangedSettings = new Dictionary<IBindable, bool>();

        protected void TransferSetting<T>(BindableNumber<T> bindable, T beatmapDefault)
            where T : struct, IComparable<T>, IConvertible, IEquatable<T>
        {
            bindable.UnbindEvents();

            userChangedSettings.TryAdd(bindable, false);

            bindable.Default = beatmapDefault;

            if (!userChangedSettings[bindable])
            {
                bindable.Value = beatmapDefault;
            }

            bindable.ValueChanged += _ => userChangedSettings[bindable] = !bindable.IsDefault;
        }

    }
}
