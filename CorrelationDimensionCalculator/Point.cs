using System;

namespace CorrelationDimensionCalculator
{
    class Point
    {
        public int Dim => m_components.Length;

        public double this[int i] => m_components[i];

        public static double Distance(Point fisrt, Point second)
        {
            var dim = fisrt.Dim;
            if (dim != second.Dim)
                throw new ArgumentException("Points should have same number of dimensions");

            var dist = 0d;
            for (var i = 0; i < dim; i++)
            {
                var sub = fisrt[i] - second[i];
                dist += sub*sub;
            }

            return Math.Sqrt(dist);
        }

        public static Point FromArray(double[] components)
        {
            return new Point(components);
        }

        private readonly double[] m_components;

        private Point(double[] components)
        {
            m_components = components;
        }
    }
}
