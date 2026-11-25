/*
  Loan Management System
  EMI payment metadata and audit-safe reversal migration.
  Run against LoanManagementDB after CreateLoanManagementTables.sql.
*/

USE LoanManagementDB;
GO

IF OBJECT_ID(N'dbo.Seq_EMIReceiptNo', N'SO') IS NULL
BEGIN
	CREATE SEQUENCE dbo.Seq_EMIReceiptNo
		AS BIGINT
		START WITH 1
		INCREMENT BY 1;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ReceiptNumber') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ReceiptNumber VARCHAR(40) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'PaymentMode') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD PaymentMode VARCHAR(20) NOT NULL
			CONSTRAINT DF_EMICollections_PaymentMode DEFAULT ('Cash');
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'PaymentReference') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD PaymentReference VARCHAR(100) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'Remarks') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD Remarks NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'IsReversed') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD IsReversed BIT NOT NULL
			CONSTRAINT DF_EMICollections_IsReversed DEFAULT (0);
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ReversalReason') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ReversalReason NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ReversedBy') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ReversedBy NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ReversedOn') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ReversedOn DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ModifiedBy') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ModifiedBy NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ModifiedOn') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ModifiedOn DATETIME2(0) NULL;
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.indexes
	WHERE name = N'UX_EMICollections_ReceiptNumber'
	  AND object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	CREATE UNIQUE INDEX UX_EMICollections_ReceiptNumber
		ON dbo.EMICollections(ReceiptNumber)
		WHERE ReceiptNumber IS NOT NULL;
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.check_constraints
	WHERE name = N'CK_EMICollections_PaymentMode'
	  AND parent_object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD CONSTRAINT CK_EMICollections_PaymentMode
		CHECK (PaymentMode IN ('Cash', 'UPI', 'Bank Transfer', 'Cheque', 'Other'));
END;
GO

PRINT 'EMI payment metadata and reversal audit fields are ready.';
GO
