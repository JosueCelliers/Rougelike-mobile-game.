using BadLie.Bot;
using BadLie.Course;
using BadLie.Holes;
using BadLie.Sim;
using NUnit.Framework;

namespace BadLie.Tests
{
    /// <summary>
    /// Completability: every pin of every hole can be reached by rolling (no carry over water
    /// required), and the shot-planner bot holes every hole close to par without upgrades.
    /// The full statistics (all loadouts, noisy bots, whole runs) come from the editor tool
    /// BadLie.EditorTools.Playtest; see Docs/Tuning.md.
    /// </summary>
    public class PlaytestTests
    {
        [Test]
        public void EveryPinHasARollingRouteFromTheTee()
        {
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var def = HoleLibrary.Get(h);
                for (int v = -1; v < def.AltCups.Count; v++)
                {
                    var course = new CourseModel(HoleLibrary.Get(h), null, v);
                    var field = new DistanceField(course);
                    float d = field.Distance(course.Tee);
                    Assert.Less(d, 300f, def.Name + " pin " + v + ": no rolling route from the tee to the cup");
                }
            }
        }

        [Test]
        public void BotHolesEveryHoleWithinParPlusOne()
        {
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var def = HoleLibrary.Get(h);
                var course = new CourseModel(def);
                var run = BotPlayer.Play(course, ShotModifiers.None, def.Par + 4, 0f, 0f, 1);
                Assert.IsTrue(run.Holed, def.Name + ": the bot did not hole out");
                Assert.LessOrEqual(run.Strokes + run.Penalties, def.Par + 1, def.Name + ": " + (run.Strokes + run.Penalties) + " strokes");
            }
        }
    }
}
