CREATE TABLE [dbo].[JobBoardProvider] (
    [JobBoardProviderId]    INT             IDENTITY (1, 1) NOT NULL,
    [JobBoardApplicationId] VARCHAR (25)    NULL,
    [JobBoardName]          NVARCHAR (200)  NOT NULL,
    [FeedUrl]               NVARCHAR (2048) NOT NULL,
    [AuthenticationType]    VARCHAR (30)    CONSTRAINT [DF_JobBoardProvider_AuthenticationType] DEFAULT ('None') NOT NULL,
    [CredentialReference]   NVARCHAR (512)  NULL,
    [ApiKeyHeaderName]      NVARCHAR (200)  NULL,
    [ApiKeyQueryParameterName] NVARCHAR (200) NULL,
    [OAuthTokenUrl]         NVARCHAR (2048) NULL,
    [ExpectedResponseType]  VARCHAR (50)    NOT NULL,
    [PaginationType]        VARCHAR (30)    CONSTRAINT [DF_JobBoardProvider_PaginationType] DEFAULT ('None') NOT NULL,
    [PageParameterName]     NVARCHAR (200)  NULL,
    [PageSizeParameterName] NVARCHAR (200)  NULL,
    [PageSize]              INT             NULL,
    [OffsetParameterName]   NVARCHAR (200)  NULL,
    [ContinuationTokenPath] NVARCHAR (1000) NULL,
    [NextPageUrlPath]       NVARCHAR (1000) NULL,
    [MaximumPages]          INT             CONSTRAINT [DF_JobBoardProvider_MaximumPages] DEFAULT ((1)) NOT NULL,
    [MaximumResults]        INT             NULL,
    [MaximumRetryCount]     INT             CONSTRAINT [DF_JobBoardProvider_MaximumRetryCount] DEFAULT ((3)) NOT NULL,
    [InitialRetryDelaySeconds] INT          CONSTRAINT [DF_JobBoardProvider_InitialRetryDelaySeconds] DEFAULT ((1)) NOT NULL,
    [MaximumRetryDelaySeconds] INT          CONSTRAINT [DF_JobBoardProvider_MaximumRetryDelaySeconds] DEFAULT ((30)) NOT NULL,
    [MaximumRateLimitWaitSeconds] INT       CONSTRAINT [DF_JobBoardProvider_MaximumRateLimitWaitSeconds] DEFAULT ((300)) NOT NULL,
    [IsEnabled]             BIT             CONSTRAINT [DF_JobBoardProvider_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDate]           DATETIME2 (3)   CONSTRAINT [DF_JobBoardProvider_CreatedDate] DEFAULT (CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time'))) NOT NULL,
    [ModifiedDate]          DATETIME2 (3)   NULL,
    CONSTRAINT [PK_JobBoardProvider] PRIMARY KEY CLUSTERED ([JobBoardProviderId] ASC),
    CONSTRAINT [CK_JobBoardProvider_AuthenticationType] CHECK ([AuthenticationType] IN ('None', 'ApiKey', 'Bearer', 'OAuth2', 'Basic')),
    CONSTRAINT [CK_JobBoardProvider_AuthenticationConfiguration] CHECK (
        [AuthenticationType] = 'None'
        OR (
            [CredentialReference] IS NOT NULL
            AND LEN(LTRIM(RTRIM([CredentialReference]))) > (0)
            AND ([AuthenticationType] <> 'ApiKey' OR NULLIF(LTRIM(RTRIM([ApiKeyHeaderName])), '') IS NOT NULL OR NULLIF(LTRIM(RTRIM([ApiKeyQueryParameterName])), '') IS NOT NULL)
            AND ([AuthenticationType] <> 'OAuth2' OR NULLIF(LTRIM(RTRIM([OAuthTokenUrl])), '') IS NOT NULL)
        )
    ),
    CONSTRAINT [CK_JobBoardProvider_ExpectedResponseType_NotBlank] CHECK (len(ltrim(rtrim([ExpectedResponseType])))>(0)),
    CONSTRAINT [CK_JobBoardProvider_FeedUrl_NotBlank] CHECK (len(ltrim(rtrim([FeedUrl])))>(0)),
    CONSTRAINT [CK_JobBoardProvider_JobBoardName_NotBlank] CHECK (len(ltrim(rtrim([JobBoardName])))>(0)),
    CONSTRAINT [CK_JobBoardProvider_PaginationType] CHECK ([PaginationType] IN ('None', 'PageNumber', 'OffsetLimit', 'Cursor', 'NextPageUrl', 'LinkHeader')),
    CONSTRAINT [CK_JobBoardProvider_PaginationConfiguration] CHECK (
        ([PaginationType] <> 'PageNumber' OR NULLIF(LTRIM(RTRIM([PageParameterName])), '') IS NOT NULL)
        AND ([PaginationType] <> 'OffsetLimit' OR (NULLIF(LTRIM(RTRIM([OffsetParameterName])), '') IS NOT NULL AND NULLIF(LTRIM(RTRIM([PageSizeParameterName])), '') IS NOT NULL))
        AND ([PaginationType] <> 'Cursor' OR NULLIF(LTRIM(RTRIM([ContinuationTokenPath])), '') IS NOT NULL)
        AND ([PaginationType] <> 'NextPageUrl' OR NULLIF(LTRIM(RTRIM([NextPageUrlPath])), '') IS NOT NULL)
    ),
    CONSTRAINT [CK_JobBoardProvider_PageSize] CHECK ([PageSize] IS NULL OR [PageSize] > (0)),
    CONSTRAINT [CK_JobBoardProvider_ExecutionLimits] CHECK (
        [MaximumPages] > (0)
        AND ([MaximumResults] IS NULL OR [MaximumResults] > (0))
        AND [MaximumRetryCount] >= (0)
        AND [InitialRetryDelaySeconds] >= (0)
        AND [MaximumRetryDelaySeconds] >= [InitialRetryDelaySeconds]
        AND [MaximumRateLimitWaitSeconds] >= (0)
    ),
    CONSTRAINT [UQ_JobBoardProvider_JobBoardName] UNIQUE NONCLUSTERED ([JobBoardName] ASC)
);




GO
