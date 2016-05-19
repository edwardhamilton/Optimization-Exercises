/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *
 ******************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SolverForDummies;

namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {

        public class Tester
        {
            static List<double> optimalityPerRun;
            public static double Run(VRPSolver.Problem_Loader.Type type, string directory, string searchPattern, int numIterations = 1, VRPSolver.Options tuner = null)
            {
                Logger.Log("Begin Testing...");
                optimalityPerRun = new List<double>();
                DateTime start = DateTime.Now;
                double totalCost = 0;
                for (int i = 0; i < numIterations; i++)
                {
                    foreach (string file in Directory.EnumerateFiles(directory, searchPattern))
                    {
                        //Logger.Log("Current problem file is " + file);
                        VRPSolver vrpSolver = new VRPSolver(file, type, tuner: tuner);
                        vrpSolver.Run();
                        //                        totalCost += vrpSolver.score.GetCombinedScore(1000, 300, 1, 1);
                        double optimality = vrpSolver.optimalityOfSolution;
                        if (vrpSolver.score.overCapacity >= .5)
                            optimality /= vrpSolver.score.overCapacity;
                        totalCost += optimality;
                        optimalityPerRun.Add(vrpSolver.optimalityOfSolution);
                        //Console.WriteLine(vrpSolver.GetProblemLabel());
                    }
                }
                TimeSpan elapse = DateTime.Now - start;
                double costPerUnitTime = totalCost * elapse.TotalSeconds;
                double score = 100.0 / (totalCost / numIterations);

                string results = "Results: Optimality = Avg " + optimalityPerRun.Average() + " Worst " + optimalityPerRun.Min() + " Best " + optimalityPerRun.Max() + ", Runtime = " + elapse;
                if (tuner != null)
                {
                    Console.WriteLine("Tuner: OverCapacityWeight = " + tuner.weightOverCapacity + ", Score = " + score + " " + results);
                    tuner.Log();
                }
                Logger.Log(results);
                return (totalCost / numIterations);
            }
        }


    }
}
