using Fy.ScriptableSettings.Editor;
using NUnit.Framework;

namespace Fy.ScriptableSettings.EditorTests
{
    public sealed class SettingsDrawerRegistryTests
    {
        [Test]
        public void GetDrawer_ForTypeWithCustomDrawer_ReturnsCustomDrawer()
        {
            ISettingsDrawer drawer = SettingsDrawerRegistry.GetDrawer(typeof(CustomDrawnSettings));

            Assert.IsInstanceOf<CustomDrawnSettingsDrawer>(drawer);
        }

        [Test]
        public void GetDrawer_ForTypeWithoutDrawer_ReturnsDefaultDrawer()
        {
            ISettingsDrawer drawer = SettingsDrawerRegistry.GetDrawer(typeof(UndrawnSettings));

            Assert.IsInstanceOf<DefaultSettingsDrawer>(drawer);
        }
    }
}
