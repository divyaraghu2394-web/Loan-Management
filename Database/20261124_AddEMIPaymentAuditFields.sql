/*
  Loan Management System
  EMI payment metadata and audit-safe reversal migration.
  Run against LoanManagementDB after CreateLoanManagementTables.sql.
*/

USE LoanManagementDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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

IF COL_LENGTH(N'dbo.EMICollections', N'EntryType') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD EntryType VARCHAR(20) NOT NULL
			CONSTRAINT DF_EMICollections_EntryType DEFAULT ('Collection');
END;
GO

IF COL_LENGTH(N'dbo.EMICollections', N'ParentCollectionId') IS NULL
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD ParentCollectionId BIGINT NULL;
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.foreign_keys
	WHERE name = N'FK_EMICollections_ParentCollection'
	  AND parent_object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD CONSTRAINT FK_EMICollections_ParentCollection
		FOREIGN KEY (ParentCollectionId)
		REFERENCES dbo.EMICollections(CollectionId);
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.check_constraints
	WHERE name = N'CK_EMICollections_EntryType'
	  AND parent_object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	ALTER TABLE dbo.EMICollections
		ADD CONSTRAINT CK_EMICollections_EntryType
		CHECK (EntryType IN ('Collection', 'Reversal'));
END;
GO

IF EXISTS
(
	SELECT 1
	FROM sys.check_constraints
	WHERE name = N'CK_EMICollections_EMIAmount'
	  AND parent_object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	ALTER TABLE dbo.EMICollections
		DROP CONSTRAINT CK_EMICollections_EMIAmount;
END;
GO

ALTER TABLE dbo.EMICollections
	ADD CONSTRAINT CK_EMICollections_EMIAmount
	CHECK (
		(EntryType = 'Collection' AND EMIAmount > 0) OR
		(EntryType = 'Reversal' AND EMIAmount < 0)
	);
GO

IF EXISTS
(
	SELECT 1
	FROM sys.check_constraints
	WHERE name = N'CK_EMICollections_PenaltyAmount'
	  AND parent_object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	ALTER TABLE dbo.EMICollections
		DROP CONSTRAINT CK_EMICollections_PenaltyAmount;
END;
GO

ALTER TABLE dbo.EMICollections
	ADD CONSTRAINT CK_EMICollections_PenaltyAmount
	CHECK (
		(EntryType = 'Collection' AND PenaltyAmount >= 0) OR
		(EntryType = 'Reversal' AND PenaltyAmount <= 0)
	);
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

IF EXISTS
(
	SELECT 1
	FROM sys.indexes
	WHERE name = N'UX_EMICollections_Loan_Installment'
	  AND object_id = OBJECT_ID(N'dbo.EMICollections')
	  AND (
		has_filter = 0 OR
		filter_definition NOT LIKE N'%EntryType%'
	  )
)
BEGIN
	IF OBJECT_ID(N'dbo.UX_EMICollections_Loan_Installment', N'UQ') IS NOT NULL
	BEGIN
		ALTER TABLE dbo.EMICollections
			DROP CONSTRAINT UX_EMICollections_Loan_Installment;
	END
	ELSE
	BEGIN
		DROP INDEX UX_EMICollections_Loan_Installment
			ON dbo.EMICollections;
	END
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.indexes
	WHERE name = N'UX_EMICollections_Loan_Installment'
	  AND object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	CREATE UNIQUE INDEX UX_EMICollections_Loan_Installment
		ON dbo.EMICollections(LoanAccountNo, InstallmentNumber)
		WHERE IsReversed = 0 AND EntryType = 'Collection';
END;
GO

IF NOT EXISTS
(
	SELECT 1
	FROM sys.indexes
	WHERE name = N'UX_EMICollections_ParentCollectionId'
	  AND object_id = OBJECT_ID(N'dbo.EMICollections')
)
BEGIN
	CREATE UNIQUE INDEX UX_EMICollections_ParentCollectionId
		ON dbo.EMICollections(ParentCollectionId)
		WHERE ParentCollectionId IS NOT NULL;
END;
GO

-- Backfill contra reversal rows for any historical reversed collections without a contra entry
INSERT INTO dbo.EMICollections
(
	LoanAccountNo,
	EMIAmount,
	PenaltyAmount,
	CollectionDate,
	CreatedBy,
	CreatedOn,
	InstallmentNumber,
	ReceiptNumber,
	PaymentMode,
	PaymentReference,
	Remarks,
	IsReversed,
	ReversalReason,
	ReversedBy,
	ReversedOn,
	EntryType,
	ParentCollectionId
)
SELECT
	E.LoanAccountNo,
	-E.EMIAmount,
	-E.PenaltyAmount,
	CAST(COALESCE(E.ReversedOn, SYSDATETIME()) AS DATE),
	COALESCE(E.ReversedBy, E.CreatedBy),
	COALESCE(E.ReversedOn, SYSDATETIME()),
	E.InstallmentNumber,
	'REV-' + CONVERT(VARCHAR(8), COALESCE(E.ReversedOn, SYSDATETIME()), 112) + '-' +
		RIGHT('000000' + CONVERT(VARCHAR(20), NEXT VALUE FOR dbo.Seq_EMIReceiptNo), 6),
	E.PaymentMode,
	E.PaymentReference,
	CONCAT(
		'Reversal of ',
		COALESCE(E.ReceiptNumber, CONCAT('Collection #', E.CollectionId)),
		CASE WHEN NULLIF(LTRIM(RTRIM(E.ReversalReason)), '') IS NOT NULL
			 THEN CONCAT(': ', E.ReversalReason)
			 ELSE ''
		END
	),
	0,
	NULL,
	NULL,
	NULL,
	'Reversal',
	E.CollectionId
FROM dbo.EMICollections E
WHERE E.EntryType = 'Collection'
  AND E.IsReversed = 1
  AND NOT EXISTS
  (
	SELECT 1
	FROM dbo.EMICollections R
	WHERE R.ParentCollectionId = E.CollectionId
  );
GO

-- Ensure Reversal contra entries do not duplicate Reversed* fields meant for the original row
UPDATE dbo.EMICollections
SET ReversalReason = NULL,
    ReversedBy = NULL,
    ReversedOn = NULL
WHERE EntryType = 'Reversal'
  AND (ReversalReason IS NOT NULL OR ReversedBy IS NOT NULL OR ReversedOn IS NOT NULL);
GO

PRINT 'EMI payment metadata and reversal audit fields are ready.';
GO
