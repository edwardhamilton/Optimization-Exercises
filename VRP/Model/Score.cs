/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *
 ******************************************************************************/

using System;
using System.Text;


using SolverForDummies;
namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {

        public class Score : ScoreBase<Score>
        {

            int featureID_OverCapacity = -1;
            int featureID_Distance = -1;
            int featureID_Lateness = -1;
            int featureID_Waiting = -1;
            int featureID_UnmetDemand = -1;
            int featureID_Combined = -1;

            public Score(VRPSolver parent, Instrumentation.FeatureInstanceProperties? featureInstanceProperties)
            {
                this.parent = parent;
                if (featureInstanceProperties != null)
                {
                    Instrumentation.FeatureInstanceProperties instance = (Instrumentation.FeatureInstanceProperties)featureInstanceProperties;
                    featureID_OverCapacity = Instrumentation.AddFeature(instance.name + ".OverCapacity", instance.applyBackwards);
                    featureID_Distance = Instrumentation.AddFeature(instance.name + ".Distance", instance.applyBackwards);
                    featureID_Lateness = Instrumentation.AddFeature(instance.name + ".Lateness", instance.applyBackwards);
                    featureID_Waiting = Instrumentation.AddFeature(instance.name + ".Waiting", instance.applyBackwards);
                    featureID_UnmetDemand = Instrumentation.AddFeature(instance.name + ".UnmetDemand", instance.applyBackwards);
                    featureID_Combined = Instrumentation.AddFeature(instance.name + ".Combined", instance.applyBackwards);
                }
            }
            public double overCapacity = double.MaxValue;
            public double overCapacityScore { get { return Math.Pow(overCapacity, 2); } }
            public double unmetDemand = double.MaxValue;
            public double unmetDemandScore { get { return Math.Pow(unmetDemand, 2); } }
            public void Bind_LatenessWaiting(Time lateness, Time waiting)
            {
                this.lateness += lateness;
                this.waiting += waiting;
            }
            public void Unbind_LatenessWaiting(Time lateness, Time waiting)
            {
                this.lateness -= lateness;
                this.waiting -= waiting;
            }
            public VRPSolver parent;
            public Time lateness, waiting;

            public Distance distance;
            public void Initialize(double overCapacity = 0, double unmetDemand = 0, Distance? distance = null, Time? lateness = null, Time? waiting = null)
            {
                this.overCapacity = overCapacity;
                this.unmetDemand = unmetDemand;
                this.distance = (distance == null) ? Distance.Zero : (Distance) distance;
                this.lateness = (lateness == null) ? Time.Zero : (Time)lateness;
                this.waiting = (waiting == null) ? Time.Zero : (Time)waiting;

            }
            public override void Copy(Score s)
            {
                overCapacity = s.overCapacity;
                unmetDemand = s.unmetDemand;
                distance = s.distance;
                lateness = s.lateness;
                waiting = s.waiting;
                TakeMeasurement();
            }
            public override string ToString()
            {
                return "OverCapacity = " + overCapacity + ", UnmetDemand = " + unmetDemand + ", Lateness = " + lateness + ", Waiting = " + waiting + ", Distance = " + distance;
            }

            public double GetClosenessMeasure()
            {
                return parent.NormalizeLateness(lateness) + parent.NormalizeWaiting(waiting) + parent.NormalizeDistance(distance);
            }

            public double GetCombinedScore()
            {
                if (unmetDemand == double.MaxValue)
                    return double.MaxValue;
                double spikePenalty = parent.options.scoreSpikePenalty;
                return parent.GetUnmetDemandScore(unmetDemand) +
                    parent.GetOverCapacityScore(overCapacity) +
                    parent.GetLatenessScore(lateness) +
                    parent.GetWaitingScore(waiting) +
                    parent.GetDistanceScore(distance);
            }
            private void TakeMeasurement()
            {
                Instrumentation.SetFeature(featureID_OverCapacity, overCapacity);
                Instrumentation.SetFeature(featureID_Distance, distance.TotalKilometers);
                Instrumentation.SetFeature(featureID_UnmetDemand, unmetDemand);
                Instrumentation.SetFeature(featureID_Lateness, lateness.TotalMinutes);
                Instrumentation.SetFeature(featureID_Waiting, waiting.TotalMinutes);
                Instrumentation.SetFeature(featureID_Combined, GetCombinedScore());
            }

            public bool IsBetterThan(Score o)
            {
                if (o == null)
                    return true;
                TakeMeasurement();
                int result;
//                int result = unmetDemandScore.CompareTo(o.unmetDemandScore);
//                if (Math.Abs(unmetDemandScore - o.unmetDemandScore) < .01)
                {
                    double combinedScore = GetCombinedScore();
                    double oCombinedScore = o.GetCombinedScore();
                    if (Math.Abs(combinedScore - oCombinedScore) < .0001)
                        result = 0;
                    else
                        result = GetCombinedScore().CompareTo(o.GetCombinedScore());
                }
                return result < 0;
            }

        }
    }
}
