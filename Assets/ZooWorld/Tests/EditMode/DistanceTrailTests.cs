using NUnit.Framework;
using UnityEngine;

namespace ZooWorld.Tests
{
    public sealed class DistanceTrailTests
    {
        [Test] public void InitialBodyIsEvenlySpacedBehindHead()
        {
            var trail = new DistanceTrail();
            trail.Reset(new Vector3(5, 0, 3), Vector3.forward, 4);
            Assert.That(Vector3.Distance(trail.Sample(2.5f), new Vector3(5, 0, 0.5f)), Is.LessThan(0.001f));
        }

        [Test] public void TailFollowsTheCornerRatherThanCuttingAcrossIt()
        {
            var trail = new DistanceTrail(); trail.Reset(Vector3.zero, Vector3.right, 4);
            for (int i = 1; i <= 20; i++) trail.Record(new Vector3(i * 0.05f, 0, 0));
            for (int i = 1; i <= 20; i++) trail.Record(new Vector3(1, 0, i * 0.05f));
            Assert.That(Vector3.Distance(trail.Sample(0.5f), new Vector3(1, 0, 0.5f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(trail.Sample(1.5f), new Vector3(0.5f, 0, 0)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(trail.Sample(2.5f), new Vector3(-0.5f, 0, 0)), Is.LessThan(0.001f));
        }

        [Test] public void StationarySolverJitterCannotConsumeTailHistory()
        {
            var trail = new DistanceTrail(); trail.Reset(Vector3.zero, Vector3.forward, 4);
            int count = trail.Count;
            for (int i = 0; i < 10000; i++) trail.Record(new Vector3(Mathf.Sin(i) * 0.005f, 0, 0));
            Assert.That(trail.Count, Is.EqualTo(count));
            Assert.That(Vector3.Distance(trail.Sample(3), Vector3.back * 3), Is.LessThan(0.001f));
        }

        [Test] public void RingBufferRemainsBoundedAndSamplesAcrossWrap()
        {
            var trail = new DistanceTrail(64); trail.Reset(Vector3.zero, Vector3.right, 3);
            for (int i = 1; i <= 1000; i++) trail.Record(Vector3.right * (i * 0.05f));
            Assert.That(trail.Count, Is.EqualTo(trail.Capacity));
            Assert.That(Vector3.Distance(trail.Sample(2), Vector3.right * 48), Is.LessThan(0.001f));
        }

        [Test] public void ResetDropsThePreviousLivesPath()
        {
            var trail = new DistanceTrail(); trail.Reset(Vector3.zero, Vector3.forward, 4);
            trail.Record(Vector3.right * 20);
            trail.Reset(Vector3.left * 8, Vector3.right, 4);
            Assert.That(Vector3.Distance(trail.Sample(3), Vector3.left * 11), Is.LessThan(0.001f));
        }

        [Test] public void RandomSpawnFitsBothHeadAndLongTail()
        {
            var bounds = new HabitatBounds(new Vector2(24, 13), null);
            var random = new Domain.SeededRandom(2026);
            for (int i = 0; i < 1000; i++)
            {
                Vector3 heading = Quaternion.Euler(0, random.Range(0, 360f), 0) * Vector3.forward;
                Vector3 head = bounds.RandomPoint(random, heading * 3.36f);
                Vector3 tail = head - heading * 3.36f;
                Assert.That(Mathf.Abs(head.x), Is.LessThanOrEqualTo(11.001f));
                Assert.That(Mathf.Abs(head.z), Is.LessThanOrEqualTo(5.501f));
                Assert.That(Mathf.Abs(tail.x), Is.LessThanOrEqualTo(11.001f));
                Assert.That(Mathf.Abs(tail.z), Is.LessThanOrEqualTo(5.501f));
            }
        }
    }
}
