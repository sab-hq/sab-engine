using SabEngine.Core;
using Xunit;

namespace SabEngine.Core.Tests;

/// <summary>
/// First real test — confirms the state machine's enum values exist and
/// are spelled the way docs/SAB_Design_Document_v0.1.2.md, Section 4.1
/// specifies. Intentionally trivial: this is here to prove the test
/// project itself is wired up correctly (PD-2), not to test real
/// orchestration logic yet.
/// </summary>
public class WorkflowStateTests
{
    [Fact]
    public void All_nine_states_from_the_design_doc_exist()
    {
        var expected = new[]
        {
            WorkflowState.Requested,
            WorkflowState.PlanDrafted,
            WorkflowState.PendingApproval,
            WorkflowState.Approved,
            WorkflowState.Declined,
            WorkflowState.Executing,
            WorkflowState.Completed,
            WorkflowState.Failed,
            WorkflowState.RolledBack,
        };

        Assert.Equal(9, expected.Length);
        Assert.Equal(9, Enum.GetValues<WorkflowState>().Length);
    }
}
