using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ControlPanel.Core.Helpers
{
    public interface IAudioHelper
    {
        #region Audio Curves
        public void AddCurve(int point, int value);
        public float ConvertToCurve(float value);
        public float ConvertFromCurve(float value);
        #endregion

        #region System Volume
        public float GetSystemVolume();
        public void SetSystemVolume(float volume);
        public float StepSystemVolume(float stepAmount);
        public bool GetSystemMute();
        public void SetSystemMute(bool mute);
        public bool ToggleSystemMute();
        #endregion

        #region Application Volume
        public float GetApplicationVolume(string appName);
        public void SetApplicationVolume(string appName, float volume);
        public float StepApplicationVolume(string appName, float stepAmount);
        public bool GetApplicationMute(string appName);
        public void SetApplicationMute(string appName, bool mute);
        public bool ToggleApplicationMute(string appName);
        public IEnumerable<string> GetAudioApps();
        public void RemoveApplication(string appName);
        #endregion
    }

    public class AudioCurveHelper
    {
        internal readonly List<AudioCurvePoint> _curvePoints = new();

        public void AddValue(int point, int value)
        {
            _curvePoints.Add(new AudioCurvePoint(point, value));
        }

        internal bool IsValid()
        {
            if (_curvePoints.Count == 0)
            {
                // A curve with zero points is assumed to be linear from 0 to 100.
                return true;
            }
            else
            {
                // A curve should not contain two values for any given point.
                if (_curvePoints.GroupBy(x => x.InflectionPoint).Any(x => x.Count() > 1))
                {
                    return false;
                }
            }

            // Sort the curve based on inflection points, then inflection values
            _curvePoints.Sort();
            return true;
        }

        public float Interpolate(float value)
        {
            if (!IsValid())
            {
                throw new ArgumentException("Configured points are invalid.");
            }

            AudioCurvePoint? pointBefore = _curvePoints.LastOrDefault(p => p.InflectionPoint <= value);
            AudioCurvePoint? pointAfter = _curvePoints.FirstOrDefault(p => p.InflectionPoint >= value);

            // If either the lower bound or upper bounds are unconfigured, assume {0, 0} and {100, 100} respectively.
            pointBefore ??= new AudioCurvePoint(0, 0);
            pointAfter ??= new AudioCurvePoint(100, 100);

            // If the value is at one of the points, simply return it's configured value
            if (pointBefore.Equals(pointAfter))
            {
                return pointBefore.InflectionValue;
            }
            // Otherwise, use the slope and yIntercept to interpolate the value
            else
            {
                float slope = (pointAfter.InflectionValue - pointBefore.InflectionValue) / (pointAfter.InflectionPoint - pointBefore.InflectionPoint);
                float yIntercept = pointBefore.InflectionValue - slope * pointBefore.InflectionPoint;

                return slope * value + yIntercept;
            }
        }

        public float DeInterpolate(float value)
        {
            if (!IsValid())
            {
                throw new ArgumentException("Configured points are invalid.");
            }

            AudioCurvePoint? pointBefore = _curvePoints.LastOrDefault(p => p.InflectionValue <= value);
            AudioCurvePoint? pointAfter = _curvePoints.FirstOrDefault(p => p.InflectionValue >= value);

            // If either the lower bound or upper bounds are unconfigured, assume {0, 0} and {100, 100} respectively.
            pointBefore ??= new AudioCurvePoint(0, 0);
            pointAfter ??= new AudioCurvePoint(100, 100);

            // If the value is at one of the points, simply return it's configured value
            if (pointBefore.Equals(pointAfter))
            {
                return pointBefore.InflectionPoint;
            }
            // Otherwise, use the slope and xIntercept to interpolate the value
            else
            {
                float slope = (pointAfter.InflectionPoint - pointBefore.InflectionPoint) / (pointAfter.InflectionValue - pointBefore.InflectionValue);
                float xIntercept = pointBefore.InflectionPoint - slope * pointBefore.InflectionValue;

                return slope * value + xIntercept;
            }
        }
    }

    public class AudioCurvePoint : IComparable, IEquatable<AudioCurvePoint>
    {
        public int InflectionPoint;
        public float InflectionValue;

        public AudioCurvePoint() { }

        public AudioCurvePoint(int inflectionPoint, float inflectionValue)
        {
            InflectionPoint = inflectionPoint;
            InflectionValue = inflectionValue;
        }

        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return -1;
            }
            else if (obj is AudioCurvePoint curvePoint)
            {
                if (InflectionPoint == curvePoint.InflectionPoint)
                {
                    return InflectionValue.CompareTo(curvePoint.InflectionValue);
                }
                else
                {
                    return InflectionPoint.CompareTo(curvePoint.InflectionPoint);
                }
            }
            else
            {
                return -1;
            }
        }

        public bool Equals(AudioCurvePoint? other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return InflectionPoint == other.InflectionPoint
                    && InflectionValue == other.InflectionValue;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is AudioCurvePoint other)
            {
                return Equals(other);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return InflectionPoint.GetHashCode() + InflectionValue.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{Inflection Point: {InflectionPoint}, Inflection Value: {InflectionValue}}}";
        }
    }
}
