using System;

namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource()
            : this(new Random())
        {
        }

        public SystemRandomSource(int seed)
            : this(new Random(seed))
        {
        }

        public SystemRandomSource(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }
    }
}
