using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>The colonist settings other than xenotype, applied to adult humanlike starting colonists.</summary>
    public static class StartingPawnFilters
    {
        public const int MinAge = 14;

        public const int MaxAge = 100;

        public const int MaxPassions = 6;

        public const int MaxMajorPassions = 4;

        // Validator rejections before a colonist is accepted anyway; the generator ignores scenario requirements from its 70th try.
        private const int MaxRejections = 50;

        // Samples drawn from the race's age curve before falling back to a uniform age in the range.
        private const int AgeCurveSamples = 500;

        /// <summary>The settings for one Just Start run, fixed when the button is clicked.</summary>
        private sealed class RunSettings
        {
            public IntRange? AgeRange;
            public GenderMix GenderMix;
            public bool RequireCapable;
            public bool RequireViolence;
            public int MinPassions;
            public int MinMajorPassions;
            public readonly HashSet<string> ExcludedTraits = new HashSet<string>();
            public bool NoAddictions;
            public bool NoPregnancies;
            public bool NoRelations;

            public bool NeedsValidator => RequireCapable || RequireViolence || MinPassions > 0 || MinMajorPassions > 0 || ExcludedTraits.Count > 0;
        }

        // World generation (which may generate the pawns) runs asynchronously, so the settings are copied on the main thread.
        private static RunSettings run = new RunSettings();

        public static void PrepareRun()
        {
            JustStartSettings settings = JustStartMod.Settings;
            var next = new RunSettings
            {
                AgeRange = IsFullAgeRange(settings.startingAgeRange) ? (IntRange?)null : ClampedRange(settings.startingAgeRange),
                GenderMix = settings.genderMix,
                RequireCapable = settings.requireCapableColonists,
                RequireViolence = settings.requireViolenceCapable,
                MinPassions = settings.minPassions,
                MinMajorPassions = settings.minMajorPassions,
                NoAddictions = settings.noAddictions,
                NoPregnancies = settings.noPregnancies,
                NoRelations = settings.noRelations
            };
            foreach (string key in settings.excludedTraits)
                next.ExcludedTraits.Add(key);
            run = next;
        }

        public static IntRange ClampedRange(IntRange range)
        {
            int min = Math.Max(MinAge, Math.Min(range.min, MaxAge));
            int max = Math.Max(min, Math.Min(range.max, MaxAge));
            return new IntRange(min, max);
        }

        public static bool IsFullAgeRange(IntRange range)
        {
            IntRange clamped = ClampedRange(range);
            return clamped.min == MinAge && clamped.max == MaxAge;
        }

        public static string TraitKey(TraitDef def, int degree) => def.defName + ":" + degree;

        public static void ApplyTo(ref PawnGenerationRequest request)
        {
            // Children, babies, mutants and non-humanlike starters (as some scenarios add) keep the scenario's request.
            if (request.KindDef?.RaceProps?.Humanlike != true || request.ForcedMutant != null || request.AllowedDevelopmentalStages != DevelopmentalStage.Adult)
                return;
            RunSettings settings = run;

            if (settings.AgeRange is IntRange ageRange && !request.FixedBiologicalAge.HasValue)
                request.FixedBiologicalAge = RandomAge(request.KindDef, ageRange);

            if (!request.FixedGender.HasValue && !request.KindDef.fixedGender.HasValue && request.KindDef.RaceProps.hasGenders)
            {
                switch (settings.GenderMix)
                {
                    case GenderMix.Male:
                        request.FixedGender = Gender.Male;
                        break;
                    case GenderMix.Female:
                        request.FixedGender = Gender.Female;
                        break;
                    case GenderMix.Balanced:
                        request.FixedGender = LessCommonStartingGender();
                        break;
                }
            }

            if (settings.NoAddictions)
                request.AllowAddictions = false;
            if (settings.NoPregnancies)
                request.AllowPregnant = false;
            if (settings.NoRelations)
                request.CanGeneratePawnRelations = false;

            if (settings.NeedsValidator)
            {
                Predicate<Pawn>? previous = request.ValidatorPreGear;
                int rejections = 0;
                request.ValidatorPreGear = pawn =>
                {
                    if (previous != null && !previous(pawn))
                        return false;
                    if (Passes(pawn, settings) || rejections >= MaxRejections)
                        return true;
                    rejections++;
                    return false;
                };
            }
        }

        private static bool Passes(Pawn pawn, RunSettings settings)
        {
            if (settings.RequireCapable && !IsFullyCapable(pawn))
                return false;
            if (settings.RequireViolence && pawn.WorkTagIsDisabled(WorkTags.Violent))
                return false;
            if ((settings.MinPassions > 0 || settings.MinMajorPassions > 0) && !HasPassions(pawn, settings))
                return false;
            if (settings.ExcludedTraits.Count > 0 && HasExcludedTrait(pawn, settings.ExcludedTraits))
                return false;
            return true;
        }

        /// <summary>No disabled work tags or work types from backstories, traits, genes, health or titles.</summary>
        private static bool IsFullyCapable(Pawn pawn) =>
            pawn.CombinedDisabledWorkTags == WorkTags.None && pawn.GetDisabledWorkTypes().Count == 0;

        private static bool HasPassions(Pawn pawn, RunSettings settings)
        {
            if (pawn.skills == null)
                return true;
            int any = 0, major = 0;
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                if (skill.passion == Passion.None || skill.TotallyDisabled)
                    continue;
                any++;
                if (skill.passion == Passion.Major)
                    major++;
            }
            return any >= settings.MinPassions && major >= settings.MinMajorPassions;
        }

        private static bool HasExcludedTrait(Pawn pawn, HashSet<string> excluded)
        {
            List<Trait>? traits = pawn.story?.traits?.allTraits;
            if (traits == null)
                return false;
            foreach (Trait trait in traits)
            {
                if (!trait.Suppressed && excluded.Contains(TraitKey(trait.def, trait.Degree)))
                    return true;
            }
            return false;
        }

        /// <summary>The gender fewer of the starting colonists generated so far have; random on a tie.</summary>
        private static Gender LessCommonStartingGender()
        {
            int male = 0, female = 0;
            List<Pawn> pawns = Find.GameInitData.startingAndOptionalPawns;
            int count = Math.Min(pawns.Count, Find.GameInitData.startingPawnCount);
            for (int i = 0; i < count; i++)
            {
                if (pawns[i]?.gender == Gender.Male)
                    male++;
                else if (pawns[i]?.gender == Gender.Female)
                    female++;
            }
            if (male == female)
                return Rand.Bool ? Gender.Male : Gender.Female;
            return male < female ? Gender.Male : Gender.Female;
        }

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
