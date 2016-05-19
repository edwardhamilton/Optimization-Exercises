/*******************************************************************************
 * Copyright (C) 2016  Edward Hamilton 
 *      Email:    edward.orourke.hamilton@gmail.com
 *      LinkedIn: https://www.linkedin.com/in/edward-hamilton-b674a76
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using SolverForDummies;

namespace vrp
{
    using Solver = Solver<VRPSolver.Delivery, VRPSolver.Visit, VRPSolver.Score>;
    public partial class VRPSolver : Solver.Problem
    {

        public class Problem_Loader
        {
            public enum Type { TSPLIB_CVRP, LARGE_CVRPTW };
            private const string TOKEN_NAME = "NAME:";
            private const string TOKEN_EOF = "EOF";
            private const string TOKEN_TYPE = "TYPE:";
            private const string TOKEN_MIN_TRUCKS = "MIN_TRUCKS:";
            private const string TOKEN_OPTIMAL_VALUE = "OPTIMAL_VALUE:";
            private const string TOKEN_DIMENSION = "DIMENSION:";
            private const string TOKEN_TYPE_VALUE_CVRP = "CVRP";
            private const string TOKEN_COMMENT = "COMMENT:";
            private const string TOKEN_CAPACITY = "CAPACITY:";
            private const string TOKEN_EDGE_WEIGHT_TYPE = "EDGE_WEIGHT_TYPE:";
            private const string TOKEN_EDGE_WEIGHT_TYPE_VALUE_EUC_2D = "EUC_2D";
            private const string TOKEN_EDGE_WEIGHT_TYPE_EXPLICIT = "EXPLICIT";
            private const string TOKEN_EDGE_WEIGHT_FORMAT = "EDGE_WEIGHT_FORMAT:";
            private const string TOKEN_EDGE_WEIGHT_FORMAT_MATRIX = "MATRIX";
            private const string TOKEN_EDGE_WEIGHT_SECTION = "EDGE_WEIGHT_SECTION";
            private const string TOKEN_NODE_COORD_SECTION = "NODE_COORD_SECTION";
            private const string TOKEN_DEMAND_SECTION = "DEMAND_SECTION";


            public int size = -1;
            public double[][] distances = null;
            public double[] demand = null;
            public double[] readyTime = null;
            public double[] dueTime = null;

            public Point[] points = null;
            public string comment = string.Empty;
            public string name = string.Empty;
            public double capacity;
            public int? minVehicles, numVehicles;
            public Distance? optimalValue;
            public Product defaultProduct;
            public string path;
            public Type type;

            public Problem_Loader(string path, Type type)
            {
                this.path = path;
                this.type = type;
                defaultProduct = new Product(Time.Zero);
                demand = readyTime = dueTime  = null;
                minVehicles = numVehicles = null;
                switch (type)
                {
                    case Type.TSPLIB_CVRP:
                        {
                            Parse_TSPLIB_From();
                            break;
                        }
                    case Type.LARGE_CVRPTW:
                        {
                            Parse_VeryLargeCVRPTW_From();
                            break;
                        }
                }

            }
            private void Parse_VeryLargeCVRPTW_From()
            {
                demand = new double[2000];
                readyTime = new double[2000];
                dueTime = new double[2000];

                FileInfo file = new FileInfo(path);

                StreamReader reader = file.OpenText();
                name = reader.ReadLine(); // 1st line is name
                reader.ReadLine(); // 2nd line blank
                reader.ReadLine(); // 3rd line has header
                reader.ReadLine(); // 4th line has header
                {
                    string line = reader.ReadLine().Trim();
                    string[] splitted_line = Regex.Split(line, @"\s+");
                    numVehicles = int.Parse(splitted_line[0]);
                    capacity = int.Parse(splitted_line[1]);
                    optimalValue = new Distance(double.Parse(splitted_line[2]) * 1000);
                }
                reader.ReadLine(); // 6th line blank
                reader.ReadLine(); // 7th line has header
                reader.ReadLine(); // 8th line has header
                reader.ReadLine(); // 9th line blank
                var points = new List<Point>();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    string[] splitted_line = Regex.Split(line, @"\s+");
                    int customerNo = int.Parse(splitted_line[0]);
                    int x = int.Parse(splitted_line[1]);
                    int y = int.Parse(splitted_line[2]);
                    demand[customerNo] = int.Parse(splitted_line[3]);
                    readyTime[customerNo] = int.Parse(splitted_line[4]);
                    dueTime[customerNo] = int.Parse(splitted_line[5]);
                    defaultProduct.serviceTime = new Time(int.Parse(splitted_line[6]));
                    Point p = new Point(x, y);
                    if (demand[customerNo] == 0)
                        depot = p;
                    points.Add(p);

                }
                this.points = points.ToArray();
                distances = Problem_Loader.CalculateEuclideanWeights(points);

            }
            /* File format and sample files can be found here: https://www.sintef.no/projectweb/top/vrptw/homberger-benchmark */
            public void LoadSolution(VRPSolver parent)
            {
                if (type == Type.LARGE_CVRPTW)
                    Parse_Solution_VeryLargeCVRPTW_From(parent);
//                else
//                    throw new NotImplementedException();

            }
            private void Parse_Solution_VeryLargeCVRPTW_From(VRPSolver parent)
            {
                FileInfo file = new FileInfo(Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".solution");
                if (!file.Exists)// || true)
                    return;
                StreamReader reader = file.OpenText();
                for (int i = 0; i < 5; i++)
                    reader.ReadLine(); // skip unused lines

                Visit[] depots = parent.depots.ToArray();
                int depot = 0;
                while (!reader.EndOfStream)
                {
                    Visit previous = depots[depot];
                    string line = reader.ReadLine().Trim();
                    string[] splitted_line = Regex.Split(line, @"\s+");
                    for (int i = 3; i < splitted_line.Length; i++)
                    {
                        int customerNo = int.Parse(splitted_line[i]);
                        parent.UpdateAssignment(parent.deliveries[customerNo - 1], previous);
                        parent.deliveries[customerNo - 1].isLocked = true;
                        previous = parent.deliveries[customerNo - 1];
                    }
                    depot++;
                }

            }


            /* File format and sample files can be found here: http://neo.lcc.uma.es/vrp/vrp-instances/capacitated-vrp-instances/ */
            private void Parse_TSPLIB_From()
            {
                FileInfo file = new FileInfo(path);

                StreamReader reader = file.OpenText();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim().Replace(" :", ":");

                    if (line.StartsWith(TOKEN_NAME))
                    {
                        name = line.Replace(TOKEN_NAME, string.Empty).Trim();
                    }
                    else if (line.StartsWith(TOKEN_TYPE))
                    {
                        string type = line.Replace(TOKEN_TYPE, string.Empty).Trim();
                        if (type.ToUpper() != TOKEN_TYPE_VALUE_CVRP)
                            throw new Exception("Invalid problem type");
                    }
                    else if (line.StartsWith(TOKEN_COMMENT))
                    {
                        comment = line.Replace(TOKEN_COMMENT, string.Empty).Trim();
                    }
                    else if (line.StartsWith(TOKEN_DIMENSION))
                    {
                        string dimension = line.Replace(TOKEN_DIMENSION, string.Empty).Trim();
                        size = int.Parse(dimension);
                    }
                    else if (line.StartsWith(TOKEN_MIN_TRUCKS))
                    {
                        string value = line.Replace(TOKEN_MIN_TRUCKS, string.Empty).Trim();
                        this.minVehicles = int.Parse(value);
                    }
                    else if (line.StartsWith(TOKEN_OPTIMAL_VALUE))
                    {
                        string value = line.Replace(TOKEN_OPTIMAL_VALUE, string.Empty).Trim();
                        this.optimalValue = new Distance(int.Parse(value) * 1000);
                    }
                    else if (line.StartsWith(TOKEN_CAPACITY))
                    {
                        string capacity = line.Replace(TOKEN_CAPACITY, string.Empty).Trim();
                        this.capacity = int.Parse(capacity);
                    }
                    else if (line.StartsWith(TOKEN_EDGE_WEIGHT_TYPE))
                    {
                        string edge_weight_type = line.Replace(TOKEN_EDGE_WEIGHT_TYPE, string.Empty).Trim();
                        if (edge_weight_type.ToUpper() != TOKEN_EDGE_WEIGHT_TYPE_VALUE_EUC_2D)
                            throw new Exception("Invalid weight type");
                    }
                    else if (line.StartsWith(TOKEN_EDGE_WEIGHT_SECTION))
                        line = Token_Edge_Weight_Section(reader, line);
                    else if (line.StartsWith(TOKEN_NODE_COORD_SECTION))
                        line = Token_Coord_Section(reader, line);
                    else if (line.StartsWith(TOKEN_DEMAND_SECTION))
                        line = Token_Demand_Section(reader, line);
                }
                for (int customerNo = 0; customerNo < size; customerNo++)
                {
                    if (demand[customerNo] == 0)
                            depot = points[customerNo];

                }
            }

            private string Token_Demand_Section(StreamReader reader, string line)
            {
                {
                    demand = new double[size];
                    for (int i = 0; i < size; i++)
                    {
                        line = reader.ReadLine().Trim();

                        if (line.StartsWith(TOKEN_EOF))
                        {
                            break;
                        }
                        else
                        {
                            var splitted_line = Regex.Split(line, @"\s+");
                            int id = (int)double.Parse(splitted_line[0]) - 1;
                            int x = (int)double.Parse(splitted_line[1]);
                            demand[id] = x;
                        }
                    }
                }

                return line;
            }

            private string Token_Coord_Section(StreamReader reader, string line)
            {
                {
                    var points = new List<Point>();
                    for (int i = 0; i < size; i++)
                    {
                        line = reader.ReadLine().Trim();

                        if (line.StartsWith(TOKEN_EOF))
                        {
                            break;
                        }
                        else
                        {
                            var splitted_line = Regex.Split(line, @"\s+");
                            int idx = (int)double.Parse(splitted_line[0]);
                            int x = (int)double.Parse(splitted_line[1]);
                            int y = (int)double.Parse(splitted_line[2]);

                            Point p = new Point(x, y);
                            points.Add(p);
                        }
                    }
                    this.points = points.ToArray();
                    distances = Problem_Loader.CalculateEuclideanWeights(points);
                }

                return line;
            }

            private string Token_Edge_Weight_Section(StreamReader reader, string line)
            {
                {
                    distances = new double[size][];
                    distances[0] = new double[size];

                    int x = 0;
                    int y = 0;
                    for (int i = 0; i < size; i++)
                    {
                        line = reader.ReadLine().Trim();

                        if (line.StartsWith(TOKEN_EOF))
                        {
                            break;
                        }
                        else
                        {
                            if (line != null && line.Length > 0)
                            {
                                string[] splitted_line = Regex.Split(line, @"\s+");
                                foreach (string weight_string in splitted_line)
                                {
                                    distances[x][y] = float.Parse(weight_string, CultureInfo.InvariantCulture);

                                    if (x == y)
                                    {
                                        distances[x][y] = 0;
                                    }

                                    if (y == size - 1)
                                    {
                                        x = x + 1;
                                        if (x < size)
                                        {
                                            distances[x] = new double[size];
                                            y = 0;
                                        }
                                    }
                                    else
                                    {
                                        y = y + 1;
                                    }
                                }
                            }
                        }
                    }
                }

                return line;
            }

            private static double[][] CalculateEuclideanWeights(List<Point> points)
            {
                var weigths = new double[points.Count][];
                for (var city1 = 0; city1 < points.Count; city1++)
                {
                    weigths[city1] = new double[points.Count];
                    for (int city2 = 0; city2 < points.Count; city2++)
                    {
                        weigths[city1][city2] = System.Math.Round(System.Math.Sqrt(
                                    System.Math.Pow(points[city1].X - points[city2].X, 2) +
                                    System.Math.Pow(points[city1].Y - points[city2].Y, 2)));
                    }
                }
                return weigths;
            }

            public Point depot;


            public Time Get_DueTime(int index)
            {
                return dueTime == null ? Time.MaxValue : new Time(dueTime[index]);
            }

            public Time Get_ReadyTime(int index)
            {
                return readyTime == null ? Time.Zero : new Time(readyTime[index]);
            }




        }
    }
}