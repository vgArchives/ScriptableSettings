using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fy.ScriptableSettings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fy.ScriptableSettings.RuntimeTests
{
    public sealed class ScriptableSettingsRegistryTests
    {
        private readonly List<ScriptableSettings> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableSettings instance in _created)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            _created.Clear();
        }

        [Test]
        public void TryGet_AfterCreate_ReturnsRegisteredInstance()
        {
            SampleSettings settings = Create<SampleSettings>();

            Assert.IsTrue(ScriptableSettingsRegistry.TryGet(out SampleSettings result));
            Assert.AreSame(settings, result);
        }

        [Test]
        public void TryGet_AfterDestroy_ReturnsFalse()
        {
            SampleSettings settings = Create<SampleSettings>();
            Object.DestroyImmediate(settings);

            Assert.IsFalse(ScriptableSettingsRegistry.TryGet(out SampleSettings _));
        }

        [Test]
        public void TryGet_WhenNeverRegistered_ReturnsFalse()
        {
            Assert.IsFalse(ScriptableSettingsRegistry.TryGet(out MissingSettings _));
        }

        [Test]
        public void Get_WhenMissing_LogsErrorAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("preloaded"));

            Assert.IsNull(ScriptableSettingsRegistry.Get<MissingSettings>());
        }

        [Test]
        public void Set_SecondInstanceOfSameType_WarnsAndKeepsFirst()
        {
            OtherSettings first = Create<OtherSettings>();

            LogAssert.Expect(LogType.Warning, new Regex("is already registered"));
            OtherSettings second = Create<OtherSettings>();

            Assert.IsTrue(ScriptableSettingsRegistry.TryGet(out OtherSettings current));
            Assert.AreSame(first, current);
            Assert.AreNotSame(second, current);
        }

        private T Create<T>() where T : ScriptableSettings
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);

            return instance;
        }

        private sealed class SampleSettings : ScriptableSettings
        {
        }

        private sealed class OtherSettings : ScriptableSettings
        {
        }

        private sealed class MissingSettings : ScriptableSettings
        {
        }
    }
}
