using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.ProcGen.Warehouse
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Evolution Recipe", fileName = "WarehouseEvolutionRecipe")]
    public sealed class WarehouseEvolutionRecipe : WarehouseRecipe
    {
        [Header("Evolution")]
        public int PopulationSize = 32;
        public int Generations = 40;
        public int EliteCount = 4;
        public int TournamentSize = 4;
        [FormerlySerializedAs("BaselineSeedCount")]
        public int RandomImmigrantCount = 4;
        public float MutationRate = 0.35f;
        public int MinMutationSteps = 1;
        public int MaxMutationSteps = 5;
        public int AggressiveMutationSteps = 9;
        public float DuplicateGenomePenalty = 200f;
        public float GroundPathBonusPerPath = 150f;
        public int MaxGroundPathBonusCount = 3;
        public float WideGroundPathBonus = 180f;
        public int WideGroundPathBonusMaxSamples = 40;
        public int MaxBlockedGroundMassSize = 24;
        public float BlockedGroundMassPenalty = 20f;
        public int DeadEndAllowedLength = 2;
        public float DeadEndPenalty = 60f;
        public float CaptureOnlySpawnHalfPenalty = 80f;

        public int PopulationCount => Mathf.Max(4, PopulationSize);
        public int GenerationCount => Mathf.Max(1, Generations);
        public int EliteKeepCount => Mathf.Clamp(EliteCount, 1, PopulationCount - 1);
        public int TournamentPickCount => Mathf.Clamp(TournamentSize, 2, PopulationCount);
        public int RandomImmigrantKeepCount => Mathf.Clamp(RandomImmigrantCount, 0, PopulationCount - EliteKeepCount);
        public float MutationProbability => Mathf.Clamp01(MutationRate);
        public int MinMutationStepCount => Mathf.Max(1, MinMutationSteps);
        public int MaxMutationStepCount => Mathf.Max(MinMutationStepCount, MaxMutationSteps);
        public int AggressiveMutationStepCount => Mathf.Max(MaxMutationStepCount, AggressiveMutationSteps);
        public float DuplicatePenalty => Mathf.Max(0f, DuplicateGenomePenalty);
        public float GroundPathBonusWeight => Mathf.Max(0f, GroundPathBonusPerPath);
        public int MaxGroundPathBonusPathCount => Mathf.Clamp(MaxGroundPathBonusCount, 0, 3);
        public float WideGroundPathBonusWeight => Mathf.Max(0f, WideGroundPathBonus);
        public int WideGroundPathBonusSampleLimit => Mathf.Max(1, WideGroundPathBonusMaxSamples);
        public int MaxBlockedGroundMassCellCount => Mathf.Max(1, MaxBlockedGroundMassSize);
        public float BlockedGroundMassPenaltyWeight => Mathf.Max(0f, BlockedGroundMassPenalty);
        public int DeadEndAllowedCellCount => Mathf.Max(0, DeadEndAllowedLength);
        public float DeadEndPenaltyWeight => Mathf.Max(0f, DeadEndPenalty);
        public float CaptureOnlySpawnHalfPenaltyWeight => Mathf.Max(0f, CaptureOnlySpawnHalfPenalty);

        public override void BuildPasses(List<ProcGenPass> passes)
        {
            passes.Add(new WarehouseEvolutionPass());
        }
    }
}
