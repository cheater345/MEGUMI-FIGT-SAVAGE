using NUnit.Framework;
using SteelTempest.Progression;
using SteelTempest.Save;

namespace SteelTempest.Tests.Editor
{
    public class PlayerProgressTests
    {
        [Test]
        public void LevelThreshold_ScalesWithLevel()
        {
            var progress = MakeProgress(1, 0);
            Assert.That(progress.LevelUpThreshold(1), Is.EqualTo(100));
            Assert.That(progress.LevelUpThreshold(2), Is.EqualTo(175));
        }

        [Test]
        public void GrantXp_LevelsUpAndGrantsPoints()
        {
            var progress = MakeProgress(1, 90);
            progress.GrantXp(20); // 110 total => crosses threshold

            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.Experience, Is.EqualTo(10));
            Assert.That(progress.SkillPoints, Is.EqualTo(2));
        }

        [Test]
        public void SpendSkillPoint_WithoutPoints_Fails()
        {
            var progress = MakeProgress(1, 0);
            Assert.IsTrue(progress.SkillPoints == 0);
            Assert.IsFalse(progress.SpendSkillPoint());
        }

        private static PlayerProgress MakeProgress(int level, int xp)
        {
            var saves = new SaveManager();
            saves.LoadFromString($"{{\"playerLevel\":{level},\"experience\":{xp},\"skillPoints\":0}}");
            return new PlayerProgress(saves);
        }
    }
}