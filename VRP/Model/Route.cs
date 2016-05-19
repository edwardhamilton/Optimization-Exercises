/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;


using SolverForDummies;
namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {
        public class Point : IEquatable<Point>
        {
            public Point(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }
            public bool Equals(Point other) { return X == other.X && Y == other.Y; }
            public override string ToString()
            {
                return  X + "/" + Y;
            }
            public static Point operator +(Point a, Point b) { return new Point(a.X + b.X, a.Y + b.Y); }

            public double X { get; set; }
            public double Y { get; set; }
        }

        public class Location : Point
        {
            public Customer customer;
            public int id { get { return customer != null ? customer.id : -1; } }
            public Time readyTime { get { return customer != null ? customer.demand.readyTime : Time.Zero; } }
            public Time serviceTime { get { return customer != null ? customer.demand.product.serviceTime : Time.Zero; } }
            public Location(Point p, Customer customer)
                : base(p.X, p.Y)
            {
                this.customer = customer;
            }
            public Location(Point p)
                : base(p.X, p.Y)
            {
                this.customer = null;
            }

            public override string ToString()
            {
                return customer != null ? customer.id.ToString() : "D";
            }
            public string GetCoordinatesString_ForInstrumentation()
            {
                return base.ToString();
            }

        }
        public class Visit
        {
            public bool inNeighborhood = false;
            public bool boundInNeighborhood = false;
            public int id;  // This id should be indexed after customer id indices, as often we will use sets that combine both types.
            public Vehicle vehicle;
            public Visit previous, next;
            public Time? arrival;
            public Time? departure 
            { 
                get 
                {
                    if (IsDepot())
                        return vehicle.readyTime;
                    else if (arrival != null)
                        return Time.Max((Time) arrival, location.readyTime) + location.serviceTime;
                    else
                        return null;
                } 
            }
            public Time dueTime { get { return IsDepot() ? vehicle.dueBackTime : location.customer.demand.dueTime; } }
            public Time earliestDeparture
            {
                get
                {
                    if (IsDepot())
                        return vehicle.readyTime;
                    else
                        return location.readyTime + location.serviceTime;
                }
            }
            public Time latestDeparture
            {
                get
                {
                    if (IsDepot())
                        return vehicle.readyTime;
                    else
                        return location.customer.demand.dueTime + location.serviceTime;
                }
            }
            public Time GetLateness()
            {
                if (vehicle != null)
                    return GetLateness((Time)arrival);
                else
                    return Time.Zero;
            }
            public Time GetLateness(Time arrival)
            {
                return Time.Max(Time.Zero, ((Time)arrival) - dueTime);
            }

            public Time readyTime
            {
                get
                {
                    if (IsDepot())
                        return vehicle.readyTime;
                    else
                        return location.readyTime;
                }
            }


            public bool IsDepot()
            {
                return location.customer == null;
            }
            public Location location;
            public bool tagged; // temp field
            public Visit(Location location, int id)
            {
                this.id = id;
                previous = next = null;
                this.location = location;
                If_Depot_ThenItHasTriviallyArrived_SoInitialize_Arrival_To_Zero(location);
            }

            private void If_Depot_ThenItHasTriviallyArrived_SoInitialize_Arrival_To_Zero(Location location)
            {
                if (location.customer == null)
                    arrival = Time.Zero;
            }
            public override string ToString()
            {
                return _ToString();
            }
            private string _ToString()
            {
                return "Location = " + location + ", Prev = " + (previous == null ? "null" : previous.location.ToString()) + ", Vehicle = " + (vehicle != null ? vehicle.id.ToString() : "null") + ", Arrival = " + arrival + ", Departure = " + departure;
            }
            public string ToInstrumentationString()
            {
                return id + ", " + location.X + ", " + location.Y;
            }
            public string GetRoute_ForInstrumentation()
            {
                if (previous != null)
                    return previous.id + " " + id;
                return "";
            }


            public void Forward_ArrivalTime()
            {

                UpdateArrivalTime();
                if (next != null)
                    next.Forward_ArrivalTime();
                else
                    vehicle.depot.arrival = departure + vehicle.parent.GetTripDuration(location, vehicle.depot.location);
            }
            public Time GetArrivalTime(Visit previous)
            {
                return ((Time) previous.departure) + location.customer.parent.GetTripDuration(previous.location, location);
            }

            private void UpdateArrivalTime()
            {
                if (arrival != null && location.customer != null)
                    location.customer.parent.score.Unbind_LatenessWaiting(location.customer.delivery.GetLateness(), location.customer.delivery.GetWaiting());

                arrival = (previous == null) ? Time.Zero : GetArrivalTime(previous);
                if (location.customer != null)
                    location.customer.parent.score.Bind_LatenessWaiting(location.customer.delivery.GetLateness(), location.customer.delivery.GetWaiting());

            }
            public void Unbind_LatenessWaiting_Forward()
            {
                if (location.customer != null)
                    location.customer.parent.score.Unbind_LatenessWaiting(location.customer.delivery.GetLateness(), location.customer.delivery.GetWaiting());
                if (next != null)
                    next.Unbind_LatenessWaiting_Forward();

            }
        }

        public class Delivery : Visit
        {
            public Delivery waitingAssignment; // waiting for this delivery to get assigned so it can be put back in order.
            public bool isLocked;
            public Customer customer { get { return location.customer; } }
            public Delivery(Location location, int id)
                : base(location, id)
            {
                isLocked = false;
            }
            public override string ToString()
            {
                return customer.id.ToString() + ": Lateness = " + GetLateness() + ", Waiting = " + GetWaiting();
            }
            public void Unassign()
            {
                if (vehicle != null)
                {
                    RemoveVisitFromRouteScore();

                    if (customer.demand.delivered != 0)
                        throw new Exception("bad scoring");
                    UnlinkVisitFromRoute();
                    if (previous != null && previous.next != null)
                        previous.next.Forward_ArrivalTime();

                    this.vehicle = null;
                    this.previous = null;
                    this.arrival = null;
                }
            }

            private void UnlinkVisitFromRoute()
            {
                if (previous != null)
                    previous.next = next;
                if (next != null)
                    next.previous = previous;
                if (this == previous || this == next || previous.vehicle == null)
                    throw new Exception("bad linkage");
            }

            private void RemoveVisitFromRouteScore()
            {
                vehicle.UnAssignDelivery(this);
                customer.UnassignDelivery();
                if (location.customer != null && arrival != null)
                    location.customer.parent.score.Unbind_LatenessWaiting(location.customer.delivery.GetLateness(), location.customer.delivery.GetWaiting());
            }

            public void Assign(Visit visit)
            {
                if (vehicle != null || visit.vehicle == null)
                    throw new Exception("something wrong");
                LinkVisitToRoute(visit);
                AddVisitToRouteScore();
                Forward_ArrivalTime();
                if (waitingAssignment != null)
                {
                    waitingAssignment.Assign(this);
                    waitingAssignment = null;
                }
            }

            private void AddVisitToRouteScore()
            {
                vehicle.AssignDelivery(this);
                customer.AssignDelivery(vehicle);
            }

            private void LinkVisitToRoute(Visit visit)
            {
                next = visit.next;
                previous = visit;
                vehicle = visit.vehicle;
                if (next != null)
                    next.previous = this;
                previous.next = this;
            }

            public Time GetWaiting()
            {
                if (vehicle != null)
                    return GetWaiting((Time) arrival);
                else
                    return Time.Zero;
            }
            public Time GetWaiting(Time arrival)
            {
                return Time.Max(Time.Zero, (customer.demand.readyTime - ((Time)arrival)));
            }
            public bool CanVehicleSatisfyCustomerDemand(Vehicle vehicle)
            {
                return vehicle.package.units >= customer.demand.requested;
            }
        }

    }
}

