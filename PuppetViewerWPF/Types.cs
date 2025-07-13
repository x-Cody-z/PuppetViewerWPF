using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetViewerWPF
{
        public enum PuppetType
        {
            Void,
            Fire,
            Water,
            Nature,
            Earth,
            Steel,
            Wind,
            Electric,
            Light,
            Dark,
            Nether,
            Poison,
            Fighting,
            Illusion,
            Sound,
            Warped,
            Dream,
            None
        }

        public static class TypeEffectiveness
        {
            private static readonly double[,] effectivenessMatrix = new double[,]
            {
                //V   F   W   N   E   S   W   E   L   D   N   P   F   I   S   W   D   N
                { 1,  1,  1,  1,  1,  1,  1,  1,  0.5,0.5,1,  1,  1,  0,  1,  1,  1,  1   }, // Void
                { 1,  0.5,0.5,2,  0.5,2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1   }, // Fire
                { 1,  2,  0.5,0.5,2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0.5,1,  1,  1   }, // Water
                { 1,  0.5,2,  0.5,2,  1,  0.5,1,  1,  1,  1,  0.5,1,  1,  1,  1,  1,  1   }, // Nature
                { 1,  2,  1,  0.5,1,  2,  0,  2,  1,  1,  1,  2,  0.5,1,  1,  1,  1,  1   }, // Earth
                { 1,  0.5,0.5,2,  1,  0.5,2,  1,  1,  2,  1,  1,  1,  1,  1,  0.5,1,  1   }, // Steel
                { 1,  1,  1,  1,  1,  0.5,1,  0.5,1,  1,  1,  2,  2,  1,  2,  0,  1,  1   }, // Wind
                { 1,  1,  2,  0.5,0,  1,  2,  0.5,0.5,1,  1,  1,  1,  1,  2,  1,  1,  1   }, // Electric
                { 1,  1,  0.5,0.5,1,  1,  1,  1,  0.5,2,  2,  1,  1,  0.5,1,  1,  1,  1   }, // Light
                { 2,  1,  1,  1,  1,  1,  1,  1,  2,  0.5,2,  1,  0.5,0.5,1,  1,  1,  1   }, // Dark
                { 1,  1,  1,  1,  1,  1,  1,  1,  0.5,1,  2,  1,  1,  1,  0.5,1,  1,  1   }, // Nether
                { 1,  1,  2,  2,  0.5,0,  1,  1,  1,  1,  0.5,0.5,1,  1,  1,  2,  1,  1   }, // Poison
                { 1,  1,  1,  1,  2,  2,  0.5,1,  1,  2,  0,  0.5,1,  0.5,1,  2,  1,  1   }, // Fighting
                { 0,  1,  1,  1,  1,  0.5,1,  1,  2,  1,  0.5,1,  1,  2,  1,  1,  1,  1   }, // Illusion
                { 1,  1,  1,  1,  1,  1,  0.5,1,  0.5,1,  1,  1,  2,  2,  0.5,2,  1,  1   }, // Sound
                { 1,  1,  1,  1,  1,  2,  2,  1,  1,  0.5,1,  0.5,0.5,2,  1,  0.5,1,  1   }, // Warped
                { 1,  1  ,1  ,1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1   }, // Dream
                { 1,  1  ,1  ,1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1   }  // None
            };

            public static double GetEffectiveness(PuppetType attacker, PuppetType defender)
            {
                int row = (int)attacker;
                int col = (int)defender;
                return effectivenessMatrix[row, col];
            }

            public static Dictionary<PuppetType, double> GetAllAttackEffectivenessAgainstDualTypes(string defenderType1, string defenderType2)
            {
                if (!Enum.TryParse(defenderType1, out PuppetType def1) || !Enum.TryParse(defenderType2, out PuppetType def2))
                {
                    throw new ArgumentException("Invalid defender type name(s).");
                }

                var results = new Dictionary<PuppetType, double>();

                foreach (PuppetType attacker in Enum.GetValues(typeof(PuppetType)))
                {
                    if (attacker == PuppetType.None) continue; // Optional: skip "None" as an attacker

                    double eff1 = GetEffectiveness(attacker, def1);
                    double eff2 = GetEffectiveness(attacker, def2);
                    double combined = eff1 * eff2;

                    results[attacker] = combined;
                }

                return results;
            }

            public static List<List<PuppetType>> GroupAttackTypesByEffectiveness(string defenderType1, string defenderType2)
            {
                if (!Enum.TryParse(defenderType1, out PuppetType def1) || !Enum.TryParse(defenderType2, out PuppetType def2))
                {
                    throw new ArgumentException("Invalid defender type name(s).");
                }

                // Create 6 buckets: 4x, 2x, 1x, 0.5x, 0.25x, 0x
                var effectivenessGroups = new List<List<PuppetType>>
                {
                    new List<PuppetType>(), // 4x
                    new List<PuppetType>(), // 2x
                    new List<PuppetType>(), // 1x
                    new List<PuppetType>(), // 0.5x
                    new List<PuppetType>(), // 0.25x
                    new List<PuppetType>()  // 0x
                };

                foreach (PuppetType attacker in Enum.GetValues(typeof(PuppetType)))
                {
                    if (attacker == PuppetType.None) continue; // Optionally skip "None"

                    double effectiveness = GetEffectiveness(attacker, def1) * GetEffectiveness(attacker, def2);

                    // Categorize into the appropriate group
                    if (effectiveness == 4.0)
                        effectivenessGroups[0].Add(attacker);
                    else if (effectiveness == 2.0)
                        effectivenessGroups[1].Add(attacker);
                    else if (effectiveness == 1.0)
                        effectivenessGroups[2].Add(attacker);
                    else if (effectiveness == 0.5)
                        effectivenessGroups[3].Add(attacker);
                    else if (effectiveness == 0.25)
                        effectivenessGroups[4].Add(attacker);
                    else if (effectiveness == 0.0)
                        effectivenessGroups[5].Add(attacker);
                    // You can add more conditions if intermediate values like 0.75 or 1.5 exist
                }

                return effectivenessGroups;
            }


        }
    }
