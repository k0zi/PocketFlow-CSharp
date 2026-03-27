using PocketFlow;

namespace A2a;

/// <summary>
/// Constructs the expense-reimbursement PocketFlow pipeline.
/// Ported from <c>flow.py</c>.
/// </summary>
public static class ExpenseFlow
{
    /// <summary>
    /// Creates and wires the expense-reimbursement flow:
    /// <code>
    /// ExtractInfo ──classify──▶ ClassifyExpense ──check_policy──▶ CheckPolicy ──approved──▶ PrepareResponse
    ///             └──respond──▶ PrepareResponse  └──respond─────▶ PrepareResponse
    ///                                                            └──rejected──▶ PrepareResponse
    ///                                                            └──more_info─▶ PrepareResponse
    /// </code>
    /// </summary>
    public static Flow Create()
    {
        var extract         = new ExtractInfoNode();
        var classify        = new ClassifyExpenseNode();
        var checkPolicy     = new CheckPolicyNode();
        var prepareResponse = new PrepareResponseNode();

        extract.On("classify").Then(classify);
        extract.On("respond").Then(prepareResponse);

        classify.On("check_policy").Then(checkPolicy);
        classify.On("respond").Then(prepareResponse);

        checkPolicy.On("approved").Then(prepareResponse);
        checkPolicy.On("rejected").Then(prepareResponse);
        checkPolicy.On("more_info").Then(prepareResponse);

        return new Flow(start: extract);
    }
}

