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
using System.Threading;
using SolverForDummies;
using System.Diagnostics;
namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {
        private string GetNeighborhoodStr(IEnumerable<Delivery> neighborhood)
        {
            string str = "";
            foreach (Delivery variable in neighborhood)
            {
                if (str.Length > 0)
                    str += ", ";
                str += variable;
            }
            return str;
        }
        Score bestScore;
        int localSearch_AcceptedCount = 0;
        int localSearch_StuckCount;
        int iteratedLocalSearch_StuckCount;
        int iteratedLocalSearch_AcceptedCount;

        public void Run_SimulatedAnnealing_Algorithm()
        {
            GenerateInitialSolution();
            Solver.LocalSearch search = new Solver.SimulatedAnnealing(startingNeighborhoodSize: 1, varianceOnNeighborhoodSize: .5, passes: new Range<int>(300, 300));
            IEnumerable<Visit> unconvergedVariables = search.Run(this);
        }

        public void Run_UseLocalSearchAlgorithm(int minPasses = 15, int maxPasses = 15)
        {
            GenerateInitialSolution();
            Solver.LocalSearch search = new Solver.LocalSearch(maxNeighborhoodEvaluations: options.maxNeighborhoodEvaluations, passes: new Range<int>(minPasses, maxPasses));
            IEnumerable<Delivery> unconvergedVariables = search.Run(this);
        }

        Probability probability = new Probability();

        private double GetRandomRelaxationWeight(double weight)
        {
            double newWeight = Math.Max(0, probability.NormalRandomVariable(weight, options.relaxation_ScoreTerms_Variance));
            return newWeight;
        }

        public void Run()
        {
            Instrumentation.SetFeature(featureID_Instance_OptimalScore_Combined, GetDistanceScore(bestKnown_DistanceTravelled)); // All other terms are assumed zero.
            Instrumentation.SetFeature(featureID_Instance_Name, problemName);
            bestScore = CreateScore(new Instrumentation.FeatureInstanceProperties("Global.BestScore"));
            DateTime start = DateTime.Now;
            options.Log();
            List<VRPSolver> additionalSolvers = new List<VRPSolver>();
            for (int i = 0; i < options.num_AdditionalThreads; i++)
            {
                VRPSolver solver = new VRPSolver(problemLoader, options);
                additionalSolvers.Add(solver);
                Thread thread = new Thread(solver.Run_IteratedLocalSearch_Algorithm);
                solver.thread = thread;
                thread.Start();
            }
            Run_IteratedLocalSearch_Algorithm();
            do
            {
                Thread.Sleep(10);
            } while (!additionalSolvers.All(s => s.thread.ThreadState == System.Threading.ThreadState.Stopped));

            TakeBestSolution_OutOfAll_Solvers(additionalSolvers);

            TimeSpan elapse = DateTime.Now - start;
            string optimalityPerThread = optimalityOfSolution.ToString();
            foreach (VRPSolver s in additionalSolvers)
                optimalityPerThread += ", " + s.optimalityOfSolution;

            Logger.Log(GetProblemLabel());
            LogCurrentSolution("Result: Optimality (.vs Best Known) = " + optimalityOfSolution + " No Vehicles Used = " + numVehiclesUsed + " Results for each thread: " + optimalityPerThread + ", Runtime = " + elapse, false);
            Console.WriteLine(problemName + " Results for each thread: " + optimalityPerThread + ", Runtime = " + elapse);

            problemLoader.LoadSolution(this);
            Notify(Solver<Delivery, Visit, Score>.Problem.Notification.New_GlobalSolution_Accepted, deliveries);

            DumpInstrumentationToFile();
            //LogSolutionMap();
        }


        public void Run_IteratedLocalSearch_Algorithm()
        {
            initialSolutionGenerated = false;
            localSearch_StuckCount = 0;
            iteratedLocalSearch_StuckCount = 0;
            GenerateInitialSolution();
            initialSolutionGenerated = true;

            newBestCount = 0;

            Run_IteratedLocalSearch_With(new Solver.BackupPerturbation(this, neighborhoodSize: GetPerturbationSizeDistribution()), options.localSearch_Passes);

        }

        private void Run_IteratedLocalSearch_With(Solver.Perturbation perturbation, Range<int> localSearch_Passes)
        {
            Solver.IteratedLocalSearch search = new Solver.IteratedLocalSearch(localSearch: new Solver.LocalSearch(neighborhoodSize: new UniformDistribution_Int(options.neighborhoodSize),
                maxNeighborhoodEvaluations: options.maxNeighborhoodEvaluations, passes: localSearch_Passes));

            search.Run(this, perturbation: perturbation,
                numIterations: options.numIterations, neighborhoodSize: new UniformDistribution_Int(options.neighborhoodSize));
        }

        private void GenerateInitialSolution()
        {
            foreach (Delivery d in deliveries)
                ClearAssignment(d);
            Solver.LocalSearch localSearch = new Solver.LocalSearch(maxNeighborhoodEvaluations: options.maxNeighborhoodEvaluations, passes: options.generateInitialSolution_LocalSearch_Passes);
            localSearch.Run(this, neighborhoodSize: options.generateInitialSolution_NeighborhoodSize);
        }
        protected bool initialSolutionGenerated;
        static int newBestCount = 0;
        public override void Notify(Notification notification, IEnumerable<Delivery> variables)
        {
            SetCurrentSolution_ForInstrumentation(variables);

            if (IsBetterThan(bestScore))
            {
                bestScore.Copy(score);
                Instrumentation.MakeObservation_AtAccepted("Global.Accepted");
                newBestCount++;
            }

            switch (notification)
            {
                case Notification.New_LocalSolution_Accepted:
                    localSearch_StuckCount = 0;
                    localSearch_AcceptedCount++;
                    break;
                case Notification.New_GlobalSolution_Accepted:
                    iteratedLocalSearch_StuckCount = 0;
                    iteratedLocalSearch_AcceptedCount++;
                    break;
                case Notification.New_LocalSolution_Rejected:
                    localSearch_StuckCount++;
                    break;
                case Notification.New_GlobalSolution_Rejected:
                    iteratedLocalSearch_StuckCount++;
                    break;
                default:
                    break;
            }
        }

        private void SetCurrentSolution_ForInstrumentation(IEnumerable<Delivery> variables)
        {
            Instrumentation.SetFeature(featureID_CurrentSolution, GetSolution_ForInstrumentation(variables));
        }

        // Need to research new methods to get neighborhoods.   Maybe explore neighbors of neighbors, route based, etc..
        public override IEnumerable<IEnumerable<Delivery>> GetNeighborhoods(int size)
        {
            bool empty = true;
            List<Delivery> neighborhood = new List<Delivery>();
            Apply_Neighborhood_SortingSchedule();
            for (int i = 0; i < sortedDeliveries.Count; i++)
            {
                Delivery d = sortedDeliveries[i];
                if (d.isLocked)
                    continue;
                neighborhood.Add(d);
                Visit[] neighbors = d.customer.neighbors;

                int sampleSize = (int)Math.Min(neighbors.Count(), ((1 + (localSearch_StuckCount * options.percentNoisePerConsecutiveStuckNotifications)) * Math.Min(size, neighbors.Count())));
                foreach (int randomChoice in Util.Get_NonRepeating_RandomSequence(sampleSize))
                {
                    if (neighborhood.Count >= size)
                        break;
                    Visit neighbor = neighbors[randomChoice];
                    if (Is_Customer_WithAssignedRoute(neighbor))
                        neighborhood.Add(neighbor.location.customer.delivery);
                }
                if (neighborhood.Count > 0)
                {
                    empty = false;
                    yield return neighborhood;
                    neighborhood.Clear();
                }
            }
            if (empty)
                throw new Exception("something wrong");

        }

        public override IEnumerable<Visit> GetDomain(Delivery delivery, int maxDomainSize, double noise)
        {
            if (options.includeUnassignedInDomain)
                yield return null; // Its not clear that we need deletion in the domain
            int domainSize = 0;
            Visit[] neighbors = delivery.customer.neighbors;
            if (maxDomainSize > neighbors.Count())
                maxDomainSize = neighbors.Count();
            int sampleSize = Math.Min(neighbors.Count(), maxDomainSize + (int)Math.Ceiling(((neighbors.Count() - maxDomainSize)) * noise));
            IEnumerable<int> randomSequence = Util.Get_NonRepeating_RandomSequence(sampleSize);
            foreach (int randomChoice in randomSequence)
            {
                Visit neighbor = neighbors[randomChoice];
                if (neighbor.vehicle != null)
                {
                    yield return neighbor;
                    domainSize++;
                    if (domainSize >= maxDomainSize)
                        break;
                }
            }
        }

        public override void LockVariable(Delivery delivery, bool truefalse)
        {
            delivery.isLocked = truefalse;
        }
        public override bool IsValueConsistent(Delivery delivery, Visit visit)
        {
            if (delivery.isLocked || visit.vehicle == null)
                return false;
            if (visit == null)
                return true;
            bool vehicleHasRightProduct = visit.vehicle.package.product == delivery.customer.demand.product;
            bool vehicleCanSatisfyDemand = (visit.vehicle.package.units * options.maxOverCapacityPercentConstraint) >= delivery.customer.demand.requested;
            bool vehicleIsBackInTime = ((Time)visit.vehicle.depot.arrival).CompareTo(visit.vehicle.dueBackTime + options.maxVehicleLateConstriant) < 0;
            bool result = vehicleHasRightProduct && vehicleCanSatisfyDemand && vehicleIsBackInTime;
            return result;
        }

        public override void ClearAssignment(Delivery delivery)
        {
            delivery.Unassign();
        }
        public override bool UpdateAssignment(Delivery delivery, Visit visit)
        {
            delivery.Unassign();
            if (visit != null)
            {
                if (visit.vehicle == null)
                    visit.location.customer.delivery.waitingAssignment = delivery; // This is necessary to prevent issues with out of order updates during restore
                else
                    delivery.Assign(visit);
            }
            return true;
        }

        public override Visit GetAssignment(Delivery variable)
        {
            Delivery delivery = variable;
            if (delivery.vehicle != null)
                return delivery.previous;
            else
                return null;
        }
        public override Score CreateScore(Instrumentation.FeatureInstanceProperties? featureInstanceProperties)
        {
            return new Score(this, featureInstanceProperties);
        }
        public override void CopyScore(Score destination)
        {
            destination.Copy(score);
        }
        public override bool IsBetterThan(Score o)
        {
            return score.IsBetterThan(o);
        }

        public override IEnumerable<Delivery> GetAllVariables()
        {
            foreach (Delivery d in deliveries)
                yield return d;
        }

        public override void RestoreAssignments(Visit[] assignments, IEnumerable<Delivery> deliveries)
        {
            Validate_Score();
            ClearAssignments(deliveries); // This is necessary to prevent issues with out of order updates during restore
            base.RestoreAssignments(assignments, deliveries);
            Validate_Score();
        }


        private static bool Is_Customer_WithAssignedRoute(Visit neighbor)
        {
            return !neighbor.IsDepot() && neighbor.vehicle != null;
        }

        private void Apply_Neighborhood_SortingSchedule()
        {
            if (iteratedLocalSearch_AcceptedCount > 1)
                sortedDeliveries = Util.Randomize<Delivery>(deliveries, Util.rand);
            else
                SortDeliveriesByClosestNeighbor(activeFirst: true);
        }



        private void SortDeliveriesByClosestNeighbor(bool activeFirst)
        {
            sortedDeliveries.Sort(delegate(Delivery x, Delivery y)
            {
                Visit neighborX = x.customer.neighbors.First();
                Visit neighborY = y.customer.neighbors.First();
                double closestNeighborX = GetNeighborCloseness(neighborX, x, true);
                double closestNeighborY = GetNeighborCloseness(neighborY, y, true);
                return closestNeighborX.CompareTo(closestNeighborY);
            });
        }

        public override string GetSolution_ForInstrumentation(IEnumerable<Delivery> variables)
        {
            string solution = "";
            foreach (Visit d in variables)
            {
                d.inNeighborhood = true;
                if (d.previous != null)
                    d.previous.boundInNeighborhood = true;
            }

            foreach (Visit v in depots)
            {
                if (v.next != null)
                {
                    if (solution.Length > 0)
                        solution += "|";
                    solution += v.vehicle.GetRoute_ForInstrumentation();
                }
            }
            foreach (Visit d in variables)
            {
                d.inNeighborhood = false;
                if (d.previous != null)
                    d.previous.boundInNeighborhood = false;
            }
            return solution;
            
        }
        private Distribution<int> GetPerturbationSizeDistribution()
        {
            return new NormalDistribution_Int((double)deliveries.Count() * options.perturbationSize_MeanPercent, options.perturbationSize_Variance);
        }

        private void TakeBestSolution_OutOfAll_Solvers(List<VRPSolver> additionalSolvers)
        {
            foreach (VRPSolver s in additionalSolvers)
            {
                for (int i = 0; i < deliveries.Count(); i++)
                {
                    if (!s.deliveries[i].location.Equals(deliveries[i].location))
                        throw new Exception("something wrong");
                }
                if (s.score.IsBetterThan(score))
                {
                    ClearAssignments(GetAllVariables());
                    for (int i = 0; i < deliveries.Count(); i++)
                    {
                        if (s.GetAssignment(s.deliveries[i]) != null)
                            UpdateAssignment(deliveries[i], visitsByID[s.GetAssignment(s.deliveries[i]).id]);
                    }
                }
            }

        }


        [ConditionalAttribute("INSTRUMENTATION")]
        private void DumpInstrumentationToFile()
        {
            DumpVisitsSection();
            DumpVehiclesSection();
            FlushObservations();
        }

        [ConditionalAttribute("INSTRUMENTATION")]
        private static void FlushObservations()
        {
            Instrumentation.FlushToCsv("..\\Logs\\Observations.csv");
        }

        [ConditionalAttribute("INSTRUMENTATION")]
        private void DumpVehiclesSection()
        {
            List<string> vehicleSection = new List<string>();
            vehicleSection.Add(Vehicle.InstrumentationHeader());
            foreach (Visit d in depots)
                vehicleSection.Add(d.vehicle.ToInstrumentationString());
            Instrumentation.WriteSection("..\\Logs\\Vehicles.csv", vehicleSection);
        }

        [ConditionalAttribute("INSTRUMENTATION")]
        private void DumpVisitsSection()
        {
            List<string> visitsSection = new List<string>();
            visitsSection.Add(Customer.InstrumentationHeader());
            foreach (Delivery d in deliveries)
                visitsSection.Add(d.customer.ToInstrumentationString());
            foreach (Visit d in depots)
                visitsSection.Add(d.ToInstrumentationString());
            Instrumentation.WriteSection("..\\Logs\\Visits.csv", visitsSection);
        }


    }


}

