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
using System.Diagnostics;

namespace SolverForDummies
{
    class TimeupException : Exception
    {
        public TimeupException() : base("Timeup")
        {

        }
    }
    public abstract class ScoreBase<T>
    {
        public abstract void Copy(T t);
    }

    public partial class Solver<Variable, Assignment, Score> where Score : ScoreBase<Score>
    {
        abstract public class ProblemBase
        {
            public virtual Range<Assignment> GetDomain(Variable variable) { throw new NotImplementedException(); }
            public abstract bool UpdateAssignment(Variable variable, Assignment value);
            public virtual Assignment GetAssignment(Variable variable) { throw new NotImplementedException(); }
            public virtual void SaveAssignments(IEnumerable<Variable> variables, Assignment[] values)
            {
                int i = 0;
                foreach (Variable variable in variables)
                    values[i++] = GetAssignment(variable);
            }
            public Assignment[] GetAssignments(IEnumerable<Variable> variables)
            {
                Assignment[] assignments = new Assignment[variables.Count()];
                int i = 0;
                foreach (Variable variable in variables)
                    assignments[i++] = GetAssignment(variable);
                return assignments;
            }
            public virtual void RestoreAssignments(Assignment[] assignments, IEnumerable<Variable> variables)
            {
                int i = 0;
                foreach (Variable variable in variables)
                    UpdateAssignment(variable, assignments[i++]);
            }
        }
        public class SolutionShare
        {
            Problem problem;
            Assignment[] values = null;
            Score score = default(Score);
            public SolutionShare(Problem problem) 
            {
                this.problem = problem;
            }
            public void SaveIfBest()
            {
                if (problem.IsBetterThan(score))
                {
                    problem.SaveAssignments(problem.GetAllVariables(), values);
                    problem.CopyScore(score);
                }
            }
            public void RestoreBest()
            {
                problem.RestoreAssignments(values, problem.GetAllVariables());
            }
        }

        // This design pattern can be used by the main problem, or some of the local search methods that work on subproblems.
        abstract public class Problem : ProblemBase
        {
            public virtual new IEnumerable<Assignment> GetDomain(Variable variable) { return GetDomain(variable, size: int.MaxValue, noise: 0); }
            public virtual IEnumerable<Assignment> GetDomain(Variable variable, int size, double noise)
            {
                return GetDomain(variable);
            }
            // NOTE: the point of solutionID is to allow for algorithms that keep track of multiple solutions.  The final solution is ID = null
            // Only updates if value is legal, otherwise returns false.  This method also allows for constraint propagation was a branch can be ruled out when this method returns false.
            public virtual void ClearAssignment(Variable variable) { throw new NotImplementedException(); }
            public virtual void LockVariable(Variable variable, bool truefalse) { throw new NotImplementedException(); }
            public virtual bool IsValueConsistent(Variable variable, Assignment value) { return true; }
            // Returns variables that this variable depends on.  Dependency can mean whatever the algorithm wants.   It could be strick, or loose.
            public virtual IEnumerable<Variable> GetDependencies(IEnumerable<Variable> variables) { throw new NotImplementedException(); }
            // Used my meta search methods that need to work on smaller parts.  Generally you want break things up into dependent subproblems.
            public virtual IEnumerable<IEnumerable<Variable>> GetNeighborhoods(int size) { throw new NotImplementedException(); }
            public virtual IEnumerable<Variable> GetAllVariables() { throw new NotImplementedException(); }
            public virtual string GetSolution_ForInstrumentation(IEnumerable<Variable> variables) 
            {
                return "NA";
            }
            public virtual string VariableToString_ForInstrumentation(Variable variable)
            {
                return "NA";
            }
            public virtual string AssignmentToString_ForInstrumentation(Assignment assignemnt)
            {
                return "NA";
            }

            public abstract Score CreateScore(Instrumentation.FeatureInstanceProperties? featureInstanceProperties = null);
            public abstract void CopyScore(Score destination);
            public abstract bool IsBetterThan(Score thisScore);

            public enum Notification
            {
                New_NeighborhoodSolution_Accepted,
                New_NeighborhoodSolution_Rejected,
                New_LocalSolution_Accepted,
                New_LocalSolution_Rejected,
                New_GlobalSolution_Accepted,
                New_GlobalSolution_Rejected,
            }
            public virtual void Notify(Notification notification, IEnumerable<Variable> variables) { }
            public void ClearAssignments(IEnumerable<Variable> variables)
            {
                foreach (Variable variable in variables)
                    ClearAssignment(variable);
            }

        }
    }
}

