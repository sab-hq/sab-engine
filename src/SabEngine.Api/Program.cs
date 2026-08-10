// SabEngine.Api — composition root, and now the minimal human-approval
// web UI (pre-development-checklist.md, PD-30). No JavaScript framework,
// no build step — plain server-rendered HTML, matching the same
// lightweight philosophy sab-kb's own UI already uses.
//
// This is a real, working implementation of the recommend-and-approve
// gate (design doc, Section 2/4.1) — not a mockup. Approve/Decline here
// actually drives WorkflowRunStateMachine and records a real Approval
// row, exactly the way the design doc's data model describes.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SabEngine.Core;
using SabEngine.Data;
using SabEngine.Orchestration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SabEngineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SabEngine")));
builder.Services.AddScoped<WorkflowRunStateMachine>();

var app = builder.Build();

// Plain (non-interpolated) raw strings — no `$` prefix, so braces below
// are just literal CSS syntax, nothing needs escaping. Kept separate
// from the interpolated HTML templates below specifically to avoid
// mixing "literal { }" with "real C# interpolation { }" in the same
// string, which is what caused a real CS9006 build error the first
// time this was written directly inline.
const string HomeCss = """
    body { font-family: system-ui, sans-serif; max-width: 900px; margin: 2rem auto; padding: 0 1rem; color: #222; }
    table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
    th, td { text-align: left; padding: 0.5rem; border-bottom: 1px solid #ddd; }
    .note { background: #fff3cd; padding: 0.75rem 1rem; border-radius: 4px; margin-top: 2rem; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    """;

const string RunCss = """
    body { font-family: system-ui, sans-serif; max-width: 700px; margin: 2rem auto; padding: 0 1rem; color: #222; }
    .reasoning { background: #f5f5f5; padding: 1rem; border-radius: 4px; white-space: pre-wrap; }
    label { display: block; margin-bottom: 1rem; }
    input { padding: 0.4rem; width: 100%; max-width: 300px; box-sizing: border-box; }
    button { padding: 0.6rem 1.2rem; margin-right: 0.5rem; border: none; border-radius: 4px; color: white; cursor: pointer; font-size: 1rem; }
    .approve { background: #2e7d32; }
    .decline { background: #c62828; }
    """;

app.MapGet("/", async (SabEngineDbContext db) =>
{
    var pending = await db.WorkflowRuns
        .Where(w => w.State == WorkflowState.PendingApproval)
        .OrderBy(w => w.CreatedAt)
        .ToListAsync();

    var rows = pending.Count == 0
        ? "<tr><td colspan=\"4\"><em>Nothing pending approval right now.</em></td></tr>"
        : string.Join("", pending.Select(w => $"""
            <tr>
              <td>{w.WorkflowId}</td>
              <td>{w.Target}</td>
              <td>{w.CreatedAt:u}</td>
              <td><a href="/runs/{w.Id}">Review &rarr;</a></td>
            </tr>
            """));

    var html = $"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8" />
          <title>sab-engine — Pending Approvals</title>
          <style>{HomeCss}</style>
        </head>
        <body>
          <h1>sab-engine — Pending Approvals</h1>
          <p>Recommend-and-approve mode: nothing runs against a real server until a human approves it here.</p>
          <table>
            <tr><th>Workflow</th><th>Target</th><th>Requested</th><th></th></tr>
            {rows}
          </table>
          <div class="note">
            <strong>No real workflow trigger exists yet</strong> — the orchestration engine doesn't
            call modules in sequence yet (still a stub, see pre-development-checklist.md). Use the
            button below to create a demo run, clearly not real data, just to exercise this page.
            <form method="post" action="/demo/create" style="margin-top: 0.5rem;">
              <button type="submit">Create a demo run</button>
            </form>
          </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

app.MapPost("/demo/create", async (SabEngineDbContext db, WorkflowRunStateMachine stateMachine) =>
{
    var run = await stateMachine.RequestAsync("patch-windows-server", "srv-01-demo", actor: "demo-seed");
    await stateMachine.TransitionAsync(run.Id, WorkflowState.PlanDrafted, actor: "demo-seed");

    db.Plans.Add(new Plan
    {
        WorkflowRunId = run.Id,
        Steps =
        [
            new ProposedModuleStep { ModuleId = "pre-flight-check", ModuleVersion = "1.0.0", Parameters = new Dictionary<string, object?>() },
        ],
        Reasoning = "DEMO DATA — not a real plan. This server has no prior issues on record; proposing the standard pre-flight health check as a first step.",
        IsFlaggedUnusual = false,
    });
    await db.SaveChangesAsync();

    await stateMachine.TransitionAsync(run.Id, WorkflowState.PendingApproval, actor: "demo-seed");

    return Results.Redirect("/");
});

app.MapGet("/runs/{id:guid}", async (Guid id, SabEngineDbContext db) =>
{
    var run = await db.WorkflowRuns.FindAsync(id);
    if (run is null)
    {
        return Results.NotFound("No run found with that ID.");
    }

    var plan = await db.Plans
        .Where(p => p.WorkflowRunId == id)
        .OrderByDescending(p => p.CreatedAt)
        .FirstOrDefaultAsync();

    var stepsHtml = plan is null
        ? "<li><em>No plan found for this run.</em></li>"
        : string.Join("", plan.Steps.Select(s => $"<li><strong>{s.ModuleId}</strong> (v{s.ModuleVersion})</li>"));

    var html = $"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8" />
          <title>Review: {run.WorkflowId}</title>
          <style>{RunCss}</style>
        </head>
        <body>
          <p><a href="/">&larr; Back to pending approvals</a></p>
          <h1>{run.WorkflowId}</h1>
          <p><strong>Target:</strong> {run.Target}</p>
          <p><strong>Proposed steps:</strong></p>
          <ol>{stepsHtml}</ol>
          <p><strong>Agent's reasoning:</strong></p>
          <div class="reasoning">{plan?.Reasoning}</div>
          <form method="post" action="/runs/{run.Id}/decide" style="margin-top: 2rem;">
            <label>Your name:
              <input type="text" name="approver" required />
            </label>
            <button type="submit" name="decision" value="approve" class="approve">Approve</button>
            <button type="submit" name="decision" value="decline" class="decline">Decline</button>
          </form>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

app.MapPost("/runs/{id:guid}/decide", async (Guid id, HttpRequest request, SabEngineDbContext db, WorkflowRunStateMachine stateMachine) =>
{
    var form = await request.ReadFormAsync();
    var approver = form["approver"].ToString();
    var decision = form["decision"].ToString();

    var plan = await db.Plans
        .Where(p => p.WorkflowRunId == id)
        .OrderByDescending(p => p.CreatedAt)
        .FirstOrDefaultAsync();

    if (plan is null)
    {
        return Results.NotFound("No plan found for this run.");
    }

    var wasApproved = decision == "approve";
    var approverName = string.IsNullOrWhiteSpace(approver) ? "unknown" : approver;

    // Section 4.1's data model: a real Approval row, recording who
    // decided and which plan they saw — not just a state flip.
    db.Approvals.Add(new Approval
    {
        PlanId = plan.Id,
        WasApproved = wasApproved,
        ApprovedByUserId = approverName,
    });
    await db.SaveChangesAsync();

    await stateMachine.TransitionAsync(
        id,
        wasApproved ? WorkflowState.Approved : WorkflowState.Declined,
        actor: approverName);

    return Results.Redirect("/");
});

app.Run();
