/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *      
 * Various References on the VRP problem:  
 *      http://cepac.cheme.cmu.edu/pasi2011/library/cerda/braysy-gendreau-vrp-review.pdf
 *      https://www.cirrelt.ca/DocumentsTravail/CIRRELT-2010-04.pdf
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SolverForDummies;

namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {
        Score score;

        public string problemName, problemComment;
        int featureID_Instance_Name = Instrumentation.AddFeature("Instance.Name");
        int featureID_Instance_OptimalScore_Combined = Instrumentation.AddFeature("Instance.OptimalScore.Combined");
        int featureID_CurrentSolution = Instrumentation.AddFeature("Solution");
        public int numVehiclesUsed { get { return depots.Count(d => d.next != null); } }
        public int bestKnown_NumVehiclesUsed;
        public Distance distanceTravelled { get { return score.distance; } }
        public double optimalityOfSolution { get { return (bestKnown_DistanceTravelled.TotalKilometers / distanceTravelled.TotalKilometers) * 100; } }
        public Distance bestKnown_DistanceTravelled;
        public double unMetDemand { get { return score.unmetDemand; } }
        List<Visit> depots =  new List<Visit>();
        Delivery[] deliveries; // we don't want to be ableto sort this.
        Dictionary<int, Visit> visitsByID = new Dictionary<int, Visit>();
        List<Delivery> sortedDeliveries; // we don't want to be ableto sort this.
        TimeSpan maxRunTime = TimeSpan.MaxValue;

        // constriants
        double vehicleCapacity;

        #region Optimization tuning parameters

        public class Options
        {
            public bool instrumentationEnabled = false;
            public bool includeUnassignedInDomain = false;

            public double learning_neighborPromotionRate = 0;  // for now keep this at zero.  Need to debug this more to see if it can be useful
            public double learning_RateMultiplier_ForNeighborhoodBest = 1;
            public double learning_RateMultiplier_ForLocalBest = 1;
            public double learning_RateMultiplier_ForGlobalBest = 1;

            public int numIterations = 100;
            public int numRelaxationPasses = 0;

            public Range<int> neighborhoodSize = new Range<int>(2, 10);
            public int maxNeighborhoodEvaluations = 4000;

            public Range<int> localSearch_Passes_ForRelaxation = new Range<int>(5, 5);
            public Range<int> localSearch_Passes = new Range<int>(5, 10);

            public Distribution<int> generateInitialSolution_NeighborhoodSize = new UniformDistribution_Int(1, 1);
            public Range<int> generateInitialSolution_LocalSearch_Passes = new Range<int>(5, 5);

            public double relaxation_ScoreTerms_Variance = .86;
            public double scoreSpikePenalty = 2.0;
            public double weightLateness = 50.0;
            public double weightWaiting = .01;
            public double weightDistance = 1.0;
            public double weightUnmetDemand = 200;
            public double weightOverCapacity = 135;

            public double perturbationSize_MeanPercent =  0.50;
            public double perturbationSize_Variance = 2;
            public double percentNoisePerConsecutiveStuckNotifications = .01;
            public double perturbationNoisePerTemperature = .2;


            public double maxOverCapacityPercentConstraint = 1.3;
            public Time maxVehicleLateConstriant = new Time(60);

            public int num_AdditionalThreads = 6;

            public void Log()
            {
                Logger.Log("Optimization parameters: " + 
                    "NumIterations = " + numIterations +
                    ", NumAdditionalThreads = " + num_AdditionalThreads +
                    ", NumRelaxationPasses = " + numRelaxationPasses +
                    ", NeighborhoodSize = " + neighborhoodSize + 
                    ", MaxNeighborhoodEvaluations = " + maxNeighborhoodEvaluations + 
                    ", PerturbationSize Mean = " + (perturbationSize_MeanPercent * 100) + "% Variance = " + perturbationSize_Variance +
                    ", NoisePerConsecutiveStuckNotifications = " + (percentNoisePerConsecutiveStuckNotifications * 100) + "%" + 
                    ", IncludeUnassignedInDomain = " + includeUnassignedInDomain + 
                    ", LocalSearch_Passes = " + localSearch_Passes +
                    ", LocalSearch_Passes_ForRelaxation = " + localSearch_Passes_ForRelaxation +
                    ", Relaxation_ScoreTerms_Variance = " + relaxation_ScoreTerms_Variance +
                    ", GenerateInitialSolution_NeighborhoodSize = " + generateInitialSolution_NeighborhoodSize +
                    ", GenerateInitialSolution_LocalSearchPasses = " + generateInitialSolution_LocalSearch_Passes +
                    ", Weights: Lateness = " + weightLateness + ", Waiting = " + weightWaiting + ", Distance = " + weightDistance + ", OverCapacity = " + weightOverCapacity +
                    ", NumThreads = " + num_AdditionalThreads
                    );
                if (learning_neighborPromotionRate > 0)
                    Logger.Log("DomainLearning Enabled: Learning Rate = " + (learning_neighborPromotionRate * 100) + "%" + ", NeighborhoodBestMultiplier = " + learning_RateMultiplier_ForNeighborhoodBest + ", LocalBestMultiplier = " + learning_RateMultiplier_ForLocalBest + ", GlobalBestMultiplier = " + learning_RateMultiplier_ForGlobalBest, 1);
            }
        }

        #endregion
        VRPSolver.Problem_Loader problemLoader;
        Options options = new Options();
        public Thread thread;
        public VRPSolver(string file, Problem_Loader.Type type, Options tuner = null) : this(new VRPSolver.Problem_Loader(file, type), tuner)
        {

        }
        protected VRPSolver(Problem_Loader loader, Options options = null)
        {
            if (options != null)
                this.options = options;
            problemLoader = loader;
            maxRunTime = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 10);
            score = new Score(this, new Instrumentation.FeatureInstanceProperties("Current.Score"));
            score.Initialize();
            Initialize_FromLoader(loader);
        }

        public string GetProblemLabel()
        {
            return "Problem: Name = " + problemName + ", Comment " + problemComment + ", Min Vehicles = " + bestKnown_NumVehiclesUsed + ", Min Distance = " + bestKnown_DistanceTravelled + ", Total demand = " + deliveries.Sum(d => d.customer.demand.requested) + ", No. Vehicles Used = " + numVehiclesUsed + ", Score = " + score + ", Optimality = " + (optimalityOfSolution);
        }

        private Time GetTripDuration(Point a, Point b)
        {
            return new Time(GetTripDistance(a, b).TotalKilometers);
        }

        private Distance GetTripDistance(Point a, Point b)
        {
            return Distance.FromKilometers(System.Math.Sqrt(
                                System.Math.Pow(a.X - b.X, 2) +
                                System.Math.Pow(a.Y - b.Y, 2)));
        }
        public double NormalizeUnmetDemand(double unmetDemand)
        {
            return unmetDemand * options.weightUnmetDemand;
        }
        public double NormalizeOverCapacity(double overCapacity)
        {
            return overCapacity * options.weightOverCapacity;
        }
        public double NormalizeLateness(Time lateness)
        {
            return lateness.TotalMinutes * options.weightLateness;
        }
        public double NormalizeWaiting(Time waiting)
        {
            return waiting.TotalMinutes * options.weightWaiting;
        }
        public double NormalizeDistance(Distance distance)
        {
            return distance.TotalKilometers * options.weightDistance;
        }
        public double GetUnmetDemandScore(double unmetDemand)
        {
            return Math.Abs(Math.Pow(NormalizeUnmetDemand(unmetDemand), options.scoreSpikePenalty));
        }
        public double GetOverCapacityScore(double overCapacity)
        {
            return Math.Abs(Math.Pow(NormalizeOverCapacity(overCapacity), options.scoreSpikePenalty));
        }
        public double GetLatenessScore(Time lateness)
        {
            return Math.Abs(Math.Pow(NormalizeLateness(lateness), options.scoreSpikePenalty));
        }
        public double GetWaitingScore(Time waiting)
        {
            return Math.Abs(Math.Pow(NormalizeWaiting(waiting), options.scoreSpikePenalty));
        }
        public double GetDistanceScore(Distance distance)
        {
            return Math.Abs(Math.Pow(NormalizeDistance(distance), options.scoreSpikePenalty));
        }


        private double GetNeighborCloseness(Visit neighbor, Delivery customer, bool activeOnly)
        {
            double cost = MinEdgeCost(neighbor, customer);
            if (neighbor.vehicle == null && activeOnly)
                cost += forceAtEndCost;
            return cost;
        }

        private double MinEdgeCost(Visit neighbor, Delivery customer)
        {
            Time tripDuration = GetTripDuration(neighbor.location, customer.location);
            Time minWaiting = customer.GetWaiting(neighbor.latestDeparture + tripDuration);
            Time minLateness = customer.GetLateness(neighbor.earliestDeparture + tripDuration);
            Distance distance = GetTripDistance(neighbor.location, customer.location);
            return Math.Max(Math.Max(NormalizeWaiting(minWaiting), NormalizeDistance(distance)), NormalizeLateness(minLateness));
        }

    }
}
