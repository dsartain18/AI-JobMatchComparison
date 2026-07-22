CREATE TABLE [dbo].[JobBoardProviderResponse] (
    [JobBoardProviderResponseId] BIGINT           IDENTITY (1, 1) NOT NULL,
    [WorkflowExecutionId]        UNIQUEIDENTIFIER NOT NULL,
    [JobBoardProviderId]         INT              NOT NULL,
    [JobSearchCriteriaId]        INT              NULL,
    [JobBoardName]               NVARCHAR (200)   NOT NULL,
    [RequestUrl]                 NVARCHAR (2048)  NOT NULL,
    [PageSequenceNumber]         INT              CONSTRAINT [DF_JobBoardProviderResponse_PageSequenceNumber] DEFAULT ((1)) NOT NULL,
    [AttemptCount]               INT              CONSTRAINT [DF_JobBoardProviderResponse_AttemptCount] DEFAULT ((1)) NOT NULL,
    [ResultCount]                INT              NULL,
    [ContinuationValueHash]      BINARY (32)      NULL,
    [RetryDelayMilliseconds]     BIGINT           CONSTRAINT [DF_JobBoardProviderResponse_RetryDelayMilliseconds] DEFAULT ((0)) NOT NULL,
    [RateLimitWaitMilliseconds]  BIGINT           CONSTRAINT [DF_JobBoardProviderResponse_RateLimitWaitMilliseconds] DEFAULT ((0)) NOT NULL,
    [RequestStartedDate]         DATETIME2 (3)    NOT NULL,
    [RequestCompletedDate]       DATETIME2 (3)    NULL,
    [DurationMilliseconds]       BIGINT           NULL,
    [HttpStatusCode]             SMALLINT         NULL,
    [ResponseContentType]        NVARCHAR (255)   NULL,
    [ResponseHeaders]            NVARCHAR (MAX)   NULL,
    [RawResponseBody]            NVARCHAR (MAX)   NULL,
    [WasSuccessful]              BIT              CONSTRAINT [DF_JobBoardProviderResponse_WasSuccessful] DEFAULT ((0)) NOT NULL,
    [FailureType]                NVARCHAR (200)   NULL,
    [FailureMessage]             NVARCHAR (4000)  NULL,
    [CreatedDate]                DATETIME2 (3)    CONSTRAINT [DF_JobBoardProviderResponse_CreatedDate] DEFAULT (CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time'))) NOT NULL,
    CONSTRAINT [PK_JobBoardProviderResponse] PRIMARY KEY CLUSTERED ([JobBoardProviderResponseId] ASC),
    CONSTRAINT [CK_JobBoardProviderResponse_CompletionDate] CHECK ([RequestCompletedDate] IS NULL OR [RequestCompletedDate]>=[RequestStartedDate]),
    CONSTRAINT [CK_JobBoardProviderResponse_Duration] CHECK ([DurationMilliseconds] IS NULL OR [DurationMilliseconds]>=(0)),
    CONSTRAINT [CK_JobBoardProviderResponse_HttpStatusCode] CHECK ([HttpStatusCode] IS NULL OR [HttpStatusCode]>=(100) AND [HttpStatusCode]<=(599)),
    CONSTRAINT [CK_JobBoardProviderResponse_JobBoardName_NotBlank] CHECK (len(ltrim(rtrim([JobBoardName])))>(0)),
    CONSTRAINT [CK_JobBoardProviderResponse_PageMetadata] CHECK (
        [PageSequenceNumber] > (0)
        AND [AttemptCount] > (0)
        AND ([ResultCount] IS NULL OR [ResultCount] >= (0))
        AND [RetryDelayMilliseconds] >= (0)
        AND [RateLimitWaitMilliseconds] >= (0)
    ),
    CONSTRAINT [CK_JobBoardProviderResponse_RequestUrl_NotBlank] CHECK (len(ltrim(rtrim([RequestUrl])))>(0)),
    CONSTRAINT [FK_JobBoardProviderResponse_JobBoardProvider] FOREIGN KEY ([JobBoardProviderId]) REFERENCES [dbo].[JobBoardProvider] ([JobBoardProviderId]),
    CONSTRAINT [FK_JobBoardProviderResponse_JobSearchCriteria] FOREIGN KEY ([JobSearchCriteriaId]) REFERENCES [dbo].[JobSearchCriteria] ([JobSearchCriteriaId]),
    CONSTRAINT [FK_JobBoardProviderResponse_WorkflowExecution] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [dbo].[JobRetrievalWorkflowExecution] ([WorkflowExecutionId])
);


GO
CREATE NONCLUSTERED INDEX [IX_JobBoardProviderResponse_JobBoardProviderId_RequestStartedDate]
    ON [dbo].[JobBoardProviderResponse]([JobBoardProviderId] ASC, [RequestStartedDate] DESC)
    INCLUDE([WorkflowExecutionId], [HttpStatusCode], [DurationMilliseconds], [WasSuccessful]);


GO
CREATE NONCLUSTERED INDEX [IX_JobBoardProviderResponse_WorkflowExecutionId]
    ON [dbo].[JobBoardProviderResponse]([WorkflowExecutionId] ASC, [JobBoardProviderResponseId] ASC)
    INCLUDE([JobBoardProviderId], [JobSearchCriteriaId], [JobBoardName], [PageSequenceNumber], [RequestStartedDate], [RequestCompletedDate], [HttpStatusCode], [WasSuccessful]);


GO
CREATE NONCLUSTERED INDEX [IX_JobBoardProviderResponse_WorkflowProviderCriteriaPage]
    ON [dbo].[JobBoardProviderResponse]([WorkflowExecutionId] ASC, [JobBoardProviderId] ASC, [JobSearchCriteriaId] ASC, [PageSequenceNumber] ASC)
    INCLUDE([AttemptCount], [ResultCount], [HttpStatusCode], [WasSuccessful], [DurationMilliseconds], [RetryDelayMilliseconds], [RateLimitWaitMilliseconds]);

