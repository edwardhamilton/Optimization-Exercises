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
        public struct Demand
        {
            public Product product;
            public double requested;
            public double delivered;
            public double unmet { get { return requested - delivered; } }
            public Time readyTime;
            public Time dueTime;
            public bool OnTime(Time arrival) { return arrival.CompareTo(dueTime) <= 0; }
            public override string ToString()
            {
                return "Requested = " + requested + ", Delivered = " + delivered + ", Ready = " + readyTime + ", Due = " + dueTime;
            }

        }
        public class Customer
        {
            public Visit[] neighbors; // this needs to be sorted by distance
            public VRPSolver parent;
            public Location location;
            public int id;
            public Demand demand;
            public Delivery delivery;
            public Customer(VRPSolver parent, int id, Point location, Product product, double demand, Time readyTime, Time dueTime)
            {
                this.parent = parent;
                this.id = id;
                this.demand.requested = demand;
                this.demand.product = product;
                this.demand.readyTime = readyTime;
                this.demand.dueTime = dueTime;
                parent.score.unmetDemand += this.demand.unmet;
                this.location = new Location(location, this);
                delivery = new Delivery(this.location, id);
            }
            public void PromoteNeighbor(int index)
            {
                if (index == 0)
                    return;
                int velocity = -1;
                Visit temp = neighbors[index + velocity];
                neighbors[index + velocity] = neighbors[index];
                neighbors[index] = temp;

            }
            private string GetDeliveryString()
            {
                if (delivery.vehicle == null)
                    return "";
                string str = ", (" + (demand.OnTime((Time) delivery.arrival) ? "Ontime" : "Late") + ") Arrival = " + delivery.arrival + ", Vehicle = " + delivery.vehicle.id; 
                if (delivery.previous.vehicle != null)
                    str += ", Prev = " + (delivery.previous.location.customer != null ? delivery.previous.location.customer.id.ToString() : "D");
                if (delivery.next != null && delivery.next.vehicle != null)
                    str += ", Next = " + delivery.next.location.customer.id.ToString();
                return str;
            }
            private string GetNeighborsString()
            {
                string str = "";
                int i = 0;
                foreach (Visit neighbor in neighbors)
                {
                    if (str.Length > 0)
                        str += ", ";
                    str += "(" + (neighbor.IsDepot() ? ("D" + neighbor.vehicle.id) : neighbor.location.customer.id.ToString()) + ", " + parent.GetNeighborCloseness(neighbors[i], delivery, false) + ", " + parent.GetTripDistance(neighbors[i].location, delivery.location) + ")";
                    i++;
                }
                return str;
            }
            public override string ToString()
            {
                return "ID = " + id + ", Location = " + location + ", Demand = " + demand + GetDeliveryString() + ", EarliestDeparture = " + delivery.earliestDeparture + ", LatestDeparture = " + delivery.latestDeparture + ", Neighbors = " + GetNeighborsString();
            }
            static public string InstrumentationHeader()
            {
                return "ID, LocationX, LocationY, Product, Requested, Delivered, ReadyTime,  DueTime, ServiceTime, Lateness, Waiting, Neighbors";
            }
            public string ToInstrumentationString()
            {
                string neighbors = "";
                foreach (Visit neighbor in this.neighbors)
                {
                    if (neighbor.location.customer != null)
                    {
                        if (neighbors.Length > 0)
                            neighbors += " ";
                        neighbors += neighbor.location.customer.id;
                    }
                }

                return id + "," + location.X + "," + location.Y + "," + demand.product.id + "," + demand.requested + "," + demand.delivered + "," + demand.readyTime.TotalMinutes + "," + demand.dueTime.TotalMinutes + "," + demand.product.serviceTime.TotalMinutes + "," + delivery.GetLateness().TotalMinutes + "," + delivery.GetWaiting().TotalMinutes + "," + neighbors;
            }
            public Visit GetRandomNeighbor()
            {
                return neighbors[Util.rand.Next(neighbors.Length)];
            }


            public void AssignDelivery(Vehicle vehicle)
            {
                parent.score.unmetDemand -= demand.unmet;
                demand.delivered += (double)demand.requested;
                parent.score.unmetDemand += demand.unmet;
                if (demand.delivered != demand.requested)
                    throw new Exception("something wrong");
            }
            public void UnassignDelivery()
            {
                parent.score.unmetDemand -= demand.unmet;
                demand.delivered -= (double)demand.requested;
                parent.score.unmetDemand += demand.unmet;
                if (demand.delivered != 0)
                    throw new Exception("something wrong");
            }
            public bool NeighborRoute_Has_CustomerProduct(Visit neighbor)
            {
                return neighbor.vehicle != null && neighbor.vehicle.package.product == demand.product;
            }

        }


    }
}