using System;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>The starting age range and "capable of all work" settings, applied to adult humanlike starting colonists.</summary>
    public static class StartingPawnFilters
    {
        public const int MinAge = 14;

        public const int MaxAge = 100;

        // Validator rejections before a colonist is accepted anyway; the generator ignores scenario requirements from its 70th try.
        private const int MaxCapableRejections = 50;

        // Samples drawn from the race's age curve before falling back to a uniform age in the range.
        private const int AgeCurveSamples = 500;

        // Fixed when Just Start is clicked; world generation (which may generate the pawns) runs asynchronously.
        private static IntRange? runAgeRange;
        private static bool runRequireCapable;

        /// <summary>Fixes the settings for the Just Start run about to begin.</summary>
        public static void PrepareRun()
        {
            JustStartSettings settings = JustStartMod.Settings;
            runAgeRange = settings.limitStartingAge ? ClampedRange(settings.startingAgeRange) : (IntRange?)null;
            runRequireCapable = settings.requireCapableColonists;
        }

        public static IntRange ClampedRange(IntRange range)
        {
            int min = Math.Max(MinAge, Math.Min(range.min, MaxAge));
            int max = Math.Max(min, Math.Min(range.max, MaxAge));
            return new IntRange(min, max);
        }

        public static void ApplyTo(ref PawnGenerationRequest request)
        {
            // Children, babies, mutants and non-humanlike starters (as some scenarios add) keep the scenario's request.
            if (request.KindDef?.RaceProps?.Humanlike != true || request.ForcedMutant != null || request.AllowedDevelopmentalStages != DevelopmentalStage.Adult)
                return;

            if (runAgeRange is IntRange ageRange && !request.FixedBiologicalAge.HasValue)
                request.FixedBiologicalAge = RandomAge(request.KindDef, ageRange);

            if (runRequireCapable)
            {
                Predicate<Pawn>? previous = request.ValidatorPreGear;
                int rejections = 0;
                request.ValidatorPreGear = pawn =>
                {
                    if (previous != null && !previous(pawn))
                        return false;
                    if (IsFullyCapable(pawn) || rejections >= MaxCapableRejections)
                        return true;
                    rejections++;
                    return false;
                };
            }
        }

        /// <summary>No disabled work tags or work types from backstories, traits, genes, health or titles.</summary>
        private static bool IsFullyCapable(Pawn pawn) =>
            pawn.CombinedDisabledWorkTags == WorkTags.None && pawn.GetDisabledWorkTypes().Count == 0;

        /// <summary>An age in the range (up to its last year), weighted by the race's age curve where it has one.</summary>
        private static float RandomAge(PawnKindDef kind, IntRange range)
        {
            float min = Math.Max(range.min, kind.minGenerationAge);
            float max = Math.Min(range.max + 0.99f, kind.maxGenerationAge);
            if (max < min)
                max = min;
            SimpleCurve? curve = kind.RaceProps.ageGenerationCurve;
            if (curve != null)
            {
                for (int i = 0; i < AgeCurveSamples; i++)
                {
                    float years = Rand.ByCurve(curve);
                    if (years >= min && years <= max)
                        return years;
                }
            }
            return Rand.Range(min, max);
        }
    }
}
