using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Turns a scenario's JustStartScenarioExtension into info panel lines phrased like vanilla's part summaries.</summary>
    public static class ScenarioRulesSummary
    {
        // The info panel asks for its text every frame; results are cached per scenario until settings or language change.
        private static readonly Dictionary<Scenario, string> cache = new Dictionary<Scenario, string>();
        private static LoadedLanguage? cachedLanguage;

        public static void ClearCache() => cache.Clear();

        public static string AppendTo(Scenario scenario, JustStartScenarioExtension ext, string vanillaText)
        {
            if (cachedLanguage != LanguageDatabase.activeLanguage)
            {
                cache.Clear();
                cachedLanguage = LanguageDatabase.activeLanguage;
            }

            if (!cache.TryGetValue(scenario, out string text))
            {
                var lines = Lines(ext).ToList();
                if (lines.Count == 0)
                    text = vanillaText;
                else
                {
                    // Vanilla leaves a blank line after the description only when some part has a summary.
                    bool hasPartLines = vanillaText.TrimEnd() != (scenario.description ?? string.Empty).TrimEnd();
                    text = vanillaText + (hasPartLines ? "\n" : "\n\n") + string.Join("\n", lines);
                }
                cache[scenario] = text;
            }
            return text;
        }

        private static IEnumerable<string> Lines(JustStartScenarioExtension ext)
        {
            if (ext.pawnRoles != null)
            {
                foreach (var role in ext.pawnRoles.Where(r => r.faction == PawnRoleFaction.PlayerColonist))
                    foreach (var line in ColonistLines(role, role.xenotypeRules ?? ext.xenotypeRules))
                        yield return line;

                foreach (var role in ext.pawnRoles.Where(r => r.faction == PawnRoleFaction.HostileOnMap && r.kindDef != null))
                {
                    string kind = Find.ActiveLanguageWorker.Pluralize(role.kindDef!.label, role.count);
                    yield return role.count == 1
                        ? "JustStart_SummaryHostileOne".Translate(kind)
                        : "JustStart_SummaryHostileMany".Translate(role.count, kind);
                    if (!role.weaponOptions.NullOrEmpty())
                        yield return "JustStart_SummaryHostileWeapon".Translate(OptionList(role.weaponOptions!));
                    if (ModsConfig.BiotechActive && role.xenotypeRules != null)
                    {
                        string? xenotype = XenotypeLine(role.xenotypeRules);
                        if (xenotype != null)
                            yield return xenotype;
                    }
                    if (role.fightToTheDeath)
                        yield return "JustStart_SummaryToTheDeath".Translate();
                }
            }

            if (ext.mapThings != null)
                foreach (var thing in ext.mapThings.Where(t => !t.options.NullOrEmpty()))
                    yield return "JustStart_SummaryMapThing".Translate(OptionList(thing.options!)).CapitalizeFirst();

            if (ext.animalRoles != null)
            {
                foreach (var role in ext.animalRoles)
                {
                    string animal = role.fixedSpecies != null
                        ? Find.ActiveLanguageWorker.Pluralize(role.fixedSpecies.label, role.count)
                        : (role.requirePackAnimal ? "JustStart_SummaryPackAnimal" : "JustStart_SummaryAnimal").Translate().Resolve();
                    yield return "JustStart_SummaryAnimals".Translate(role.count, animal);
                    if (!role.carriedInventory.NullOrEmpty())
                        yield return "JustStart_SummaryAnimalCarrying".Translate(ItemList(role.carriedInventory!));
                }
            }

            bool constraintsApply = !ext.curatedRestrictionOnly || JustStartMod.Settings.useCuratedVanillaRestrictions;
            if (constraintsApply && ext.tileConstraints != null)
                foreach (var constraint in ext.tileConstraints)
                    yield return constraint.Describe();

            if (ext.allowAnyBiome)
                yield return "JustStart_SummaryAnyBiome".Translate();
            if (!ext.startingSeasons.NullOrEmpty())
                yield return "JustStart_SummarySeasons".Translate(OrList(ext.startingSeasons.Distinct().Select(s => s.Label())));
            if (!ext.arrivalMethods.NullOrEmpty())
                yield return "JustStart_SummaryArrival".Translate(OrList(ext.arrivalMethods.Distinct().Select(m => ("JustStart_Arrival" + m).Translate().Resolve())));

            if (ModsConfig.IdeologyActive && ext.ideologyRules != null)
                foreach (var line in IdeologyLines(ext.ideologyRules))
                    yield return line;
        }

        private static IEnumerable<string> ColonistLines(PawnRole role, XenotypeRuleSet? xenotypeRules)
        {
            if (role.countRange is IntRange range && range.max > range.min)
                yield return "JustStart_SummaryStartWithRange".Translate(range.min, range.max);
            else
            {
                int count = role.MinCount;
                yield return count == 1
                    ? "ScenPart_StartWithPawn".Translate()
                    : "ScenPart_StartWithPawns".Translate(count);
            }

            if (role.gender.HasValue)
                yield return "JustStart_SummaryGender".Translate(role.gender.Value.GetLabel());
            if (role.biologicalAgeRange is FloatRange age)
                yield return "JustStart_SummaryAge".Translate(age.min.ToString("F0"), age.max.ToString("F0"));
            if (role.mustBeCapableOfViolence)
                yield return "JustStart_SummaryViolence".Translate();

            bool hasFixed = !role.fixedEquipment.NullOrEmpty();
            if (!role.useDefaultVanillaGear)
                yield return hasFixed
                    ? "JustStart_SummaryColonistCarrying".Translate(ItemList(role.fixedEquipment!))
                    : "JustStart_SummaryNothing".Translate();
            else if (hasFixed)
                yield return "JustStart_SummaryColonistAlsoCarrying".Translate(ItemList(role.fixedEquipment!));

            if (ModsConfig.BiotechActive && xenotypeRules != null)
            {
                string? line = XenotypeLine(xenotypeRules);
                if (line != null)
                    yield return line;
            }
        }

        private static string? XenotypeLine(XenotypeRuleSet rules)
        {
            string? list = rules.xenotypes.NullOrEmpty() ? null : rules.xenotypes!.Select(x => x.label).ToCommaList();
            switch (rules.mode)
            {
                case XenotypeRuleMode.Random:
                    return "JustStart_SummaryXenotypeRandom".Translate();
                case XenotypeRuleMode.SameForAll:
                    return list == null
                        ? "JustStart_SummaryXenotypeSameForAll".Translate()
                        : "JustStart_SummaryXenotypeSameForAllFrom".Translate(list);
                case XenotypeRuleMode.Fixed:
                    return list == null ? null : "JustStart_SummaryXenotypeFixed".Translate(rules.xenotypes![0].label);
                case XenotypeRuleMode.AllowedPool:
                    return list == null ? null : "JustStart_SummaryXenotypePool".Translate(list);
                case XenotypeRuleMode.Excluded:
                    return list == null ? null : "JustStart_SummaryXenotypeExcluded".Translate(list);
                case XenotypeRuleMode.Required:
                    return list == null ? null : "JustStart_SummaryXenotypeRequired".Translate(rules.requiredCount, list);
                default:
                    return null;
            }
        }

        private static IEnumerable<string> IdeologyLines(IdeologyRuleSet rules)
        {
            if (!rules.modes.NullOrEmpty())
                yield return "JustStart_SummaryIdeoModes".Translate(OrList(rules.modes.Distinct().Select(m => ("JustStart_IdeoOption" + m).Translate().Resolve())));
            else if (rules.mode.HasValue)
                yield return ("JustStart_SummaryIdeo" + rules.mode.Value).Translate();
            if (!rules.forcedMemes.NullOrEmpty())
                yield return "JustStart_SummaryIdeoForced".Translate(rules.forcedMemes.Select(m => m.label).ToCommaList());
            if (!rules.disallowedMemes.NullOrEmpty())
                yield return "JustStart_SummaryIdeoDisallowedMemes".Translate(rules.disallowedMemes.Select(m => m.label).ToCommaList());
            if (!rules.disallowedPrecepts.NullOrEmpty())
                yield return "JustStart_SummaryIdeoDisallowedPrecepts".Translate(rules.disallowedPrecepts.Select(p => p.LabelCap.Resolve()).ToCommaList());
        }

        // "a revolver", "a revolver or an autopistol", "a club, a knife or a spear".
        private static string OptionList(List<ThingDefCountClass> options) =>
            OrList(options.Select(o => Find.ActiveLanguageWorker.WithIndefiniteArticle(o.thingDef.label)));

        // "spring", "spring or summer", "spring, summer or fall".
        private static string OrList(IEnumerable<string> items)
        {
            var list = items.ToList();
            if (list.Count == 1)
                return list[0];
            return string.Join(", ", list.Take(list.Count - 1)) + " " + "JustStart_SummaryOr".Translate() + " " + list.Last();
        }

        private static string ItemList(List<ThingDefCountClass> items) => items.Select(i => i.Label).ToCommaList(useAnd: true);
    }
}
