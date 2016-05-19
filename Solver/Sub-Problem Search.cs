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

namespace SolverForDummies
{
    public partial class Solver<Variable, Assignment, Score>
    {
        public class SubProblem
        {
            // NOTE: we assume variables and their bindings are in the same order in the array.   Otherwise we'd have to use a Dictionary.
            public struct Solution
            {
                public Assignment[] constraints;
                public Assignment[] bindings;
                public void RemoveConstraints(IEnumerable<int> indicesToRemove)
                {
                    constraints = Util.RemoveIndicesFromArray<Assignment>(constraints, indicesToRemove);
                }
            }
            public Solution solution;
            public Variable[] variables;
            public IEnumerable<Solution> possibleSolutions;
            public SubProblem(Variable[] variables, IEnumerable<Solution> possibleSolutions)
            {
                this.variables = variables;
                this.possibleSolutions = possibleSolutions;
            }
        }

        public class SubProblemFactory : Problem
        {
            // NOTE: we assume variables and their bindings are in the same order in the array.   Otherwise we'd have to use a Dictionary.
            private SubProblemSearch parent;
            public Variable[] variables;
            public Variable[] dependencies;
            public List<SubProblem.Solution> possibleSolutions;
            NeighborhoodSearch neighborhoodSearch = new DepthFirstSearch();

            public SubProblem Create(SubProblemSearch parent, IEnumerable<Variable> subProblem)
            {
                this.parent = parent;
                this.variables = subProblem.ToArray();
                this.dependencies = this.parent.mainProblem.GetDependencies(subProblem).ToArray();
                possibleSolutions = new List<SubProblem.Solution>();
                Explore explore = new Explore();
                explore.Run(this, this.dependencies, FindBestSolutionGivenTheseDependencyConstraints_AndCache);
                return new SubProblem(variables, possibleSolutions);
            }

            public override IEnumerable<Assignment> GetDomain(Variable variable)
            {
                return parent.mainProblem.GetDomain(variable);
            }
            public override Score CreateScore(Instrumentation.FeatureInstanceProperties? featureInstanceProperties)
            {
                return parent.mainProblem.CreateScore(featureInstanceProperties);
            }

            public override void CopyScore(Score destination)
            {
                parent.mainProblem.CopyScore(destination);
            }
            public override bool IsBetterThan(Score bestKnown)
            {
                return parent.mainProblem.IsBetterThan(bestKnown);
            }

            public override bool UpdateAssignment(Variable variable, Assignment value)
            {
                return parent.mainProblem.UpdateAssignment(variable, value);
            }
            public void FindBestSolutionGivenTheseDependencyConstraints_AndCache()
            {
                Assignment[] savedBindings = parent.mainProblem.GetAssignments(variables);
                neighborhoodSearch.Run(parent.mainProblem, variables);
                possibleSolutions.Add(new SubProblem.Solution { constraints = parent.mainProblem.GetAssignments(dependencies), bindings = parent.mainProblem.GetAssignments(variables) });
                parent.mainProblem.RestoreAssignments(savedBindings, variables);
            }
            public override void ClearAssignment(Variable variable)
            {
                parent.mainProblem.ClearAssignment(variable);
            }
        }



        // This is a gauranteed optimal algorithm.  It utilizes the fact the a subproblem may only be dependent on a small set of variables.   This fact allows it to greatly reduce the search space 
        // compared to Depth First Search.   
        // Suppose: The total problem space is 40, domain size of each variable is 2, subproblem size = 10, and each subproblem is dependent on exactly 4 variables outside the subspace.
        // Then there are 2^4 = 16 possible assignments for the dependent variables.  For each one of these assignments (i.e. constraints)
        // we can compute the optimal subproblem -- given those constraints.   The global optimal solution will be a combination from these optimal subproblems.
        // To build the optimal subproblem cache, since each variable has a domain size of 2, and the subproblem size = 10, then DFS evaluates 2^10 = 1024 states.  So it will cost 4 * 16 * 1024 = 65536 evaluations
        // to build the cache.  There will be 16^4 = 65536 combinations of optimal subproblem solutions to explore.  Note: evaulating the an optimal subproblem is cheap since the real work was done during the caching
        // Its not clear if this method would have practical value beyond toy problems.
        public class SubProblemSearch : Solver<SubProblem, SubProblem.Solution, Score>.Problem
        {
            public Problem mainProblem { get; set; }
            public Solver<SubProblem, SubProblem.Solution, Score>.NeighborhoodSearch neighborhoodSearch { get; set; }

            private List<SubProblem> subProblems;
            int neighborhoodSize;
            public SubProblemSearch(Solver<SubProblem, SubProblem.Solution, Score>.NeighborhoodSearch neighborhoodSearch = null, int neighborhoodSize = 10)
            {
                if (neighborhoodSearch == null)
                    neighborhoodSearch = new Solver<SubProblem, SubProblem.Solution, Score>.DepthFirstSearch();
                this.neighborhoodSearch = neighborhoodSearch;
                this.neighborhoodSize = neighborhoodSize;
            }
            public IEnumerable<Variable> Run(Problem parent)
            {
                this.mainProblem = parent;
                SubProblemFactory subProblemFactory = new SubProblemFactory();
                subProblems = new List<SubProblem>();
                foreach (IEnumerable<Variable> neighborhood in parent.GetNeighborhoods(neighborhoodSize))
                    subProblems.Add(subProblemFactory.Create(this, neighborhood));

                neighborhoodSearch.Run(this, subProblems);
                return null;
            }
            public override Score CreateScore(Instrumentation.FeatureInstanceProperties? featureInstanceProperties)
            {
                return mainProblem.CreateScore(featureInstanceProperties);
            }

            public override void CopyScore(Score destination)
            {
                mainProblem.CopyScore(destination);
            }
            public override bool IsBetterThan(Score bestKnown)
            {
                return mainProblem.IsBetterThan(bestKnown);
            }
            public override SubProblem.Solution GetAssignment(SubProblem subProblem)
            {
                return subProblem.solution;
            }

            public override IEnumerable<SubProblem.Solution> GetDomain(SubProblem subProblem)
            {
                return subProblem.possibleSolutions;
            }
            public override bool UpdateAssignment(SubProblem subProblem, SubProblem.Solution value)
            {
                subProblem.solution = (SubProblem.Solution)value;
                mainProblem.RestoreAssignments(subProblem.solution.bindings, subProblem.variables);
                return true;  // There's an assumption that legality has already been checked.
            }
        }
    }

}