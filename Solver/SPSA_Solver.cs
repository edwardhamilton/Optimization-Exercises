/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 * 
 * Reference: http://www.jhuapl.edu/spsa/PDF-SPSA/Spall_Implementation_of_the_Simultaneous.PDF
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SolverForDummies
{
    public class BasicScore<T> : ScoreBase<BasicScore<T>>
    {
        public T value;
        public static implicit operator T(BasicScore<T> o)
        {
            return o.value;
        }
        public override void Copy(BasicScore<T> from)
        {
            value = from.value;
        }
    }
    public class RandomVariable<Domain>
    {
        protected Domain value;
        Distribution<Domain> distribution;
        public RandomVariable(Domain value, Distribution<Domain> distribution)
        {
            this.value = value;
            this.distribution = distribution;
        }
        // User-defined conversion from Digit to double
        public static implicit operator Domain(RandomVariable<Domain> v)
        {
            return v.value;
        }
        public IEnumerable<Domain> GetDomain(int maxDomainSize, double noise)
        {
            for (int i = 0; i < maxDomainSize; i++)
                yield return distribution.Next(noise);
        }
        public void Assign(Domain value)
        {
            this.value = value;
        }
        public override string ToString()
        {
            return value.ToString();
        }
    }
    public class RandomSolver<Domain> : Solver<RandomVariable<Domain>, Domain, BasicScore<double>>.Problem
    {
        List<RandomVariable<Domain>> variables = new List<RandomVariable<Domain>>();
        Func<double> getScore;
        public RandomSolver(Func<double> getScore)
        {
            this.getScore = getScore;
        }
        public void AddVariable(RandomVariable<Domain> variable)
        {
            variables.Add(variable);
        }
        public override IEnumerable<Domain> GetDomain(RandomVariable<Domain> variable, int size, double noise)
        {
            return variable.GetDomain(size, noise);
        }
        // NOTE: the point of solutionID is to allow for algorithms that keep track of multiple solutions.  The final solution is ID = null
        // Only updates if value is legal, otherwise returns false.  This method also allows for constraint propagation was a branch can be ruled out when this method returns false.
        public override bool UpdateAssignment(RandomVariable<Domain> variable, Domain value)
        {
            variable.Assign(value);
            return true;
        }
        public override Domain GetAssignment(RandomVariable<Domain> variable)
        {
            return variable;
        }
        public override BasicScore<double> CreateScore(Instrumentation.FeatureInstanceProperties? featureInstanceProperties)
        {
            return new BasicScore<double>();
        }
        public override void CopyScore(BasicScore<double> destination)
        {
            destination.value = getScore.Invoke();
        }
        public override bool IsBetterThan(BasicScore<double> o)
        {
            return getScore.Invoke().CompareTo(o.value) < 0;
        }
        // Returns variables that this variable depends on.  Dependency can mean whatever the algorithm wants.   It could be strick, or loose.
        // Used my meta search methods that need to work on smaller parts.  Generally you want break things up into dependent subproblems.
        public override IEnumerable<IEnumerable<RandomVariable<Domain>>> GetNeighborhoods(int size)
        {
            List<RandomVariable<Domain>> neighborhood = new List<RandomVariable<Domain>>();
            foreach (RandomVariable<Domain> v in variables)
            {
                if (neighborhood.Count > size)
                {
                    yield return neighborhood;
                    neighborhood.Clear();
                }
            }
            if (neighborhood.Count > 0)
                yield return neighborhood;
        }
        public override IEnumerable<RandomVariable<Domain>> GetAllVariables()
        {
            foreach (RandomVariable<Domain> v in variables)
                yield return v;
        }
        public void Run(int numPasses)
        {
            Solver<RandomVariable<Domain>, Domain, BasicScore<double>>.DepthFirstSearch dfs = new Solver<RandomVariable<Domain>, Domain, BasicScore<double>>.DepthFirstSearch();
            dfs.Run(this, variables, domainSize: new UniformDistribution_Int(new Range<int>(numPasses, numPasses)));
        }
    }

    public class SPSA
    {
        public class Variable
        {
            public double value;
            protected Range<double> range;
            public Variable(double value, Range<double> range)
            {
                this.value = value;
                this.range = range;
            }
            public bool Equals(Variable other) { return Util.Equals(this.value, other.value); }
            public bool Equals(double other) { return Util.Equals(this.value, other); }
            public bool Equals(int other) { return Util.Equals(this.value, other); }
            public static implicit operator double(Variable v)
            {
                return v.value;
            }
            public static implicit operator int(Variable v)
            {
                return (int)Math.Round(v.value);
            }
            public void Perturb(double delta)
            {
                value += delta;
                value = Math.Max(range.min, value);
                value = Math.Min(range.max, value);
                //                if (!(value >= range.min && value <= range.max))
                //                    throw new Exception("out of range");
            }
            public double GetRange()
            {
                return range.max - range.min;
            }
            public override string ToString()
            {
                return value.ToString();
            }
        }
        class UnitTest
        {
            Variable x = new Variable(0, new Range<double>(-10, 10));
            Variable y = new Variable(0, new Range<double>(-10, 10));
            public void Run()
            {
                SPSA optimizer = new SPSA(GetScore);
                optimizer.AddVariable(x);
                optimizer.AddVariable(y);
                optimizer.Run(100000);
                double bestScore = GetScore();
                if (!Util.Equals(x, 2.0) || !Util.Equals(y, -1.0))
                    throw new Exception("Solution not found");
            }
            double GetScore()
            {
                return (2.0 * Math.Pow(x, 2.0)) + (2.0 * x * y) + (2.0 * Math.Pow(y, 2.0)) - (6.0 * x);
            }
        }
        static public void Test()
        {
            UnitTest unitTest = new UnitTest();
            unitTest.Run();
        }

        List<Variable> variables = new List<Variable>();
        Func<double> getScore;
        //Random rand = new Random();

        public SPSA(Func<double> getScore, int numIterations = 100, double alpha = 0.602, double gamma = 0.101, double a = 0.1, double A = 0.1, double c = 0.1)
        {
            this.getScore = getScore;
        }
        public void AddVariable(Variable v)
        {
            variables.Add(v);
        }
        int GetBernoulliSample()
        {
            return (int)(2 * Math.Round(Util.rand.NextDouble())) - 1;
        }
        public void Run(int numIterations = 100, double alpha = 0.602, double gamma = 0.101, double a = 0.1, double A = 0.1, double c = 0.1)
        {
            double[] delta = new double[this.variables.Count];
            Variable[] variables = this.variables.ToArray();
            Queue<double> gradients = new Queue<double>();

            for (int iteration = 0; iteration < numIterations; iteration++)
            {
                double ak = a / Math.Pow(A + iteration, alpha);
                double ck = c / Math.Pow(iteration + 1, gamma);

                {
                    for (int i = 0; i < variables.Count(); i++)
                        delta[i] = GetBernoulliSample();
                }
                {
                    for (int i = 0; i < variables.Count(); i++)
                        variables[i].value += (ck * delta[i]);
                }
                Variable[] variablesPlus = variables.ToArray();
                double yplus = getScore();
                {
                    for (int i = 0; i < variables.Count(); i++)
                        variables[i].value -= (ck * delta[i]);
                }
                Variable[] variablesMinus = variables.ToArray();
                double yminus = getScore();
                double ydiff = yplus - yminus;
                {
                    for (int i = 0; i < variables.Count(); i++)
                    {
                        if (delta[i] != 0)
                        {
//                            double scale = (variables[i].GetRange() / numIterations) * .001;
                            double gradient = (ydiff / (2.0 * ck * delta[i]));
                            variables[i].value -= ak * (variables[i].GetRange() / 10.0) * gradient;
                            //gradients.Enqueue(gradient);
                        }
                    }
                }
            }
        }
    }
}



