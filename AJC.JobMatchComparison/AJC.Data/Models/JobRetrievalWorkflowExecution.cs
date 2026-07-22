using System;
using System.Collections.Generic;

namespace AJC.Data.Models;

public partial class JobRetrievalWorkflowExecution
{
    public Guid WorkflowExecutionId { get; set; }

    public DateTime StartedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string ExecutionStatus { get; set; } = null!;

    public int ProvidersAttempted { get; set; }

    public int ProvidersSucceeded { get; set; }

    public int ProvidersFailed { get; set; }

    public string? FailureMessage { get; set; }

    public virtual ICollection<JobBoardProviderResponse> JobBoardProviderResponses { get; set; } = new List<JobBoardProviderResponse>();
}
