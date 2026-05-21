using System;

namespace Game.ProcGen
{
    public static class GenerationRandom
    {
        public static Random Create(int seed, string streamId)
        {
            return new Random(CombineSeed(seed, streamId));
        }

        public static int CombineSeed(int seed, string streamId)
        {
            unchecked
            {
                uint hash = 2166136261u;
                HashInt(ref hash, seed);

                if (string.IsNullOrEmpty(streamId) == false)
                {
                    for (int i = 0; i < streamId.Length; i++)
                    {
                        hash ^= streamId[i];
                        hash *= 16777619u;
                    }
                }

                return (int)(hash & 0x7fffffffu);
            }
        }

        private static void HashInt(ref uint hash, int value)
        {
            unchecked
            {
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(value >> (i * 8));
                    hash *= 16777619u;
                }
            }
        }
    }
}
