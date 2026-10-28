using System;
using System.Collections.Generic;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
namespace Aevoria.Tests
{
    public sealed class WorldPreferencesTests
    {
        [Test] public void BindingsRejectOverlappingContextsPreserveDefaultsAndRoundTripWithoutAliasing()
        {
            var defaults=InputBindings.Defaults();InputBindings.Validate(defaults);
            Assert.IsNull(InputBindings.Conflict(defaults,GameAction.Inventory,"E"),"World orbit and Explore inventory may share a key");
            Assert.IsNotNull(InputBindings.Conflict(defaults,GameAction.Forward,"S"));
            Assert.IsNotNull(InputBindings.Conflict(defaults,GameAction.Map,"Escape"));
            var original=new ApplicationSettings();original.Bindings.Add(new KeyBinding(GameAction.Forward,"UpArrow"));
            var copy=original.Copy();copy.Bindings[0].Input="I";Assert.AreEqual("UpArrow",original.Bindings[0].Input);
            var restored=ArchiveCodec.Decode<ApplicationSettings>(ArchiveCodec.Encode(copy));restored.Validate();
            Assert.AreEqual("I",InputBindings.Input(restored.Bindings,GameAction.Forward));
            Assert.AreEqual("M",InputBindings.Input(restored.Bindings,GameAction.Map));
            restored.Bindings.Clear();Assert.AreEqual("W",InputBindings.Input(restored.Bindings,GameAction.Forward));
        }
        [Test] public void ConsoleDispatchesRegisteredHandlersAndReportsUnsupportedInput()
        {
            var registry=new CommandRegistry();Assert.IsTrue(registry.Dispatch("/unknown").Contains("not yet available"));
            registry.Register("echo",args=>string.Join(" ",args));Assert.AreEqual("one two",registry.Dispatch("/ECHO one two"));
            Assert.Throws<ArgumentException>(()=>registry.Register("bad name",args=>""));
        }
        [Test] public void BlankSeedsResolveToDistinctNumericIdentitiesAndExplicitSeedsStayExact()
        {
            var seeds=new HashSet<string>();foreach(string blank in new[]{null,""," ","\t"})
            {string seed=WorldSeed.Resolve(blank);Assert.IsTrue(long.TryParse(seed,out long value)&&value>0);Assert.IsTrue(seeds.Add(seed));}
            foreach(string seed in new[]{"000123","shaping-continents"," user seed "})Assert.AreEqual(seed,WorldSeed.Resolve(seed));
        }
        [Test] public void UnitsConvertAbsoluteTemperaturesAndIntervalsWithoutChangingInternalSettings()
        {
            Assert.AreEqual(1,UnitFormatting.LengthValue(.3048,UnitSystem.Imperial),1e-9);
            Assert.AreEqual(32,UnitFormatting.TemperatureValue(0,UnitSystem.Imperial));
            Assert.AreEqual(212,UnitFormatting.TemperatureValue(100,UnitSystem.Imperial));
            Assert.AreEqual(60,UnitFormatting.SpeedValue(26.8224,UnitSystem.Imperial),1e-8);
            Assert.IsTrue(UnitFormatting.TemperatureDifference(10,UnitSystem.Imperial).StartsWith(18.ToString()));
            var settings=new ApplicationSettings();var copy=settings.Copy();copy.Units=UnitSystem.Imperial;copy.Validate();
            Assert.AreEqual(UnitSystem.Metric,settings.Units);Assert.AreEqual(settings.FlightSpeed,copy.FlightSpeed);
            copy.Units=(UnitSystem)99;Assert.Throws<ArgumentException>(()=>copy.Validate());
        }
    }
}
