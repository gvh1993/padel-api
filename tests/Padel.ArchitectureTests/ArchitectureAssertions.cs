using NetArchTest.Rules;

namespace Padel.ArchitectureTests;

internal static class ArchitectureAssertions
{
    /// <summary>Asserts that a NetArchTest rule holds; on failure the message lists the offending types.</summary>
    public static async Task ShouldPass(this ConditionList conditions)
    {
        var offenders = conditions.GetResult().FailingTypeNames ?? [];

        await Assert.That(offenders).IsEmpty();
    }
}
