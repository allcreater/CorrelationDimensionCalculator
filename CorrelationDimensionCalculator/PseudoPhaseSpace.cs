using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorrelationDimensionCalculator
{
    public static class FloatExtensions
    {
        public static int GetExponent(this float value)
        {
            var raw = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            return raw >> 23 & ((1 << 8) - 1);
        }

        public static int GetMantissa(this float value)
        {
            var raw = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            return (raw & ((1 << 23) - 1));
        }

        public static int GetExponent(this double value)
        {
            var raw = BitConverter.DoubleToInt64Bits(value);
            return (int)(raw >> 52 & ((1 << 11) - 1));
        }

        public static long GetMantissa(this double value)
        {
            var raw = BitConverter.DoubleToInt64Bits(value);
            return (raw & ((1 << 52) - 1));
        }
    }

    public class PseudoPhaseSpace
    {
        private IList<double> m_points;

        public int Shift { get; private set; }

        public PseudoPhaseSpace(IEnumerable<double> points, int shift)
        {
            m_points = new List<double>(points);
            Shift = shift;
        }

        public double GetDistanceBetweenPoints(int i, int j, int N)
        {
            double result = 0.0f;
            for (int n = 0; n < N; ++n)
            {
                int ii = GetNIndex(i, n);
                int jj = GetNIndex(j, n);
                result += ((m_points[ii] - m_points[jj]) * (m_points[ii] - m_points[jj]));
            }

            return result;//Math.Sqrt(result);
        }

        private int GetNIndex(int i, int N)
        {
            return (i + Shift * N) % m_points.Count;
        }

        [Obsolete]
        public double ComputeCorrelation(double epsilon, int N)
        {
            int count = 0;

            lock (m_points)
            {
                for (int i = 0; i < m_points.Count; ++i)
                    for (int j = i + 1; j < m_points.Count; ++j)
                    {
                        bool isNear = GetDistanceBetweenPoints(i, j, N) <= epsilon;
                        count += (isNear) ? 1 : 0;
                    }
            }

            return (double)(count * 2.0) / (m_points.Count * m_points.Count);
        }

        public IEnumerable<System.Windows.Point> ComputeCorrelation(int N)
        {
            const int e_min = -1023, e_max = 1024, offset = 1 - e_min;
            const int K = e_max - e_min + 1;
            int[] Ne = new int[K];
            int K_min = K, K_max = 1;

            const int m = 4;

            Array.Clear(Ne, 0, Ne.Length);
            lock (m_points)
            {
                int count = m_points.Count;
                for (int i = 0; i < count - 1; ++i)
                    for (int j = i + 1; j < count; ++j)
                    {
                        var distance = GetDistanceBetweenPoints(i, j, N);
                        int k = (int)(Math.Log(distance + 0.0001, 2.0) * m) + offset;
                        Ne[k]++;
                        if (k == 0)
                            continue;

                        if (k > K_max)
                            K_max = (int)k;
                        if (k < K_min)
                            K_min = (int)k;
                    }
            }

            double summ = Ne[0];
            for (int k = K_min; k < K_max; ++k)
            {
                summ += Ne[k];
                double x = (k-offset)/(2.0 * m); //because we have a distance^(2*m)
                double y = Math.Log(summ * 2 / m_points.Count / m_points.Count, 2.0);
                yield return new System.Windows.Point(x, y);
            }
        }
        /*
        public double ComputeHurstExponent()
        {
            int count = m_points.Count;

            //find average
            double averageValue = 0.0, averageSquaredValue = 0.0;
            for (int i = 0; i < count; ++i)
            {
                averageValue += m_points[i];
                averageSquaredValue += (m_points[i] * m_points[i]);
            }
            averageValue /= count;
            averageSquaredValue /= count;

            double[] partialSumms = new double [count];
            double summ = 0.0;
            for (int i = 0; i < count; ++i)
                partialSumms[i] = (summ += (m_points[i] - averageValue));


            double R = partialSumms.Max() - partialSumms.Min(); //range
            double D = averageSquaredValue - averageValue*averageValue; //variance


            return Math.Log(R / Math.Sqrt(D), 10.0) / Math.Log(count, 10.0);
        }
        */
        public double ComputeHurstExponent()
        {
            int count = m_points.Count;

            //find average
            double averageValue = m_points.Sum() / count;

            double[] partialSumms = new double[count];
            double summ = 0.0, summarySquaredValue = 0.0;
            for (int i = 0; i < count; ++i)
            {
                double value = m_points[i] - averageValue;
                partialSumms[i] = (summ += value);
                summarySquaredValue += (value * value);
            }

            double R = partialSumms.Max() - partialSumms.Min(); //range
            double S = Math.Sqrt(summarySquaredValue / count); //standard deviation 


            return Math.Log(R / S) / Math.Log(count / 2);
        }
    }
}
