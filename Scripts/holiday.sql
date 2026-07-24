USE [NX-lims Lab Command Sys]
GO

/****** Object:  Table [dbo].[holiday]    Script Date: 2026/7/23 14:05:43 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[holiday](
	[date] [date] NOT NULL,
	[name] [nvarchar](100) NULL,
	[is_makeup] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[holiday] ADD  DEFAULT ((0)) FOR [is_makeup]
GO

