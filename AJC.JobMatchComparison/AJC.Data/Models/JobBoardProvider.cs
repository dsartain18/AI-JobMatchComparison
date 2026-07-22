using System;
using System.Collections.Generic;

namespace AJC.Data.Models;

public partial class JobBoardProvider
{
    public int JobBoardProviderId { get; set; }

    public string? JobBoardApplicationId { get; set; }

    public string JobBoardName { get; set; } = null!;

    public string FeedUrl { get; set; } = null!;

    public string? CredentialReference { get; set; }

    public string ExpectedResponseType { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<JobBoardProviderResponse> JobBoardProviderResponses { get; set; } = new List<JobBoardProviderResponse>();
}
