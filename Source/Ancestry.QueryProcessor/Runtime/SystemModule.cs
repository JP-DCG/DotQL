﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Ancestry.QueryProcessor.Type.Module(typeof(Ancestry.QueryProcessor.Runtime.SystemModule), new[] { "System" })]

namespace Ancestry.QueryProcessor.Runtime
{
	public class SystemModule
	{
		public static readonly Boolean Boolean;
		public static readonly Int32 Int32;
		public static readonly Int64 Int64;
		public static readonly Char Char;
		public static readonly String String;
		// TODO: public static readonly Date Date;
		// TODO: public static readonly Time Time;
		public static readonly DateTime DateTime;
		public static readonly Name Name;
		public static readonly Double Double;
		public static readonly Guid GUID;
		public static readonly TimeSpan TimeSpan;
		public static readonly Version Version;

		public Storage.IRepository<ISet<ModuleTuple>> Modules;

		public Storage.IRepository<ISet<UsingTuple>> DefaultUsings;

		public static IList<T> ToList<T>(ISet<T> setValue)
		{
			// TODO: ensure that the items coming from the set are ordered so that this is deterministic
			return new ListEx<T>(setValue);
		}

		public static ISet<T> ToSet<T>(IList<T> listValue)
		{
			return new Set<T>(listValue);
		}

		public static DateTime AddMonth(DateTime start, int months)
		{
			return start.AddMonths(months);
		}

        public static DateTime AddDay(DateTime start, double days)
        {
            return start.AddDays(days);
        }

        //Numeric - TODO: overloading for various data types.
        public static double Abs(double value)
        {
            return Math.Abs(value);
        }

        public static double Acos(double value)
        {
            return Math.Acos(value);
        }

        public static double Asin(double value)
        {
            return Math.Asin(value);
        }

        public static double Atan(double value)
        {
            return Math.Atan(value);
        }

        public static double Atan2(double x, double y)
        {
            return Math.Atan2(x, y);
        }

        public static long BigMul(int x, int y)
        {
            return Math.BigMul(x, y);
        }

        public static double Ceiling(double value)
        {
            return Math.Ceiling(value);
        }

        public static double Cos(double value)
        {
            return Math.Cos(value);
        }

        public static double Cosh(double value)
        {
            return Math.Cosh(value);
        }

        //public static double DivRem(double value)
        //{
        //    return Math.DivRem(value);
        //}

        public static double Exp(double value)
        {
            return Math.Exp(value);
        }

        //public static long Factorial(int value)
        //{

        //}

        public static double Frac(double value)
        {
            return value - Math.Truncate(value);
        }

        public static double Floor(double value)
        {
            return Math.Floor(value);
        }

        public static double IEEERemainder(double x, double y)
        {
            return Math.IEEERemainder(x, y);
        }

        public static double Ln(double value)
        {
            return Math.Log(value);
        }

        public static double Log(double value, double newBase)
        {
            return Math.Log(value, newBase);
        }

        public static double Log10(double value)
        {
            return Math.Log10(value);
        }

        public static double Max(double x, double y)
        {
            return Math.Max(x, y);
        }

        public static double Min(double x, double y)
        {
            return Math.Min(x, y);
        }

        public static double Pow(double x, double y)
        {
            return Math.Pow(x, y);
        }

        public static double Round(double value, int decimals)
        {
            return Math.Round(value, decimals);
        }

        public static int Sign(double value)
        {
            return Math.Sign(value);
        }

        public static double Sin(double value)
        {
            return Math.Sin(value);
        }

        public static double Sinh(double value)
        {
            return Math.Sinh(value);
        }

        public static double Sqrt(double value)
        {
            return Math.Sqrt(value);
        }

        public static double Tan(double value)
        {
            return Math.Tan(value);
        }

        public static double Tanh(double value)
        {
            return Math.Tanh(value);
        }

        public static double Truncate(double value)
        {
            return Math.Truncate(value);
        }

	}
}
