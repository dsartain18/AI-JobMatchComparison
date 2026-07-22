CREATE TABLE [dbo].[JobBoardProvider] (
    [JobBoardProviderId]    INT             IDENTITY (1, 1) NOT NULL,
    [JobBoardApplicationId] VARCHAR (25)    NULL,
    [JobBoardName]          NVARCHAR (200)  NOT NULL,
    [FeedUrl]               NVARCHAR (2048) NOT NULL,
    [CredentialReference]   NVARCHAR (512)  NULL,
    [ExpectedResponseType]  VARCHAR (50)    NOT NULL,
    [IsEnabled]             BIT             CONSTRAINT [DF_JobBoardProvider_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDate]           DATETIME2 (3)   CONSTRAINT [DF_JobBoardProvider_CreatedDate] DEFAULT (CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time'))) NOT NULL,
    [ModifiedDate]          DATETIME2 (3)   NULL,
    CONSTRAINT [PK_JobBoardProvider] PRIMARY KEY CLUSTERED ([JobBoardProviderId] ASC),
    CONSTRAINT [CK_JobBoardProvider_ExpectedResponseType_NotBlank] CHECK (len(ltrim(rtrim([ExpectedResponseType])))>(0)),
    CONSTRAINT [CK_JobBoardProvider_FeedUrl_NotBlank] CHECK (len(ltrim(rtrim([FeedUrl])))>(0)),
    CONSTRAINT [CK_JobBoardProvider_JobBoardName_NotBlank] CHECK (len(ltrim(rtrim([JobBoardName])))>(0)),
    CONSTRAINT [UQ_JobBoardProvider_JobBoardName] UNIQUE NONCLUSTERED ([JobBoardName] ASC)
);




GO
