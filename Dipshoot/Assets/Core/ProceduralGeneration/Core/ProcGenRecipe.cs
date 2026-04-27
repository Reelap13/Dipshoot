using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen
{
    public abstract class ProcGenRecipe : ScriptableObject
    {
        public abstract void BuildPasses(List<ProcGenPass> passes);
    }
}
