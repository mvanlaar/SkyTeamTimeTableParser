-- Create Table for Flight Import

USE [CI-Import]
GO

/****** Object:  Table [dbo].[Flights]    Script Date: 5-9-2015 21:42:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Flights](
	[FlightID] [bigint] IDENTITY(1,1) NOT NULL,
	[FlightSource] [nvarchar](20) NULL,
	[FromIATA] [nchar](3) NULL,
	[ToIATA] [nchar](3) NULL,
	[FromDate] [datetime] NULL,
	[ToDate] [datetime] NULL,
	[FlightMonday] [bit] NULL,
	[FlightTuesday] [bit] NULL,
	[FlightWednesday] [bit] NULL,
	[FlightThursday] [bit] NULL,
	[FlightFriday] [bit] NULL,
	[FlightSaterday] [bit] NULL,
	[FlightSunday] [bit] NULL,
	[DepartTime] [time](7) NULL,
	[ArrivalTime] [time](7) NULL,
	[FlightNumber] [nvarchar](7) NULL,
	[FlightAirline] [nvarchar](50) NULL,
	[FlightOperator] [nvarchar](50) NULL,
	[FlightAircraft] [nvarchar](30) NULL,
	[FlightCodeShare] [bit] NULL,
	[FlightNextDayArrival] [bit] NULL,
	[FlightDuration] [nvarchar](50) NULL,
	[FlightNextDays] [int] NULL,
 CONSTRAINT [PK_Flights_1] PRIMARY KEY CLUSTERED 
(
	[FlightID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

USE [CI-Import]
GO

/****** Object:  StoredProcedure [dbo].[InsertFlight]    Script Date: 5-9-2015 21:42:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		AuthorName
-- Create date: Create Date
-- Description:	Description
-- =============================================
CREATE PROCEDURE [dbo].[InsertFlight] 
	@FlightSource nvarchar(20),
    @FromIATA nchar(3),
    @ToIATA nchar(3),
    @FromDate datetime,
    @ToDate datetime,
    @FlightMonday bit,
    @FlightTuesday bit,
    @FlightWednesday bit,
    @FlightThursday bit,
    @FlightFriday bit,
    @FlightSaterday bit,
    @FlightSunday bit,
    @DepartTime time(7),
    @ArrivalTime time(7),
    @FlightNumber nvarchar(7),
    @FlightAirline nvarchar(50),
    @FlightOperator nvarchar(50),
    @FlightAircraft nvarchar(30),
    @FlightCodeShare bit,
    @FlightNextDayArrival bit,
    @FlightDuration nvarchar(50),
    @FlightNextDays int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
INSERT INTO [dbo].[Flights]
           ([FlightSource],
           [FromIATA],
           [ToIATA],
           [FromDate],
           [ToDate],
           [FlightMonday],
           [FlightTuesday],
           [FlightWednesday],
           [FlightThursday],
           [FlightFriday],
           [FlightSaterday],
           [FlightSunday],
           [DepartTime],
           [ArrivalTime],
           [FlightNumber],
           [FlightAirline],
           [FlightOperator],
           [FlightAircraft],
           [FlightCodeShare],
           [FlightNextDayArrival],
           [FlightDuration],
           [FlightNextDays])
     VALUES
           (@FlightSource,
           @FromIATA,
           @ToIATA,
           @FromDate,
           @ToDate,
           @FlightMonday,
           @FlightTuesday,
           @FlightWednesday,
           @FlightThursday,
           @FlightFriday,
           @FlightSaterday,
           @FlightSunday,
           @DepartTime,
           @ArrivalTime,
           @FlightNumber,
           @FlightAirline,
           @FlightOperator,
           @FlightAircraft,
           @FlightCodeShare,
           @FlightNextDayArrival,
           @FlightDuration,
           @FlightNextDays)

END

GO



