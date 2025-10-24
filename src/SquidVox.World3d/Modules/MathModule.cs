using System;
using SquidVox.Core.Attributes.Scripts;

namespace SquidVox.World3d.Modules;

/// <summary>
///     Provides functions equivalent to the JavaScript Math module.
/// </summary>
[ScriptModule("math", "Provides functions equivalent to the JavaScript Math module.")]
public class MathModule
{
    /// <summary>
    ///     Gets the base of the natural logarithm.
    /// </summary>
    public double E => Math.E;

    /// <summary>
    ///     Gets the natural logarithm of 2.
    /// </summary>
    public double Ln2 => Math.Log(2.0);

    /// <summary>
    ///     Gets the natural logarithm of 10.
    /// </summary>
    public double Ln10 => Math.Log(10.0);

    /// <summary>
    ///     Gets the base 2 logarithm of e.
    /// </summary>
    public double Log2E => 1.0 / Math.Log(2.0);

    /// <summary>
    ///     Gets the base 10 logarithm of e.
    /// </summary>
    public double Log10E => 1.0 / Math.Log(10.0);

    /// <summary>
    ///     Gets the ratio of a circle's circumference to its diameter.
    /// </summary>
    public double Pi => Math.PI;

    /// <summary>
    ///     Gets the square root of one half.
    /// </summary>
    public double Sqrt1_2 => Math.Sqrt(0.5);

    /// <summary>
    ///     Gets the square root of two.
    /// </summary>
    public double Sqrt2 => Math.Sqrt(2.0);

    /// <summary>
    ///     Returns the absolute value of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The absolute value of the specified number.</returns>
    [ScriptFunction("abs", "Returns the absolute value of the specified number.")]
    public double Abs(double value)
    {
        return Math.Abs(value);
    }

    /// <summary>
    ///     Returns the arc cosine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The arc cosine of the specified number.</returns>
    [ScriptFunction("acos", "Returns the arc cosine of the specified number.")]
    public double Acos(double value)
    {
        return Math.Acos(value);
    }

    /// <summary>
    ///     Returns the inverse hyperbolic cosine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The inverse hyperbolic cosine of the specified number.</returns>
    [ScriptFunction("acosh", "Returns the inverse hyperbolic cosine of the specified number.")]
    public double Acosh(double value)
    {
        return Math.Acosh(value);
    }

    /// <summary>
    ///     Returns the arc sine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The arc sine of the specified number.</returns>
    [ScriptFunction("asin", "Returns the arc sine of the specified number.")]
    public double Asin(double value)
    {
        return Math.Asin(value);
    }

    /// <summary>
    ///     Returns the inverse hyperbolic sine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The inverse hyperbolic sine of the specified number.</returns>
    [ScriptFunction("asinh", "Returns the inverse hyperbolic sine of the specified number.")]
    public double Asinh(double value)
    {
        return Math.Asinh(value);
    }

    /// <summary>
    ///     Returns the arc tangent of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The arc tangent of the specified number.</returns>
    [ScriptFunction("atan", "Returns the arc tangent of the specified number.")]
    public double Atan(double value)
    {
        return Math.Atan(value);
    }

    /// <summary>
    ///     Returns the angle whose tangent is the quotient of two specified numbers.
    /// </summary>
    /// <param name="y">The y coordinate.</param>
    /// <param name="x">The x coordinate.</param>
    /// <returns>The angle whose tangent is the quotient y/x.</returns>
    [ScriptFunction("atan2", "Returns the angle whose tangent is the quotient of two numbers.")]
    public double Atan2(double y, double x)
    {
        return Math.Atan2(y, x);
    }

    /// <summary>
    ///     Returns the inverse hyperbolic tangent of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The inverse hyperbolic tangent of the specified number.</returns>
    [ScriptFunction("atanh", "Returns the inverse hyperbolic tangent of the specified number.")]
    public double Atanh(double value)
    {
        return Math.Atanh(value);
    }

    /// <summary>
    ///     Returns the cube root of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The cube root of the specified number.</returns>
    [ScriptFunction("cbrt", "Returns the cube root of the specified number.")]
    public double Cbrt(double value)
    {
        if (value < 0)
        {
            return -Math.Pow(-value, 1.0 / 3.0);
        }

        return Math.Pow(value, 1.0 / 3.0);
    }

    /// <summary>
    ///     Returns the smallest integer greater than or equal to the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The smallest integer greater than or equal to the specified number.</returns>
    [ScriptFunction("ceil", "Returns the smallest integer greater than or equal to the specified number.")]
    public double Ceil(double value)
    {
        return Math.Ceiling(value);
    }

    /// <summary>
    ///     Returns the number of leading zero bits in the 32-bit binary representation of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The number of leading zero bits.</returns>
    [ScriptFunction("clz32", "Returns the number of leading zero bits in the 32-bit representation.")]
    public int Clz32(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return 32;
        }

        var truncated = Math.Truncate(value);
        var modulo = truncated % 4_294_967_296d;
        if (modulo < 0)
        {
            modulo += 4_294_967_296d;
        }

        var uintValue = (uint)modulo;
        if (uintValue == 0)
        {
            return 32;
        }

        var count = 0;
        var mask = 0x80000000;
        while ((uintValue & mask) == 0)
        {
            count++;
            uintValue <<= 1;
        }

        return count;
    }

    /// <summary>
    ///     Returns the cosine of the specified angle.
    /// </summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The cosine of the specified angle.</returns>
    [ScriptFunction("cos", "Returns the cosine of the specified angle.")]
    public double Cos(double value)
    {
        return Math.Cos(value);
    }

    /// <summary>
    ///     Returns the hyperbolic cosine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The hyperbolic cosine of the specified number.</returns>
    [ScriptFunction("cosh", "Returns the hyperbolic cosine of the specified number.")]
    public double Cosh(double value)
    {
        return Math.Cosh(value);
    }

    /// <summary>
    ///     Returns e raised to the specified power.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the specified power.</returns>
    [ScriptFunction("exp", "Returns e raised to the specified power.")]
    public double Exp(double value)
    {
        return Math.Exp(value);
    }

    /// <summary>
    ///     Returns e raised to the specified power minus one.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the specified power minus one.</returns>
    [ScriptFunction("expm1", "Returns e raised to the specified power minus one.")]
    public double Expm1(double value)
    {
        return Math.Exp(value) - 1.0;
    }

    /// <summary>
    ///     Returns the largest integer less than or equal to the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The largest integer less than or equal to the specified number.</returns>
    [ScriptFunction("floor", "Returns the largest integer less than or equal to the specified number.")]
    public double Floor(double value)
    {
        return Math.Floor(value);
    }

    /// <summary>
    ///     Returns the nearest 32-bit single-precision float representation of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The nearest single-precision float representation.</returns>
    [ScriptFunction("fround", "Returns the nearest single-precision float representation of the specified number.")]
    public double Fround(double value)
    {
        return (double)(float)value;
    }

    /// <summary>
    ///     Returns the square root of the sum of squares of the provided numbers.
    /// </summary>
    /// <param name="values">The numbers to evaluate.</param>
    /// <returns>The square root of the sum of squares.</returns>
    [ScriptFunction("hypot", "Returns the square root of the sum of squares of the provided numbers.")]
    public double Hypot(params double[] values)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        var sum = 0d;
        foreach (var value in values)
        {
            if (double.IsInfinity(value))
            {
                return double.PositiveInfinity;
            }

            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            sum += value * value;
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    ///     Performs 32-bit integer multiplication and returns the lower 32 bits of the result.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The lower 32 bits of the product.</returns>
    [ScriptFunction("imul", "Performs 32-bit integer multiplication and returns the lower 32 bits of the result.")]
    public int Imul(int left, int right)
    {
        return unchecked((int)((uint)left * (uint)right));
    }

    /// <summary>
    ///     Returns the natural logarithm of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The natural logarithm of the specified number.</returns>
    [ScriptFunction("log", "Returns the natural logarithm of the specified number.")]
    public double Log(double value)
    {
        return Math.Log(value);
    }

    /// <summary>
    ///     Returns the natural logarithm of one plus the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The natural logarithm of one plus the specified number.</returns>
    [ScriptFunction("log1p", "Returns the natural logarithm of one plus the specified number.")]
    public double Log1p(double value)
    {
        return Math.Log(1.0 + value);
    }

    /// <summary>
    ///     Returns the base 10 logarithm of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The base 10 logarithm of the specified number.</returns>
    [ScriptFunction("log10", "Returns the base 10 logarithm of the specified number.")]
    public double Log10(double value)
    {
        return Math.Log10(value);
    }

    /// <summary>
    ///     Returns the base 2 logarithm of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The base 2 logarithm of the specified number.</returns>
    [ScriptFunction("log2", "Returns the base 2 logarithm of the specified number.")]
    public double Log2(double value)
    {
        return Math.Log2(value);
    }

    /// <summary>
    ///     Returns the largest of the specified numbers.
    /// </summary>
    /// <param name="values">The numbers to evaluate.</param>
    /// <returns>The largest of the specified numbers.</returns>
    [ScriptFunction("max", "Returns the largest of the specified numbers.")]
    public double Max(params double[] values)
    {
        if (values.Length == 0)
        {
            return double.NegativeInfinity;
        }

        var result = values[0];
        foreach (var value in values)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            if (value > result || double.IsNegativeInfinity(result))
            {
                result = value;
            }
        }

        return result;
    }

    /// <summary>
    ///     Returns the smallest of the specified numbers.
    /// </summary>
    /// <param name="values">The numbers to evaluate.</param>
    /// <returns>The smallest of the specified numbers.</returns>
    [ScriptFunction("min", "Returns the smallest of the specified numbers.")]
    public double Min(params double[] values)
    {
        if (values.Length == 0)
        {
            return double.PositiveInfinity;
        }

        var result = values[0];
        foreach (var value in values)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            if (value < result || double.IsPositiveInfinity(result))
            {
                result = value;
            }
        }

        return result;
    }

    /// <summary>
    ///     Returns the value of the first argument raised to the power of the second.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="exponent">The exponent.</param>
    /// <returns>The base raised to the specified power.</returns>
    [ScriptFunction("pow", "Returns the value of the first argument raised to the power of the second.")]
    public double Pow(double value, double exponent)
    {
        return Math.Pow(value, exponent);
    }

    /// <summary>
    ///     Returns a pseudo-random number greater than or equal to 0.0 and less than 1.0.
    /// </summary>
    /// <returns>A pseudo-random number in the range [0, 1).</returns>
    [ScriptFunction("random", "Returns a pseudo-random number between zero and one.")]
    public double RandomFun()
    {
        return Random.Shared.NextDouble();
    }

    /// <summary>
    ///     Returns the value of a number rounded to the nearest integer.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The number rounded to the nearest integer.</returns>
    [ScriptFunction("round", "Returns the value of a number rounded to the nearest integer.")]
    public double Round(double value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    ///     Returns the sign of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The sign of the specified number.</returns>
    [ScriptFunction("sign", "Returns the sign of the specified number.")]
    public double Sign(double value)
    {
        if (double.IsNaN(value))
        {
            return double.NaN;
        }

        if (value == 0)
        {
            return BitConverter.DoubleToInt64Bits(value) < 0 ? -0.0 : 0.0;
        }

        return value > 0 ? 1.0 : -1.0;
    }

    /// <summary>
    ///     Returns the sine of the specified angle.
    /// </summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The sine of the specified angle.</returns>
    [ScriptFunction("sin", "Returns the sine of the specified angle.")]
    public double Sin(double value)
    {
        return Math.Sin(value);
    }

    /// <summary>
    ///     Returns the hyperbolic sine of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The hyperbolic sine of the specified number.</returns>
    [ScriptFunction("sinh", "Returns the hyperbolic sine of the specified number.")]
    public double Sinh(double value)
    {
        return Math.Sinh(value);
    }

    /// <summary>
    ///     Returns the positive square root of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The positive square root of the specified number.</returns>
    [ScriptFunction("sqrt", "Returns the positive square root of the specified number.")]
    public double Sqrt(double value)
    {
        return Math.Sqrt(value);
    }

    /// <summary>
    ///     Returns the tangent of the specified angle.
    /// </summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The tangent of the specified angle.</returns>
    [ScriptFunction("tan", "Returns the tangent of the specified angle.")]
    public double Tan(double value)
    {
        return Math.Tan(value);
    }

    /// <summary>
    ///     Returns the hyperbolic tangent of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The hyperbolic tangent of the specified number.</returns>
    [ScriptFunction("tanh", "Returns the hyperbolic tangent of the specified number.")]
    public double Tanh(double value)
    {
        return Math.Tanh(value);
    }

    /// <summary>
    ///     Removes the fractional part of the specified number.
    /// </summary>
    /// <param name="value">The number to evaluate.</param>
    /// <returns>The integral part of the specified number.</returns>
    [ScriptFunction("trunc", "Removes the fractional part of the specified number.")]
    public double Trunc(double value)
    {
        return Math.Truncate(value);
    }
}
