/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *
 ******************************************************************************/

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using SolverForDummies;

namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {
        public class Product
        {
            public int id { get { return 1; } }
            public Time serviceTime = Time.Zero;
            public double capacityPerUnit = 1;
            public Product(Time serviceTime)
            {
                this.serviceTime = serviceTime;
            }
        }
        public struct Package
        {
            public Product product;
            public double units;
        }

        public struct TravelCost
        {
            //public double duration;
            public Distance distance;
        }

        public class Vehicle
        {
            public int id;
            public TravelCost travelCost;
            public VRPSolver parent;
            public Visit depot;
            public Time dueBackTime = Time.MaxValue;
            public Time readyTime = Time.Zero;
            public Package package;
            public double capacity; // is only needed for initialization.  Afterwards, package.units serves the purpose.
            public Time maxWait = new Time(20);

            public Vehicle(VRPSolver parent, int id, Visit depot, Product product, double capacity, Time readyTime, Time dueBackTime)
            {
                this.parent = parent;
                this.id = id;
                this.depot = depot;
                this.package.units = this.capacity = capacity;
                this.package.product = product;
                this.travelCost.distance = Distance.Zero;
                this.dueBackTime = dueBackTime;
                this.readyTime = readyTime;
            }
            public struct Edge
            {
                public Visit from;
                public Visit to;
            }

            public IEnumerable<Edge> GetRoute()
            {
                Visit v = depot;
                Edge edge;
                edge.from = depot;
                while (v.next != null)
                {
                    edge.to = v.next;
                    yield return edge;
                    edge.from = edge.to;
                    v = v.next;
                }
                edge.to = depot;
                yield return edge;
            }


            public override string ToString()
            {
                Visit v = depot;
                string route = "";
                int stops = 0;
                for (; v.next != null && stops < 100; v = v.next, stops++)
                    route += " " + v.next.location.customer.id;
                return "Route " + id + ": " + route;
            }
            static public string InstrumentationHeader()
            {
                return "ID, Product, Units, ReadyTime,  DueBackTime, OverCapacity";
            }
            public string ToInstrumentationString()
            {

                return id + ", " + package.product.id + ", " + package.units + ", " + readyTime.TotalMinutes + ", " + dueBackTime.TotalMinutes;
            }

            public string GetRoute_ForInstrumentation_Save()
            {
                Visit v = depot;
                string route = "";
                int stops = 0;
                for (; v.next != null && stops < 100; v = v.next, stops++)
                    route += " " + v.next.location.GetCoordinatesString_ForInstrumentation();
                return depot.location.GetCoordinatesString_ForInstrumentation() + route + " " + depot.location.GetCoordinatesString_ForInstrumentation();
            }

            public string GetRoute_ForInstrumentation()
            {
                Visit v = depot;
                string route = "";
                int stops = 0;
                for (; v.next != null && stops < 100; v = v.next, stops++)
                {
                    route += " " + v.next.location.customer.id + "/" + ((int)v.next.arrival.Value.TotalMinutes);
                    if (v.next.inNeighborhood)
                        route += "*";
                    else if (v.next.boundInNeighborhood)
                        route += "^";
                }
                return depot.id + "/0" + route + " " + depot.id + "/" + ((int) depot.arrival.Value.TotalMinutes);  //  Notice we use depot.id instead of depot.vehicle.id.  This is because ids are unique across depots and customers.
            }

            public void UnAssignDelivery(Delivery delivery)
            {
                parent.score.overCapacity -= Math.Max(0, -this.package.units);
                this.package.units += delivery.customer.demand.requested;
                parent.score.overCapacity += Math.Max(0, -this.package.units);

                Update_TheChangeInDistanceTravelled_ThatResultsFrom_DeletingThisDelivery(delivery);

            }

            public void AssignDelivery(Delivery delivery)
            {
                parent.score.overCapacity -= Math.Max(0, -this.package.units);
                this.package.units -= delivery.customer.demand.requested;
                parent.score.overCapacity += Math.Max(0, -this.package.units);
                Update_TheChangeInDistanceTravelled_ThatResultsFrom_AddingThisDelivery(delivery);

            }


            private void Update_TheChangeInDistanceTravelled_ThatResultsFrom_DeletingThisDelivery(Delivery delivery)
            {
                Remove_DistanceTravelled_From_PrevDelivery_To_ThisDelivery(delivery);
                if (!IsLastDelivery(delivery))
                {
                    Remove_DistanceTravelled_From_ThisDelivery_To_NextDelivery(delivery);
                    Add_DistanceTravelled_From_PrevDelivery_To_NextDelivery(delivery);
                }
                else
                {
                    Remove_DistanceTravelled_From_ThisDelivery_To_Depot(delivery);
                    Add_DistanceTravelled_From_PrevDelivery_To_Depot(delivery);
                }
            }
            private void AddDistanceTravelled(Distance amount)
            {
                parent.score.distance += amount;
                travelCost.distance += amount;
            }
            private void RemoveDistanceTravelled(Distance amount)
            {
                parent.score.distance -= amount;
                travelCost.distance -= amount;
            }

            private void Add_DistanceTravelled_From_PrevDelivery_To_Depot(Delivery delivery)
            {
                AddDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.vehicle.depot.location));
            }

            private void Remove_DistanceTravelled_From_ThisDelivery_To_Depot(Delivery delivery)
            {
                RemoveDistanceTravelled(parent.GetTripDistance(delivery.location, delivery.vehicle.depot.location));
            }

            private void Add_DistanceTravelled_From_PrevDelivery_To_NextDelivery(Delivery delivery)
            {
                AddDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.next.location));
            }

            private void Remove_DistanceTravelled_From_ThisDelivery_To_NextDelivery(Delivery delivery)
            {
                RemoveDistanceTravelled(parent.GetTripDistance(delivery.location, delivery.next.location));
            }

            private void Remove_DistanceTravelled_From_PrevDelivery_To_ThisDelivery(Delivery delivery)
            {
                RemoveDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.location));
            }

            private static bool IsLastDelivery(Visit delivery)
            {
                return delivery.next == null;
            }

            private void Update_TheChangeInDistanceTravelled_ThatResultsFrom_AddingThisDelivery(Visit delivery)
            {
                if (IsLastDelivery(delivery))
                {
                    Remove_DistanceTravelled_From_PrevDelivery_To_Depot(delivery);
                    Add_DistanceTravelled_From_ThisDelivery_To_Depot(delivery);
                }
                else
                {
                    Remove_DistanceTravelled_From_PrevDelivery_To_NextDelivery(delivery);
                    Add_DistanceTravelled_From_ThisDelivery_To_NextDelivery(delivery);
                }
                Add_DistanceTravelled_From_PrevDelivery_To_ThisDelivery(delivery);
            }
            private void Remove_DistanceTravelled_From_PrevDelivery_To_NextDelivery(Visit delivery)
            {
                RemoveDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.next.location));
            }

            private void Remove_DistanceTravelled_From_PrevDelivery_To_Depot(Visit delivery)
            {
                RemoveDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.vehicle.depot.location));
            }

            private void Add_DistanceTravelled_From_ThisDelivery_To_NextDelivery(Visit delivery)
            {
                AddDistanceTravelled(parent.GetTripDistance(delivery.location, delivery.next.location));
            }

            private void Add_DistanceTravelled_From_ThisDelivery_To_Depot(Visit delivery)
            {
                AddDistanceTravelled(parent.GetTripDistance(delivery.location, delivery.vehicle.depot.location));
            }


            private void Add_DistanceTravelled_From_PrevDelivery_To_ThisDelivery(Visit delivery)
            {
                AddDistanceTravelled(parent.GetTripDistance(delivery.previous.location, delivery.location));
            }

        }
    }
}
