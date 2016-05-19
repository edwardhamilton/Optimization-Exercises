/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *      
 * Reference: https://en.wikipedia.org/wiki/Local_search_(optimization)
 *
 ******************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SolverForDummies
{
    public partial class Solver<Variable, Assignment, Score>
    {
        interface IMetaSearch
        {
            // This design pattern is not well developed at the moment, and is more of a placeholder.  But as the name suggests, this is where we'll keep algorithms that
            // uses other local search methods to work on subproblems and combine them in various ways.
            // Returns that list of variables that haven't converged
            Problem problem { get; }
            NeighborhoodSearch neighborhoodSearch { get; }

        }

        public class LocalSearch : IMetaSearch
        {
            // Not much here, but depending on the intelligence in the parent's GetSubProblems method, this can still be quite powerful.
            public Problem problem { get; set; }
            public NeighborhoodSearch neighborhoodSearch { get; set; }

            int featureID_Pass = Instrumentation.AddFeature("Local.Pass");
            int featureID_StuckCount = Instrumentation.AddFeature("Local.StuckCount");
            int featureID_Accepted = Instrumentation.AddFeature("Local.Accepted", applyBackwards: true);
            int featureID_NeighborhoodSize_Mean = Instrumentation.AddFeature("Local.NeighborhoodSize_Mean");
            int featureID_NeighborhoodSize_Variance = Instrumentation.AddFeature("Local.NeighborhoodSize_Variance");
            int featureID_PercentNoisePerStuck = Instrumentation.AddFeature("Local.PercentNoisePerStuck");

            protected Assignment[] originalSolution;
            protected Score originalScore;
            protected Distribution<int> neighborhoodSize;
            protected int maxNeighborhoodEvaluations, stuckCount;
            protected Range<int> passes;
            public double percentNoisePerStuck;
            protected double domainSize_Variance;
            //protected Random rand = new Random();
            public LocalSearch(NeighborhoodSearch neighborhoodSearch = null, Distribution<int> neighborhoodSize = null, int maxNeighborhoodEvaluations = 1000, Range<int>? passes = null, double percentNoisePerStuck = .10, double domainSize_Variance = .5)
            {
                if (neighborhoodSearch == null)
                    neighborhoodSearch = new DepthFirstSearch();
                if (passes == null)
                    passes = new Range<int>(2, 6);
                this.passes = (Range<int>)passes;
                this.neighborhoodSearch = neighborhoodSearch;
                if (neighborhoodSize != null)
                    this.neighborhoodSize = neighborhoodSize;
                else
                    this.neighborhoodSize = new UniformDistribution_Int(1, 5);
                this.maxNeighborhoodEvaluations = maxNeighborhoodEvaluations;
                this.percentNoisePerStuck = percentNoisePerStuck;
                this.domainSize_Variance = domainSize_Variance;

            }
            protected int pass;
            public virtual IEnumerable<Variable> Run(Problem parent, Distribution<int> neighborhoodSize = null, Range<int>? passes = null)
            {
                if (neighborhoodSize != null)
                    this.neighborhoodSize = neighborhoodSize;
                if (passes != null)
                    this.passes = (Range<int>)passes;
                problem = parent;
                Instrumentation.SetFeature(featureID_NeighborhoodSize_Mean, neighborhoodSize.mean);
                Instrumentation.SetFeature(featureID_NeighborhoodSize_Variance, neighborhoodSize.deviation);
                Instrumentation.SetFeature(featureID_PercentNoisePerStuck, percentNoisePerStuck);
                SaveOriginalSolution();
                IEnumerable<Variable> unconvergedVariables = null;
                stuckCount = 0;
                Score bestScore = problem.CreateScore(new Instrumentation.FeatureInstanceProperties("Local.BestScore", applyBackwards: true));
                problem.CopyScore(bestScore);
                for (pass = 0; pass < this.passes.max; pass++)
                {
                    Instrumentation.SetFeature(featureID_StuckCount, stuckCount);
                    Instrumentation.SetFeature(featureID_Pass, pass);
                    OptimizeEachNeighborhood(parent);
                    if (problem.IsBetterThan(bestScore))
                    {
                        stuckCount = 0;
                        problem.CopyScore(bestScore);
                        Instrumentation.MakeObservation_AtAccepted("Local.Accepted");
                    }
                    else
                    {
                        stuckCount++;
                        Instrumentation.MakeObservation_AtRejected("Local.Rejected");
                    }
                    problem.Notify(stuckCount == 0 ? Problem.Notification.New_LocalSolution_Accepted : Problem.Notification.New_LocalSolution_Rejected, parent.GetAllVariables());
                    if (pass > this.passes.min)
                    {
                        unconvergedVariables = FindUnconvergedVariables(parent);
                        if (unconvergedVariables.Count() == 0 || pass >= this.passes.max)
                            break;
                    }
                }
                if (!parent.IsBetterThan(originalScore))
                    RestoreOriginalSolution();
                return unconvergedVariables;
            }
            private void RestoreOriginalSolution()
            {
                problem.RestoreAssignments(originalSolution, problem.GetAllVariables());
            }
            private void SaveOriginalSolution()
            {
                originalScore = problem.CreateScore(); 
                originalSolution = problem.GetAssignments(problem.GetAllVariables());
                problem.CopyScore(originalScore);
            }

            private IEnumerable<Variable> FindUnconvergedVariables(Problem problem)
            {
                List<Variable> unconvergedVariables = new List<Variable>();
                Variable[] variables = problem.GetAllVariables().ToArray();
                Assignment[] valuesBefore = problem.GetAssignments(variables);
                OptimizeEachNeighborhood(problem);
                Assignment[] valuesAfter = problem.GetAssignments(variables);
                for (int i = 0; i < variables.Length; i++)
                {
                    if (valuesAfter[i] != null)
                    {
                        if (!valuesAfter[i].Equals(valuesBefore[i]))
                            unconvergedVariables.Add(variables[i]);
                    }
                }
                return unconvergedVariables;
            }

            private void OptimizeEachNeighborhood(Problem problem)
            {
                // Note that GetSubProblems doesn't have to return the same set for each pass.
                int neighborhoodSize = this.neighborhoodSize.Next();
                double mean = Math.Ceiling(Math.Pow(maxNeighborhoodEvaluations, 1.0 / neighborhoodSize) / 2);
                double variance = domainSize_Variance;
                Distribution<int> domainSize = new UniformDistribution_Int(mean, variance);
                IEnumerable<IEnumerable<Variable>> neighborhoods = this.problem.GetNeighborhoods(size: neighborhoodSize);
                foreach (IEnumerable<Variable> neighborhood in neighborhoods)
                {
                    Instrumentation.MakeObservation_AtAccepted("New.Neighborhood");
                    this.neighborhoodSearch.Run(this.problem, neighborhood, domainSize: domainSize, noise: stuckCount * percentNoisePerStuck);
                }
            }
            protected virtual Distribution<int> GetNeighborhoodSize()
            {
                return neighborhoodSize;
            }

        }

        /* Reference: https://en.wikipedia.org/wiki/Simulated_annealing */
        public class SimulatedAnnealing : LocalSearch
        {
            int startingNeighborhoodSize;
            double varianceOnNeighborhoodSize;
            public SimulatedAnnealing(NeighborhoodSearch neighborhoodSearch = null, int startingNeighborhoodSize = 10, double varianceOnNeighborhoodSize = .2, int maxNeighborhoodEvaluations = 1000, Range<int>? passes = null) :
                base(neighborhoodSearch, maxNeighborhoodEvaluations: maxNeighborhoodEvaluations, passes: passes)
            {
                this.startingNeighborhoodSize = startingNeighborhoodSize;
                this.varianceOnNeighborhoodSize = varianceOnNeighborhoodSize;

            }
            double GetTemperature() { return (1 - ((double)pass / (double)passes.max)); }
            protected override Distribution<int> GetNeighborhoodSize()
            {
                int neighborhoodSize = (int)Math.Ceiling((double)1 + (GetTemperature() * (double)(startingNeighborhoodSize - 1)));
                return new UniformDistribution_Int(neighborhoodSize, varianceOnNeighborhoodSize);
            }

        }
    }

}
