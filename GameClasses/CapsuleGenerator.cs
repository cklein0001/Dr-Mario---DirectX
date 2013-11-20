using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
    public class CapsuleGenerator
    {
        protected int Seed;
        Random rng;
        private static vColors[] cArray= new vColors[]{ vColors.R, vColors.B,vColors.Y};

        public CapsuleGenerator() : this(null) { }
        public CapsuleGenerator(int seed) : this((int?)seed) { }
        public CapsuleGenerator(int? seed)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void Next(out Capsule partA, out Capsule partB)
        {
            partA = null;
            partB = null;
            Capsule.New(cArray[rng.Next(0, 6) % 3], cArray[rng.Next(0, 6) % 3], ref partA, ref partB);
        }
    }
}
