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

using SolverForDummies;

namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {
        public void LogCurrentSolution(string label, bool logCustomers)
        {
            LogVehicles_And_ValidateSolution(label, false, 0);
            if (logCustomers)
                LogCustomers(logNeighbors: false);
        }
        public void LogVehicles_And_ValidateSolution(string label, bool logUnusedVehicles, int tab = 0)
        {
            if (score.distance.TotalKilometers == 0)
                return;
            Logger.Log(label + ": CurrentScore = " + score, tab);
            Logger.Log("Vehicles: Num Used = " + numVehiclesUsed, tab);
            Distance distance = Distance.Zero;
            Distance vehicleDistance = Distance.Zero;
            foreach (Visit vehicle in depots)
            {
                List<Visit> route = new List<Visit>();
                if (vehicle.next != null)
                {
                    Visit v = vehicle;
                    for (int i = 0; v.next != null && i < 100; v = v.next, i++)
                    {
                        if (route.Contains(v))
                            throw new Exception("cycle detected");
                        if (v.next.previous != v)
                            throw new Exception("bad linkage");
                        distance += GetTripDistance(v.location, v.next.location);
                        route.Add(v);
                    }
                    distance += GetTripDistance(v.location, v.vehicle.depot.location);
                    vehicleDistance += v.vehicle.travelCost.distance;
                    Logger.Log(vehicle.vehicle.ToString(), tab + 1);
                }
                else if (logUnusedVehicles)
                    Logger.Log(vehicle.vehicle.ToString(), tab + 1);
            }
            double unmetDemand = deliveries.Sum(d => d.customer.demand.unmet);
//            if (distanceTravelled != score.distanceTravelled)
//                throw new Exception("Invalid DistanceTravelled: Incremental " + score.distanceTravelled + " != Actual " + distanceTravelled);
//            if (vehicleDistanceTravelled != distanceTravelled)
//                throw new Exception("Invalid VehicleDistanceTravelled: Incremental " + vehicleDistanceTravelled + " != Actual " + distanceTravelled);
            if (unmetDemand != score.unmetDemand)
                throw new Exception("Invalid UnmetDemand: Incremental " + score.unmetDemand + " != Actual " + unmetDemand);
        }

        private void LogCustomers(bool logNeighbors)
        {
            logNeighbors = false;
            Logger.Log("Active Customers:  Total Demand = " + deliveries.Sum(d => d.customer.demand.requested));
            Time lateness = Time.Zero;
            foreach (Delivery d in deliveries)
            {
                lateness += d.GetLateness();
                Logger.Log(d.customer.ToString(), 1);
                if (logNeighbors)
                {
                    Logger.Log("Neighbors ", 2);
                    List<Visit> neighbors = new List<Visit>(d.customer.neighbors);
                    neighbors.Sort(delegate(Visit x, Visit y)
                    {
                        return GetNeighborCloseness(x, d, false).CompareTo(GetNeighborCloseness(y, d, false));
                    });
                    foreach (Visit neighbor in d.customer.neighbors)
                    {
                        if (neighbor.location.customer != null)
                            Logger.Log("Customer(" + GetNeighborCloseness(neighbor, d, false) + "): " + neighbor.location.customer.ToString() + "--> Distance = " + GetTripDistance(neighbor.location, d.location), 3);
                        else
                            Logger.Log("Depot(" + GetNeighborCloseness(neighbor, d, false) + "): " + neighbor.vehicle + "--> Distance = " + GetTripDistance(neighbor.location, d.location), 3);
                    }

                }
            }

            if (!lateness.Equals(score.lateness))
                throw new Exception("Incremental Score Error on lateness: incremental = " + score.lateness + ", Actual = " + lateness);
        }
        private bool HasArrived(Point current, Point to, Distance tolerance)
        {
            return GetTripDistance(current, to).CompareTo(tolerance) < 0;
        }
        public void LogSolutionMap()
        {
            Logger.Log("Text-based Graphical Map of Solution -----------------------------------------------------------------------------------------");
            int cellSizeX = 4;
            int cellSizeY = 2;
            int mapSizeX = (int)Math.Ceiling(deliveries.Max(d => d.location.X)) + 1;
            int mapSizeY = (int)Math.Ceiling(deliveries.Max(d => d.location.Y)) + 1;
            Visit[,] map = new Visit[mapSizeX, mapSizeY];
            foreach (Delivery d in deliveries)
                map[(int) d.location.X, (int) d.location.Y] = d;
            foreach (Visit d in depots)
                map[(int)d.location.X, (int)d.location.Y] = d;

            bool[,] route = new bool[mapSizeX * cellSizeX, mapSizeY * cellSizeY];
            Point velocity = new Point(0, 0);
            Distance closeEnough = new Distance(600);
            int numRoutePointsPlotted = 0;
            foreach (Visit d in depots)
            {
                foreach (Vehicle.Edge e in d.vehicle.GetRoute())
                {
                    velocity.X = (e.to.location.X - e.from.location.X) / 1000.0;
                    velocity.Y = (e.to.location.Y - e.from.location.Y) / 1000.0;
                    for (Point current = e.from.location; !HasArrived(current, e.to.location, closeEnough); current += velocity)
                    {
                        Point mapPoint = new Point(current.X * cellSizeX, current.Y * cellSizeY);
                        Point mapPointIndex = new Point(Math.Floor(current.X * cellSizeX), Math.Floor(current.Y * cellSizeY));
                        if (GetTripDistance(mapPoint, mapPointIndex).CompareTo(closeEnough) < 0)
                        {
                            if (!route[(int)mapPointIndex.X, (int)mapPointIndex.Y])
                            {
                                route[(int)mapPointIndex.X, (int)mapPointIndex.Y] = true;
                                numRoutePointsPlotted++;
                            }
                        }
                    }
                }
            }
            int displayX_Size = mapSizeX * cellSizeX;
            int displayY_Size = mapSizeY * cellSizeY;
            char[,] displayMap = new char[displayX_Size, displayY_Size];
            for (int y = 0; y < displayY_Size; y++)
            {
                for (int x = 0; x < displayX_Size; x++)
                    displayMap[x, y] = route[x, y] ? '.' : ' ';
            }
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    int displayX = x * cellSizeX;
                    int displayY = y * cellSizeY;
                    if (map[x, y] != null)
                    {
                        if (map[x, y].location.customer == null)
                            displayMap[displayX, displayY] = 'D';
                        else
                        {
                            string customerID = map[x, y].location.customer.id.ToString();
                            int i = 0;
                            foreach (char c in customerID)
                            {
                                displayMap[displayX + i, displayY] = c;
                                i++;
                            }
                        }
                    }

                }
            }
            for (int y = 0; y < displayY_Size; y++)
            {
                string line = "";
                for (int x = 0; x < displayX_Size; x++)
                    line += displayMap[x, y];
                Logger.Log(line);
            }
            Logger.Log("----------------------------------------------------------------------------------------------------------------------------");
        }


        private void Validate_Score()
        {
            Time lateness = Time.Zero;
            foreach (Delivery d in deliveries)
                lateness += d.GetLateness();
            if (!lateness.Equals(score.lateness))
            {
                foreach (Delivery d in deliveries)
                {
                    if (d.GetLateness().CompareTo(Time.Zero) > 0)
                        Logger.Log(d.ToString());
                }
                throw new Exception("Incremental Score Error on lateDelivery: incremental = " + score.lateness + ", Actual = " + lateness);
            }
            Distance distance = Distance.Zero;
            Distance vehicleDistance = Distance.Zero;
            foreach (Visit vehicle in depots)
            {
                if (vehicle.next != null)
                {
                    Visit v = vehicle;
                    for (int i = 0; v.next != null && i < 100; v = v.next, i++)
                        distance += GetTripDistance(v.location, v.next.location);
                    distance += GetTripDistance(v.location, v.vehicle.depot.location);
                    vehicleDistance += v.vehicle.travelCost.distance;
                }
            }
            double unmetDemand = deliveries.Sum(d => d.customer.demand.unmet);
            if (!distance.Equals(score.distance))
                throw new Exception("Invalid DistanceTravelled: Incremental " + score.distance + " != Actual " + distance);
            if (!vehicleDistance.Equals(score.distance))
                throw new Exception("Invalid VehicleDistanceTravelled: Incremental " + vehicleDistance + " != Actual " + distance);
            if (!unmetDemand.Equals(score.unmetDemand))
                throw new Exception("Invalid UnmetDemand: Incremental " + score.unmetDemand + " != Actual " + unmetDemand);



        }





    }
}
