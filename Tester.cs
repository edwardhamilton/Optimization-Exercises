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
using System.Timers;
using System.IO;

using vrp;
using SolverForDummies;

namespace exercise
{


    class Tester
    {

        static void Main(string[] args)
        {
            Util.PreBuildRandomSequences(200);
            if (false)
            {
                /* The problem sets for following test cases can be found here: https://www.sintef.no/projectweb/top/vrptw/homberger-benchmark */
                VRPSolver.Tester.Run(VRPSolver.Problem_Loader.Type.LARGE_CVRPTW, "..\\Test files\\200 Customers - With Time Windows", "*.txt");
            }
//            if (false)
            {
                /* The problem sets for following test cases can be found here: http://neo.lcc.uma.es/vrp/vrp-instances/capacitated-vrp-instances/ */
                VRPSolver.Tester.Run(VRPSolver.Problem_Loader.Type.TSPLIB_CVRP, "..\\Test files\\Capacitated", "*.*");
            }
        }
    }
}
