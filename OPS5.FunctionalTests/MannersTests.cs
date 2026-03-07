using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class MannersTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public MannersTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "Manners");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "Manners.ops5");

        success.Should().BeTrue("the Manners OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_GuestsSeated()
    {
        await _engine.LoadAndRun(_projectDir, "Manners.ops5");

        _engine.GetObjects("seating").Should().HaveCountGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AllGuestsChosen()
    {
        await _engine.LoadAndRun(_projectDir, "Manners.ops5");

        var guests = _engine.GetObjects("guest");
        guests.Should().HaveCount(8, "there should be 8 guests");

        foreach (var guest in guests)
        {
            var chosen = guest.GetAttributeValue("chosen");
            chosen.Should().NotBeNull("every guest should have a chosen value");
            chosen.Should().NotBe("NIL", "every guest should have been chosen");
        }
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_SequentialSeatNumbers()
    {
        await _engine.LoadAndRun(_projectDir, "Manners.ops5");

        var seatings = _engine.GetObjects("seating");
        seatings.Should().HaveCountGreaterThan(0);

        var seatNumbers = seatings
            .Select(s => int.Parse(s.GetAttributeValue("seat-number")!))
            .OrderBy(n => n)
            .ToList();

        for (int i = 0; i < seatNumbers.Count; i++)
        {
            seatNumbers[i].Should().Be(i + 1, "seat numbers should be sequential starting from 1");
        }
    }

    public void Dispose() => _engine.Dispose();
}
