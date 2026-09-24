using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace JustStart
{
    /// <summary>
    /// Holds the non-colonist pawns, animals and map items JustStartFlow generated until that game's
    /// starting map exists, then places them around the player's start spot.
    /// </summary>
    public static class JustStartMapSpawnQueue
    {
        private static readonly List<(Pawn pawn, PawnRole role)> pending = new List<(Pawn, PawnRole)>();
        private static readonly List<(Pawn pawn, AnimalRole role)> pendingAnimals = new List<(Pawn, AnimalRole)>();
        private static readonly List<MapThing> pendingThings = new List<MapThing>();
        private static Game? queuedFor;

        /// <summary>Empties the queue and ties it to the current game.</summary>
        public static void Begin()
        {
            Clear();
            queuedFor = Current.Game;
        }

        public static void Enqueue(List<(Pawn pawn, PawnRole role)> pawns) => pending.AddRange(pawns);

        // Animals stay out of startingAndOptionalPawns, whose entries past startingPawnCount GameInitData.PrepForMapGen discards.
        public static void EnqueueAnimals(List<(Pawn pawn, AnimalRole role)> animals) => pendingAnimals.AddRange(animals);

        public static void EnqueueMapThings(List<MapThing>? things)
        {
            if (things != null)
                pendingThings.AddRange(things);
        }

        private static void Clear()
        {
            pending.Clear();
            pendingAnimals.Clear();
            pendingThings.Clear();
            queuedFor = null;
        }

        public static void SpawnAllOn(Map map)
        {
            // A queue left by a game whose start failed is dropped rather than spawned into another game.
            if (queuedFor == null || queuedFor != Current.Game)
            {
                Clear();
                return;
            }

            IntVec3 start = StartSpot(map);

            foreach (var (pawn, role) in pending)
            {
                GenSpawn.Spawn(pawn, CellAtDistance(map, start, role.SpawnDistance), map);
                if (role.faction == PawnRoleFaction.PlayerPrisoner)
                    pawn.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            }

            // Each hostile role gets vanilla's assault AI with its own switches; canTimeoutOrFlee off removes the "given up" and "satisfied" exits.
            foreach (var group in pending.Where(p => p.role.faction == PawnRoleFaction.HostileOnMap).GroupBy(p => (p.role, p.pawn.Faction)))
            {
                PawnRole role = group.Key.role;
                Faction faction = group.Key.Faction;
                var job = new LordJob_AssaultColony(faction,
                    canKidnap: role.canKidnap,
                    canTimeoutOrFlee: !role.fightToTheDeath,
                    canSteal: role.canSteal,
                    canPickUpOpportunisticWeapons: role.canPickUpWeapons);
                LordMaker.MakeNewLord(faction, job, map, group.Select(p => p.pawn));
            }

            foreach (var (animal, role) in pendingAnimals)
                GenSpawn.Spawn(animal, CellAtDistance(map, start, role.SpawnDistance), map);

            foreach (var mapThing in pendingThings)
            {
                IntVec3 cell = CellAtDistance(map, start, mapThing.distance);
                foreach (Thing thing in ThingFactory.Make(mapThing.options!.RandomElement()))
                    GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }

            Clear();
        }

        // Where the starting colonists were placed; the colonists themselves may still be in drop pods.
        private static IntVec3 StartSpot(Map map)
        {
            if (MapGenerator.PlayerStartSpotValid)
                return MapGenerator.PlayerStartSpot;
            Pawn colonist = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
            return colonist?.Position ?? map.Center;
        }

        // A standable, unfogged cell in the distance band that is reachable from the start spot; failing that, any standable cell in the band.
        private static IntVec3 CellAtDistance(Map map, IntVec3 start, IntRange distance)
        {
            bool InBand(IntVec3 c) =>
                c.InHorDistOf(start, distance.max) && (distance.min == 0 || !c.InHorDistOf(start, distance.min));

            bool Reachable(IntVec3 c) =>
                c.Standable(map) && !c.Fogged(map) && InBand(c)
                && map.reachability.CanReach(start, c, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors));

            if (CellFinder.TryFindRandomCellNear(start, map, distance.max, Reachable, out IntVec3 cell))
                return cell;
            if (CellFinder.TryFindRandomCellNear(start, map, distance.max, c => c.Standable(map) && InBand(c), out cell))
                return cell;
            return CellFinder.RandomClosewalkCellNear(start, map, distance.max);
        }
    }
}
