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
        static double forceAtEndCost = 60 * 24 * 7;


        private void InitializeNeighborsCollections()
        {
            List<Visit> sortedNeighbors = new List<Visit>(deliveries);
            sortedNeighbors.AddRange(depots);
            foreach (Delivery delivery in deliveries)
            {
                {
                    sortedNeighbors.Sort(delegate(Visit x, Visit y)
                    {
                        return GetNeighborCloseness(x, delivery, activeOnly: false).CompareTo(GetNeighborCloseness(y, delivery, activeOnly: false));
                    });

                    List<Visit> neighbors = sortedNeighbors.ToList();
                    neighbors.Remove(delivery);
                    neighbors.RemoveAll(n => delivery.GetLateness(n.earliestDeparture + GetTripDuration(n.location, delivery.location)).CompareTo(Time.Zero) > 0);
                    delivery.customer.neighbors = neighbors.ToArray();
                }

            }
            sortedDeliveries = new List<Delivery>(deliveries);
//            LogCustomers(logNeighbors: true);

            bestScore = score;

        }

        private void LoadVehicles(Problem_Loader loader, int visitID_Start)
        {
            vehicleCapacity = loader.capacity;
            int numVehicles;
            if (loader.numVehicles == null)
                numVehicles = (int)Math.Ceiling((loader.demand.Sum() / vehicleCapacity) * 1.5);
            else
                numVehicles = (int) loader.numVehicles;
            for (int id = 0; id < (int)numVehicles; id++)
            {
                Visit depot = new Visit(new Location(loader.depot), visitID_Start + id);
                depot.vehicle = new Vehicle(this, id, depot, loader.defaultProduct, vehicleCapacity, loader.Get_ReadyTime(0), loader.Get_DueTime(0));
                depots.Add(depot);
            }
        }

        private void LoadCustomers(Problem_Loader loader)
        {
            List<Delivery> deliveries = new List<Delivery>();
            int index = 0;
            foreach (Point p in loader.points)
            {
                Customer c = new Customer(this, index, p, loader.defaultProduct, loader.demand[index], loader.Get_ReadyTime(index),
                    loader.Get_DueTime(index));
                if (loader.demand[index] > 0)
                    deliveries.Add(c.delivery);
                index++;
            }
            this.deliveries = deliveries.ToArray();
        }

        public void Initialize_FromLoader(Problem_Loader loader)
        {
            LoadCustomers(loader);
            LoadVehicles(loader, deliveries.Count() + 1);
            InitializeNeighborsCollections();
            problemName = loader.name;
            problemComment = loader.comment;
            if (loader.minVehicles != null)
                bestKnown_NumVehiclesUsed = (int) loader.minVehicles;
            if (loader.optimalValue != null)
                bestKnown_DistanceTravelled = (Distance) loader.optimalValue;

            foreach (Visit v in deliveries)
                visitsByID.Add(v.id, v);
            foreach (Visit v in depots)
                visitsByID.Add(v.id, v);
        }

    }
}
