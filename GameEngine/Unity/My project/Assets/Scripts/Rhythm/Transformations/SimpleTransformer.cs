using System;
using UnityEngine;
using IT4s.Data;
using IT4s.Rhythm;



namespace IT4s.Rhythm.Transformations
{
    public static class SimpleTransformer
    {
        public enum Mode
        {
            Triplah,
            Triplets
        }

        public static PatternTurn Transform(PatternTurn src, Mode mode, int seed = 0)
        {
            return mode switch
            {
                Mode.Triplah => Triplah(src),
                Mode.Triplets => Triplets(src),
                _ => Triplah(src)
            };
        }

        static PatternTurn Triplah(PatternTurn pattern)
        {
            int patternSteps = pattern.velocity.Length;
            var targetVelocities = new int[patternSteps];
            var off = new int[patternSteps];
            
            int finalIndex = 0, hit = 0, stepHit = 0;
            // const int velThresh = 90;
            bool tripling = false;


            for (int i = 0; i < patternSteps; i++)
            {
                // Debug.Log($"i = {i}");
                stepHit = pattern.velocity[i];
                

                if (stepHit > 0)
                {
                    tripling = true;
                    finalIndex = i + 3;
                    hit = stepHit;

                }
                if (tripling)
                {
                    // Debug.Log($"Tripling = {tripling}: finalIndex = {finalIndex}");
                    targetVelocities[i] = hit;
                    // Debug.Log("just hit!");
                    finalIndex--;
                    if (i > finalIndex)
                    {
                        // Debug.Log("i = finalindex therefore tripling = false");
                        tripling = false;
                    }
                    
                    off[i] = pattern.offsetSamples[i];
                    // Debug.Log($"end of triple sequence: finalIndex = {finalIndex}");
                }
                else 
                { continue;}
                
                // Debug.Log($"[LOOP{i}]______| tripling = {tripling} | finalIndex = {finalIndex} | stepHit = {stepHit} | hit = {hit}");
            }

            Debug.Log($"finalIndex velocities: {string.Join(", ", targetVelocities)}");
            var outTurn = pattern;
            pattern.velocity = targetVelocities;
            return outTurn; 
        }


        // this works but maybe we make it more modular so that a triplet can be added to any pattern segment with a hit proceeded by 7 offs
        static PatternTurn Triplets(PatternTurn pattern)
        {
            int patternSteps = pattern.velocity.Length;
            var targetVelocities = new int[patternSteps];
            var off = new int[patternSteps];
            
            int finalIndex = 0, hit = 0, stepHit = 0;
            // const int velThresh = 90;
            bool tripling = false;


            for (int i = 0; i < patternSteps; i++)
            {
                // Debug.Log($"i = {i}, final index = {finalIndex}");
                stepHit = pattern.velocity[i];
                
                if (stepHit > 0)
                {
                    tripling = true;
                    finalIndex = i + 12;
                    hit = stepHit;
                }
                if (tripling)
                {
                    // Debug.Log($"Tripling = {tripling}: finalIndex = {finalIndex}");
                    if(finalIndex%4 == 0 || i == 0)
                    {
                        targetVelocities[i] = hit;
                        // Debug.Log($"Added a hit");
                    }
                    // Debug.Log("just hit!");
                    finalIndex--;
                    if (finalIndex == 0)
                    {
                        // Debug.Log("i = finalindex therefore tripling = false");
                        tripling = false;
                    }
                    
                    off[i] = pattern.offsetSamples[i];
                    // Debug.Log($"end of triple sequence: finalIndex = {finalIndex}");
                }
                else 
                { continue;}
                
                // Debug.Log($"[LOOP{i}]______| tripling = {tripling} | finalIndex = {finalIndex} | stepHit = {stepHit} | hit = {hit}");
            }

            Debug.Log($"finalIndex velocities: {string.Join(", ", targetVelocities)}");
            var outTurn = pattern;
            pattern.velocity = targetVelocities;
            return outTurn; 
        }
    }
}



// [TurnCaptureController] END turn 0: 6 hits | 
// pattern: 
// 79, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 85, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 99, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 98, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 97, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0


// 79, 79, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 104, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 85, 85, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 99, 99, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
// 97, 97, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
