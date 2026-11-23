USE LoanManagementDB;
GO

IF COL_LENGTH('dbo.Customers', 'AadhaarDocumentPath') IS NULL
BEGIN
	ALTER TABLE dbo.Customers
		ADD AadhaarDocumentPath NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH('dbo.Customers', 'PANDocumentPath') IS NULL
BEGIN
	ALTER TABLE dbo.Customers
		ADD PANDocumentPath NVARCHAR(500) NULL;
END;
GO
