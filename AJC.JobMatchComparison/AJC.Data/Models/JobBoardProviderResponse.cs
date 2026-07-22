using System;
using System.Collections.Generic;

namespace AJC.Data.Models;

public partial class JobBoardProviderResponse
{
    public long JobBoardProviderResponseId { get; set; }

    public Guid WorkflowExecutionId { get; set; }

    public int JobBoardProviderId { get; set; }

    public string JobBoardName { get; set; } = null!;

    public string RequestUrl { get; set; } = null!;

    public DateTime RequestStartedDate { get; set; }

    public DateTime? RequestCompletedDate { get; set; }

    public long? DurationMilliseconds { get; set; }

    public short? HttpStatusCode { get; set; }

    public string? ResponseContentType { get; set; }

    public string? ResponseHeaders { get; set; }

    public string? RawResponseBody { get; set; }

    public bool WasSuccessful { get; set; }

    public string? FailureType { get; set; }

    public string? FailureMessage { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual JobBoardProvider JobBoardProvider { get; set; } = null!;

    public virtual JobRetrievalWorkflowExecution WorkflowExecution { get; set; } = null!;
}
