/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 * 
 * Reference: http://www.econ.upf.edu/~ramalhin/PDFfiles/2010_ILSv2.pdf
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
        public abstract class Perturbation
        {
            public Problem parent;
            public Perturbation(Problem parent)
            {
                this.parent = parent;
            }
            public virtual void Cleanup()
            {

            }
            public abstract void Next();
        }
        public class NeighborhoodPerturbation : Perturbation
        {
            int featureID_NeighborhoodSize = Instrumentation.AddFeature("Perturbation.Neighborhood.Size");
            public NeighborhoodSearch neighborhoodSearch;
            protected IEnumerator<IEnumerable<Variable>> neighborhoods;
            public Distribution<int> neighborhoodSize;
            protected double noise;
            protected Distribution<int> domainSize;
            public NeighborhoodPerturbation(Problem parent, NeighborhoodSearch neighborhoodSearch = null, Distribution<int> neighborhoodSize = null, Distribution<int> domainSize = null, double noise = 1)
                : base(parent)
            {
                if (neighborhoodSearch == null)
                    this.neighborhoodSearch = new DepthFirstSearch();
                if (neighborhoodSize == null)
                    neighborhoodSize = new UniformDistribution_Int(2, 20);
                this.neighborhoodSize = neighborhoodSize;
                this.noise = noise;
                if (domainSize == null)
                    domainSize = new UniformDistribution_Int(1, 2);
                this.domainSize = domainSize;
                DefineNeighborhoods();
            }

            protected void DefineNeighborhoods()
            {
                int neighborhoodSize = this.neighborhoodSize.Next();
                Instrumentation.SetFeature(featureID_NeighborhoodSize, neighborhoodSize);
                neighborhoods = parent.GetNeighborhoods(neighborhoodSize).GetEnumerator();
                neighborhoods.MoveNext();
            }

            public override void Next()
            {
                if (!neighborhoods.MoveNext())
                    DefineNeighborhoods();
                IEnumerable<Variable> neighborhood = neighborhoods.Current;
                neighborhoodSearch.Run(parent, variables: neighborhood, acceptLocalBest: true, domainSize: domainSize, noise: noise);
            }
            public override void Cleanup()
            {
                Instrumentation.ClearFeature(featureID_NeighborhoodSize);
            }
        }

        public class AnnealingPerturbation : NeighborhoodPerturbation
        {
            int featureID_Noise = Instrumentation.AddFeature("AnnealingPerturbation.Noise");
            protected int numPerturbations, perturbation;
            protected double noisePerTemperature;
            public AnnealingPerturbation(Problem parent, int numPerturbations, Distribution<int> neighborhoodSize = null, Distribution<int> domainSize = null, double noisePerTemperature = 1)
                : base(parent, neighborhoodSize: neighborhoodSize, domainSize: domainSize)
            {
                this.numPerturbations = numPerturbations;
                perturbation = 0;
                this.noisePerTemperature = noisePerTemperature;
            }
            double GetTemperature() { return (1 - ((double)perturbation / (double)numPerturbations)); }

            public override void Next()
            {
                if (!neighborhoods.MoveNext())
                    DefineNeighborhoods();
                IEnumerable<Variable> neighborhood = neighborhoods.Current;
                double noise = GetTemperature() * noisePerTemperature;
                Instrumentation.SetFeature(featureID_Noise, noise);
                neighborhoodSearch.Run(parent, variables: neighborhood, acceptLocalBest: true, domainSize: domainSize, noise: noise);
                perturbation++;
            }
            public override void Cleanup()
            {
                Instrumentation.ClearFeature(featureID_Noise);
                base.Cleanup();
            }
        }

        public class RelaxationPerturbation : NeighborhoodPerturbation
        {
            private Action<bool> relaxation;
            LocalSearch localSearch;

            public RelaxationPerturbation(Problem parent, Action<bool> relaxation, int numPerturbations, Distribution<int> neighborhoodSize = null)
                : base(parent, neighborhoodSize: neighborhoodSize)
            {
                this.relaxation = relaxation;
                this.localSearch = new LocalSearch();

            }
            public override void Next()
            {
                if (!neighborhoods.MoveNext())
                    DefineNeighborhoods();
                LockNeighborhood();
                ApplyRelaxation();
                ReoptimizeNeighborhood();
                UnlockNeighborhood();
                RevertRelaxation();
                ReoptimizeNeighborhood();
            }

            private void ReoptimizeNeighborhood()
            {
                localSearch.Run(parent, neighborhoodSize: new UniformDistribution_Int(4, 5));
            }

            private void RevertRelaxation()
            {
                relaxation.Invoke(false);
            }

            private void ApplyRelaxation()
            {
                relaxation.Invoke(true);
            }

            private void UnlockNeighborhood()
            {
                foreach (Variable v in neighborhoods.Current)
                    parent.LockVariable(v, false);
            }

            private void LockNeighborhood()
            {
                if (neighborhoods.Current != null)
                {
                    foreach (Variable v in neighborhoods.Current)
                        parent.LockVariable(v, false);
                }
            }
        }



        public class BackupPerturbation : NeighborhoodPerturbation
        {
            public BackupPerturbation(Problem parent, Distribution<int> neighborhoodSize = null)
                : base(parent, neighborhoodSize: neighborhoodSize)
            {
            }

            public override void Next()
            {
                IEnumerable<Variable> neighborhood = neighborhoods.Current;
                foreach (Variable variable in neighborhood)
                    parent.ClearAssignment(variable);
                if (!neighborhoods.MoveNext())
                    DefineNeighborhoods();
            }
        }
        public interface Acceptor
        {
            bool Accept_If_ConditionsMet(Problem problem);
        }
        public class SimpleAcceptor : Acceptor
        {
            Assignment[] values;
            Score bestScore = default(Score);
            public SimpleAcceptor(Problem problem, int numVariables)
            {
                bestScore = problem.CreateScore(new Instrumentation.FeatureInstanceProperties("Iterated.BestScore", applyBackwards: false));
                values = new Assignment[numVariables];
            }
            public bool Accept_If_ConditionsMet(Problem problem)
            {
                bool result;
                lock (values)
                {
                    if (problem.IsBetterThan(bestScore))
                    {
                        problem.SaveAssignments(problem.GetAllVariables(), values);
                        problem.CopyScore(bestScore);
                        result = true;
                        Instrumentation.MakeObservation_AtAccepted("Iterated.Accepted");
                    }
                    else
                    {
                        problem.RestoreAssignments(values, problem.GetAllVariables());
                        result = false;
                        Instrumentation.MakeObservation_AtRejected("Iterated.Rejected");
                    }
                }
                return result;
            }
        }


        public class IteratedLocalSearch
        {
            int featureID_Iteration = Instrumentation.AddFeature("Iterated.Iteration");
            int featureID_StuckCount = Instrumentation.AddFeature("Iterated.StuckCount");

            public Problem problem;
            LocalSearch localSearch;
            Perturbation perturbation;
            Acceptor acceptor;
            protected double percentNoisePerStuck;
            protected int stuckCount;

            public IteratedLocalSearch(LocalSearch localSearch = null, Acceptor acceptor = null, double percentNoisePerStuck = .1)
            {
                this.percentNoisePerStuck = percentNoisePerStuck;
                this.localSearch = localSearch;
                if (this.localSearch == null)
                    this.localSearch = new LocalSearch(neighborhoodSize: new UniformDistribution_Int(2, 5));

                this.acceptor = acceptor;

            }
            int iteration, numIterations;
            double GetTemperature() { return (1 - ((double)iteration / (double)numIterations)); }

            public void Run(Problem parent, Perturbation perturbation = null, int numIterations = 10, Distribution<int> neighborhoodSize = null)
            {
                problem = parent;
                if (this.acceptor == null)
                    this.acceptor = new SimpleAcceptor(parent, problem.GetAllVariables().Count());
                stuckCount = 0;
                if (neighborhoodSize == null)
                    neighborhoodSize = new UniformDistribution_Int(2, 6);
                if (perturbation == null)
                    perturbation = new NeighborhoodPerturbation(parent);
                this.perturbation = perturbation;
                this.numIterations = numIterations;
                acceptor.Accept_If_ConditionsMet(problem);
                for (iteration = 0; iteration < numIterations; iteration++)
                {
                    Instrumentation.SetFeature(featureID_Iteration, iteration);
                    perturbation.Next();
                    localSearch.percentNoisePerStuck = ((1 + stuckCount) * percentNoisePerStuck);
                    localSearch.Run(parent, neighborhoodSize: neighborhoodSize);
                    Instrumentation.SetFeature(featureID_StuckCount, stuckCount);
                    if (!acceptor.Accept_If_ConditionsMet(problem))
                    {
                        problem.Notify(Problem.Notification.New_GlobalSolution_Rejected, parent.GetAllVariables());
                        stuckCount = 0;
                    }
                    else
                    {
                        problem.Notify(Problem.Notification.New_GlobalSolution_Accepted, parent.GetAllVariables());
                        stuckCount++;
                    }
                }
                perturbation.Cleanup();

            }

        }
    }

}