using AwesomeAssertions;
using NetArchTest.Rules;

namespace Padel.ArchitectureTests;

internal static class ArchitectureAssertions
{
    /// <summary>Asserts that a NetArchTest rule holds; on failure the message lists the offending types.</summary>
    public static void ShouldPass(this ConditionList conditions)
    {
        var offenders = conditions.GetResult().FailingTypeNames ?? [];

        offenders.Should().BeEmpty("these types break the rule");
    }
}
