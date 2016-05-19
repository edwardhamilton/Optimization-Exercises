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
using System.Diagnostics;

namespace SolverForDummies
{
    public interface Distribution<T>
    {
        T Next(double additionalVariance = 0);
        void SetMean(double mean);
        double deviation { get; }
        double mean { get; }
    }
    public class Probability
    {
        public double NormalRandomVariable(double mean, double deviation)
        {
            Util.rand.NextDouble();

            double u1 = Util.rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = Util.rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = mean + deviation * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        }

    }
    public struct Range<T>
    {
        public T min, max;
        public Range(T min, T max)
        {
            this.min = min;
            this.max = max;
        }
        public override string ToString()
        {
            return "Min/Max = " + min + "/" + max;
        }
    }
    public class NormalDistribution_Double : Distribution<double>
    {
        public double mean { get; set; }
        public double deviation { get; }
        Probability probability = new Probability();
        public NormalDistribution_Double(double mean, double variance)
        {
            this.mean = mean;
            this.deviation = mean * variance;
        }
        public void SetMean(double mean)
        {
            this.mean = mean;
        }
        public double Next(double additionalVariance = 0) { return Math.Max(0, probability.NormalRandomVariable(mean, deviation * (1 + additionalVariance))); }
        public override string ToString()
        {
            return "Mean = = " + mean + ", Deviation = " + deviation;
        }
    }

    public class NormalDistribution_Int : Distribution<int>
    {
        public double mean { get; set; }
        public double deviation { get; }
        Probability probability = new Probability();
        public NormalDistribution_Int(double mean, double variance)
        {
            this.mean = mean;
            this.deviation = mean * variance;
        }
        public void SetMean(double mean)
        {
            this.mean = mean;
        }

        public int Next(double additionalVariance = 0) { return Math.Max(1, (int)probability.NormalRandomVariable(mean, deviation * (1 + additionalVariance))); }
        public override string ToString()
        {
            return "Mean = = " + mean + ", Deviation = " + deviation;
        }
    }

    public class UniformDistribution_Int : Distribution<int>
    {
        Range<int> range;
        public double mean { get; set; }
        public double deviation { get; }
        public UniformDistribution_Int(Range<int> range)
        {
            this.range = range;
            mean = (range.min + range.max) / 2;
            deviation = range.max - range.min;
        }
        public UniformDistribution_Int(double mean, double variance)
        {
            this.mean = mean;
            this.deviation = variance;
            SetMean(mean);
        }
        public void SetMean(double mean)
        {
            this.mean = mean;
            this.range = new Range<int>((int)Math.Floor(mean * (1 - deviation)), (int)Math.Ceiling(mean * (1 + deviation)));
        }

        public int Next(double additionalVariance = 0) { return Math.Max(1, Util.rand.Next((int) Math.Floor(range.min * (1 - additionalVariance)), (int) Math.Ceiling(range.max * (1 + additionalVariance)))); }
        public override string ToString()
        {
            return range.ToString();
        }
    }
    public struct DoubleComparer : IComparable<DoubleComparer>, IComparable
    {
        double value;
        public DoubleComparer(double value)
        {
            this.value = value;
        }
        public int CompareTo(object o)
        {
            if (o == null)
                return -1;
            return this.CompareTo((DoubleComparer)o);
        }
        public bool Equals(DoubleComparer other) { return Util.Equals(this.value, other.value); }
        public int CompareTo(DoubleComparer other)
        {
            if (Equals(other))
                return 0;
            return this.value.CompareTo(other.value);
        }
        public override string ToString()
        {
            return value.ToString();
        }

    }

    public struct Distance : IEquatable<Distance>, IComparable<Distance>
    {
        private static readonly double MetersPerKilometer = 1000.0;
        private static readonly double CentimetersPerMeter = 100.0;
        private static readonly double CentimetersPerInch = 2.54;
        private static readonly double InchesPerFoot = 12.0;
        private static readonly double FeetPerYard = 3.0;
        private static readonly double FeetPerMeter = CentimetersPerMeter / (CentimetersPerInch * InchesPerFoot);
        private static readonly double InchesPerMeter = CentimetersPerMeter / CentimetersPerInch;

        private readonly double _meters;
        public static readonly Distance Zero = new Distance(0);
        public static readonly Distance MaxValue = new Distance(double.MaxValue);

        public Distance(double meters)
        {
            this._meters = meters;
        }

        public double TotalKilometers { get { return _meters / MetersPerKilometer; } }
        public double TotalMeters { get { return _meters; } }
        public double TotalCentimeters { get { return _meters * CentimetersPerMeter; } }
        public double TotalYards { get { return _meters * FeetPerMeter / FeetPerYard; } }
        public double TotalFeet { get { return _meters * FeetPerMeter; } }
        public double TotalInches { get { return _meters * InchesPerMeter; } }
        public static Distance FromKilometers(double value) { return new Distance(value * MetersPerKilometer); }
        public static Distance FromMeters(double value) { return new Distance(value); }
        public static Distance FromCentimeters(double value) { return new Distance(value / CentimetersPerMeter); }
        public static Distance FromYards(double value) { return new Distance(value * FeetPerYard / FeetPerMeter); }
        public static Distance FromFeet(double value) { return new Distance(value / FeetPerMeter); }
        public static Distance FromInches(double value) { return new Distance(value / InchesPerMeter); }
        public static Distance operator +(Distance a, Distance b) { return new Distance(a._meters + b._meters); }
        public static Distance operator -(Distance a, Distance b) { return new Distance(a._meters - b._meters); }
        public static Distance operator -(Distance a) { return new Distance(-a._meters); }
        public override bool Equals(object obj)
        {
            if (!(obj is Distance))
                return false;
            return Equals((Distance)obj);
        }
        public bool Equals(Distance other) { return Util.Equals(this._meters, other._meters); }
        public int CompareTo(Distance other) { return this._meters.CompareTo(other._meters); }
        public override int GetHashCode() { return _meters.GetHashCode(); }
        public override string ToString()
        {
            return string.Format("{0}km", TotalKilometers);
        }
    }

    public struct Time : IEquatable<Time>, IComparable<Time>
    {
        private static readonly double MinutesPerHour = 60;

        private readonly double _minutes;

        public Time(double minutes)
        {
            this._minutes = minutes;
        }
        public static Time Max(Time a, Time b)
        {
            return a.CompareTo(b) > 0 ? a : b;
        }
        public static Time Min(Time a, Time b)
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        public static readonly Time Zero = new Time(0);
        public static readonly Time MaxValue = new Time(60 * 24 * 7); // I don't use double.MaxValue here, to prevent overflow.

        public int Hour { get { return (int)Math.Max(0, Math.Floor(_minutes / MinutesPerHour)); } }
        public int Minute { get { return (int)(_minutes % MinutesPerHour); } }
        public double TotalMinutes { get { return _minutes; } }

        public static Time operator +(Time a, Time b) { return new Time(a._minutes + b._minutes); }
        public static Time operator -(Time a, Time b) { return new Time(a._minutes - b._minutes); }
        public static Time operator -(Time a) { return new Time(-a._minutes); }
        public static Time operator *(Time a, Time b) { return new Time(a._minutes * b._minutes); }
        public static Time operator *(Time a, double m) { return new Time(a._minutes * m); }
        public override bool Equals(object obj)
        {
            if (!(obj is Time))
                return false;
            return Equals((Time)obj);
        }
        public bool Equals(Time other) { return Util.Equals(this._minutes, other._minutes); }
        public int CompareTo(Time other) { return this._minutes.CompareTo(other._minutes); }
        public override int GetHashCode() { return _minutes.GetHashCode(); }
        public override string ToString()
        {
            if (_minutes == double.MaxValue)
                return "max";
            return string.Format("{0}:{1}", Hour, (Minute < 10) ? ("0" + Minute.ToString()) : Minute.ToString());
        }
    }

    static class Util
    {
        public static bool Equals(double a, double b)
        {
            return Math.Abs(a - b) <= Math.Max(.001, (a * .0001));
        }

        [ThreadStatic]
        private static Random _rand = new Random();

         public static Random rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        class SequenceCache
        {
            struct Sequence
            {
                public int[] values;
            }
            int length;
            int count;
            Sequence[] sequences;
            public SequenceCache(int length, int maxSequences)
            {
                this.length = length;
                count = 0;
                sequences = new Sequence[maxSequences];
            }
            public int[] Get()
            {
                if (count < sequences.Length)
                {
                    int[] random = new int[length];
                    for (int i = 0; i < length; i++)
                        random[i] = i;
                    Util.Shuffle<int>(random, rand);
                    sequences[count].values = random;
                    count++;
                }
                return sequences[rand.Next(count)].values;
            }
        }
        static Dictionary<int, SequenceCache> randomSequences = new Dictionary<int, SequenceCache>();

        static public void PreBuildRandomSequences(int upTo)
        {
            for (int length = 0; length < upTo; length++)
            {
                for (int i = 0; i < maxSequences; i++)
                    Get_NonRepeating_RandomSequence(length);
            }
        }

        const int maxSequences = 10000;
        static public int[] Get_NonRepeating_RandomSequence(int length)
        {
            int[] result;
            if (!randomSequences.ContainsKey(length))
            {
                SequenceCache sequence = new SequenceCache(length, maxSequences);
                if (!randomSequences.ContainsKey(length))
                    randomSequences.Add(length, sequence);
            }
            result = randomSequences[length].Get();
            return result;
        }

        public static void Shuffle<T>(T[] array, Random rand)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                int r = i + (int)(rand.NextDouble() * (n - i));
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }
        public static void RemoveAt<T>(ref T[] arr, int index)
        {
            for (int a = index; a < arr.Length - 1; a++)
            {
                // moving elements downwards, to fill the gap at [index]
                arr[a] = arr[a + 1];
            }
            // finally, let's decrement Array's size by one
            Array.Resize(ref arr, arr.Length - 1);
        }
        public static List<T> Randomize<T>(IEnumerable<T> source, Random rand)
        {
            return source.OrderBy<T, int>((item) => rand.Next()).ToList();
        }
        public static void CopyArrayRange<T>(T[] src, int srcOffset, T[] dst, int dstOffset, int length)
        {
            for (int i = 0; i < length; i++)
                dst[dstOffset + i] = src[srcOffset + i];
        }
        public static T[] RemoveIndicesFromArray<T>(T[] array, IEnumerable<int> indices)
        {
            List<T> newArray = new List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (!indices.Contains(i))
                    newArray.Add(array[i]);
            }
            return newArray.ToArray();

        }
    }
    public struct Pair<F, S>
    {
        public F first;
        public S second;
    }
    static public class Instrumentation
    {
        public struct FeatureInstanceProperties
        {
            public string name;
            public bool applyBackwards;
            public FeatureInstanceProperties(string name, bool applyBackwards = false)
            {
                this.name = name;
                this.applyBackwards = applyBackwards;
            }
        }
        static public int AddFeature(string instanceName, bool applyBackwards = false)
        {
            return AddFeature(new Feature(instanceName, applyBackwards));
        }
        static public int AddFeature(Feature feature)
        {
            AddFeature_IfNew(feature);

            if (!registeredFeatures.ContainsKey(feature.name))
                return -1;
            return registeredFeatures[feature.name].id;

        }

        [ConditionalAttribute("INSTRUMENTATION")]
        private static void AddFeature_IfNew(Feature feature)
        {
            if (!registeredFeatures.ContainsKey(feature.name))
            {
                initialized = false;
                feature.id = registeredFeatures.Count();
                registeredFeatures.Add(feature.name, feature);
            }
        }

        public struct Feature
        {
            public int id;
            public string name;
            public bool applyBackwards;
            public object value;
            public Feature(int id, string name, bool applyBackwards)
            {
                this.id = id;
                this.name = name;
                this.applyBackwards = applyBackwards;
                value = null;
            }
            public Feature(string name, bool applyBackwards = false)
            {
                this.id = -1;
                this.name = name;
                this.applyBackwards = applyBackwards;
                value = null;
            }

        }
        [ConditionalAttribute("INSTRUMENTATION")]
        static public void FlushToCsv(string path)
        {
            //ApplyMeasuresBackward_IfApplicable();

            CsvFileWriter writer = new CsvFileWriter(path);
            if (HeadHasNotYetBeenWritten(path))
                writer.WriteRow<string>(from m in registeredFeatures.Values select m.name);
            foreach (object[] observation in observations)
                writer.WriteRow<object>(observation);
            observations.Clear();
            writer.Dispose();
        }

        static public void WriteSection(string path, IEnumerable<string> rows)
        {
            CsvFileWriter writer = new CsvFileWriter(path);
            foreach (string row in rows)
                writer.WriteLine(row);
            writer.Dispose();
        }


        private static void ApplyMeasuresBackward_IfApplicable()
        {
            // Apply mearsures backwards when measure has that property
            object[] activeObservation = new object[Instrumentation.features.Count()];
            for (int observaton = observations.Count - 1; observaton >= 0; observaton--)
            {
                for (int feature = 0; feature < observations[observaton].Count(); feature++)
                {
                    if (observations[observaton][feature] != null)
                        activeObservation[feature] = observations[observaton][feature];
                    else
                        observations[observaton][feature] = activeObservation[feature];
                }
            }
        }

        private static bool HeadHasNotYetBeenWritten(string path)
        {
            bool outputHeader = true;
            try
            {
                outputHeader = new System.IO.FileInfo(path).Length == 0;
            }
            catch (Exception)
            {
            }

            return outputHeader;
        }

        static DateTime beginTime;
        static Dictionary<string, Feature> registeredFeatures = new Dictionary<string, Feature>();
        static Feature[] features;
        static List<object[]> observations = new List<object[]>();
        static int featureID_ElapseTime = Instrumentation.AddFeature(new Feature("ElapseTime"));
        static int featureID_Type = Instrumentation.AddFeature(new Feature("Type"));
        static bool initialized = false;
        [ConditionalAttribute("INSTRUMENTATION")]
        static void Initialize()
        {
            Feature[] prevFeatures = Instrumentation.features;
            Instrumentation.features = new Feature[registeredFeatures.Count()];
            foreach (Feature feature in registeredFeatures.Values)
            {
                Instrumentation.features[feature.id].id = feature.id;
                Instrumentation.features[feature.id].name = feature.name;
                Instrumentation.features[feature.id].applyBackwards = feature.applyBackwards;
                Instrumentation.features[feature.id].value = null;
            }
            if (prevFeatures != null)
            {
                foreach (Feature f in prevFeatures)
                    Instrumentation.features[f.id].value = f.value;
            }
            else
                beginTime = DateTime.Now;
            initialized = true;
        }
        [ConditionalAttribute("INSTRUMENTATION")]
        static public void SetFeature(int index, object value)
        {
            if (index < 0)
                return;  // this means the feature has been disabled
            if (!initialized)
                Initialize();
            features[index].value = value;
        }
        [ConditionalAttribute("INSTRUMENTATION")]
        static public void ClearFeature(int index)
        {
            features[index].value = null;
        }
        [ConditionalAttribute("INSTRUMENTATION_OBSERVEREJECTS")]
        static public void MakeObservation_AtRejected(object type)
        {
            MakeObservation_AtAccepted(type);
        }
        static int observationCount = 0;
        [ConditionalAttribute("INSTRUMENTATION")]
        static public void MakeObservation_AtAccepted(object type)
        {
            TimeSpan elapse = DateTime.Now - beginTime;
            SetFeature(featureID_Type, type);
            SetFeature(featureID_ElapseTime, elapse.TotalMilliseconds);
            object[] observation = new object[Instrumentation.features.Count()];
            for (int i = 0; i < Instrumentation.features.Count(); i++)
            {
                observation[i] = Instrumentation.features[i].value;
                if (Instrumentation.features[i].applyBackwards)
                    ClearFeature(i); // So we'll know how far back to apply backwards
            }
            observations.Add(observation);
            observationCount++;
        }
    }


    static class Logger
    {
        static System.IO.StreamWriter stream;

            //        [ConditionalAttribute("DEBUG")]
        static public void Log(string msg, int tab = 0)
        {
            if (stream == null)
                OpenLog("..\\Logs\\Log " + System.DateTime.Now.ToString().Replace(':', '_').Replace('/', '-') + ".log");
            lock (stream)
            {
                stream.WriteLine(GetTab(tab) + msg);
                stream.Flush();
                //            Console.WriteLine(msg);
            }
        }
        static public void OpenLog(string filename, bool append = false)
        {
            //logFile = new System.IO.StreamWriter("c:\\Users\\Edward\\" + filename);
            stream = new System.IO.StreamWriter(filename, append);
        }
        static public string GetTab(int tab)
        {
            string tabs = "";
            for (int n = 0; n < tab; n++)
                tabs += '\t';
            return tabs;
        }
    }



}