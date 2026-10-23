using System;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 可复现的伪随机数发生器（SplitMix32）。
    ///
    /// 为什么不直接用 UnityEngine.Random / System.Random：
    ///   - UnityEngine.Random 是全局状态，会被任何其它代码污染，无法复现一局地牢；
    ///   - System.Random 的实现跨运行时可能变化，同种子不一定同结果。
    /// 地牢生成只依赖这一个实例，种子相同 = 地牢完全相同。
    /// </summary>
    public sealed class SeededRandom
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            _state = unchecked((uint)seed);
            if (_state == 0u) _state = 0x9E3779B9u; // 0 是 SplitMix 的退化输入
        }

        /// <summary>原始 32 位无符号随机数。</summary>
        public uint NextUInt()
        {
            unchecked
            {
                _state += 0x9E3779B9u;
                uint z = _state;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }

        /// <summary>[0, maxExclusive)</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        /// <summary>[minInclusive, maxExclusive)</summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + Next(maxExclusive - minInclusive);
        }

        /// <summary>[0, 1)</summary>
        public float NextFloat()
        {
            // 取高 24 位，正好是 float 的尾数精度
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public bool Chance(float probability) => NextFloat() < probability;

        /// <summary>从数组里随机取一个元素。</summary>
        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Next(list.Count)];
        }
    }
}
