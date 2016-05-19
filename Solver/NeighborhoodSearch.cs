/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *      
 * References:
 *      https://en.wikipedia.org/wiki/Very_large-scale_neighborhood_search
 *      https://en.wikipedia.org/wiki/Greedy_randomized_adaptive_search_procedure
 *      
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
        // Most local search classes are derived from this.   
        // Search is looking for some best value.  Explore is so you can do something with every possible combination.  I have combined both notions into a single base class for now -- somewhat questionable.

        public class PickBestSolution
        {
            protected Problem problem;
            public IEnumerable<Variable> variables;
            protected Score bestScore, originalScore;
            protected Assignment[] bestSolution;
            protected List<Action> actionsToTry = new List<Action>();

            #region Used by search type parent classes
            protected virtual void Initialize_And_SaveOriginalSolution(Problem parent, IEnumerable<Variable> variables, bool acceptLocalBest)
            {
                this.variables = variables;
                this.problem = parent;
                bestSolution = parent.GetAssignments(variables);
                originalScore = parent.CreateScore();
                bestScore = parent.CreateScore(new Instrumentation.FeatureInstanceProperties("Neighborhood.BestScore"));
                if (!acceptLocalBest)
                    parent.CopyScore(bestScore);
            }
            protected void RestoreBestSolution()
            {
                problem.RestoreAssignments(bestSolution, variables);
            }
            protected bool SaveSolutionIfBest()
            {
                bool result;
                if (problem.IsBetterThan(bestScore))
                {
                    problem.SaveAssignments(variables, bestSolution);
                    problem.CopyScore(bestScore);
                    result = true;
                }
                else
                    result = false;
                return result;
            }
            public void AddActionToTry(Action action)
            {
                actionsToTry.Add(action);
            }
            public void Run(Problem parent)
            {
                Initialize_And_SaveOriginalSolution(parent, parent.GetAllVariables(), acceptLocalBest: false);
                foreach (Action a in actionsToTry)
                {
                    a.Invoke();
                    SaveSolutionIfBest();
                }
                RestoreBestSolution();
            }
            #endregion
        }
        static long neighborhoodID;
        abstract public class NeighborhoodSearch : PickBestSolution
        {
            int featureID_NeghborhoodID = Instrumentation.AddFeature("Neighborhood.ID");
            protected Queue<Variable> variablesQueue;
            protected void BeginSearch(Problem parent, IEnumerable<Variable> variables, bool acceptLocalBest)
            {
                neighborhoodID++;
                Instrumentation.SetFeature(featureID_NeghborhoodID, neighborhoodID);
                Initialize_And_SaveOriginalSolution(parent, variables, acceptLocalBest);
                Initialize_ExplorationQueue(variables);
            }

            protected void Initialize_ExplorationQueue(IEnumerable<Variable> variables)
            {
                this.variablesQueue = new Queue<Variable>(variables);
            }

            protected virtual void EvaluatePossibleSolution()
            {
                bool accepted = SaveSolutionIfBest();
                problem.Notify(accepted ? Problem.Notification.New_NeighborhoodSolution_Accepted : Problem.Notification.New_NeighborhoodSolution_Rejected, variables);
                if (accepted)
                    Instrumentation.MakeObservation_AtAccepted("Neighborhood.Accepted");
                else
                    Instrumentation.MakeObservation_AtRejected("Neighborhood.Rejected");


            }
            public abstract void Run(Problem parent, IEnumerable<Variable> variables, bool acceptLocalBest = false, Distribution<int> domainSize = null, double noise = 0);

            #region Used by explore type parent classes
            #endregion

        }
        /* Reference: https://en.wikipedia.org/wiki/Depth-first_search */
        public class DepthFirstSearch : NeighborhoodSearch
        {
            int featureID_DomainSize_Mean = Instrumentation.AddFeature("DepthFirstSearch.DomainSize_Mean");
            int featureID_DomainSize_Variance = Instrumentation.AddFeature("DepthFirstSearch.DomainSize_Variance");
            int featureID_Noise = Instrumentation.AddFeature("DepthFirstSearch.Noise");
            protected Distribution<int> domainSize;
            protected double noise;

            public DepthFirstSearch()
            {
            }


            private void LogDomain(object variable, IEnumerable<object> domain)
            {
                string log = "";
                foreach (object value in domain)
                {
                    if (log.Length > 0)
                        log += ", ";
                    log += value;
                }
                Logger.Log("Domain for " + variable + " = " + log);
            }

            public override void Run(Problem parent, IEnumerable<Variable> variables, bool acceptLocalBest = false, Distribution<int> domainSize = null, double noise = 0)
            {
                if (domainSize == null)
                {
                    domainSize = new UniformDistribution_Int(new Range<int>(int.MaxValue, int.MaxValue));
                    if (noise > 0)
                        throw new Exception("something wrong");
                }
                this.domainSize = domainSize;
                Instrumentation.SetFeature(featureID_DomainSize_Mean, domainSize.mean);
                Instrumentation.SetFeature(featureID_DomainSize_Variance, domainSize.deviation);
                Instrumentation.SetFeature(featureID_Noise, noise);
                this.noise = noise;
                BeginSearch(parent, variables, acceptLocalBest);
                Explore();
                RestoreBestSolution();
            }
            protected void Explore()
            {
                if (variablesQueue.Count > 0)
                {
                    Variable variable = variablesQueue.Dequeue();
                    int domainSize = this.domainSize.Next();
                    IEnumerable<Assignment> domain = problem.GetDomain(variable, domainSize, noise);
                    bool success = false;
                    foreach (Assignment value in domain)
                    {
                        if (problem.IsValueConsistent(variable, value))
                        {
                            problem.UpdateAssignment(variable, value); // If update not successful then we are pruning that branch
                            Explore();
                            success = true;
                        }
                    }
                    if (!success)
                        Explore();
                    variablesQueue.Enqueue(variable);
                }
                else
                    EvaluatePossibleSolution();

            }
        }
        class Explore : DepthFirstSearch
        {
            // Allows you to explore an entire space and maybe cache or analyze all possible solutions (for example).
            Action doSomethingWithAPossibleSolution;
            public void Run(Problem parent, IEnumerable<Variable> variables, Action doSomethingWithAPossibleSolution)
            {
                domainSize = new UniformDistribution_Int(new Range<int>(int.MaxValue, int.MaxValue));
                this.doSomethingWithAPossibleSolution = doSomethingWithAPossibleSolution;
                this.variables = variables;
                this.problem = parent;
                Initialize_ExplorationQueue(variables);
                Explore();
            }
            protected override void EvaluatePossibleSolution()
            {
                this.doSomethingWithAPossibleSolution();
            }
        }
    }

}

