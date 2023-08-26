using ControlPanel.Core.Helpers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ControlPanel.Core.Tests")]

namespace ControlPanel.Core.Tests
{
    public class CurveTests
    {
        public AudioCurveHelper _curveHelper;

        [SetUp]
        public void Setup()
        {
            _curveHelper = new AudioCurveHelper();
        }

        #region Basic Tests
        [Test]
        public void Test_InvalidCurve()
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(0, 10);

            Assert.That(_curveHelper.IsValid(), Is.False);
        }

        [Test]
        public void Test_PointSort()
        {
            _curveHelper.AddValue(0, 10);
            _curveHelper.AddValue(10, 50);
            _curveHelper.AddValue(100, 10);
            _curveHelper.AddValue(20, 10);

            bool isValid = true;
            AudioCurvePoint? prev = null;
            foreach (var point in _curveHelper._curvePoints)
            {
                if (prev == null)
                {
                    prev = point;
                    continue;
                }
                else if (prev.InflectionPoint > point.InflectionPoint)
                {
                    isValid = false;
                    break;
                }
                else
                {
                    prev = point;
                }
            }

            Assert.That(isValid, Is.False);

            // IsValid sorts the points for us
            _curveHelper.IsValid();

            prev = null;
            isValid = true;
            foreach (var point in _curveHelper._curvePoints)
            {
                if (prev == null)
                {
                    prev = point;
                    continue;
                }
                else if (prev.InflectionPoint > point.InflectionPoint)
                {
                    isValid = false;
                    break;
                }
                else
                {
                    prev = point;
                }
            }

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Test_NoPoints([Range(0, 100)] int x)
        {
            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.ConvertFromCurve(x), Is.EqualTo(x));
                Assert.That(_curveHelper.ConvertToCurve(x), Is.EqualTo(x));

                Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(x));
                Assert.That(_curveHelper.DeInterpolate(x), Is.EqualTo(x));
            });
        }
        #endregion

        #region 0-100 Linear Tests
        [Test]
        public void Test_0_100([Range(0, 100)] int x)
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(100, 100);
            Assert.Multiple(() =>
            {

                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.ConvertFromCurve(x), Is.EqualTo(x));
                Assert.That(_curveHelper.ConvertToCurve(x), Is.EqualTo(x));

                Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(x));
                Assert.That(_curveHelper.DeInterpolate(x), Is.EqualTo(x));
            });
        }

        [Test]
        public void Test_0_50_100([Range(0, 100)] int x)
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(50, 50);
            _curveHelper.AddValue(100, 100);

            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.ConvertFromCurve(x), Is.EqualTo(x));
                Assert.That(_curveHelper.ConvertToCurve(x), Is.EqualTo(x));

                Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(x));
                Assert.That(_curveHelper.DeInterpolate(x), Is.EqualTo(x));
            });
        }

        [Test]
        public void Test_0_50__0_10([Range(0, 50)] int x)
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(50, 10);

            float y = x / 5f;
            Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(y).Within(0.001));
        }

        [Test]
        public void Test_50_100__10_100([Range(50, 100)] int x)
        {
            _curveHelper.AddValue(50, 10);
            float y = x / 5f;

            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.ConvertToCurve(y), Is.EqualTo(x).Within(0.001));
            });
        }

        [Test]
        public void Test_0_10_TO_0_50([Range(0, 10)] int x)
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(10, 50);
            float y = x * 5f;

            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(y).Within(0.001));
            });
        }

        [Test]
        public void Test_0_10_FROM_0_50([Range(0, 10)] int y)
        {
            _curveHelper.AddValue(0, 0);
            _curveHelper.AddValue(10, 50);
            float x = y / 5f;

            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.DeInterpolate(y), Is.EqualTo(x).Within(0.001));
            });
        }
        #endregion

        #region Multiple Line Tests
        [Test]
        public void Test_MultipleCurves([Range(0, 100)] int x)
        {
            _curveHelper.AddValue(0, 10);
            _curveHelper.AddValue(20, 10);
            _curveHelper.AddValue(50, 50);
            _curveHelper.AddValue(100, 100);

            float y = x switch
            {
                >= 0 and <= 20 => 10f,
                > 20 and <= 50 => (4f / 3 * x) - 16.6666f,
                > 50 => x,
                _ => -1f
            };

            Assert.Multiple(() =>
            {
                Assert.That(_curveHelper.IsValid(), Is.True);
                Assert.That(_curveHelper.Interpolate(x), Is.EqualTo(y).Within(0.001));
            });
        }
        #endregion
    }
}