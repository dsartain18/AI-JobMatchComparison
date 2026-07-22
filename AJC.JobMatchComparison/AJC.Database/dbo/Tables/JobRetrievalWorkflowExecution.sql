CREATE TABLE [dbo].[JobRetrievalWorkflowExecution] (
    [WorkflowExecutionId] UNIQUEIDENTIFIER CONSTRAINT [DF_JobRetrievalWorkflowExecution_WorkflowExecutionId] DEFAULT (newsequentialid()) NOT NULL,
    [StartedDate]         DATETIME2 (3)    CONSTRAINT [DF_JobRetrievalWorkflowExecution_StartedDate] DEFAULT (CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time'))) NOT NULL,
    [CompletedDate]       DATETIME2 (3)    NULL,
    [ExecutionStatus]     VARCHAR (30)     CONSTRAINT [DF_JobRetrievalWorkflowExecution_ExecutionStatus] DEFAULT ('Started') NOT NULL,
    [ProvidersAttempted]  INT              CONSTRAINT [DF_JobRetrievalWorkflowExecution_ProvidersAttempted] DEFAULT ((0)) NOT NULL,
    [ProvidersSucceeded]  INT              CONSTRAINT [DF_JobRetrievalWorkflowExecution_ProvidersSucceeded] DEFAULT ((0)) NOT NULL,
    [ProvidersFailed]     INT              CONSTRAINT [DF_JobRetrievalWorkflowExecution_ProvidersFailed] DEFAULT ((0)) NOT NULL,
    [FailureMessage]      NVARCHAR (4000)  NULL,
    CONSTRAINT [PK_JobRetrievalWorkflowExecution] PRIMARY KEY CLUSTERED ([WorkflowExecutionId] ASC),
    CONSTRAINT [CK_JobRetrievalWorkflowExecution_CompletedDate] CHECK ([CompletedDate] IS NULL OR [CompletedDate]>=[StartedDate]),
    CONSTRAINT [CK_JobRetrievalWorkflowExecution_Counts] CHECK ([ProvidersAttempted]>=(0) AND [ProvidersSucceeded]>=(0) AND [ProvidersFailed]>=(0) AND ([ProvidersSucceeded]+[ProvidersFailed])<=[ProvidersAttempted]),
    CONSTRAINT [CK_JobRetrievalWorkflowExecution_Status] CHECK ([ExecutionStatus]='Failed' OR [ExecutionStatus]='CompletedWithErrors' OR [ExecutionStatus]='Completed' OR [ExecutionStatus]='Started')
);

